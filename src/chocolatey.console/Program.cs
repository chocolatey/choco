using chocolatey.infrastructure.configuration;
using chocolatey.infrastructure.platforms;
using log4net;
using log4net.Core;
using log4net.Repository;

namespace chocolatey
{
    using System;
    using System.IO;
    using System.Reflection;
    using infrastructure;
    using infrastructure.licensing;
    using infrastructure.logging;
    using infrastructure.registration;
    using infrastructure.runners;

    public class Program
    {
        private static void Main(string[] args)
        {
            var output_directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ApplicationParameters.name, "logs");
            if (!Directory.Exists(output_directory)) Directory.CreateDirectory(output_directory);

            Log4NetAppender.configure(output_directory);
            Bootstrap.initialize();
            Bootstrap.startup();

            try
            {
                "chocolatey".Log().Info(() => "Starting {0}".FormatWith(ApplicationParameters.name));
                var current_assembly = Assembly.GetExecutingAssembly().Location;
                var assembly_dir = Path.GetDirectoryName(current_assembly);
                var license_file = Path.Combine(assembly_dir, "license.xml");
                LicenseValidation.Validate(license_file);

                ConfigurationSettings config_settings = new ConfigurationSettings();
                set_logging_level_debug_when_debug(config_settings);
                report_platform(config_settings);

                var runner = new ChocolateyInstallRunner();
                runner.run(args);

#if DEBUG
                Console.WriteLine("Press enter to continue...");
                Console.ReadKey();
#endif
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                "chocolatey".Log().Error(() => "{0} had an error on {1} (with user {2}):{3}{4}".FormatWith(
                                      ApplicationParameters.name,
                                      Environment.MachineName,
                                      Environment.UserName,
                                      Environment.NewLine,
                                      ex.ToString()));

#if DEBUG
                Console.WriteLine("Press enter to continue...");
                Console.ReadKey();
#endif
                Environment.Exit(1);
            }
        }

        private static void report_platform(ConfigurationSettings config_settings)
        {
            "chocolatey".Log().Info(() => "{0} is running on {1}".FormatWith(ApplicationParameters.name, Platform.get_platform()));
        }
        
        private static void set_logging_level_debug_when_debug(ConfigurationSettings config_settings)
        {
            if (config_settings.Debug)
            {
                ILoggerRepository log_repository = LogManager.GetRepository(Assembly.GetCallingAssembly());
                log_repository.Threshold = Level.Debug;
                foreach (log4net.Core.ILogger log in log_repository.GetCurrentLoggers())
                {
                    var logger = log as log4net.Repository.Hierarchy.Logger;
                    if (logger != null)
                    {
                        logger.Level = Level.Debug;
                    }
                }
            }
        }
    }
}