namespace chocolatey.infrastructure.app.configuration
{
    using System;
    using System.Xml.Serialization;

    /// <summary>
    ///   XML config file api keys element
    /// </summary>
    [Serializable]
    [XmlType("apiKeys")]
    public sealed class ConfigFileApiKeySetting
    {
        [XmlAttribute(AttributeName = "source")]
        public string Source { get; set; }

        [XmlAttribute(AttributeName = "key")]
        public string Key { get; set; }
    }
}