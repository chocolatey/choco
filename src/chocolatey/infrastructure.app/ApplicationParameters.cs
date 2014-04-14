namespace chocolatey.infrastructure.app
{
    using System;
    using infrastructure.configuration;

    public static class ApplicationParameters
    {
        public static string Name = "Chocolatey";
        public static readonly string LoggingFile = @"chocolatey.log";
        public static readonly string Log4NetConfigurationAssembly = @"chocolatey";
        public static readonly string Log4NetConfigurationResource = @"chocolatey.infrastructure.logging.log4net.config.xml";


        private static T TryGetConfig<T>(Func<T> func, T defaultValue)
        {
            try
            {
                return func.Invoke();
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        /// <summary>
        ///   Are we in Debug Mode?
        /// </summary>
        public static bool IsDebug
        {
            get { return TryGetConfig(() => Config.GetConfigurationSettings().Debug, false); }
        }
    }
}