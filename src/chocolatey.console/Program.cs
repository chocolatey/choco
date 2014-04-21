namespace chocolatey.console
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using infrastructure.app;
    using infrastructure.app.builders;
    using infrastructure.app.configuration;
    using infrastructure.app.extractors;
    using infrastructure.app.runners;
    using infrastructure.configuration;
    using infrastructure.filesystem;
    using infrastructure.licensing;
    using infrastructure.logging;
    using infrastructure.registration;
    using infrastructure.services;
    using resources;

    public sealed class Program
    {
// ReSharper disable InconsistentNaming
        private static void Main(string[] args)
// ReSharper restore InconsistentNaming
        {
            try
            {
                string outputDirectory = ApplicationParameters.LoggingLocation;
                //no file system at this point
                if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

                Log4NetAppenderConfiguration.configure(outputDirectory);
                Bootstrap.initialize();
                Bootstrap.startup();

                var container = SimpleInjectorContainer.initialize();
                var config = container.GetInstance<ChocolateyConfiguration>();
                var fileSystem = container.GetInstance<IFileSystem>();

                ConfigurationBuilder.set_up_configuration(args, config, fileSystem, container.GetInstance<IXmlService>());
                Config.initialize_with(config);

                "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "{0} v{1}".format_with(ApplicationParameters.Name, config.ChocolateyVersion));

                if (config.HelpRequested)
                {
                    pause_execution_if_debug();
                    Environment.Exit(-1);
                }

                Log4NetAppenderConfiguration.set_verbose_logger_when_verbose(config.Verbose, ChocolateyLoggers.Verbose.to_string());
                Log4NetAppenderConfiguration.set_logging_level_debug_when_debug(config.Debug);
                "chocolatey".Log().Debug(() => "{0} is running on {1} v {2}".format_with(ApplicationParameters.Name, config.PlatformType, config.PlatformVersion.to_string()));

                LicenseValidation.validate(fileSystem);

                //refactor - thank goodness this is temporary, cuz manifest resource streams are dumb
                IList<string> folders = new List<string>
                    {
                        "helpers",
                        "functions",
                        "redirects",
                        "tools"
                    };
                AssemblyFileExtractor.extract_all_chocolatey_resources_to_relative_directory(fileSystem, Assembly.GetAssembly(typeof (ChocolateyResourcesAssembly)), ApplicationParameters.InstallLocation, folders);

                var application = new ConsoleApplication();
                application.run(args, config, container);
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

        private static void pause_execution_if_debug()
        {
#if DEBUG
            Console.WriteLine("Press enter to continue...");
            Console.ReadKey();
#endif
        }
    }
}