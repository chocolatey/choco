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

        [XmlElement(ElementName = "commandExecutionTimeoutSeconds")]
        public int CommandExecutionTimeoutSeconds { get; set; }

        [XmlArray("sources")]
        public HashSet<ConfigFileSourceSetting> Sources { get; set; }
        
        [XmlArray("apiKeys")]
        public HashSet<ConfigFileApiKeySetting> ApiKeys { get; set; }
    }
}