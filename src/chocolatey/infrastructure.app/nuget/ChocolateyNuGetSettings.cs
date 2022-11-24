using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chocolatey.infrastructure.app.nuget
{
    using configuration;
    using NuGet.Configuration;
    using NuGet.Packaging.Signing;

    public class ChocolateyNuGetSettings : ISettings
    {
        //private ClientPolicyContext
        private const string CONFIG_SECTION_NAME = "config";
        private SettingSection _configSettingSection;

        public ChocolateyNuGetSettings(ChocolateyConfiguration config)
        {
            //new SettingSection
            //_clientCertItem =
        }

        public event EventHandler SettingsChanged = delegate { };

        public void AddOrUpdate(string sectionName, SettingItem item)
        {
            this.Log().Warn("NuGet tried to add an item to section {0}".format_with(sectionName));
        }

        public IList<string> GetConfigFilePaths() => Enumerable.Empty<string>().ToList();

        public IList<string> GetConfigRoots() => Enumerable.Empty<string>().ToList();

        public SettingSection GetSection(string sectionName)
        {
            switch (sectionName)
            {
                case CONFIG_SECTION_NAME:
                    //TODO fix
                    return null;
                    break;
                default:
                    return null;
            }
        }

        public void Remove(string sectionName, SettingItem item)
        {
            this.Log().Warn("NuGet tried to remove an item to section {0}".format_with(sectionName));
        }

        public void SaveToDisk()
        {
            this.Log().Warn("NuGet tried to save settings to disk");
        }
    }
}
