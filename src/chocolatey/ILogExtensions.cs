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

namespace chocolatey
{
    using System;
    using System.Runtime;
    using infrastructure.logging;

    // ReSharper disable InconsistentNaming

    /// <summary>
    ///   Extensions for ILog
    /// </summary>
    public static class ILogExtensions
    {
        /// <summary>
        /// This is changed for testing only
        /// </summary>
        public static bool LogTraceMessages = true;

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void Trace(this ILog logger, string message, params object[] formatting)
        {
           if (LogTraceMessages) ChocolateyLoggers.Trace.to_string().Log().Debug(message, formatting);
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void Trace(this ILog logger, Func<string> message)
        {
            if (LogTraceMessages) ChocolateyLoggers.Trace.to_string().Log().Debug(message);
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void Debug(this ILog logger, ChocolateyLoggers logType, string message, params object[] formatting)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:

                    logger.Debug(message, formatting);
                    break;
                case ChocolateyLoggers.Trace:
                    Trace(logger,message,formatting);
                    break;
                default:
                    logType.to_string().Log().Debug(message, formatting);
                    break;
            }
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void Debug(this ILog logger, ChocolateyLoggers logType, Func<string> message)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:
                    logger.Debug(message);
                    break;
                case ChocolateyLoggers.Trace:
                    Trace(logger, message);
                    break;
                default:
                    logType.to_string().Log().Debug(message);
                    break;
            }
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void Info(this ILog logger, ChocolateyLoggers logType, string message, params object[] formatting)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:
                    logger.Info(message, formatting);
                    break;
                case ChocolateyLoggers.Trace:
                    Trace(logger, message, formatting);
                    break;
                default:
                    logType.to_string().Log().Info(message, formatting);
                    break;
            }
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void Info(this ILog logger, ChocolateyLoggers logType, Func<string> message)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:
                    logger.Info(message);
                    break;
                case ChocolateyLoggers.Trace:
                    Trace(logger, message);
                    break;
                default:
                    logType.to_string().Log().Info(message);
                    break;
            }
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void Warn(this ILog logger, ChocolateyLoggers logType, string message, params object[] formatting)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:
                    logger.Warn(message, formatting);
                    break;
                case ChocolateyLoggers.Trace:
                    Trace(logger, message, formatting);
                    break;
                default:
                    logType.to_string().Log().Warn(message, formatting);
                    break;
            }
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void Warn(this ILog logger, ChocolateyLoggers logType, Func<string> message)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:
                    logger.Warn(message);
                    break;
                case ChocolateyLoggers.Trace:
                    Trace(logger, message);
                    break;
                default:
                    logType.to_string().Log().Warn(message);
                    break;
            }
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void Error(this ILog logger, ChocolateyLoggers logType, string message, params object[] formatting)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:
                    logger.Error(message, formatting);
                    break;
                case ChocolateyLoggers.Trace:
                    Trace(logger, message, formatting);
                    break;
                default:
                    logType.to_string().Log().Error(message, formatting);
                    break;
            }
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void Error(this ILog logger, ChocolateyLoggers logType, Func<string> message)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:
                    logger.Error(message);
                    break;
                case ChocolateyLoggers.Trace:
                    Trace(logger, message);
                    break;
                default:
                    logType.to_string().Log().Error(message);
                    break;
            }
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void Fatal(this ILog logger, ChocolateyLoggers logType, string message, params object[] formatting)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:
                    logger.Fatal(message, formatting);
                    break;
                case ChocolateyLoggers.Trace:
                    Trace(logger, message, formatting);
                    break;
                default:
                    logType.to_string().Log().Fatal(message, formatting);
                    break;
            }
        }

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void Fatal(this ILog logger, ChocolateyLoggers logType, Func<string> message)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:
                    logger.Fatal(message);
                    break;
                case ChocolateyLoggers.Trace:
                    Trace(logger, message);
                    break;
                default:
                    logType.to_string().Log().Fatal(message);
                    break;
            }
        }
    }


    // ReSharper restore InconsistentNaming
}