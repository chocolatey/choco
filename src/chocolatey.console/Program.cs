namespace chocolatey.console
{
    using System;
    using System.IO;
    using System.Reflection;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.builders;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.configuration;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.information;
    using chocolatey.infrastructure.licensing;
    using chocolatey.infrastructure.logging;
    using chocolatey.infrastructure.registration;
    using chocolatey.infrastructure.services;
    using infrastructure.registration;
    using log4net;
    using log4net.Core;
    using log4net.Repository;
    using log4net.Repository.Hierarchy;

    public sealed class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                string outputDirectory = ApplicationParameters.LoggingLocation;
                //no file system at this point
                if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

                Log4NetAppender.configure(outputDirectory);
                Bootstrap.initialize();
                Bootstrap.startup();

                var container = SimpleInjectorContainer.Initialize();
                var config = container.GetInstance<ChocolateyConfiguration>();
                var fileSystem = container.GetInstance<IFileSystem>();

                config.ChocolateyVersion = VersionInformation.get_current_assembly_version();
                "chocolatey".Log().Info(() => "{0} v{1}".format_with(ApplicationParameters.Name, config.ChocolateyVersion));

                ConfigurationBuilder.set_up_configuration(args, config, fileSystem, container.GetInstance<IXmlService>());
                Config.InitializeWith(config);
                if (config.HelpRequested)
                {
                    pause_execution_if_debug();
                    Environment.Exit(-1);
                }

                set_logging_level_debug_when_debug(config);
                "chocolatey".Log().Debug(() => "{0} is running on {1} v {2}".format_with(ApplicationParameters.Name, config.PlatformType, config.PlatformVersion.to_string()));

                LicenseValidation.Validate(fileSystem);

                var application = new ConsoleApplication();
                application.run(args, config, container);

                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                "chocolatey".Log().Error(() => "{0} had an error on {1} (with user {2}):{3}{4}".format_with(
                    ApplicationParameters.Name,
                    Environment.MachineName,
                    Environment.UserName,
                    Environment.NewLine,
                    ex.ToString()));

                Environment.ExitCode = 1;
            }
            finally
            {
                pause_execution_if_debug();
                Bootstrap.shutdown();
                Environment.Exit(Environment.ExitCode);
            }
        }

        private static void set_logging_level_debug_when_debug(ChocolateyConfiguration configSettings)
        {
            if (configSettings.Debug)
            {
                ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetCallingAssembly());
                logRepository.Threshold = Level.Debug;
                foreach (ILogger log in logRepository.GetCurrentLoggers())
                {
                    var logger = log as Logger;
                    if (logger != null)
                    {
                        logger.Level = Level.Debug;
                    }
                }
            }
        }

        private static void pause_execution_if_debug()
        {
#if DEBUG
            Console.WriteLine("Press enter to continue...");
            Console.ReadKey();
#endif
        }
    }
}