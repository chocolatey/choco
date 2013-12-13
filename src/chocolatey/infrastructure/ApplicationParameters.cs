namespace chocolatey.infrastructure
{

    public static class ApplicationParameters
    {
        public static string name = "chocolatey";
        public static readonly string logging_file = @"chocolatey.log";
        public static readonly string log4net_configuration_assembly = @"chocolatey";
        public static readonly string log4net_configuration_resource = @"chocolatey.infrastructure.logging.log4net.config.xml";
    }
}