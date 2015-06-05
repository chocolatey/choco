// Copyright © 2011 - Present RealDimensions Software, LLC
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

namespace chocolatey.infrastructure.logging
{
    using System.IO;
    using adapters;
    using app;
    using log4net;
    using log4net.Appender;
    using log4net.Config;
    using log4net.Core;
    using log4net.Layout;
    using log4net.Repository;
    using log4net.Repository.Hierarchy;
    using platforms;

    public sealed class Log4NetAppenderConfiguration
    {
        private static readonly log4net.ILog _logger = LogManager.GetLogger(typeof (Log4NetAppenderConfiguration));

        private static bool _alreadyConfiguredFileAppender;

        /// <summary>
        ///   Pulls xml configuration from embedded location and applies it. 
        ///   Then it configures a file appender to the specified output directory if one is provided.
        /// </summary>
        /// <param name="outputDirectory">The output directory.</param>
        public static void configure(string outputDirectory = null)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resource = ApplicationParameters.Log4NetConfigurationResource;
            if (Platform.get_platform() != PlatformType.Windows)
            {
                // it became much easier to do this once we realized that updating the current mappings is about impossible.
                resource = resource.Replace("log4net.", "log4net.mono.");
            }
            Stream xmlConfigStream = assembly.get_manifest_stream(resource);

            XmlConfigurator.Configure(xmlConfigStream);

            if (outputDirectory != null)
            {
                set_file_appender(outputDirectory);
            }

            _logger.DebugFormat("Configured {0} from assembly {1}", resource, assembly.FullName);
        }

        /// <summary>
        ///   Adds a file appender to all current loggers. Only runs one time.
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

                ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetCallingAssembly().UnderlyingType);
                foreach (ILogger log in logRepository.GetCurrentLoggers())
                {
                    var logger = log as Logger;
                    if (logger != null)
                    {
                        logger.AddAppender(app);
                    }
                }
            }
        }

        /// <summary>
        ///   Sets all loggers to Debug level
        /// </summary>
        /// <param name="enableDebug">
        ///   if set to <c>true</c> [enable debug].
        /// </param>
        public static void set_logging_level_debug_when_debug(bool enableDebug)
        {
            if (enableDebug)
            {
                ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetCallingAssembly().UnderlyingType);
                logRepository.Threshold = Level.Debug;
                foreach (ILogger log in logRepository.GetCurrentLoggers())
                {
                    var logger = log as Logger;
                    if (logger != null)
                    {
                        logger.Level = Level.Debug;
                    }
                } 
                foreach (var append in logRepository.GetAppenders())
                {
                    var appender = append as AppenderSkeleton;
                    if (appender != null)
                    {
                        // slightly naive implementation
                        appender.ClearFilters();
                    }
                }
            }
        }

        /// <summary>
        ///   Sets a named verbose logger to Info Level when enableVerbose is true.
        /// </summary>
        /// <param name="enableVerbose">
        ///   if set to <c>true</c> [enable verbose].
        /// </param>
        /// <param name="verboseLoggerName">Name of the verbose logger.</param>
        public static void set_verbose_logger_when_verbose(bool enableVerbose, string verboseLoggerName)
        {
            if (enableVerbose)
            {
                ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetCallingAssembly().UnderlyingType);
                foreach (var append in logRepository.GetAppenders())
                {

                    var appender = append as AppenderSkeleton;
                    if (appender != null && appender.Name.is_equal_to(verboseLoggerName))
                    {
                        appender.ClearFilters();
                        appender.AddFilter( new log4net.Filter.LevelRangeFilter {LevelMin = Level.Info, LevelMax = Level.Fatal});
                    }
                }

            }
        }
    }
}