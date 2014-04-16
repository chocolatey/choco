namespace chocolatey.infrastructure.app.configuration
{
    using System;
    using System.Xml.Serialization;

    [Serializable]
    [XmlType("source")]
    public sealed class ConfigFileSourceSetting
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }

        [XmlAttribute(AttributeName = "disabled")]
        public bool Disabled { get; set; }
    }
}