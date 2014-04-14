namespace chocolatey.infrastructure
{
    public static class ApplicationParameters
    {
        public static string Name = "chocolatey";
        public static readonly string LoggingFile = @"chocolatey.log";
        public static readonly string Log4NetConfigurationAssembly = @"chocolatey";
        public static readonly string Log4NetConfigurationResource = @"chocolatey.infrastructure.logging.log4net.config.xml";
    }
}