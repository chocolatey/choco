namespace chocolatey
{
    using System;
    using System.IO;
    using System.Reflection;
    using infrastructure;
    using infrastructure.configuration;
    using infrastructure.licensing;
    using infrastructure.logging;
    using infrastructure.platforms;
    using infrastructure.registration;
    using infrastructure.runners;
    using log4net;
    using log4net.Core;
    using log4net.Repository;
    using log4net.Repository.Hierarchy;

    public class Program
    {
        private static void Main(string[] args)
        {
            string outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ApplicationParameters.Name, "logs");
            if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

            Log4NetAppender.configure(outputDirectory);
            Bootstrap.initialize();
            Bootstrap.startup();

            try
            {
                "chocolatey".Log().Info(() => "Starting {0}".FormatWith(ApplicationParameters.Name));
                string currentAssembly = Assembly.GetExecutingAssembly().Location;
                string assemblyDirectory = Path.GetDirectoryName(currentAssembly);
                string licenseFile = Path.Combine(assemblyDirectory, "license.xml");
                LicenseValidation.Validate(licenseFile);

                var configSettings = new ConfigurationSettings();
                set_logging_level_debug_when_debug(configSettings);
                report_platform(configSettings);

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
                    ApplicationParameters.Name,
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

        private static void report_platform(ConfigurationSettings configSettings)
        {
            "chocolatey".Log().Info(() => "{0} is running on {1}".FormatWith(ApplicationParameters.Name, Platform.get_platform()));
        }

        private static void set_logging_level_debug_when_debug(ConfigurationSettings configSettings)
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
    }
}