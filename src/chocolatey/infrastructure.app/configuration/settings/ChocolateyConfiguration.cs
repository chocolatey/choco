namespace chocolatey.infrastructure.app.configuration.settings
{
    using System.Configuration;

    public sealed class ChocolateyConfiguration : ConfigurationSection
    {
        static readonly ChocolateyConfiguration _settings =
           ConfigurationManager.GetSection("chocolatey") as ChocolateyConfiguration;

        public static ChocolateyConfiguration settings
        {
            get { return _settings; }
        }

        [ConfigurationProperty("sources", IsRequired = true, IsDefaultCollection = true)]
        public SourcesConfigurationCollection sources
        {
            get { return (SourcesConfigurationCollection)this["sources"]; }
        }

        [ConfigurationProperty("useNuGetForSources", IsRequired = false, DefaultValue = false)]
        public bool useNuGetForSources
        {
            get { return (bool)this["useNuGetForSources"]; }
        }  
        
        [ConfigurationProperty("checksumFiles", IsRequired = false, DefaultValue = true)]
        public bool checksumFiles
        {
            get { return (bool)this["checksumFiles"]; }
        }
        
        [ConfigurationProperty("virusCheck", IsRequired = false, DefaultValue = false)]
        public bool virusCheck
        {
            get { return (bool)this["virusCheck"]; }
        }
    }
}