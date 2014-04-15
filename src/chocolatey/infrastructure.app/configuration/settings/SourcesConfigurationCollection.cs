namespace chocolatey.infrastructure.app.configuration.settings
{
    using System.Configuration;

    public sealed class SourcesConfigurationCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new SourceConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return element;
        }

        public SourceConfigurationElement Item(int index)
        {
            return (SourceConfigurationElement)(base.BaseGet(index));
        }
    }
}