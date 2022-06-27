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
            cloned._allCommands = _allCommands.deep_copy();
            cloned._instanceActionRegistrations = new ConcurrentDictionary<Type, Func<IContainerResolver, object>>();

            foreach (var instanceRegistration in _instanceActionRegistrations)
            {
                var key = instanceRegistration.Key;
                var value = (Func<IContainerResolver, object>)instanceRegistration.Value.Clone();

                cloned._instanceActionRegistrations.TryAdd(key, value);
            }

            cloned._multiServices = _multiServices.deep_copy();
            cloned._registeredCommands = _registeredCommands.deep_copy();
            cloned._singletonServices = _singletonServices.deep_copy();
            cloned._transientServices = _transientServices.deep_copy();
            cloned._validationHandlers = new List<Func<Type, bool>>();

            return cloned;
        }

        public void register_assembly_commands(IAssembly assembly)
        {
            try
            {
                var types = assembly.get_loadable_types()
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

                    register_command(t);
                }
            }
            catch (Exception ex)
            {
                this.Log().Warn("Unable to register commands for '{0}'. Continuing without registering commands!", assembly.GetName().Name);
                this.Log().Warn(ex.Message);
                RegistrationFailed = true;
            }
        }

        public void register_command(Type commandType)
        {
            ensure_not_built();

            if (!can_register_service(commandType))
            {
                return;
            }

            var commandForAttribute = commandType.GetCustomAttributes<CommandForAttribute>().FirstOrDefault();

            if (commandForAttribute == null)
            {
                throw new ArgumentException("{0} does not register a specific command!".format_with(commandType.Name));
            }

            if (!commandType.GetInterfaces().Contains(typeof(ICommand)))
            {
                throw new ArgumentException("{0} does not implement the interface 'ICommand'. All commands must implement this interface!".format_with(commandType.Name));
            }

            var commandName = get_command_name(commandForAttribute);

            _registeredCommands.AddOrUpdate(commandName, addValueFactory: (key) =>
            {
                var commandTypeAttributes = commandType.GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>();
                validate_commands_replacement(commandTypeAttributes);

                add_commands(commandTypeAttributes);

                this.Log().Debug("Registering new command '{0}' in assembly '{1}'",
                    commandName,
                    commandType.Assembly.GetName().Name);
                return commandType;
            }, updateValueFactory: (key, value) =>
            {
                if (!value.Assembly.FullName.is_equal_to(_chocoAssembly.FullName) && !commandType.IsAssignableFrom(value))
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

                validate_replace_permissions();

                var removedCommands = remove_commands(value, commandName).ToList();

                try
                {
                    var commandTypeAttributes = commandType.GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>();

                    validate_commands_replacement(commandTypeAttributes);

                    this.Log().Debug("Replacing existing command '{0}' from assembly '{1}' with implementation in assembly '{2}'",
                        commandName,
                        value.Assembly.GetName().Name,
                        commandType.Assembly.GetName().Name);
                    add_commands(commandTypeAttributes);
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

        public void register_instance<TService, TImplementation>(Func<TImplementation> instance)
            where TImplementation : class, TService
        {
            register_instance<TService, TImplementation>((container) => instance());
        }

        public void register_instance<TService, TImplementation>(Func<IContainerResolver, TImplementation> instance)
        where TImplementation : class, TService
        {
            register_instance(typeof(TService), (container) => instance(container));
        }

        public void register_instance<TImplementation>(Func<TImplementation> instance)
            where TImplementation : class
        {
            register_instance<TImplementation, TImplementation>(instance);
        }

        public void register_service<TImplementation>(params Type[] types)
        {
            foreach (var serviceType in types)
            {
                register_service(typeof(TImplementation), serviceType);
            }
        }

        public void register_service<TService, TImplementation>(bool transient = false)
                                                where TImplementation : class, TService
        {
            var interfaceType = typeof(TService);
            var serviceType = typeof(TImplementation);

            register_service(interfaceType, serviceType, transient);
        }

        public void register_validator(Func<Type, bool> validation_func)
        {
            _validationHandlers.Add(validation_func);
        }

        internal Container build_container(Container container)
        {
            container.RegisterAll<ICommand>(_registeredCommands.Values);

            add_services_to_container(container, _singletonServices, Lifestyle.Singleton);
            add_services_to_container(container, _transientServices, Lifestyle.Transient);

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

            EventManager.initialize_with(container.GetInstance<IEventSubscriptionManagerService>);

            _isBuilt = true;

            return container;
        }

        private static void add_services_to_container(Container container, ConcurrentDictionary<Type, Type> services, Lifestyle lifestyle)
        {
            foreach (var service in services)
            {
                container.Register(service.Key, service.Value, lifestyle);
            }
        }

        private void add_commands(IEnumerable<CommandForAttribute> commandTypeAttributes)
        {
            foreach (var commandFor in commandTypeAttributes)
            {
                var commandName = commandFor.CommandName.to_lower();

                if (!_allCommands.Contains(commandName))
                {
                    _allCommands.Add(commandName);
                }
            }
        }

        private void add_to_multi_services(Type interfaceType, Type serviceType)
        {
            ensure_not_built();
            validate_service_registration(interfaceType, serviceType, validate_multi_services: false);

            remove_existing_registration(interfaceType);

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

        private bool can_register_service(Type serviceType)
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

        private void ensure_not_built()
        {
            if (_isBuilt)
            {
                throw new ApplicationException("Registration has been completed, as such it is not possible to register any new commands!");
            }
        }

        private string get_command_name(CommandForAttribute commandForAttribute)
        {
            var commandName = commandForAttribute.CommandName.to_lower();

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
                    if (aliasCommand.CommandName.is_equal_to(commandName))
                    {
                        return command.Key;
                    }
                }
            }

            // If we have gotten here, that means all commands have a registered
            // command for this type, but it can not be found. As such we need to
            // throw an error so it can be looked at.
            throw new ApplicationException("The command '{0}' has been globally registered, but can not be found!".format_with(commandName));
        }

        private void register_instance(Type serviceType, Func<IContainerResolver, object> instanceAction)
        {
            ensure_not_built();

            validate_service_registration(serviceType, serviceType, validate_multi_services: true);
            remove_existing_registration(serviceType);

            _instanceActionRegistrations.AddOrUpdate(serviceType, instanceAction, (key, value) => instanceAction);
        }

        private void register_service(Type interfaceType, Type serviceType, bool transient = false)
        {
            ensure_not_built();

            if (!can_register_service(serviceType))
            {
                return;
            }

            var multiServiceAttribute = interfaceType.GetCustomAttribute<MultiServiceAttribute>();

            if (multiServiceAttribute != null && multiServiceAttribute.IsMultiService)
            {
                add_to_multi_services(interfaceType, serviceType);
            }
            else
            {
                validate_service_registration(interfaceType, serviceType, validate_multi_services: true);
                remove_existing_registration(interfaceType);

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

        private IEnumerable<string> remove_commands(Type commandType, string initialCommand)
        {
            var allCommandsForAttribute = commandType.GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>();

            foreach (var commandFor in allCommandsForAttribute)
            {
                var commandName = commandFor.CommandName.to_lower();
                if (_allCommands.Contains(commandName))
                {
                    _allCommands.Remove(commandName);
                }

                Type tempType;

                if (!commandName.is_equal_to(initialCommand) && _registeredCommands.TryRemove(commandName, out tempType))
                {
                    yield return commandName;
                }
            }
        }

        private void remove_existing_registration(Type interfaceType)
        {
            Type tempType;
            Func<IContainerResolver, object> tempAction;
            _transientServices.TryRemove(interfaceType, out tempType);
            _singletonServices.TryRemove(interfaceType, out tempType);
            _instanceActionRegistrations.TryRemove(interfaceType, out tempAction);
        }

        private void validate_commands_replacement(IEnumerable<CommandForAttribute> commandTypeAttributes)
        {
            validate_replace_permissions();

            foreach (var commandFor in commandTypeAttributes)
            {
                var commandName = commandFor.CommandName.to_lower();

                if (_allCommands.Contains(commandName))
                {
                    throw new ApplicationException("The command '{0}' is already registered for a different command handler!".format_with(commandName));
                }
            }
        }

        private void validate_replace_permissions()
        {
            if (!CanReplaceRegister)
            {
                throw new ApplicationException("{0} tried to replace an existing command without permission!");
            }
        }

        private void validate_service_registration(Type interfaceType, Type serviceType, bool validate_multi_services)
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
                throw new ApplicationException("The type '{0}' is not inheriting from '{1}'. Unable to continue the registration.".format_with(
                    serviceType.Name,
                    interfaceType.Name));
            }

            if (_transientServices.ContainsKey(interfaceType) ||
                _singletonServices.ContainsKey(interfaceType) ||
                _instanceActionRegistrations.ContainsKey(interfaceType) ||
                (validate_multi_services && _multiServices.ContainsKey(interfaceType)))
            {
                validate_replace_permissions();
            }
        }
    }
}
