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
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using chocolatey.infrastructure.app.builders;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.information;
    using chocolatey.infrastructure.licensing;
    using chocolatey.infrastructure.logging;
    using chocolatey.infrastructure.services;
    using infrastructure.configuration;
    using SimpleInjector;
    using Assembly = adapters.Assembly;
    using nuget;
    using NuGet.Common;
    using NuGet.Versioning;
    using CryptoHashProvider = cryptography.CryptoHashProvider;
    using IFileSystem = filesystem.IFileSystem;
    using IHashProvider = cryptography.IHashProvider;

    // ReSharper disable InconsistentNaming

    /// <summary>
    ///   The main inversion container registration for the application. Look for other container bindings in client projects.
    /// </summary>
    public sealed partial class ContainerBinding
    {
        /// <summary>
        ///   Loads the module into the kernel.
        /// </summary>
        public IEnumerable<ExtensionInformation> RegisterComponents(Container container)
        {
            var availableExtensions = new List<ExtensionInformation>();

            var configuration = Config.get_configuration_settings();

            var registrator = new SimpleInjectorContainerRegistrator();

            // We can not resolve this class, as that wil prevent future registrations
            var fileSystem = new DotNetFileSystem();
            var xmlService = new XmlService(fileSystem, new cryptography.CryptoHashProvider(fileSystem));

            var mainRegistrator = new ChocolateyRegistrationModule();
            registrator.CanReplaceRegister = true;
            registrator.register_instance<filesystem.IFileSystem>(() => fileSystem);
            registrator.register_instance(() => Config.get_configuration_settings());
            mainRegistrator.register_dependencies(registrator, configuration);
            registrator.register_assembly_commands(Assembly.GetExecutingAssembly());
            registrator.CanReplaceRegister = false;

            var assemblies = fileSystem.get_extension_assemblies();
            var currentAssemblyVersionString = VersionInformation.get_current_assembly_version();
            Version currentAssemblyVersion;
            if (!Version.TryParse(currentAssemblyVersionString, out currentAssemblyVersion))
            {
                currentAssemblyVersion = new Version("0.0.0.0");
            }

            var arguments = Environment.GetCommandLineArgs();

            var disableCompatibilityChecks = ConfigurationBuilder.is_compatibility_checks_disabled(fileSystem, xmlService) ||
                arguments.Any(a => a.is_equal_to("--skip-compatibility-checks"));

            var chocoVersion = NuGetVersion.Parse(VersionInformation.get_current_assembly_version());
            registrator = register_extensions(availableExtensions, configuration, registrator, assemblies, currentAssemblyVersion, chocoVersion, disableCompatibilityChecks);

            container = registrator.build_container(container);

            var availableExtensionsArray = availableExtensions.Distinct().ToArray();

            foreach (var extension in availableExtensionsArray)
            {
                this.Log().Debug("Loaded extension {0} v{1} with status '{2}'",
                    extension.Name,
                    extension.Version,
                    extension.Status);
            }

            container.RegisterAll(availableExtensionsArray.AsEnumerable());

            return availableExtensionsArray;
        }

        private SimpleInjectorContainerRegistrator register_extensions(List<ExtensionInformation> availableExtensions, ChocolateyConfiguration configuration, SimpleInjectorContainerRegistrator registrator, IEnumerable<adapters.IAssembly> assemblies, Version currentAssemblyVersion, SemanticVersion chocoVersion, bool disableCompatibilityChecks)
        {
            foreach (var assembly in assemblies)
            {
                var assemblyName = assembly.GetName().Name;
                var extensionInformation = new ExtensionInformation(assembly);
                var minimumChocolateyVersionString = VersionInformation.get_minimum_chocolatey_version(assembly);
                Version minimumChocolateyVersion;

                if (!disableCompatibilityChecks && Version.TryParse(minimumChocolateyVersionString, out minimumChocolateyVersion) && currentAssemblyVersion < minimumChocolateyVersion)
                {
                    this.Log().Warn(@"
You are running a version of Chocolatey that may not be compatible with the the extension {0} version {1}.
The Chocolatey version required is {2}, the extension will not be loaded.

You can override this compatibility check and force loading the extension by passing in the --skip-compatibility-checks
option when executing a command, or by enabling the DisableCompatibilityChecks feature with the following command:
choco feature enable --name=""disableCompatibilityChecks""",
                        extensionInformation.Name,
                        extensionInformation.Version,
                        minimumChocolateyVersion
                    );

                    extensionInformation.Status = ExtensionStatus.Disabled;
                    availableExtensions.Add(extensionInformation);
                    continue;
                }

                var hasRegisteredDependencies = false;

                try
                {
                    var registrationClasses = assembly.get_extension_modules();

                    this.Log().Debug("Trying to load and register extension '{0}'", assemblyName);

                    // We make a clone of the existing registrator to prevent
                    // the registrations being applied if something fails for
                    // the extension
                    var clonedRegistrator = (SimpleInjectorContainerRegistrator)registrator.Clone();
                    clonedRegistrator.CanReplaceRegister = true;

                    foreach (var registration in registrationClasses)
                    {
                        if (clonedRegistrator.RegistrationFailed)
                        {
                            break;
                        }

                        this.Log().Debug("Calling registration module '{0}' in extension '{1}'!", registration.GetType().Name, assemblyName);
                        clonedRegistrator._validationHandlers.Clear();
                        clonedRegistrator.register_validator((instanceType) => validate_minimum_chocolatey_version(instanceType, chocoVersion));
                        registration.register_dependencies(clonedRegistrator, configuration.deep_copy());
                        hasRegisteredDependencies = !clonedRegistrator.RegistrationFailed;
                    }

                    if (hasRegisteredDependencies)
                    {
                        clonedRegistrator.register_assembly_commands(assembly);
                        hasRegisteredDependencies = !clonedRegistrator.RegistrationFailed;
                    }

                    if (hasRegisteredDependencies && !clonedRegistrator.RegistrationFailed)
                    {
                        registrator = clonedRegistrator;
                        extensionInformation.Status = ExtensionStatus.Loaded;
                    }
                    else if (clonedRegistrator.RegistrationFailed)
                    {
                        extensionInformation.Status = ExtensionStatus.Failed;
                    }
                    else
                    {
                        // In this case we can assume there was no registration class,
                        // as such we just ignore adding it as an available extension.
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    this.Log().Error("Unable to load extension {0}: {1}.", assemblyName, ex.Message);
                    this.Log().Error(ChocolateyLoggers.LogFileOnly, ex.StackTrace);
                    extensionInformation.Status = ExtensionStatus.Failed;
                }
                finally
                {
                    registrator.CanReplaceRegister = false;

                    if (hasRegisteredDependencies || !extensionInformation.Name.is_equal_to("chocolatey.licensed"))
                    {
                        availableExtensions.Add(extensionInformation);
                    }
                }
            }

            return registrator;
        }

        private bool validate_minimum_chocolatey_version(Type instanceType, SemanticVersion chocoVersion)
        {
            if (instanceType == null)
            {
                return false;
            }

            var methodImpl = instanceType.GetMethod("supports_chocolatey", BindingFlags.Static | BindingFlags.Public);

            if (methodImpl == null)
            {
                return true;
            }

            try
            {
                return (bool)methodImpl.Invoke(null, new object[] { chocoVersion });
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    // ReSharper restore InconsistentNaming
}
