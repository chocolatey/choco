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

namespace chocolatey
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
    using infrastructure.synchronization;
    using log4net;

#if !NoResources

    using resources;

#endif

    using Assembly = infrastructure.adapters.Assembly;
    using IFileSystem = infrastructure.filesystem.IFileSystem;
    using ILog = infrastructure.logging.ILog;
    using System.Linq;

    /// <summary>
    /// Entry point for API
    /// </summary>
    public static class Lets
    {
        private static readonly log4net.ILog _logger = LogManager.GetLogger(typeof(Lets));

        private static GetChocolatey Setup(bool initializeLogging)
        {
            AddAssemblyResolver();

            return new GetChocolatey(initializeLogging);
        }

        public static GetChocolatey GetChocolatey()
        {
            return GetChocolatey(initializeLogging: true);
        }

        public static GetChocolatey GetChocolatey(bool initializeLogging)
        {
            return GlobalMutex.Enter(() => Setup(initializeLogging), 10);
        }

        private static void AddAssemblyResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolution.ResolveExtensionOrMergedAssembly;
        }
    }

    /// <summary>
    /// The place where all the magic happens.
    /// NOTE: When using the API, this is the only means of accessing the ChocolateyConfiguration without side effects.
    /// DO NOT call `Config.get_configuration_settings()` or access the container to pull out the ChocolateyConfiguration.
    /// Doing so can set configuration items that are retained on next use.
    /// </summary>
    /// <remarks>Chocolatey - the most magical place on Windows</remarks>
    public class GetChocolatey
    {
        private readonly Container _container;
        private readonly ChocolateyLicense _license;
        private readonly LogSinkLog _logSinkLogger = new LogSinkLog();
        private Action<ChocolateyConfiguration> _propConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetChocolatey"/> class.
        /// </summary>
        public GetChocolatey()
            : this(initializeLogging: true)
        {
        }

        public GetChocolatey(bool initializeLogging)
        {
            _container = SimpleInjectorContainer.Container;
            if (initializeLogging)
            {
                string loggingLocation = ApplicationParameters.LoggingLocation;
                var fileSystem = _container.GetInstance<IFileSystem>();
                fileSystem.EnsureDirectoryExists(loggingLocation);

                Log4NetAppenderConfiguration.Configure(loggingLocation, excludeLoggerNames: ChocolateyLoggers.Trace.ToStringSafe());
                Log.InitializeWith(new AggregateLog(new List<ILog>() { new Log4NetLog(), _logSinkLogger }));
                "chocolatey".Log().Debug("XmlConfiguration is now operational");
            }
            _license = License.ValidateLicense();
        }

        /// <summary>
        ///   This is an optional helper to give you the correct settings for a logger. You can still set this in the set by calling propConfig.Logger without having to call this method.
        /// </summary>
        /// <param name="logger">This is the logger you want Chocolatey to also use.</param>
        /// <returns>This <see cref="GetChocolatey"/> instance</returns>
        public GetChocolatey SetCustomLogging(ILog logger)
        {
            return SetCustomLogging(logger, logExistingMessages: true, addToExistingLoggers: false);
        }

        public GetChocolatey SetCustomLogging(ILog logger, bool logExistingMessages)
        {
            return SetCustomLogging(logger, logExistingMessages, addToExistingLoggers: false);
        }

        public GetChocolatey SetCustomLogging(ILog logger, bool logExistingMessages, bool addToExistingLoggers)
        {
            var aggregateLog = new AggregateLog(new List<ILog> { logger });
            if (addToExistingLoggers)
            {
                aggregateLog = new AggregateLog(new List<ILog> { logger, Log.GetLoggerFor("chocolatey") });
            }

            Log.InitializeWith(aggregateLog, resetLoggers: false);
            if (logExistingMessages)
            {
                DrainLogSink(logger);
            }

            return this;
        }

        private void DrainLogSink(ILog logger)
        {
            foreach (var logMessage in _logSinkLogger.Messages.OrEmpty())
            {
                switch (logMessage.LogLevel)
                {
                    case LogLevelType.Trace:
                        logger.Trace(logMessage.Message);
                        break;
                    case LogLevelType.Debug:
                        logger.Debug(logMessage.Message);
                        break;
                    case LogLevelType.Information:
                        logger.Info(logMessage.Message);
                        break;
                    case LogLevelType.Warning:
                        logger.Warn(logMessage.Message);
                        break;
                    case LogLevelType.Error:
                        logger.Error(logMessage.Message);
                        break;
                    case LogLevelType.Fatal:
                        logger.Fatal(logMessage.Message);
                        break;
                }
            }

            _logSinkLogger.Messages.Clear();
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
        /// <typeparam name="Service">The type of the service.</typeparam>
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
        /// WARNING: Ensure you don't nest additional calls to running Chocolatey here.
        /// Make a call, then finish up and make another call. This includes
        ///  - Run()
        ///  - RunConsole()
        ///  - List()
        ///  - ListCount()
        /// </summary>
        public void Run()
        {
            EnsureEnvironment();
            ExtractResources();

            EnsureOriginalConfiguration(new List<string>(),
                (config) =>
                {
                    var runner = new GenericRunner();
                    runner.Run(config, _container, isConsole: false, parseArgs: command =>
                    {
                        command.Validate(config);
                    });
                });
        }

        /// <summary>
        ///   Call this method to run chocolatey after you have set the options.
        /// WARNING: Once this is called, you will not be able to register additional container components.
        /// WARNING: Ensure you don't nest additional calls to running Chocolatey here.
        /// Make a call, then finish up and make another call. This includes
        ///  - Run()
        ///  - RunConsole()
        ///  - List()
        ///  - ListCount()
        /// </summary>
        /// <param name="args">Commandline arguments to add to configuration.</param>
        public void RunConsole(string[] args)
        {
            EnsureEnvironment();
            ExtractResources();

            EnsureOriginalConfiguration(new List<string>(args),
              (config) =>
              {
                  var runner = new ConsoleApplication();
                  runner.Run(args, config, _container);
              });

        }

        /// <summary>
        ///    Run chocolatey after setting the options, and list the results.
        /// WARNING: Once this is called, you will not be able to register additional container components.
        /// WARNING: Ensure you don't nest additional calls to running Chocolatey here.
        /// Make a call, then finish up and make another call. This includes
        ///  - Run()
        ///  - RunConsole()
        ///  - List()
        ///  - ListCount()
        /// </summary>
        /// <typeparam name="T">The typer of results you're expecting back.</typeparam>
        public IEnumerable<T> List<T>()
        {
            EnsureEnvironment();
            ExtractResources();

            return EnsureOriginalConfiguration(new List<string>(),
                (config) =>
                {
                    var runner = new GenericRunner();
                    return runner.List<T>(config, _container, isConsole: false, parseArgs: null);
                });
        }

        /// <summary>
        ///    Run chocolatey after setting the options,
        ///    and get the count of items that would be returned if you listed the results.
        /// WARNING: Once this is called, you will not be able to register additional container components.
        /// WARNING: Ensure you don't nest additional calls to running Chocolatey here.
        /// Make a call, then finish up and make another call. This includes
        ///  - Run()
        ///  - RunConsole()
        ///  - List()
        ///  - ListCount()
        /// </summary>
        /// <remarks>
        ///    Is intended to be more efficient then simply calling <see cref="List{T}">List</see> and then Count() on the returned list.
        ///    It also returns the full count as is ignores paging.
        /// </remarks>
        public int ListCount()
        {
            EnsureEnvironment();
            ExtractResources();

            return EnsureOriginalConfiguration(new List<string>(),
               (config) =>
               {
                   var runner = new GenericRunner();
                   return runner.Count(config, _container, isConsole: false, parseArgs: null);
               });
        }

        /// <summary>
        /// Gets a copy of the configuration. Any changes here will have no effect as this is provided purely for informational purposes.
        /// </summary>
        /// <returns>The configuration for Chocolatey</returns>
        /// <remarks>Only call this once you have registered all container components with Chocolatey</remarks>
        public ChocolateyConfiguration GetConfiguration()
        {
            EnsureEnvironment();

            // ensure_original_configuration() already calls create_configuration()
            // so no need to repeat, just grab the result
            var configuration = EnsureOriginalConfiguration(
                new List<string>(),
                (config) => config
            );

            return configuration;
        }

        private void EnsureOriginalConfiguration(IList<string> args, Action<ChocolateyConfiguration> action)
        {
            var success = EnsureOriginalConfiguration(args,
                (config) =>
                {
                    if (action != null) action.Invoke(config);
                    return true;
                });
        }

        /// <summary>
        /// After the construction of GetChocolatey, we should have a ChocolateyConfiguration or LicensedChocolateyConfiguration loaded into the environment.
        /// We want that original configuration to live on between calls to the API. This function ensures that the
        /// original default configuration from new() is reset after each command finishes running, even as each command
        /// may make changes to the configuration it uses.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args">The arguments.</param>
        /// <param name="function">The function.</param>
        /// <returns></returns>
        private T EnsureOriginalConfiguration<T>(IList<string> args, Func<ChocolateyConfiguration, T> function)
        {
            var originalConfig = Config.GetConfigurationSettings().DeepCopy();
            var configuration = CreateConfiguration(args);
            var returnValue = default(T);
            try
            {
                if (function != null)
                {
                    returnValue = function.Invoke(configuration);
                }

                var verboseAppenderName = "{0}LoggingColoredConsoleAppender".FormatWith(ChocolateyLoggers.Verbose.ToStringSafe());
                var traceAppenderName = "{0}LoggingColoredConsoleAppender".FormatWith(ChocolateyLoggers.Trace.ToStringSafe());
                Log4NetAppenderConfiguration.EnableDebugLoggingIf(configuration.Debug, verboseAppenderName, traceAppenderName);
                Log4NetAppenderConfiguration.EnableVerboseLoggingIf(configuration.Verbose, configuration.Debug, verboseAppenderName);
                Log4NetAppenderConfiguration.EnableTraceLoggingIf(configuration.Trace, traceAppenderName);
            }
            finally
            {
                // reset that configuration each time
                configuration = originalConfig;
                Config.InitializeWith(originalConfig);
            }

            return returnValue;
        }

        /// <summary>
        /// Creates the configuration.
        /// This should never be called directly, as it can cause issues that are very difficult to debug.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The configuration for Chocolatey</returns>
        private ChocolateyConfiguration CreateConfiguration(IList<string> args)
        {
            // get or create a ChocolateyConfiguration. This maps directly
            // to the same thing that is loaded into the container
            var configuration = Config.GetConfigurationSettings();
            ConfigurationBuilder.SetupConfiguration(
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

        private void EnsureEnvironment()
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

        private void ExtractResources()
        {
            //refactor - thank goodness this is temporary, cuz manifest resource streams are dumb
            IList<string> folders = new List<string>
            {
                "helpers",
                "functions",
                "redirects",
                "tools"
            };

#if !NoResources
            try
            {
                AssemblyFileExtractor.ExtractAssemblyResourcesToRelativeDirectory(_container.GetInstance<IFileSystem>(), Assembly.GetAssembly(typeof(ChocolateyResourcesAssembly)), ApplicationParameters.InstallLocation, folders, ApplicationParameters.ChocolateyFileResources);
            }
            catch (Exception ex)
            {
                this.Log().Warn(ChocolateyLoggers.Important, "Please ensure that ChocolateyInstall environment variable is set properly and you've run once as an administrator to ensure all resources are extracted.");
                this.Log().Error("Unable to extract resources. Please ensure the ChocolateyInstall environment variable is set properly. You may need to run once as an admin to ensure all resources are extracted. Details:{0} {1}".FormatWith(Environment.NewLine, ex.ToString()));
            }
#endif
        }
    }
}
