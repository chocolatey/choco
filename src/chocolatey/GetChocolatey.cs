// Copyright © 2017 Chocolatey Software, Inc
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

namespace chocolatey
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using infrastructure.licensing;
    using SimpleInjector;
    using infrastructure.app;
    using infrastructure.app.builders;
    using infrastructure.app.configuration;
    using infrastructure.app.runners;
    using infrastructure.configuration;
    using infrastructure.extractors;
    using infrastructure.logging;
    using infrastructure.registration;
    using resources;
    using Assembly = infrastructure.adapters.Assembly;
    using IFileSystem = infrastructure.filesystem.IFileSystem;

    // ReSharper disable InconsistentNaming

    /// <summary>
    /// Entry point for API
    /// </summary>
    public static class Lets
    {
        public static GetChocolatey GetChocolatey()
        {
            add_assembly_resolver();
            return new GetChocolatey();
        }

        private static ResolveEventHandler _handler = null;

        private static void add_assembly_resolver()
        {
            _handler = (sender, args) =>
            {
                var requestedAssembly = new AssemblyName(args.Name);
                if (requestedAssembly.get_public_key_token().is_equal_to(ApplicationParameters.OfficialChocolateyPublicKey)
                    && !requestedAssembly.Name.is_equal_to("chocolatey.licensed")
                    && !requestedAssembly.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                {
                    return typeof(Lets).Assembly;
                }

                try
                {
                    if (requestedAssembly.get_public_key_token().is_equal_to(ApplicationParameters.OfficialChocolateyPublicKey)
                        && requestedAssembly.Name.is_equal_to("chocolatey.licensed"))
                    {
                        return Assembly.LoadFile(ApplicationParameters.LicensedAssemblyLocation).UnderlyingType;
                    }
                }
                catch (Exception ex)
                {
                    "chocolatey".Log().Warn("Unable to load chocolatey.licensed assembly. {0}".format_with(ex.Message));
                }

                return null;
            };

            AppDomain.CurrentDomain.AssemblyResolve += _handler;
        }
    }

    /// <summary>
    /// The place where all the magic happens.
    /// </summary>
    /// <remarks>Chocolatey - the most magical place on Windows</remarks>
    public class GetChocolatey
    {
        private readonly Container _container;
        private readonly ChocolateyLicense _license;
        private Action<ChocolateyConfiguration> _propConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetChocolatey"/> class.
        /// </summary>
        public GetChocolatey()
        {
            Log4NetAppenderConfiguration.configure(null, excludeLoggerNames: ChocolateyLoggers.Trace.to_string());
            Bootstrap.initialize();
            _license = License.validate_license();
            _container = SimpleInjectorContainer.Container;
        }

        /// <summary>
        ///   This is an optional helper to give you the correct settings for a logger. You can still set this in the set by calling propConfig.Logger without having to call this method.
        /// </summary>
        /// <param name="logger">This is the logger you want Chocolatey to also use.</param>
        /// <returns>This <see cref="GetChocolatey"/> instance</returns>
        public GetChocolatey SetCustomLogging(ILog logger)
        {
            Log.InitializeWith(logger, resetLoggers: false);
            return this;
        }

        /// <summary>
        ///   Set your options for running chocolatey here. It looks like Set(c => {c.CommandName = "install"; c.PackageNames = "bob";}).Run();
        /// </summary>
        /// <param name="propConfig">The configuration to set</param>
        /// <returns>This <see cref="GetChocolatey"/> instance</returns>
        public GetChocolatey Set(Action<ChocolateyConfiguration> propConfig)
        {
            _propConfig = propConfig;
            return this;
        }

        /// <summary>
        /// Registers a container component. Does not require a dependency on Simple Injector.
        /// Will override existing component if registered.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="implementation">The implementation.</param>
        /// <returns>This <see cref="GetChocolatey"/> instance</returns>
        /// <remarks>
        /// This requires you to use ILMerged SimpleInjector. If you use SimpleInjector in your codebase, you must now use Chocolatey's version. This is required to not be internalized so licensed code will work appropriately.
        /// </remarks>
        public GetChocolatey RegisterContainerComponent(Type service, Type implementation)
        {
            _container.Register(service, implementation, Lifestyle.Singleton);
            return this;
        }

        /// <summary>
        /// Registers a container component. Does not require a dependency on Simple Injector. 
        /// Will override existing component if registered.
        /// </summary>
        /// <typeparam name="Service">The type of the service.</typeparam>
        /// <typeparam name="Implementation">The type of the Implementation.</typeparam>
        /// <returns>This <see cref="GetChocolatey"/> instance</returns>
        /// <remarks>
        /// This requires you to use ILMerged SimpleInjector. If you use SimpleInjector in your codebase, you must now use Chocolatey's version. This is required to not be internalized so licensed code will work appropriately.
        /// </remarks>
        public GetChocolatey RegisterContainerComponent<Service, Implementation>()
            where Service : class
            where Implementation : class, Service
        {
            return RegisterContainerComponent<Service, Implementation>(Lifestyle.Singleton);
        }

        /// <summary>
        /// Registers a container component. 
        /// Will override existing component if registered.
        /// </summary>
        /// <typeparam name="Service">The type of the service.</typeparam>
        /// <typeparam name="Implementation">The type of the Implementation.</typeparam>
        /// <param name="lifestyle">The lifestyle.</param>
        /// <returns>This <see cref="GetChocolatey"/> instance</returns>
        /// <remarks>
        /// This requires you to use ILMerged SimpleInjector. If you use SimpleInjector in your codebase, you must now use Chocolatey's version. This is required to not be internalized so licensed code will work appropriately.
        /// </remarks>
        public GetChocolatey RegisterContainerComponent<Service, Implementation>(Lifestyle lifestyle)
            where Service : class
            where Implementation : class, Service
        {
            _container.Register<Service, Implementation>(lifestyle);
            return this;
        }

        /// <summary>
        /// Registers a container component. Does not require a dependency on Simple Injector. 
        /// Will override existing component if registered.
        /// </summary>
        /// <typeparam name="Service">The type of the ervice.</typeparam>
        /// <param name="implementationCreator">The implementation creator.</param>
        /// <returns>This <see cref="GetChocolatey"/> instance</returns>
        /// <remarks>
        /// This requires you to use ILMerged SimpleInjector. If you use SimpleInjector in your codebase, you must now use Chocolatey's version. This is required to not be internalized so licensed code will work appropriately.
        /// </remarks>
        public GetChocolatey RegisterContainerComponent<Service>(Func<Service> implementationCreator)
             where Service : class
        {
            _container.Register(implementationCreator, Lifestyle.Singleton);
            return this;
        }

        /// <summary>
        /// Register container components when you need to do multiple setups and want to work with the container directly. 
        /// Will override existing components if registered.
        /// </summary>
        /// <param name="containerSetup">The container setup.</param>
        /// <returns>This <see cref="GetChocolatey"/> instance</returns>
        /// <remarks>
        /// This requires you to use ILMerged SimpleInjector. If you use SimpleInjector in your codebase, you must now use Chocolatey's version. This is required to not be internalized so licensed code will work appropriately.
        /// </remarks>
        public GetChocolatey RegisterContainerComponents(Action<Container> containerSetup)
        {
            if (containerSetup != null)
            {
                containerSetup.Invoke(_container);
            }

            return this;
        }

        /// <summary>
        /// Returns the Chocolatey container. 
        /// WARNING: Once you call GetInstance of any kind, no more items can be registered on the container
        /// </summary>
        /// <returns>The IoC Container (implemented as a SimpleInjector.Container)</returns>
        /// <remarks>
        /// This requires you to use ILMerged SimpleInjector. If you use SimpleInjector in your codebase, you must now use Chocolatey's version. This is required to not be internalized so licensed code will work appropriately.
        /// </remarks>
        public Container Container()
        {
            return _container;
        }

        /// <summary>
        /// Call this method to run Chocolatey after you have set the options.
        /// WARNING: Once this is called, you will not be able to register additional container components.
        /// </summary>
        public void Run()
        {
            ensure_environment();
            extract_resources();
            
            ensure_original_configuration(new List<string>(),
                (config) =>
                {
                    config.RegularOutput = true;
                    var runner = new GenericRunner();
                    runner.run(config, _container, isConsole: false, parseArgs: command =>
                    {
                        command.handle_validation(config);
                    });
                });
        }

        /// <summary>
        ///   Call this method to run chocolatey after you have set the options.
        /// WARNING: Once this is called, you will not be able to register additional container components.
        /// </summary>
        /// <param name="args">Commandline arguments to add to configuration.</param>
        public void RunConsole(string[] args)
        {
            ensure_environment();
            extract_resources();
            
            ensure_original_configuration(new List<string>(args),
              (config) =>
              {
                  var runner = new ConsoleApplication();
                  runner.run(args, config, _container);
              });

        }

        /// <summary>
        ///    Run chocolatey after setting the options, and list the results.
        /// WARNING: Once this is called, you will not be able to register additional container components.
        /// </summary>
        /// <typeparam name="T">The typer of results you're expecting back.</typeparam>
        public IEnumerable<T> List<T>()
        {
            ensure_environment();
            extract_resources();
            
            return ensure_original_configuration(new List<string>(),
                (config) =>
                {
                    config.RegularOutput = true;
                    var runner = new GenericRunner();
                    return runner.list<T>(config, _container, isConsole: false, parseArgs: null);        
                });
        }

        /// <summary>
        ///    Run chocolatey after setting the options,
        ///    and get the count of items that would be returned if you listed the results.
        /// WARNING: Once this is called, you will not be able to register additional container components.
        /// </summary>
        /// <remarks>
        ///    Is intended to be more efficient then simply calling <see cref="List{T}">List</see> and then Count() on the returned list.
        ///    It also returns the full count as is ignores paging.
        /// </remarks>
        public int ListCount()
        {
            ensure_environment();
            extract_resources();

            return ensure_original_configuration(new List<string>(),
               (config) =>
               {
                   config.RegularOutput = true;
                   var runner = new GenericRunner();
                   return runner.count(config, _container, isConsole: false, parseArgs: null);
               });
        }

        private void ensure_original_configuration(IList<string> args, Action<ChocolateyConfiguration> action)
        {
            var success = ensure_original_configuration(args,
                (config) =>
                {
                    if (action != null) action.Invoke(config);
                    return true;
                });
        }

        private T ensure_original_configuration<T>(IList<string> args, Func<ChocolateyConfiguration, T> function)
        {
            var originalConfig = Config.get_configuration_settings().deep_copy();
            var configuration = create_configuration(args);
            var returnValue = default(T);
            try
            {
                if (function != null)
                {
                    returnValue = function.Invoke(configuration);
                }
            }
            finally
            {
                // reset that configuration each time
                configuration = originalConfig;
                Config.initialize_with(originalConfig);
            }
            
            return returnValue;
        }

        /// <summary>
        /// Creates the configuration.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The configuration for Chocolatey</returns>
        private ChocolateyConfiguration create_configuration(IList<string> args)
        {
            // get or create a ChocolateyConfiguration. This maps directly
            // to the same thing that is loaded into the container
            var configuration = Config.get_configuration_settings();
            ConfigurationBuilder.set_up_configuration(
                args,
                configuration,
                _container,
                _license,
                null);

            configuration.PromptForConfirmation = false;
            configuration.AcceptLicense = true;
            if (_propConfig != null)
            {
                _propConfig.Invoke(configuration);
            }

            return configuration;
        }

        /// <summary>
        /// Gets the configuration. Should be used purely for informational purposes
        /// </summary>
        /// <returns>The configuration for Chocolatey</returns>
        /// <remarks>Only call this once you have registered all container components with Chocolatey</remarks>
        public ChocolateyConfiguration GetConfiguration()
        {
            ensure_environment();

            return create_configuration(new List<string>());
        }

        private void ensure_environment()
        {
            string chocolateyInstall = string.Empty;

#if !DEBUG
            chocolateyInstall = Environment.GetEnvironmentVariable(ApplicationParameters.ChocolateyInstallEnvironmentVariableName, EnvironmentVariableTarget.Machine);
            if (string.IsNullOrWhiteSpace(chocolateyInstall))
            {
                chocolateyInstall = Environment.GetEnvironmentVariable(ApplicationParameters.ChocolateyInstallEnvironmentVariableName, EnvironmentVariableTarget.User);
            }
#endif

            if (string.IsNullOrWhiteSpace(chocolateyInstall))
            {
                chocolateyInstall = Environment.GetEnvironmentVariable(ApplicationParameters.ChocolateyInstallEnvironmentVariableName);
            }

            Environment.SetEnvironmentVariable(ApplicationParameters.ChocolateyInstallEnvironmentVariableName, chocolateyInstall);
        }

        private void extract_resources()
        {
            //refactor - thank goodness this is temporary, cuz manifest resource streams are dumb
            IList<string> folders = new List<string>
            {
                "helpers",
                "functions",
                "redirects",
                "tools"
            };

            try
            {
                AssemblyFileExtractor.extract_all_resources_to_relative_directory(_container.GetInstance<IFileSystem>(), Assembly.GetAssembly(typeof(ChocolateyResourcesAssembly)), ApplicationParameters.InstallLocation, folders, ApplicationParameters.ChocolateyFileResources);
            }
            catch (Exception ex)
            {
                this.Log().Warn(ChocolateyLoggers.Important, "Please ensure that ChocolateyInstall environment variable is set properly and you've run once as an administrator to ensure all resources are extracted.");
                this.Log().Error("Unable to extract resources. Please ensure the ChocolateyInstall environment variable is set properly. You may need to run once as an admin to ensure all resources are extracted. Details:{0} {1}".format_with(Environment.NewLine,ex.ToString()));
            }

        }
    }

    // ReSharper restore InconsistentNaming
}
