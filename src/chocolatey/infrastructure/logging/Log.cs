// Copyright © 2017 - 2018 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
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
    using System;

    // ReSharper disable InconsistentNaming

    /// <summary>
    ///   Logger type initialization
    /// </summary>
    public static class Log
    {
        private static Type _logType = typeof (NullLog);
        private static ILog _logger;

        /// <summary>
        ///   Sets up logging to be with a certain type
        /// </summary>
        /// <typeparam name="T">The type of ILog for the application to use</typeparam>
        /// <param name="resetLoggers">Should other loggers be reset?</param>
        public static void InitializeWith<T>(bool resetLoggers = true) where T : ILog, new()
        {
            _logType = typeof (T);
            if (resetLoggers) LogExtensions.ResetLoggers();
        }

        /// <summary>
        ///   Sets up logging to be with a certain instance. The other method is preferred.
        /// </summary>
        /// <param name="loggerType">Type of the logger.</param>
        /// <param name="resetLoggers">Should other loggers be reset?</param>
        /// <remarks>Resetting the loggers is mostly geared towards testing</remarks>
        public static void InitializeWith(ILog loggerType, bool resetLoggers = true)
        {
            _logType = loggerType.GetType();
            _logger = loggerType;
            if (resetLoggers) LogExtensions.ResetLoggers();
        }

        /// <summary>
        ///   Initializes a new instance of a logger for an object.
        ///   This should be done only once per object name.
        /// </summary>
        /// <param name="objectName">Name of the object.</param>
        /// <returns>ILog instance for an object if log type has been initialized; otherwise null</returns>
        public static ILog GetLoggerFor(string objectName)
        {
            ILog logger = _logger;

            if (_logger == null)
            {
                logger = Activator.CreateInstance(_logType) as ILog;
            }

            if (logger != null)
            {
                logger.InitializeFor(objectName);
            }

            return logger;
        }
    }

    // ReSharper restore InconsistentNaming
}