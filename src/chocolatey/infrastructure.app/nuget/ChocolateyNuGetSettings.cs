// Copyright © 2017 - 2025 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using chocolatey.infrastructure.app.configuration;
using NuGet.Configuration;
using NuGet.Packaging.Signing;

namespace chocolatey.infrastructure.app.nuget
{
    public class ChocolateyNuGetSettings : ISettings
    {
        //private ClientPolicyContext
        private const string ConfigSectionName = "config";
        //private SettingSection _configSettingSection;

#pragma warning disable IDE0060 // unused method parameter
        public ChocolateyNuGetSettings(ChocolateyConfiguration config)
#pragma warning restore IDE0060 // unused method parameter
        {
            //new SettingSection
            //_clientCertItem =
        }

        public event EventHandler SettingsChanged = delegate { };

        public void AddOrUpdate(string sectionName, SettingItem item)
        {
            this.Log().Warn("NuGet tried to add an item to section {0}".FormatWith(sectionName));
        }

        public IList<string> GetConfigFilePaths()
        {
            return Enumerable.Empty<string>().ToList();
        }

        public IList<string> GetConfigRoots()
        {
            return Enumerable.Empty<string>().ToList();
        }

        public SettingSection GetSection(string sectionName)
        {
            switch (sectionName)
            {
                case ConfigSectionName:
                    //TODO fix
                    return null;
                //break;
                default:
                    return null;
            }
        }

        public void Remove(string sectionName, SettingItem item)
        {
            this.Log().Warn("NuGet tried to remove an item to section {0}".FormatWith(sectionName));
        }

        public void SaveToDisk()
        {
            this.Log().Warn("NuGet tried to save settings to disk");
        }
    }
}
