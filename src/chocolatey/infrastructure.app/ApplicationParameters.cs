namespace chocolatey.infrastructure.app
{
    using System;
    using System.IO;

    public static class ApplicationParameters
    {
        public static readonly string Name = "Chocolatey";
        public static readonly string LoggingLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Name, "logs");
        public static readonly string LoggingFile = @"chocolatey.log";
        public static readonly string Log4NetConfigurationAssembly = @"chocolatey";
        public static readonly string Log4NetConfigurationResource = @"chocolatey.infrastructure.logging.log4net.config.xml";
        public static readonly string ChocolateyConfigFileResource = @"chocolatey.infrastructure.app.configuration.chocolatey.config";
        public static readonly string GlobalConfigFileLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Name, "config", "chocolatey.config");
        public static readonly string LicenseFileLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Name, "license", "chocolatey.license.xml");

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

        ///// <summary>
        /////   Are we in Debug Mode?
        ///// </summary>
        //public static bool IsDebug
        //{
        //    get { return TryGetConfig(() => Config.GetConfigurationSettings().Debug, false); }
        //}
    }
}