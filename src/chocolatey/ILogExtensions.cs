namespace chocolatey
{
    using System;
    using infrastructure.logging;

    public static class ILogExtensions
    {
        public static void Debug(this ILog logger, ChocolateyLoggers logType, string message, params object[] formatting)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:

                    logger.Debug(message, formatting);
                    break;
                default:
                    logType.to_string().Log().Debug(message,formatting);
                    break;
            }
        }

        public static void Debug(this ILog logger, ChocolateyLoggers logType, Func<string> message)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:
                    logger.Debug(message);
                    break;
                default:
                    logType.to_string().Log().Debug(message);
                    break;
            }
        }  
        
        public static void Info(this ILog logger, ChocolateyLoggers logType, string message, params object[] formatting)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:

                    logger.Info(message, formatting);
                    break;
                default:
                    logType.to_string().Log().Info(message,formatting);
                    break;
            }
        }

        public static void Info(this ILog logger, ChocolateyLoggers logType, Func<string> message)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:
                    logger.Info(message);
                    break;
                default:
                    logType.to_string().Log().Info(message);
                    break;
            }
        }
       
        public static void Warn(this ILog logger, ChocolateyLoggers logType, string message, params object[] formatting)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:

                    logger.Warn(message, formatting);
                    break;
                default:
                    logType.to_string().Log().Warn(message, formatting);
                    break;
            }
        }

        public static void Warn(this ILog logger, ChocolateyLoggers logType, Func<string> message)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:
                    logger.Warn(message);
                    break;
                default:
                    logType.to_string().Log().Warn(message);
                    break;
            }
        }       

        public static void Error(this ILog logger, ChocolateyLoggers logType, string message, params object[] formatting)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:

                    logger.Error(message, formatting);
                    break;
                default:
                    logType.to_string().Log().Error(message, formatting);
                    break;
            }
        }

        public static void Error(this ILog logger, ChocolateyLoggers logType, Func<string> message)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:
                    logger.Error(message);
                    break;
                default:
                    logType.to_string().Log().Error(message);
                    break;
            }
        }

        public static void Fatal(this ILog logger, ChocolateyLoggers logType, string message, params object[] formatting)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:

                    logger.Fatal(message, formatting);
                    break;
                default:
                    logType.to_string().Log().Fatal(message, formatting);
                    break;
            }
        }

        public static void Fatal(this ILog logger, ChocolateyLoggers logType, Func<string> message)
        {
            switch (logType)
            {
                case ChocolateyLoggers.Normal:
                    logger.Fatal(message);
                    break;
                default:
                    logType.to_string().Log().Fatal(message);
                    break;
            }
        }
    }
}