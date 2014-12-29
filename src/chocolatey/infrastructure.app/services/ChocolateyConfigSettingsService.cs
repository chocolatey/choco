namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Linq;
    using configuration;
    using infrastructure.services;
    using nuget;

    class ChocolateyConfigSettingsService : IChocolateyConfigSettingsService
    {
        private readonly Lazy<ConfigFileSettings> _configFileSettings;
        private readonly IXmlService _xmlService;

        private ConfigFileSettings configFileSettings
        {
            get { return _configFileSettings.Value; }
        }

        public ChocolateyConfigSettingsService(IXmlService xmlService)
        {
            _xmlService = xmlService;
            _configFileSettings = new Lazy<ConfigFileSettings>(()=> _xmlService.deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation));
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            //todo: something
        }

        public void source_list(ChocolateyConfiguration configuration)
        {
            foreach (var source in configFileSettings.Sources)
            {
                this.Log().Info(() => "{0}{1} - {2}".format_with(source.Id, source.Disabled ? " [Disabled]" : string.Empty, source.Value));
            }
        }
        
        public void source_add(ChocolateyConfiguration configuration)
        {
            var source = configFileSettings.Sources.FirstOrDefault(p => p.Id.is_equal_to(configuration.SourceCommand.Name));
            if (source == null)
            {
                configFileSettings.Sources.Add(new ConfigFileSourceSetting()
                    {
                        Id = configuration.SourceCommand.Name,
                        Value = configuration.Source, 
                        UserName = configuration.SourceCommand.Name,
                        Password = NugetEncryptionUtility.EncryptString(configuration.SourceCommand.Password),
                    });

                _xmlService.serialize(_configFileSettings, ApplicationParameters.GlobalConfigFileLocation);

                this.Log().Info(() =>"Added {0} - {1}".format_with(configuration.SourceCommand.Name, configuration.Source));
            }
        }

        public void source_remove(ChocolateyConfiguration configuration)
        {
            var source = configFileSettings.Sources.FirstOrDefault(p => p.Id.is_equal_to(configuration.SourceCommand.Name));
            if (source != null)
            {
                configFileSettings.Sources.Remove(source);
                _xmlService.serialize(_configFileSettings, ApplicationParameters.GlobalConfigFileLocation);

                this.Log().Info(() => "Removed {0}".format_with(source.Id));
            }
        }

        public void source_disable(ChocolateyConfiguration configuration)
        {
            var source = configFileSettings.Sources.FirstOrDefault(p => p.Id.is_equal_to(configuration.SourceCommand.Name));
            if (source != null && !source.Disabled)
            {
                source.Disabled = true;
                _xmlService.serialize(_configFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                this.Log().Info(() => "Disabled {0}".format_with(source.Id));
            }
        }

        public void source_enable(ChocolateyConfiguration configuration)
        {
            var source = configFileSettings.Sources.FirstOrDefault(p => p.Id.is_equal_to(configuration.SourceCommand.Name));
            if (source != null && source.Disabled)
            {
                source.Disabled = false;
                _xmlService.serialize(_configFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                this.Log().Info(() => "Enabled {0}".format_with(source.Id));
            }
        }

        public string get_api_key(ChocolateyConfiguration configuration, Action<ConfigFileApiKeySetting> keyAction)
        {
            string apiKeyValue = null;

            if (!string.IsNullOrWhiteSpace(configuration.Source))
            {
                var apiKey = configFileSettings.ApiKeys.FirstOrDefault(p => p.Source.is_equal_to(configuration.Source));
                if (apiKey != null)
                {
                    apiKeyValue = NugetEncryptionUtility.DecryptString(apiKey.Key).to_string();

                    if (keyAction != null)
                    {
                        keyAction.Invoke(new ConfigFileApiKeySetting{Key=apiKeyValue,Source=apiKey.Source});
                    }
                }
            }
            else
            {
                foreach (var apiKey in configFileSettings.ApiKeys.or_empty_list_if_null())
                {
                    var keyValue = NugetEncryptionUtility.DecryptString(apiKey.Key).to_string();
                    if (keyAction != null)
                    {
                        keyAction.Invoke(new ConfigFileApiKeySetting { Key = keyValue, Source = apiKey.Source });
                    }
                }
            }

            return apiKeyValue;
        }

        public void set_api_key(ChocolateyConfiguration configuration)
        {
            var apiKey = configFileSettings.ApiKeys.FirstOrDefault(p => p.Source.is_equal_to(configuration.Source));
            if (apiKey == null)
            {
                configFileSettings.ApiKeys.Add(new ConfigFileApiKeySetting()
                    {
                        Source = configuration.Source,
                        Key = NugetEncryptionUtility.EncryptString(configuration.ApiKeyCommand.Key),
                    });

                _xmlService.serialize(_configFileSettings, ApplicationParameters.GlobalConfigFileLocation);

                this.Log().Info(() => "Added ApiKey for {0}".format_with(configuration.Source));
            }
            else
            {
                if (!NugetEncryptionUtility.DecryptString(apiKey.Key).to_string().is_equal_to(configuration.ApiKeyCommand.Key))
                {
                    apiKey.Key = NugetEncryptionUtility.EncryptString(configuration.ApiKeyCommand.Key);
                    _xmlService.serialize(_configFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                    this.Log().Info(() => "Updated ApiKey for {0}".format_with(configuration.Source));
                }
            }
        }
    }
}