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
        [XmlAttribute(AttributeName = "key")]
        public string Key { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }
}