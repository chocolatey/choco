// Copyright © 2011 - Present RealDimensions Software, LLC
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

namespace chocolatey.infrastructure.app.configuration
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    ///   XML configuration file
    /// </summary>
    [Serializable]
    [XmlRoot("chocolatey")]
    public class ConfigFileSettings
    {
        [XmlElement(ElementName = "checksumFiles")]
        public bool ChecksumFiles { get; set; }

        [XmlElement(ElementName = "virusCheckFiles")]
        public bool VirusCheckFiles { get; set; }

        [XmlElement(ElementName = "cacheLocation")]
        public string CacheLocation { get; set; }

        [XmlElement(ElementName = "containsLegacyPackageInstalls")]
        public bool ContainsLegacyPackageInstalls { get; set; }

        [XmlElement(ElementName = "commandExecutionTimeoutSeconds")]
        public int CommandExecutionTimeoutSeconds { get; set; }

        [XmlArray("sources")]
        public HashSet<ConfigFileSourceSetting> Sources { get; set; }

        [XmlArray("apiKeys")]
        public HashSet<ConfigFileApiKeySetting> ApiKeys { get; set; }
    }
}