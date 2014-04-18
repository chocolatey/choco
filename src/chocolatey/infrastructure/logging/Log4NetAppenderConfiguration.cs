namespace chocolatey.infrastructure.logging
{
    using System.IO;
    using System.Reflection;
    using app;
    using app.configuration;
    using log4net;
    using log4net.Appender;
    using log4net.Config;
    using log4net.Core;
    using log4net.Layout;
    using log4net.Repository;
    using log4net.Repository.Hierarchy;

    public sealed class Log4NetAppenderConfiguration
    {
        private static readonly log4net.ILog _logger = LogManager.GetLogger(typeof (Log4NetAppenderConfiguration));

        private static bool _alreadyConfiguredFileAppender;

        /// <summary>
        /// Pulls xmlconfiguration from embedded location and applies it. Then it configures a file appender to the specified output directory. 
        /// </summary>
        /// <param name="outputDirectory">The output directory.</param>
        public static void configure(string outputDirectory)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream xmlConfigStream = assembly.get_manifest_stream(ApplicationParameters.Log4NetConfigurationResource);

            XmlConfigurator.Configure(xmlConfigStream);
            set_file_appender(outputDirectory);

            _logger.DebugFormat("Configured {0} from assembly {1}", ApplicationParameters.Log4NetConfigurationResource, assembly.FullName);
        }

        /// <summary>
        /// Adds a file appender to all current loggers. Only runs one time.
        /// </summary>
        /// <param name="outputDirectory">The output directory.</param>
        private static void set_file_appender(string outputDirectory)
        {
            if (!_alreadyConfiguredFileAppender)
            {
                _alreadyConfiguredFileAppender = true;
             
                var layout = new PatternLayout
                    {
                        ConversionPattern = "%date [%-5level] - %message%newline"
                    };
                layout.ActivateOptions();

                var lockingModel = new FileAppender.MinimalLock();

                var app = new RollingFileAppender
                    {
                        Name = "{0}.changes.log.appender".format_with(ApplicationParameters.Name),
                        File = Path.Combine(Path.GetFullPath(outputDirectory), ApplicationParameters.LoggingFile),
                        Layout = layout,
                        AppendToFile = true,
                        RollingStyle = RollingFileAppender.RollingMode.Size,
                        MaxFileSize = 1024*1024,
                        MaxSizeRollBackups = 10,
                        LockingModel = lockingModel,
                    };
                app.ActivateOptions();

                ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetCallingAssembly());
                foreach (ILogger log in logRepository.GetCurrentLoggers())
                {
                    var logger = log as Logger;
                    logger.AddAppender(app);
                }
            }
        }

        /// <summary>
        /// Sets all loggers to Debug level
        /// </summary>
        /// <param name="enableDebug">if set to <c>true</c> [enable debug].</param>
        public static void set_logging_level_debug_when_debug(bool enableDebug)
        {
            if (enableDebug)
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

        /// <summary>
        /// Sets a named verbose logger to Info Level when enableVerbose is true.
        /// </summary>
        /// <param name="enableVerbose">if set to <c>true</c> [enable verbose].</param>
        /// <param name="verboseLoggerName">Name of the verbose logger.</param>
        public static void set_verbose_logger_when_verbose(bool enableVerbose,string verboseLoggerName)
        {
            if (enableVerbose)
            {
                ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetCallingAssembly());
                foreach (ILogger log in logRepository.GetCurrentLoggers())
                {
                    var logger = log as Logger;
                    if (logger != null && logger.Name.is_equal_to(verboseLoggerName))
                    {
                        logger.Level = Level.Info;
                    }
                }
            }
        }
    
    }
}