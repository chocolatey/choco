namespace chocolatey.infrastructure.app.configuration
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [Serializable]
    [XmlRoot("chocolatey")]
    public class ConfigFileSettings
    {
        [XmlElement(ElementName = "useNugetForSources")]
        public bool UseNugetForSources { get; set; }

        [XmlElement(ElementName = "checksumFiles")]
        public bool ChecksumFiles { get; set; }

        [XmlElement(ElementName = "virusCheckFiles")]
        public bool VirusCheckFiles { get; set; }

        [XmlArray("sources")]
        public HashSet<ConfigFileSourceSetting> Sources { get; set; }
    }
}