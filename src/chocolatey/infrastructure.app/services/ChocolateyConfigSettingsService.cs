namespace chocolatey.infrastructure.app.services
{
    using System.Linq;
    using configuration;
    using infrastructure.services;
    using nuget;

    class ChocolateyConfigSettingsService : IChocolateyConfigSettingsService
    {
        private readonly ConfigFileSettings _configFileSettings;
        private readonly IXmlService _xmlService;

        public ChocolateyConfigSettingsService(IXmlService xmlService)
        {
            _xmlService = xmlService;
            _configFileSettings = _xmlService.deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation);
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            //todo: something
        }

        public void source_list(ChocolateyConfiguration configuration)
        {
            foreach (var source in _configFileSettings.Sources)
            {
                this.Log().Info(() => "{0}{1} - {2}".format_with(source.Id, source.Disabled ? " [Disabled]" : string.Empty, source.Value));
            }
        }
        
        public void source_add(ChocolateyConfiguration configuration)
        {
            var source = _configFileSettings.Sources.FirstOrDefault(p => p.Id == configuration.SourceCommand.Name);
            if (source == null)
            {
                _configFileSettings.Sources.Add(new ConfigFileSourceSetting()
                    {
                        Id = configuration.SourceCommand.Name,
                        Value = configuration.SourceCommand.Source, 
                        UserName = configuration.SourceCommand.Name,
                        Password = NugetEncryptionUtility.EncryptString(configuration.SourceCommand.Password),
                    });

                _xmlService.serialize(_configFileSettings, ApplicationParameters.GlobalConfigFileLocation);

                this.Log().Info(() =>"Added {0} - {1}".format_with(configuration.SourceCommand.Name, configuration.SourceCommand.Source));
            }
        }

        public void source_remove(ChocolateyConfiguration configuration)
        {
            var source = _configFileSettings.Sources.FirstOrDefault(p => p.Id == configuration.SourceCommand.Name);
            if (source != null)
            {
                _configFileSettings.Sources.Remove(source);
                _xmlService.serialize(_configFileSettings, ApplicationParameters.GlobalConfigFileLocation);

                this.Log().Info(() => "Removed {0}".format_with(source.Id));
            }
        }

        public void source_disable(ChocolateyConfiguration configuration)
        {
            var source = _configFileSettings.Sources.FirstOrDefault(p => p.Id == configuration.SourceCommand.Name);
            if (source != null && !source.Disabled)
            {
                source.Disabled = true;
                _xmlService.serialize(_configFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                this.Log().Info(() => "Disabled {0}".format_with(source.Id));
            }
        }

        public void source_enable(ChocolateyConfiguration configuration)
        {
            var source = _configFileSettings.Sources.FirstOrDefault(p => p.Id == configuration.SourceCommand.Name);
            if (source != null && source.Disabled)
            {
                source.Disabled = false;
                _xmlService.serialize(_configFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                this.Log().Info(() => "Enabled {0}".format_with(source.Id));
            }
        }
    }
}