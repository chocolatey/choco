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

namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using configuration;
    using infrastructure.services;
    using logging;
    using nuget;

    public class ChocolateyConfigSettingsService : IChocolateyConfigSettingsService
    {
        private readonly Lazy<ConfigFileSettings> _configFileSettings;
        private readonly IXmlService _xmlService;
        private const string NO_CHANGE_MESSAGE = "Nothing to change. Config already set.";

        private ConfigFileSettings configFileSettings
        {
            get { return _configFileSettings.Value; }
        }

        public ChocolateyConfigSettingsService(IXmlService xmlService)
        {
            _xmlService = xmlService;
            _configFileSettings = new Lazy<ConfigFileSettings>(() => _xmlService.deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation));
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            this.Log().Info("Would have made a change to the configuration.");
        }

        public virtual bool skip_source(ConfigFileSourceSetting source, ChocolateyConfiguration configuration)
        {
            return false;
        }

        public virtual IEnumerable<ChocolateySource> source_list(ChocolateyConfiguration configuration)
        {
            var list = new List<ChocolateySource>();
            foreach (var source in configFileSettings.Sources)
            {
                if (skip_source(source, configuration)) continue;

                if (!configuration.QuietOutput) {
                    if (configuration.RegularOutput)
                    {
                        this.Log().Info(() => "{0}{1} - {2} {3}| Priority {4}|Bypass Proxy - {5}|Self-Service - {6}|Admin Only - {7}.".format_with(
                        source.Id,
                        source.Disabled ? " [Disabled]" : string.Empty,
                        source.Value,
                        (string.IsNullOrWhiteSpace(source.UserName) && string.IsNullOrWhiteSpace(source.Certificate)) ? string.Empty : "(Authenticated)",
                        source.Priority,
                        source.BypassProxy.to_string(),
                        source.AllowSelfService.to_string(),
                        source.VisibleToAdminsOnly.to_string()
                        ));
                    }
                    else
                    {
                        this.Log().Info(() => "{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}".format_with(
                        source.Id.quote_if_pipe_found(),
                        source.Value,
                        source.Disabled.to_string(),
                        source.UserName.quote_if_pipe_found(),
                        source.Certificate,
                        source.Priority,
                        source.BypassProxy.to_string(),
                        source.AllowSelfService.to_string(),
                        source.VisibleToAdminsOnly.to_string()
                        ));
                    }
                }
                list.Add(new ChocolateySource {
                    Id = source.Id,
                    Value = source.Value,
                    Disabled = source.Disabled,
                    Authenticated = !(string.IsNullOrWhiteSpace(source.UserName) && string.IsNullOrWhiteSpace(source.Certificate)),
                    Priority = source.Priority,
                    BypassProxy = source.BypassProxy,
                    AllowSelfService = source.AllowSelfService,
                    VisibleToAdminOnly = source.VisibleToAdminsOnly
                });
            }
            return list;
        }

        public void source_add(ChocolateyConfiguration configuration)
        {
            var source = configFileSettings.Sources.FirstOrDefault(p => p.Id.is_equal_to(configuration.SourceCommand.Name));
            if (source == null)
            {
                source = new ConfigFileSourceSetting
                {
                    Id = configuration.SourceCommand.Name,
                    Value = configuration.Sources,
                    UserName = configuration.SourceCommand.Username,
                    Password = NugetEncryptionUtility.EncryptString(configuration.SourceCommand.Password),
                    Certificate = configuration.SourceCommand.Certificate,
                    CertificatePassword = NugetEncryptionUtility.EncryptString(configuration.SourceCommand.CertificatePassword),
                    Priority = configuration.SourceCommand.Priority,
                    BypassProxy = configuration.SourceCommand.BypassProxy,
                    AllowSelfService = configuration.SourceCommand.AllowSelfService,
                    VisibleToAdminsOnly = configuration.SourceCommand.VisibleToAdminsOnly
                };
                configFileSettings.Sources.Add(source);

                _xmlService.serialize(configFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                if (!configuration.QuietOutput) this.Log().Warn(() => "Added {0} - {1} (Priority {2})".format_with(source.Id, source.Value, source.Priority));
            }
            else
            {
                var currentPassword = string.IsNullOrWhiteSpace(source.Password) ? null : NugetEncryptionUtility.DecryptString(source.Password);
                var currentCertificatePassword = string.IsNullOrWhiteSpace(source.CertificatePassword) ? null : NugetEncryptionUtility.DecryptString(source.CertificatePassword);
                if (configuration.Sources.is_equal_to(source.Value) &&
                    configuration.SourceCommand.Priority == source.Priority &&
                    configuration.SourceCommand.Username.is_equal_to(source.UserName) &&
                    configuration.SourceCommand.Password.is_equal_to(currentPassword) &&
                    configuration.SourceCommand.CertificatePassword.is_equal_to(currentCertificatePassword) &&
                    configuration.SourceCommand.Certificate.is_equal_to(source.Certificate) &&
                    configuration.SourceCommand.BypassProxy == source.BypassProxy && 
                    configuration.SourceCommand.AllowSelfService == source.AllowSelfService &&
                    configuration.SourceCommand.VisibleToAdminsOnly == source.VisibleToAdminsOnly 
                    )
                {
                    if (!configuration.QuietOutput) this.Log().Warn(NO_CHANGE_MESSAGE);
                }
                else
                {
                    source.Value = configuration.Sources;
                    source.Priority = configuration.SourceCommand.Priority;
                    source.UserName = configuration.SourceCommand.Username;
                    source.Password = NugetEncryptionUtility.EncryptString(configuration.SourceCommand.Password);
                    source.CertificatePassword = NugetEncryptionUtility.EncryptString(configuration.SourceCommand.CertificatePassword);
                    source.Certificate = configuration.SourceCommand.Certificate;
                    source.BypassProxy = configuration.SourceCommand.BypassProxy;
                    source.AllowSelfService = configuration.SourceCommand.AllowSelfService;
                    source.VisibleToAdminsOnly = configuration.SourceCommand.VisibleToAdminsOnly;

                    _xmlService.serialize(configFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                    if (!configuration.QuietOutput) this.Log().Warn(() => "Updated {0} - {1} (Priority {2})".format_with(source.Id, source.Value, source.Priority));
                }
            }
        }

        public void source_remove(ChocolateyConfiguration configuration)
        {
            var source = configFileSettings.Sources.FirstOrDefault(p => p.Id.is_equal_to(configuration.SourceCommand.Name));
            if (source != null)
            {
                configFileSettings.Sources.Remove(source);
                _xmlService.serialize(configFileSettings, ApplicationParameters.GlobalConfigFileLocation);

                if (!configuration.QuietOutput) this.Log().Warn(() => "Removed {0}".format_with(source.Id));
            }
            else
            {
                if (!configuration.QuietOutput) this.Log().Warn(NO_CHANGE_MESSAGE);
            }
        }

        public void source_disable(ChocolateyConfiguration configuration)
        {
            var source = configFileSettings.Sources.FirstOrDefault(p => p.Id.is_equal_to(configuration.SourceCommand.Name));
            if (source != null && !source.Disabled)
            {
                source.Disabled = true;
                _xmlService.serialize(configFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                if (!configuration.QuietOutput) this.Log().Warn(() => "Disabled {0}".format_with(source.Id));
            }
            else
            {
                if (!configuration.QuietOutput) this.Log().Warn(NO_CHANGE_MESSAGE);
            }
        }

        public void source_enable(ChocolateyConfiguration configuration)
        {
            var source = configFileSettings.Sources.FirstOrDefault(p => p.Id.is_equal_to(configuration.SourceCommand.Name));
            if (source != null && source.Disabled)
            {
                source.Disabled = false;
                _xmlService.serialize(configFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                if (!configuration.QuietOutput) this.Log().Warn(() => "Enabled {0}".format_with(source.Id));
            }
            else
            {
                if (!configuration.QuietOutput) this.Log().Warn(NO_CHANGE_MESSAGE);
            }
        }

        public void feature_list(ChocolateyConfiguration configuration)
        {
            foreach (var feature in configFileSettings.Features)
            {
                if (configuration.RegularOutput)
                {
                    this.Log().Info(() => "{0} {1} - {2}".format_with(feature.Enabled ? "[x]" : "[ ]", feature.Name, feature.Description));
                }
                else
                {
                    this.Log().Info(() => "{0}|{1}|{2}".format_with(feature.Name, !feature.Enabled ? "Disabled" : "Enabled", feature.Description));
                }
            }
        }

        public void feature_disable(ChocolateyConfiguration configuration)
        {
            var feature = configFileSettings.Features.FirstOrDefault(p => p.Name.is_equal_to(configuration.FeatureCommand.Name));
            if (feature == null)
            {
                throw new ApplicationException("Feature '{0}' not found".format_with(configuration.FeatureCommand.Name));
            }

            if (feature.Enabled || !feature.SetExplicitly)
            {
                if (!feature.Enabled && !feature.SetExplicitly)
                {
                    this.Log().Info(() => "{0} was disabled by default. Explicitly setting value.".format_with(feature.Name));
                }
                feature.Enabled = false;
                feature.SetExplicitly = true;
                _xmlService.serialize(configFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                this.Log().Warn(() => "Disabled {0}".format_with(feature.Name));
            }
            else
            {
                this.Log().Warn(NO_CHANGE_MESSAGE);
            }
        }

        public void feature_enable(ChocolateyConfiguration configuration)
        {
            var feature = configFileSettings.Features.FirstOrDefault(p => p.Name.is_equal_to(configuration.FeatureCommand.Name));

            if (feature == null)
            {
                throw new ApplicationException("Feature '{0}' not found".format_with(configuration.FeatureCommand.Name));
            }

            if (!feature.Enabled || !feature.SetExplicitly)
            {
                if (feature.Enabled && !feature.SetExplicitly)
                {
                    this.Log().Info(() => "{0} was enabled by default. Explicitly setting value.".format_with(feature.Name));
                }
                feature.Enabled = true;
                feature.SetExplicitly = true;
                _xmlService.serialize(configFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                this.Log().Warn(() => "Enabled {0}".format_with(feature.Name));
            }
            else
            {
                this.Log().Warn(NO_CHANGE_MESSAGE);
            }
        }

        public string get_api_key(ChocolateyConfiguration configuration, Action<ConfigFileApiKeySetting> keyAction)
        {
            string apiKeyValue = null;

            if (!string.IsNullOrWhiteSpace(configuration.Sources))
            {
                var apiKey = configFileSettings.ApiKeys.FirstOrDefault(p => p.Source.TrimEnd('/').is_equal_to(configuration.Sources.TrimEnd('/')));
                if (apiKey != null)
                {
                    apiKeyValue = NugetEncryptionUtility.DecryptString(apiKey.Key).to_string();

                    if (keyAction != null)
                    {
                        keyAction.Invoke(new ConfigFileApiKeySetting {Key = apiKeyValue, Source = apiKey.Source});
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
                        keyAction.Invoke(new ConfigFileApiKeySetting {Key = keyValue, Source = apiKey.Source});
                    }
                }
            }

            return apiKeyValue;
        }

        public void set_api_key(ChocolateyConfiguration configuration)
        {
            var apiKey = configFileSettings.ApiKeys.FirstOrDefault(p => p.Source.is_equal_to(configuration.Sources));
            if (apiKey == null)
            {
                configFileSettings.ApiKeys.Add(new ConfigFileApiKeySetting
                    {
                        Source = configuration.Sources,
                        Key = NugetEncryptionUtility.EncryptString(configuration.ApiKeyCommand.Key),
                    });

                _xmlService.serialize(configFileSettings, ApplicationParameters.GlobalConfigFileLocation);

                this.Log().Info(() => "Added ApiKey for {0}".format_with(configuration.Sources));
            }
            else
            {
                if (!NugetEncryptionUtility.DecryptString(apiKey.Key).to_string().is_equal_to(configuration.ApiKeyCommand.Key))
                {
                    apiKey.Key = NugetEncryptionUtility.EncryptString(configuration.ApiKeyCommand.Key);
                    _xmlService.serialize(configFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                    this.Log().Info(() => "Updated ApiKey for {0}".format_with(configuration.Sources));
                }
                else this.Log().Warn(NO_CHANGE_MESSAGE);
            }
        }

        public void remove_api_key(ChocolateyConfiguration configuration)
        {
            var apiKey = configFileSettings.ApiKeys.FirstOrDefault(p => p.Source.is_equal_to(configuration.Sources));
            if (apiKey != null)
            {
                configFileSettings.ApiKeys.RemoveWhere(x => x.Source.is_equal_to(configuration.Sources));

                _xmlService.serialize(configFileSettings, ApplicationParameters.GlobalConfigFileLocation);

                this.Log().Info(() => "Removed ApiKey for {0}".format_with(configuration.Sources));
            }
            else
            {
                this.Log().Info(() => "ApiKey was not found for {0}".format_with(configuration.Sources));
            }

        }

        public void config_list(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Settings");
            foreach (var config in configFileSettings.ConfigSettings)
            {
                this.Log().Info(() => "{0} = {1} | {2}".format_with(config.Key, config.Value, config.Description));
            }

            this.Log().Info("");
            this.Log().Info(ChocolateyLoggers.Important, "Sources");
            source_list(configuration);
            this.Log().Info("");
            this.Log().Info(@"NOTE: Use choco source to interact with sources.");
            this.Log().Info("");
            this.Log().Info(ChocolateyLoggers.Important, "Features");
            feature_list(configuration);
            this.Log().Info("");
            this.Log().Info(@"NOTE: Use choco feature to interact with features.");
            ;
            this.Log().Info("");
            this.Log().Info(ChocolateyLoggers.Important, "API Keys");
            this.Log().Info(@"NOTE: Api Keys are not shown through this command.
 Use choco apikey to interact with API keys.");
        }

        public void config_get(ChocolateyConfiguration configuration)
        {
            var config = config_get(configuration.ConfigCommand.Name);
            if (config == null) throw new ApplicationException("No configuration value by the name '{0}'".format_with(configuration.ConfigCommand.Name));
            this.Log().Info("{0}".format_with(config.Value));
        }

        public ConfigFileConfigSetting config_get(string configKeyName)
        {
            var config = configFileSettings.ConfigSettings.FirstOrDefault(p => p.Key.is_equal_to(configKeyName));
            if (config == null) return null;

            return config;
        }

        public void config_set(ChocolateyConfiguration configuration)
        {
            var encryptValue = configuration.ConfigCommand.Name.contains("password");
            var config = config_get(configuration.ConfigCommand.Name);
            var configValue = encryptValue
                                  ? NugetEncryptionUtility.EncryptString(configuration.ConfigCommand.ConfigValue)
                                  : configuration.ConfigCommand.ConfigValue;

            if (config == null)
            {
                var setting = new ConfigFileConfigSetting
                {
                    Key = configuration.ConfigCommand.Name,
                    Value = configValue,
                };

                configFileSettings.ConfigSettings.Add(setting);

                _xmlService.serialize(configFileSettings, ApplicationParameters.GlobalConfigFileLocation);

                this.Log().Warn(() => "Added {0} = {1}".format_with(setting.Key, setting.Value));
            }
            else
            {
                var currentValue = encryptValue && !string.IsNullOrWhiteSpace(config.Value)
                                       ? NugetEncryptionUtility.DecryptString(config.Value)
                                       : config.Value;

                if (configuration.ConfigCommand.ConfigValue.is_equal_to(currentValue.to_string()))
                {
                    this.Log().Warn(NO_CHANGE_MESSAGE);
                }
                else
                {
                    config.Value = configValue;
                    _xmlService.serialize(configFileSettings, ApplicationParameters.GlobalConfigFileLocation);

                    this.Log().Warn(() => "Updated {0} = {1}".format_with(config.Key, config.Value));
                }
            }
        }

        public void config_unset(ChocolateyConfiguration configuration)
        {
            var config = config_get(configuration.ConfigCommand.Name);
            if (config == null || string.IsNullOrEmpty(config.Value))
            {
                this.Log().Warn(NO_CHANGE_MESSAGE);
            }
            else
            {
                config.Value = "";
                _xmlService.serialize(configFileSettings, ApplicationParameters.GlobalConfigFileLocation);

                this.Log().Warn(() => "Unset {0}".format_with(config.Key));
            }
        }
    }
}
