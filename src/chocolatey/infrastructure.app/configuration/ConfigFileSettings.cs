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

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace chocolatey.infrastructure.app.configuration
{
    /// <summary>
    ///   XML configuration file
    /// </summary>
    [Serializable]
    [XmlRoot("chocolatey")]
    public class ConfigFileSettings
    {
        [XmlArray("config")]
        public HashSet<ConfigFileConfigSetting> ConfigSettings { get; set; }

        [XmlArray("sources")]
        public HashSet<ConfigFileSourceSetting> Sources { get; set; }

        [XmlArray("features")]
        public HashSet<ConfigFileFeatureSetting> Features { get; set; }

        [XmlArray("apiKeys")]
        public HashSet<ConfigFileApiKeySetting> ApiKeys { get; set; }

        public override bool Equals(object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var item = (ConfigFileSettings)obj;

            return (ConfigSettings == item.ConfigSettings)
                && (Sources == item.Sources)
                && (Features == item.Features)
                && (ApiKeys == item.ApiKeys);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();

            if (ConfigSettings != null)
            {
                foreach (var item in ConfigSettings)
                {
                    hash.Add(item);
                }
            }

            if (Sources != null)
            {
                foreach (var item in Sources)
                {
                    hash.Add(item);
                }
            }

            if (Features != null)
            {
                foreach (var item in Features)
                {
                    hash.Add(item);
                }
            }

            if (ApiKeys != null)
            {
                foreach (var item in ApiKeys)
                {
                    hash.Add(item);
                }
            }

            return hash.ToHashCode();

        }
    }
}
