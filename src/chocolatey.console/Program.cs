namespace chocolatey.console
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using infrastructure.adapters;
    using infrastructure.app;
    using infrastructure.app.builders;
    using infrastructure.app.configuration;
    using infrastructure.app.runners;
    using infrastructure.configuration;
    using infrastructure.extractors;
    using infrastructure.filesystem;
    using infrastructure.licensing;
    using infrastructure.logging;
    using infrastructure.registration;
    using infrastructure.services;
    using resources;
    using Console = System.Console;
    using Environment = System.Environment;

    public sealed class Program
    {
// ReSharper disable InconsistentNaming
        private static void Main(string[] args)
// ReSharper restore InconsistentNaming
        {
            try
            {
                string loggingLocation = ApplicationParameters.LoggingLocation;
                //no file system at this point
                if (!Directory.Exists(loggingLocation)) Directory.CreateDirectory(loggingLocation);

                Log4NetAppenderConfiguration.configure(loggingLocation);
                Bootstrap.initialize();
                Bootstrap.startup();

                var container = SimpleInjectorContainer.initialize();
                var config = container.GetInstance<ChocolateyConfiguration>();
                var fileSystem = container.GetInstance<IFileSystem>();

                ConfigurationBuilder.set_up_configuration(args, config, fileSystem, container.GetInstance<IXmlService>());
                Config.initialize_with(config);

                if (config.RegularOuptut)
                {
                    "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "{0} v{1}".format_with(ApplicationParameters.Name, config.Information.ChocolateyVersion));
                }
                
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
                AssemblyFileExtractor.extract_all_resources_to_relative_directory(fileSystem, Assembly.GetAssembly(typeof(ChocolateyResourcesAssembly)), ApplicationParameters.InstallLocation, folders, ApplicationParameters.ChocolateyFileResources);

                var application = new ConsoleApplication();
                application.run(args, config, container);
            }
            catch (Exception ex)
            {
                var debug = false;
                // no access to the good stuff here, need to go a bit primitive in parsing args
                foreach (var arg in args.or_empty_list_if_null())
                {
                    if (arg.Contains("debug") || arg.is_equal_to("-d") || arg.is_equal_to("/d"))
                    {
                        debug = true;
                        break;
                    }
                }

                if (debug)
                {
                    "chocolatey".Log().Error(() => "{0} had an error occur:{1}{2}".format_with(
                                  ApplicationParameters.Name,
                                  Environment.NewLine,
                                  ex.ToString()));
                }
                else
                {
                    "chocolatey".Log().Error(ChocolateyLoggers.Important,() => "{0}".format_with(ex.Message));
                }

                Environment.ExitCode = 1;
            }
            finally
            {
                "chocolatey".Log().Debug(() => "Exiting with {0}".format_with(Environment.ExitCode));
#if DEBUG
                "chocolatey".Log().Info(() => "Exiting with {0}".format_with(Environment.ExitCode));
#endif
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