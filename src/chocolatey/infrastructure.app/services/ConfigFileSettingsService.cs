namespace chocolatey.infrastructure.app.services
{
    using configuration;
    using infrastructure.services;

    public sealed class ConfigFileSettingsService : IConfigFileSettingsService
    {
        private readonly IXmlService _xmlService;

        public ConfigFileSettingsService(IXmlService xmlService)
        {
            _xmlService = xmlService;
        }

        public ConfigFileSettings get_settings(string filePath)
        {
            return _xmlService.deserialize<ConfigFileSettings>(filePath);
        }
    }
}