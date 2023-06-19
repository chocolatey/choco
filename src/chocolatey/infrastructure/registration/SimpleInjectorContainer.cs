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

namespace chocolatey.infrastructure.registration
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using app;
    using app.registration;
    using chocolatey.infrastructure.information;
    using chocolatey.infrastructure.licensing;
    using logging;
    using SimpleInjector;

    /// <summary>
    ///   The inversion container
    /// </summary>
    public static class SimpleInjectorContainer
    {
        private static readonly Lazy<Container> _container = new Lazy<Container>(Initialize);
        private static readonly IList<Type> _componentRegistries = new List<Type>();
        private const string RegisterComponentsMethod = "RegisterComponents";

#if DEBUG
        private static bool _verifyContainer = true;
#else
        private static bool _verifyContainer = false;
#endif

        public static bool VerifyContainer
        {
            get { return _verifyContainer; }
            set { _verifyContainer = value; }
        }

        /// <summary>
        ///   Add a component registry class to the container.
        ///   Must have `public void RegisterComponents(Container container)`
        ///   and a parameterless constructor.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        public static void AddComponentRegistryClass(Type componentType)
        {
            _componentRegistries.Add(componentType);
        }

        /// <summary>
        ///   Gets the container.
        /// </summary>
        public static Container Container { get { return _container.Value; } }

        /// <summary>
        ///   Initializes the container
        /// </summary>
        private static Container Initialize()
        {
            var container = new Container();
            container.Options.AllowOverridingRegistrations = true;
            var originalConstructorResolutionBehavior = container.Options.ConstructorResolutionBehavior;
            container.Options.ConstructorResolutionBehavior = new SimpleInjectorContainerResolutionBehavior(originalConstructorResolutionBehavior);

            var binding = new ContainerBinding();
            var extensions = binding.RegisterComponents(container);

            // TODO: Remove once we can do a breaking release, ie 2.0.0

            foreach (var componentRegistry in _componentRegistries)
            {
                LoadComponentRegistry(componentRegistry, container, extensions);
            }

            if (_verifyContainer) container.Verify();

            return container;
        }

        /// <summary>
        /// Loads a component registry for simple injector.
        /// </summary>
        /// <param name="componentRegistry">The component registry.</param>
        /// <param name="container">The container.</param>
        /// <param name="extensions">Any extension libraries</param>
        private static void LoadComponentRegistry(Type componentRegistry, Container container, IEnumerable<ExtensionInformation> extensions)
        {
            if (componentRegistry == null)
            {
                if (!extensions.Any(e => e.Name.IsEqualTo("chocolatey.licensed")))
                {
                    "chocolatey".Log().Warn(ChocolateyLoggers.Important,
    @"Unable to register licensed components. This is likely related to a
 missing or outdated licensed DLL.");
                }
                return;
            }
            try
            {
                if (!extensions.Any(e => e.Name.IsEqualTo(componentRegistry.Assembly.GetName().Name)))
                {
                    var registrations = container.GetCurrentRegistrations();

                    object componentClass = Activator.CreateInstance(componentRegistry);

                    componentRegistry.InvokeMember(
                        RegisterComponentsMethod,
                        BindingFlags.InvokeMethod,
                        null,
                        componentClass,
                        new Object[] { container }
                        );
                }
            }
            catch (Exception ex)
            {
                var isDebug = ApplicationParameters.IsDebugModeCliPrimitive();
                var message = isDebug ? ex.ToString() : ex.Message;

                if (isDebug && ex.InnerException != null)
                {
                    message += "{0}{1}".FormatWith(Environment.NewLine, ex.InnerException.ToString());
                }

                "chocolatey".Log().Error(
                    ChocolateyLoggers.Important,
                    @"Error when registering components for '{0}':{1} {2}".FormatWith(
                        componentRegistry.FullName,
                        Environment.NewLine,
                        message
                        ));
            }
        }


#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static void add_component_registry_class(Type componentType)
            => AddComponentRegistryClass(componentType);
#pragma warning restore IDE1006
    }
}
