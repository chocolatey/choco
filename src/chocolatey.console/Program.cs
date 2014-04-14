namespace chocolatey.console
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.configuration;
    using chocolatey.infrastructure.information;
    using chocolatey.infrastructure.licensing;
    using chocolatey.infrastructure.logging;
    using chocolatey.infrastructure.platforms;
    using chocolatey.infrastructure.registration;
    using log4net;
    using log4net.Core;
    using log4net.Repository;
    using log4net.Repository.Hierarchy;

    public class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                string outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ApplicationParameters.Name, "logs");
                if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

                Log4NetAppender.configure(outputDirectory);
                Bootstrap.initialize();
                Bootstrap.startup();

                IConfigurationSettings config = new ConfigurationSettings();

                config.ChocolateyVersion = VersionInformation.get_current_assembly_version();
                "chocolatey".Log().Info(() => "{0} v{1}".format_with(ApplicationParameters.Name, config.ChocolateyVersion));

                ConfigurationOptions.parse_arguments_and_update_configuration(args, config, 
                    (option_set) =>
                    {
                        option_set
                            .Add("d|debug",
                                 "Run in Debug Mode",
                                 option => config.Debug = option != null)
                            .Add("f|force",
                                 "Force",
                                 option => config.Force = option != null)
                            .Add("noop",
                                 "Noop - Don't actually do anything",
                                 option => config.Noop = option != null)
                            ;
                    },
                    (unparsedArgs) =>
                        {
                            if (!string.IsNullOrWhiteSpace(config.CommandName))
                            {
                                // save help for next menu
                                config.HelpRequested = false;
                            }
                        },
                    () =>
                        {
                            var commandsLog = new StringBuilder();
                            foreach (var command in Enum.GetValues(typeof (CommandNameType)).Cast<CommandNameType>())
                            {
                                commandsLog.AppendFormat(" * {0}\n", command.GetDescriptionOrValue());
                            }

                            "chocolatey".Log().Info(@"
Commands:
{1}
Please run chocolatey with `choco command -help` for specific help on each command.".format_with(config.ChocolateyVersion, commandsLog.ToString()));
                                                                                  });

                if (config.HelpRequested)
                {
                    pause_execution_if_debug();
                    Environment.Exit(-1);
                }

                set_logging_level_debug_when_debug(config);
                set_and_report_platform(config);

                string currentAssemblyLocation = Assembly.GetExecutingAssembly().Location;
                string assemblyDirectory = Path.GetDirectoryName(currentAssemblyLocation);
                string licenseFile = Path.Combine(assemblyDirectory, "license.xml");
                LicenseValidation.Validate(licenseFile);

                Config.InitializeWith(config);
                var application = new ConsoleApplication();
                application.run(args, config);

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

        private static void set_and_report_platform(IConfigurationSettings config)
        {
            config.PlatformType = Platform.get_platform();
            config.PlatformVersion = Platform.get_version();
            "chocolatey".Log().Debug(() => "{0} is running on {1} v {2}".format_with(ApplicationParameters.Name, config.PlatformType, config.PlatformVersion.to_string()));
        }

        private static void set_logging_level_debug_when_debug(IConfigurationSettings configSettings)
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