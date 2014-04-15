namespace chocolatey.infrastructure.logging
{
    using System.IO;
    using System.Reflection;
    using app;
    using log4net;
    using log4net.Appender;
    using log4net.Config;
    using log4net.Layout;
    using log4net.Repository.Hierarchy;

    public sealed class Log4NetAppender
    {
        private static readonly log4net.ILog _logger = LogManager.GetLogger(typeof (Log4NetAppender));

        private static bool _alreadyConfiguredFileAppender;

        public static void configure(string outputDirectory)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Stream xmlConfigStream = assembly.get_manifest_stream(ApplicationParameters.Log4NetConfigurationResource);

            XmlConfigurator.Configure(xmlConfigStream);
            set_file_appender(outputDirectory);

            _logger.DebugFormat("Configured {0} from assembly {1}", ApplicationParameters.Log4NetConfigurationResource, assembly.FullName);
        }

        public static void set_file_appender(string outputDirectory)
        {
            if (!_alreadyConfiguredFileAppender)
            {
                _alreadyConfiguredFileAppender = true;
                log4net.ILog log = LogManager.GetLogger(ApplicationParameters.Name);
                var l = (Logger) log.Logger;

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

                l.AddAppender(app);
            }
        }
    }
}