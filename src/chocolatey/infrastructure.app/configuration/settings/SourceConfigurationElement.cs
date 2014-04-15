namespace chocolatey.infrastructure.app.configuration.settings
{
    using System.Configuration;

    public sealed class SourceConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("id", IsRequired = true)]
        public string id
        {
            get { return (string)this["id"]; }
        }

        [ConfigurationProperty("value", IsRequired = true)]
        public string value
        {
            get { return (string)this["value"]; }
        }

        [ConfigurationProperty("enabled", IsRequired = false, DefaultValue = true)]
        public bool enabled
        {
            get { return (bool)this["enabled"]; }
        }

    }
}