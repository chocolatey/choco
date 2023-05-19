// Copyright © 2017 - 2022 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.infrastructure.app.registration
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using chocolatey.infrastructure.adapters;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.services;
    using infrastructure.commands;
    using infrastructure.events;
    using infrastructure.services;
    using NuGet.Packaging;
    using SimpleInjector;
    using Assembly = adapters.Assembly;

    internal sealed class SimpleInjectorContainerRegistrator : IContainerRegistrator, ICloneable
    {
        internal List<Func<Type, bool>> _validationHandlers = new List<Func<Type, bool>>();

        // We need to store the aliases for the commands to prevent them from
        // being overridden when the original class implementing these hasn't been removed.
        private HashSet<string> _allCommands = new HashSet<string>();

        private IAssembly _chocoAssembly;
        private ConcurrentDictionary<Type, Func<IContainerResolver, object>> _instanceActionRegistrations = new ConcurrentDictionary<Type, Func<IContainerResolver, object>>();
        private bool _isBuilt;
        private ConcurrentDictionary<Type, List<Type>> _multiServices = new ConcurrentDictionary<Type, List<Type>>();
        private ConcurrentDictionary<string, Type> _registeredCommands = new ConcurrentDictionary<string, Type>();
        private ConcurrentDictionary<Type, Type> _singletonServices = new ConcurrentDictionary<Type, Type>();
        private ConcurrentDictionary<Type, Type> _transientServices = new ConcurrentDictionary<Type, Type>();

        public SimpleInjectorContainerRegistrator()
        {
            _chocoAssembly = Assembly.GetExecutingAssembly();
        }

        public bool CanReplaceRegister { get; internal set; }

        public bool RegistrationFailed { get; internal set; }

        // We add a specific clone handler due to some fields can not be
        // serialized through the deep_copy extension helper.
        public object Clone()
        {
            var cloned = (SimpleInjectorContainerRegistrator)MemberwiseClone();
            cloned._allCommands = _allCommands.DeepCopy();
            cloned._instanceActionRegistrations = new ConcurrentDictionary<Type, Func<IContainerResolver, object>>();

            foreach (var instanceRegistration in _instanceActionRegistrations)
            {
                var key = instanceRegistration.Key;
                var value = (Func<IContainerResolver, object>)instanceRegistration.Value.Clone();

                cloned._instanceActionRegistrations.TryAdd(key, value);
            }

            cloned._multiServices = _multiServices.DeepCopy();
            cloned._registeredCommands = _registeredCommands.DeepCopy();
            cloned._singletonServices = _singletonServices.DeepCopy();
            cloned._transientServices = _transientServices.DeepCopy();
            cloned._validationHandlers = new List<Func<Type, bool>>();

            return cloned;
        }

        public void RegisterAssemblyCommands(IAssembly assembly)
        {
            try
            {
                var types = assembly.GetLoadableTypes()
                    .Where(t => t.IsClass &&
                                !t.IsAbstract &&
                                typeof(ICommand).IsAssignableFrom(t) &&
                                t.GetCustomAttributes<CommandForAttribute>().Any()).ToList();

                foreach (var t in types)
                {
                    if (RegistrationFailed)
                    {
                        break;
                    }

                    RegisterCommand(t);
                }
            }
            catch (Exception ex)
            {
                this.Log().Warn("Unable to register commands for '{0}'. Continuing without registering commands!", assembly.GetName().Name);
                this.Log().Warn(ex.Message);
                RegistrationFailed = true;
            }
        }

        public void RegisterCommand(Type commandType)
        {
            EnsureNotBuilt();

            if (!CanRegisterService(commandType))
            {
                return;
            }

            var commandForAttribute = commandType.GetCustomAttributes<CommandForAttribute>().FirstOrDefault();

            if (commandForAttribute == null)
            {
                throw new ArgumentException("{0} does not register a specific command!".FormatWith(commandType.Name));
            }

            if (!commandType.GetInterfaces().Contains(typeof(ICommand)))
            {
                throw new ArgumentException("{0} does not implement the interface 'ICommand'. All commands must implement this interface!".FormatWith(commandType.Name));
            }

            var commandName = GetCommandName(commandForAttribute);

            _registeredCommands.AddOrUpdate(commandName, addValueFactory: (key) =>
            {
                var commandTypeAttributes = commandType.GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>();
                ValidateCommandReplacements(commandTypeAttributes);

                AddCommands(commandTypeAttributes);

                this.Log().Debug("Registering new command '{0}' in assembly '{1}'",
                    commandName,
                    commandType.Assembly.GetName().Name);
                return commandType;
            }, updateValueFactory: (key, value) =>
            {
                if (!value.Assembly.FullName.IsEqualTo(_chocoAssembly.FullName) && !commandType.IsAssignableFrom(value))
                {
                    // We do not allow extensions to override eachothers command.
                    // This may change in the future to allow multiple command handlers.
                    // However we do not want to throw an exception in this case.
                    this.Log().Debug("Command '{0}' implementation in assembly '{1}' tried to replace command in extension '{2}'. Ignoring replacement...",
                        commandName,
                        commandType.Assembly.GetName().Name,
                        value.Assembly.GetName().Name);
                    return value;
                }

                ValidateReplacePermissions();

                var removedCommands = RemoveCommands(value, commandName).ToList();

                try
                {
                    var commandTypeAttributes = commandType.GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>();

                    ValidateCommandReplacements(commandTypeAttributes);

                    this.Log().Debug("Replacing existing command '{0}' from assembly '{1}' with implementation in assembly '{2}'",
                        commandName,
                        value.Assembly.GetName().Name,
                        commandType.Assembly.GetName().Name);
                    AddCommands(commandTypeAttributes);
                }
                catch (Exception ex)
                {
                    RegistrationFailed = true;
                    _allCommands.AddRange(removedCommands);
                    throw ex;
                }

                return commandType;
            });
        }

        public void RegisterInstance<TService, TImplementation>(Func<TImplementation> instance)
            where TImplementation : class, TService
        {
            RegisterInstance<TService, TImplementation>((container) => instance());
        }

        public void RegisterInstance<TService, TImplementation>(Func<IContainerResolver, TImplementation> instance)
        where TImplementation : class, TService
        {
            RegisterInstance(typeof(TService), (container) => instance(container));
        }

        public void RegisterInstance<TImplementation>(Func<TImplementation> instance)
            where TImplementation : class
        {
            RegisterInstance<TImplementation, TImplementation>(instance);
        }

        public void RegisterService<TImplementation>(params Type[] types)
        {
            foreach (var serviceType in types)
            {
                RegisterService(typeof(TImplementation), serviceType);
            }
        }

        public void RegisterService<TService, TImplementation>(bool transient = false)
                                                where TImplementation : class, TService
        {
            var interfaceType = typeof(TService);
            var serviceType = typeof(TImplementation);

            RegisterService(interfaceType, serviceType, transient);
        }

        public void RegisterValidator(Func<Type, bool> validation_func)
        {
            _validationHandlers.Add(validation_func);
        }

        public void RegisterSourceRunner<TService>() where TService : class
        {
            RegisterSourceRunner(typeof(TService));
        }

        public void RegisterSourceRunner(Type serviceType)
        {
            EnsureNotBuilt();

            if (!CanRegisterService(serviceType))
            {
                return;
            }

            if (!typeof(IAlternativeSourceRunner).IsAssignableFrom(serviceType))
            {
                return;
            }

            AddToMultiServices(typeof(IAlternativeSourceRunner), serviceType);

            foreach (var interfaceType in serviceType.GetInterfaces())
            {
                if (interfaceType == typeof(ICountSourceRunner))
                {
                    AddToMultiServices(interfaceType, serviceType);
                }

                if (interfaceType == typeof(IListSourceRunner))
                {
                    AddToMultiServices(interfaceType, serviceType);
                }

                if (interfaceType == typeof(ISearchableSourceRunner))
                {
                    AddToMultiServices(interfaceType, serviceType);
                }

                if (interfaceType == typeof(IInstallSourceRunner))
                {
                    AddToMultiServices(interfaceType, serviceType);
                }

                if (interfaceType == typeof(IUpgradeSourceRunner))
                {
                    AddToMultiServices(interfaceType, serviceType);
                }

                if (interfaceType == typeof(IUninstallSourceRunner))
                {
                    AddToMultiServices(interfaceType, serviceType);
                }
            }
        }

        internal Container BuildContainer(Container container)
        {
            container.RegisterAll<ICommand>(_registeredCommands.Values);

            AddServicesToContainer(container, _singletonServices, Lifestyle.Singleton);
            AddServicesToContainer(container, _transientServices, Lifestyle.Transient);

            foreach (var multiService in _multiServices)
            {
                container.RegisterAll(multiService.Key, multiService.Value.AsEnumerable());
            }

            foreach (var instanceAction in _instanceActionRegistrations)
            {
                container.Register(instanceAction.Key, () =>
                {
                    var resolver = container.GetInstance<IContainerResolver>();
                    return instanceAction.Value(resolver);
                }, Lifestyle.Singleton);
            }

            _registeredCommands.Clear();
            _singletonServices.Clear();
            _transientServices.Clear();
            _multiServices.Clear();
            _allCommands.Clear();

            container.RegisterSingle<IContainerResolver, SimpleInjectorContainerResolver>();

            EventManager.InitializeWith(container.GetInstance<IEventSubscriptionManagerService>);

            _isBuilt = true;

            return container;
        }

        private static void AddServicesToContainer(Container container, ConcurrentDictionary<Type, Type> services, Lifestyle lifestyle)
        {
            foreach (var service in services)
            {
                container.Register(service.Key, service.Value, lifestyle);
            }
        }

        private void AddCommands(IEnumerable<CommandForAttribute> commandTypeAttributes)
        {
            foreach (var commandFor in commandTypeAttributes)
            {
                var commandName = commandFor.CommandName.ToLowerSafe();

                if (!_allCommands.Contains(commandName))
                {
                    _allCommands.Add(commandName);
                }
            }
        }

        private void AddToMultiServices(Type interfaceType, Type serviceType)
        {
            EnsureNotBuilt();
            ValidateServiceRegistrations(interfaceType, serviceType, validate_multi_services: false);

            RemoveRegistration(interfaceType);

            _multiServices.AddOrUpdate(interfaceType, new List<Type> { serviceType }, (key, value) =>
            {
                this.Log().Debug("Adding new type '{0}' for type '{1}' from assembly '{2}'",
                    serviceType.Name,
                    interfaceType.Name,
                    serviceType.Assembly.GetName().Name);
                value.Add(serviceType);
                return value;
            });
        }

        private bool CanRegisterService(Type serviceType)
        {
            foreach (var validator in _validationHandlers)
            {
                if (!validator(serviceType))
                {
                    return false;
                }
            }

            return true;
        }

        private void EnsureNotBuilt()
        {
            if (_isBuilt)
            {
                throw new ApplicationException("Registration has been completed, as such it is not possible to register any new commands!");
            }
        }

        private string GetCommandName(CommandForAttribute commandForAttribute)
        {
            var commandName = commandForAttribute.CommandName.ToLowerSafe();

            // First check if we have stored the actual command
            if (_registeredCommands.ContainsKey(commandName))
            {
                return commandName;
            }

            // If we have not registered a command, but it is in all commands
            // this is most likely an alias on an existing command, as such we
            // need to iterate through all commands.
            if (!_allCommands.Contains(commandName))
            {
                return commandName;
            }

            foreach (var command in _registeredCommands)
            {
                var allCommandForAttributes = command.Value.GetCustomAttributes(typeof(CommandForAttribute), inherit: false).Cast<CommandForAttribute>();

                foreach (var aliasCommand in allCommandForAttributes)
                {
                    if (aliasCommand.CommandName.IsEqualTo(commandName))
                    {
                        return command.Key;
                    }
                }
            }

            // If we have gotten here, that means all commands have a registered
            // command for this type, but it can not be found. As such we need to
            // throw an error so it can be looked at.
            throw new ApplicationException("The command '{0}' has been globally registered, but can not be found!".FormatWith(commandName));
        }

        private void RegisterInstance(Type serviceType, Func<IContainerResolver, object> instanceAction)
        {
            EnsureNotBuilt();

            ValidateServiceRegistrations(serviceType, serviceType, validate_multi_services: true);
            RemoveRegistration(serviceType);

            _instanceActionRegistrations.AddOrUpdate(serviceType, instanceAction, (key, value) => instanceAction);
        }

        private void RegisterService(Type interfaceType, Type serviceType, bool transient = false)
        {
            EnsureNotBuilt();

            if (!CanRegisterService(serviceType))
            {
                return;
            }

            var multiServiceAttribute = interfaceType.GetCustomAttribute<MultiServiceAttribute>();

            if (multiServiceAttribute != null && multiServiceAttribute.IsMultiService)
            {
                AddToMultiServices(interfaceType, serviceType);
            }
            else
            {
                ValidateServiceRegistrations(interfaceType, serviceType, validate_multi_services: true);
                RemoveRegistration(interfaceType);

                if (transient)
                {
                    _transientServices.AddOrUpdate(interfaceType, serviceType, (key, value) => serviceType);
                }
                else
                {
                    _singletonServices.AddOrUpdate(interfaceType, serviceType, (key, value) => serviceType);
                }
            }
        }

        private IEnumerable<string> RemoveCommands(Type commandType, string initialCommand)
        {
            var allCommandsForAttribute = commandType.GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>();

            foreach (var commandFor in allCommandsForAttribute)
            {
                var commandName = commandFor.CommandName.ToLowerSafe();
                if (_allCommands.Contains(commandName))
                {
                    _allCommands.Remove(commandName);
                }

                Type tempType;

                if (!commandName.IsEqualTo(initialCommand) && _registeredCommands.TryRemove(commandName, out tempType))
                {
                    yield return commandName;
                }
            }
        }

        private void RemoveRegistration(Type interfaceType)
        {
            Type tempType;
            Func<IContainerResolver, object> tempAction;
            _transientServices.TryRemove(interfaceType, out tempType);
            _singletonServices.TryRemove(interfaceType, out tempType);
            _instanceActionRegistrations.TryRemove(interfaceType, out tempAction);
        }

        private void ValidateCommandReplacements(IEnumerable<CommandForAttribute> commandTypeAttributes)
        {
            ValidateReplacePermissions();

            foreach (var commandFor in commandTypeAttributes)
            {
                var commandName = commandFor.CommandName.ToLowerSafe();

                if (_allCommands.Contains(commandName))
                {
                    throw new ApplicationException("The command '{0}' is already registered for a different command handler!".FormatWith(commandName));
                }
            }
        }

        private void ValidateReplacePermissions()
        {
            if (!CanReplaceRegister)
            {
                throw new ApplicationException("{0} tried to replace an existing command without permission!");
            }
        }

        private void ValidateServiceRegistrations(Type interfaceType, Type serviceType, bool validate_multi_services)
        {
            if (interfaceType == typeof(IContainerRegistrator) ||
                serviceType.GetInterfaces().Contains(typeof(IContainerRegistrator)))
            {
                throw new ApplicationException("Registering a new container registrator is not allowed!");
            }

            var valid = serviceType.GetInterfaces().Contains(interfaceType)
                || serviceType == interfaceType;
            var typeCheck = serviceType.BaseType;

            while (!valid && interfaceType.IsClass && typeCheck != null)
            {
                if (typeCheck == interfaceType)
                {
                    valid = true;
                }

                typeCheck = serviceType.BaseType;
            }

            if (!valid)
            {
                throw new ApplicationException("The type '{0}' is not inheriting from '{1}'. Unable to continue the registration.".FormatWith(
                    serviceType.Name,
                    interfaceType.Name));
            }

            if (_transientServices.ContainsKey(interfaceType) ||
                _singletonServices.ContainsKey(interfaceType) ||
                _instanceActionRegistrations.ContainsKey(interfaceType) ||
                (validate_multi_services && _multiServices.ContainsKey(interfaceType)))
            {
                ValidateReplacePermissions();
            }
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void register_assembly_commands(IAssembly assembly)
            => RegisterAssemblyCommands(assembly);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void register_command(Type commandType)
            => RegisterCommand(commandType);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void register_instance<TService, TImplementation>(Func<TImplementation> instance)
            where TImplementation : class, TService
            => RegisterInstance<TService, TImplementation>(instance);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void register_instance<TService, TImplementation>(Func<IContainerResolver, TImplementation> instance)
            where TImplementation : class, TService
            => RegisterInstance<TService, TImplementation>(instance);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void register_instance<TImplementation>(Func<TImplementation> instance)
            where TImplementation : class
            => RegisterInstance(instance);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void register_service<TImplementation>(params Type[] types)
            => RegisterService<TImplementation>(types);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void register_service<TService, TImplementation>(bool transient = false)
            where TImplementation : class, TService
            => RegisterService<TService, TImplementation>(transient);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void register_validator(Func<Type, bool> validation_func)
            => RegisterValidator(validation_func);
#pragma warning restore IDE1006
    }
}
