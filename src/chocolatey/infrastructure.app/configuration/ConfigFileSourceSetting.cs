namespace chocolatey.infrastructure.app.configuration
{
    using System;
    using System.Xml.Serialization;

    /// <summary>
    ///   XML config file sources element
    /// </summary>
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

        [XmlAttribute(AttributeName = "user")]
        public string UserName { get; set; }

        [XmlAttribute(AttributeName = "password")]
        public string Password { get; set; }
    }
}