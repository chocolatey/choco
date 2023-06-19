// Copyright © 2017 - 2021 Chocolatey Software, Inc
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
    using System.Reflection;
    using chocolatey.infrastructure.app.commands;
    using configuration;
    using infrastructure.services;
    using logging;
    using nuget;

    public class ChocolateyConfigSettingsService : IChocolateyConfigSettingsService
    {
        private readonly HashSet<string> _knownFeatures = new HashSet<string>();
        private readonly Lazy<ConfigFileSettings> _configFileSettings;
        private readonly IXmlService _xmlService;
        private const string NoChangeMessage = "Nothing to change. Config already set.";

        public ChocolateyConfigSettingsService()
        {
            AddKnownFeaturesFromStaticClass(typeof(ApplicationParameters.Features));
        }

        private ConfigFileSettings ConfigFileSettings
        {
            get { return _configFileSettings.Value; }
        }

        public ChocolateyConfigSettingsService(IXmlService xmlService)
            : this()
        {
            _xmlService = xmlService;
            _configFileSettings = new Lazy<ConfigFileSettings>(() => _xmlService.Deserialize<ConfigFileSettings>(ApplicationParameters.GlobalConfigFileLocation));
        }

        public void DryRun(ChocolateyConfiguration configuration)
        {
            this.Log().Info("Would have made a change to the configuration.");
        }

        public virtual bool SkipSource(ConfigFileSourceSetting source, ChocolateyConfiguration configuration)
        {
            return false;
        }

        public virtual IEnumerable<ChocolateySource> ListSources(ChocolateyConfiguration configuration)
        {
            var list = new List<ChocolateySource>();
            foreach (var source in ConfigFileSettings.Sources.OrEmpty().OrderBy(s => s.Id))
            {
                if (SkipSource(source, configuration)) continue;

                if (!configuration.QuietOutput)
                {
                    if (configuration.RegularOutput)
                    {
                        this.Log().Info(() => "{0}{1} - {2} {3}| Priority {4}|Bypass Proxy - {5}|Self-Service - {6}|Admin Only - {7}.".FormatWith(
                        source.Id,
                        source.Disabled ? " [Disabled]" : string.Empty,
                        source.Value,
                        (string.IsNullOrWhiteSpace(source.UserName) && string.IsNullOrWhiteSpace(source.Certificate)) ? string.Empty : "(Authenticated)",
                        source.Priority,
                        source.BypassProxy.ToStringSafe(),
                        source.AllowSelfService.ToStringSafe(),
                        source.VisibleToAdminsOnly.ToStringSafe()
                        ));
                    }
                    else
                    {
                        this.Log().Info(() => "{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}".FormatWith(
                        source.Id.QuoteIfContainsPipe(),
                        source.Value,
                        source.Disabled.ToStringSafe(),
                        source.UserName.QuoteIfContainsPipe(),
                        source.Certificate,
                        source.Priority,
                        source.BypassProxy.ToStringSafe(),
                        source.AllowSelfService.ToStringSafe(),
                        source.VisibleToAdminsOnly.ToStringSafe()
                        ));
                    }
                }
                list.Add(new ChocolateySource
                {
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

        public void AddSource(ChocolateyConfiguration configuration)
        {
            var source = ConfigFileSettings.Sources.FirstOrDefault(p => p.Id.IsEqualTo(configuration.SourceCommand.Name));
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
                ConfigFileSettings.Sources.Add(source);

                _xmlService.Serialize(ConfigFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                if (!configuration.QuietOutput) this.Log().Warn(() => "Added {0} - {1} (Priority {2})".FormatWith(source.Id, source.Value, source.Priority));
            }
            else
            {
                var currentPassword = string.IsNullOrWhiteSpace(source.Password) ? null : NugetEncryptionUtility.DecryptString(source.Password);
                var currentCertificatePassword = string.IsNullOrWhiteSpace(source.CertificatePassword) ? null : NugetEncryptionUtility.DecryptString(source.CertificatePassword);
                if (configuration.Sources.IsEqualTo(source.Value) &&
                    configuration.SourceCommand.Priority == source.Priority &&
                    configuration.SourceCommand.Username.IsEqualTo(source.UserName) &&
                    configuration.SourceCommand.Password.IsEqualTo(currentPassword) &&
                    configuration.SourceCommand.CertificatePassword.IsEqualTo(currentCertificatePassword) &&
                    configuration.SourceCommand.Certificate.IsEqualTo(source.Certificate) &&
                    configuration.SourceCommand.BypassProxy == source.BypassProxy &&
                    configuration.SourceCommand.AllowSelfService == source.AllowSelfService &&
                    configuration.SourceCommand.VisibleToAdminsOnly == source.VisibleToAdminsOnly
                    )
                {
                    if (!configuration.QuietOutput) this.Log().Warn(NoChangeMessage);
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

                    _xmlService.Serialize(ConfigFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                    if (!configuration.QuietOutput) this.Log().Warn(() => "Updated {0} - {1} (Priority {2})".FormatWith(source.Id, source.Value, source.Priority));
                }
            }
        }

        public void RemoveSource(ChocolateyConfiguration configuration)
        {
            var source = ConfigFileSettings.Sources.FirstOrDefault(p => p.Id.IsEqualTo(configuration.SourceCommand.Name));
            if (source != null)
            {
                ConfigFileSettings.Sources.Remove(source);
                _xmlService.Serialize(ConfigFileSettings, ApplicationParameters.GlobalConfigFileLocation);

                if (!configuration.QuietOutput) this.Log().Warn(() => "Removed {0}".FormatWith(source.Id));
            }
            else
            {
                if (!configuration.QuietOutput) this.Log().Warn(NoChangeMessage);
            }
        }

        public void DisableSource(ChocolateyConfiguration configuration)
        {
            var source = ConfigFileSettings.Sources.FirstOrDefault(p => p.Id.IsEqualTo(configuration.SourceCommand.Name));
            if (source != null && !source.Disabled)
            {
                source.Disabled = true;
                _xmlService.Serialize(ConfigFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                if (!configuration.QuietOutput) this.Log().Warn(() => "Disabled {0}".FormatWith(source.Id));
            }
            else
            {
                if (!configuration.QuietOutput) this.Log().Warn(NoChangeMessage);
            }
        }

        public void EnableSource(ChocolateyConfiguration configuration)
        {
            var source = ConfigFileSettings.Sources.FirstOrDefault(p => p.Id.IsEqualTo(configuration.SourceCommand.Name));
            if (source != null && source.Disabled)
            {
                source.Disabled = false;
                _xmlService.Serialize(ConfigFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                if (!configuration.QuietOutput) this.Log().Warn(() => "Enabled {0}".FormatWith(source.Id));
            }
            else
            {
                if (!configuration.QuietOutput) this.Log().Warn(NoChangeMessage);
            }
        }

        public void ListFeatures(ChocolateyConfiguration configuration)
        {
            foreach (var feature in ConfigFileSettings.Features.OrEmpty().OrderBy(f => f.Name))
            {
                if (configuration.RegularOutput)
                {
                    this.Log().Info(() => "{0} {1} - {2}".FormatWith(feature.Enabled ? "[x]" : "[ ]", feature.Name, feature.Description));
                }
                else
                {
                    this.Log().Info(() => "{0}|{1}|{2}".FormatWith(feature.Name, !feature.Enabled ? "Disabled" : "Enabled", feature.Description));
                }
            }
        }

        public void GetFeature(ChocolateyConfiguration configuration)
        {
            var feature = GetFeatureValue(configuration.FeatureCommand.Name);
            if (feature == null)
            {
                throw new ApplicationException("No feature value by the name '{0}'".FormatWith(configuration.FeatureCommand.Name));
            }

            this.Log().Info("{0}".FormatWith(feature.Enabled ? "Enabled" : "Disabled"));
        }

        public ConfigFileFeatureSetting GetFeatureValue(string featureName)
        {
            var feature = ConfigFileSettings.Features.FirstOrDefault(f => f.Name.IsEqualTo(featureName));
            if (feature == null)
            {
                return null;
            }

            return feature;
        }

        public void DisableFeature(ChocolateyConfiguration configuration)
        {
            var feature = ConfigFileSettings.Features.FirstOrDefault(p => p.Name.IsEqualTo(configuration.FeatureCommand.Name));
            if (feature == null)
            {
                throw new ApplicationException("Feature '{0}' not found".FormatWith(configuration.FeatureCommand.Name));
            }

            ValidateSupportedFeature(feature);

            if (feature.Enabled || !feature.SetExplicitly)
            {
                if (!feature.Enabled && !feature.SetExplicitly)
                {
                    this.Log().Info(() => "{0} was disabled by default. Explicitly setting value.".FormatWith(feature.Name));
                }
                feature.Enabled = false;
                feature.SetExplicitly = true;
                _xmlService.Serialize(ConfigFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                this.Log().Warn(() => "Disabled {0}".FormatWith(feature.Name));
            }
            else
            {
                this.Log().Warn(NoChangeMessage);
            }
        }

        public void EnableFeature(ChocolateyConfiguration configuration)
        {
            var feature = ConfigFileSettings.Features.FirstOrDefault(p => p.Name.IsEqualTo(configuration.FeatureCommand.Name));

            if (feature == null)
            {
                throw new ApplicationException("Feature '{0}' not found".FormatWith(configuration.FeatureCommand.Name));
            }

            ValidateSupportedFeature(feature);

            if (!feature.Enabled || !feature.SetExplicitly)
            {
                if (feature.Enabled && !feature.SetExplicitly)
                {
                    this.Log().Info(() => "{0} was enabled by default. Explicitly setting value.".FormatWith(feature.Name));
                }
                feature.Enabled = true;
                feature.SetExplicitly = true;
                _xmlService.Serialize(ConfigFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                this.Log().Warn(() => "Enabled {0}".FormatWith(feature.Name));
            }
            else
            {
                this.Log().Warn(NoChangeMessage);
            }
        }

        public string GetApiKey(ChocolateyConfiguration configuration, Action<ConfigFileApiKeySetting> keyAction)
        {
            string apiKeyValue = null;

            if (!string.IsNullOrWhiteSpace(configuration.Sources))
            {
                var apiKey = ConfigFileSettings.ApiKeys.FirstOrDefault(p => p.Source.TrimEnd('/').IsEqualTo(configuration.Sources.TrimEnd('/')));
                if (apiKey != null)
                {
                    apiKeyValue = NugetEncryptionUtility.DecryptString(apiKey.Key).ToStringSafe();

                    if (keyAction != null)
                    {
                        keyAction.Invoke(new ConfigFileApiKeySetting { Key = apiKeyValue, Source = apiKey.Source });
                    }
                }
            }
            else
            {
                foreach (var apiKey in ConfigFileSettings.ApiKeys.OrEmpty().OrderBy(a => a.Source))
                {
                    var keyValue = NugetEncryptionUtility.DecryptString(apiKey.Key).ToStringSafe();
                    if (keyAction != null)
                    {
                        keyAction.Invoke(new ConfigFileApiKeySetting { Key = keyValue, Source = apiKey.Source });
                    }
                }
            }

            return apiKeyValue;
        }

        public void SetApiKey(ChocolateyConfiguration configuration)
        {
            var apiKey = ConfigFileSettings.ApiKeys.FirstOrDefault(p => p.Source.IsEqualTo(configuration.Sources));
            if (apiKey == null)
            {
                ConfigFileSettings.ApiKeys.Add(new ConfigFileApiKeySetting
                {
                    Source = configuration.Sources,
                    Key = NugetEncryptionUtility.EncryptString(configuration.ApiKeyCommand.Key),
                });

                _xmlService.Serialize(ConfigFileSettings, ApplicationParameters.GlobalConfigFileLocation);

                this.Log().Info(() => "Added API key for {0}".FormatWith(configuration.Sources));
            }
            else
            {
                if (!NugetEncryptionUtility.DecryptString(apiKey.Key).ToStringSafe().IsEqualTo(configuration.ApiKeyCommand.Key))
                {
                    apiKey.Key = NugetEncryptionUtility.EncryptString(configuration.ApiKeyCommand.Key);
                    _xmlService.Serialize(ConfigFileSettings, ApplicationParameters.GlobalConfigFileLocation);
                    this.Log().Info(() => "Updated API key for {0}".FormatWith(configuration.Sources));
                }
                else this.Log().Warn(NoChangeMessage);
            }
        }

        public void RemoveApiKey(ChocolateyConfiguration configuration)
        {
            var apiKey = ConfigFileSettings.ApiKeys.FirstOrDefault(p => p.Source.IsEqualTo(configuration.Sources));
            if (apiKey != null)
            {
                ConfigFileSettings.ApiKeys.RemoveWhere(x => x.Source.IsEqualTo(configuration.Sources));

                _xmlService.Serialize(ConfigFileSettings, ApplicationParameters.GlobalConfigFileLocation);

                this.Log().Info(() => "Removed API key for {0}".FormatWith(configuration.Sources));
            }
            else
            {
                this.Log().Info(() => "API key was not found for {0}".FormatWith(configuration.Sources));
            }

        }

        public void ListConfig(ChocolateyConfiguration configuration)
        {
            foreach (var config in ConfigFileSettings.ConfigSettings.OrEmpty().OrderBy(c => c.Key))
            {
                if (configuration.RegularOutput)
                {
                    this.Log().Info(() => "{0} = {1} | {2}".FormatWith(config.Key, config.Value, config.Description));

                }
                else
                {
                    this.Log().Info(() => "{0}|{1}|{2}".FormatWith(config.Key, config.Value, config.Description));
                }
            }
        }

        public void GetConfig(ChocolateyConfiguration configuration)
        {
            var config = GetConfigValue(configuration.ConfigCommand.Name);
            if (config == null) throw new ApplicationException("No configuration value by the name '{0}'".FormatWith(configuration.ConfigCommand.Name));
            this.Log().Info("{0}".FormatWith(config.Value));
        }

        public ConfigFileConfigSetting GetConfigValue(string configKeyName)
        {
            var config = ConfigFileSettings.ConfigSettings.FirstOrDefault(p => p.Key.IsEqualTo(configKeyName));
            if (config == null) return null;

            return config;
        }

        public void SetConfig(ChocolateyConfiguration configuration)
        {
            var encryptValue = configuration.ConfigCommand.Name.ContainsSafe("password");
            var config = GetConfigValue(configuration.ConfigCommand.Name);
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

                ConfigFileSettings.ConfigSettings.Add(setting);

                _xmlService.Serialize(ConfigFileSettings, ApplicationParameters.GlobalConfigFileLocation);

                this.Log().Warn(() => "Added {0} = {1}".FormatWith(setting.Key, setting.Value));
            }
            else
            {
                var currentValue = encryptValue && !string.IsNullOrWhiteSpace(config.Value)
                                       ? NugetEncryptionUtility.DecryptString(config.Value)
                                       : config.Value;

                if (configuration.ConfigCommand.ConfigValue.IsEqualTo(currentValue.ToStringSafe()))
                {
                    this.Log().Warn(NoChangeMessage);
                }
                else
                {
                    config.Value = configValue;
                    _xmlService.Serialize(ConfigFileSettings, ApplicationParameters.GlobalConfigFileLocation);

                    this.Log().Warn(() => "Updated {0} = {1}".FormatWith(config.Key, config.Value));
                }
            }
        }

        public void UnsetConfig(ChocolateyConfiguration configuration)
        {
            var config = GetConfigValue(configuration.ConfigCommand.Name);
            if (config == null || string.IsNullOrEmpty(config.Value))
            {
                this.Log().Warn(NoChangeMessage);
            }
            else
            {
                config.Value = "";
                _xmlService.Serialize(ConfigFileSettings, ApplicationParameters.GlobalConfigFileLocation);

                this.Log().Warn(() => "Unset {0}".FormatWith(config.Key));
            }
        }

        protected void AddKnownFeaturesFromStaticClass(Type classType)
        {
            var fieldInfos = classType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.GetField);

            foreach (var fi in fieldInfos)
            {
                try
                {
                    var value = (string)fi.GetValue(null);
                    if (!string.IsNullOrEmpty(value))
                    {
                        AddKnownFeature(value);
                    }
                }
                catch
                {
                    typeof(ChocolateyConfigSettingsService).Log().Debug("Unable to get value for known feature name for variable '{0}'!".FormatWith(fi.Name));
                }
            }
        }

        protected void AddKnownFeature(string name)
        {
            if (!_knownFeatures.Contains(name.ToLowerSafe()))
            {
                _knownFeatures.Add(name.ToLowerSafe());
            }
        }

        protected void ValidateSupportedFeature(ConfigFileFeatureSetting feature)
        {
            if (!_knownFeatures.Contains(feature.Name.ToLowerSafe()))
            {
                throw new ApplicationException("Feature '{0}' is not supported.".FormatWith(feature.Name));
            }
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void noop(ChocolateyConfiguration configuration)
            => DryRun(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual bool skip_source(ConfigFileSourceSetting source, ChocolateyConfiguration configuration)
            => SkipSource(source, configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual IEnumerable<ChocolateySource> source_list(ChocolateyConfiguration configuration)
            => ListSources(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void source_add(ChocolateyConfiguration configuration)
            => AddSource(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void source_remove(ChocolateyConfiguration configuration)
            => RemoveSource(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void source_disable(ChocolateyConfiguration configuration)
            => DisableSource(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void source_enable(ChocolateyConfiguration configuration)
            => EnableSource(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void feature_list(ChocolateyConfiguration configuration)
            => ListFeatures(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void feature_disable(ChocolateyConfiguration configuration)
            => DisableFeature(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void feature_enable(ChocolateyConfiguration configuration)
            => EnableFeature(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public string get_api_key(ChocolateyConfiguration configuration, Action<ConfigFileApiKeySetting> keyAction)
            => GetApiKey(configuration, keyAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void set_api_key(ChocolateyConfiguration configuration)
            => SetApiKey(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void remove_api_key(ChocolateyConfiguration configuration)
            => RemoveApiKey(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void config_list(ChocolateyConfiguration configuration)
            => ListConfig(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void config_get(ChocolateyConfiguration configuration)
            => GetConfig(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public ConfigFileConfigSetting config_get(string configKeyName)
            => GetConfigValue(configKeyName);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void config_set(ChocolateyConfiguration configuration)
            => SetConfig(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void config_unset(ChocolateyConfiguration configuration)
            => UnsetConfig(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected void add_known_features_from_static_class(Type classType)
            => AddKnownFeaturesFromStaticClass(classType);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected void add_known_feature(string name)
            => AddKnownFeature(name);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected void validate_supported_feature(ConfigFileFeatureSetting feature)
            => ValidateSupportedFeature(feature);
#pragma warning disable IDE1006
    }
}
