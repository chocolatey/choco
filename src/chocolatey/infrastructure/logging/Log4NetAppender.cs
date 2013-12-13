namespace chocolatey.infrastructure.logging
{
    using System.IO;
    using System.Reflection;
    using log4net;
    using log4net.Appender;
    using log4net.Config;
    using log4net.Layout;
    using log4net.Repository.Hierarchy;

    public class Log4NetAppender
    {
        private static readonly log4net.ILog the_logger = LogManager.GetLogger(typeof (Log4NetAppender));

        public static void configure(string output_directory)
        {
            var assembly = Assembly.GetExecutingAssembly();
            Stream xml_config_stream = assembly.get_manifest_stream(ApplicationParameters.log4net_configuration_resource);

            XmlConfigurator.Configure(xml_config_stream);
            set_file_appender(output_directory);

            the_logger.DebugFormat("Configured {0} from assembly {1}", ApplicationParameters.log4net_configuration_resource, assembly.FullName);
        }

        private static bool already_configured_file_appender;

        public static void set_file_appender(string output_directory)
        {
            if (!already_configured_file_appender)
            {
                already_configured_file_appender = true;
                var log = LogManager.GetLogger(ApplicationParameters.name);
                var l = (Logger) log.Logger;

                var layout = new PatternLayout
                                 {
                                     ConversionPattern = "%date [%-5level] - %message%newline"
                                 };
                layout.ActivateOptions();

                var lockingModel = new FileAppender.MinimalLock();

                var app = new RollingFileAppender
                              {
                                  Name = "{0}.changes.log.appender".FormatWith(ApplicationParameters.name),
                                  File = Path.Combine(Path.GetFullPath(output_directory), ApplicationParameters.logging_file),
                                  Layout = layout,
                                  AppendToFile = true,
                                  RollingStyle = RollingFileAppender.RollingMode.Size,
                                  MaxFileSize = 1024 * 1024,
                                  MaxSizeRollBackups = 10,
                                  LockingModel = lockingModel,
                              };
                app.ActivateOptions();

                l.AddAppender(app);
            }
        }
    }
}