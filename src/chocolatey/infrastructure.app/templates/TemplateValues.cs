// Copyright © 2017 - 2018 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.app.templates
{
    using System;
    using System.Collections.Generic;

    public class TemplateValues
    {
        public TemplateValues()
        {
            set_normal();
            AdditionalProperties = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        }

        public void set_normal()
        {
            PackageName = "__NAME_REPLACE__";
            PackageVersion = "__REPLACE__";
            MaintainerName = "__REPLACE_YOUR_NAME__";
            MaintainerRepo = "__REPLACE_YOUR_REPO__";
            AutomaticPackageNotesInstaller = "";
            InstallerType = "EXE_MSI_OR_MSU";
            Url = "";
            Url64 = "";
            SilentArgs = @"/qn /norestart /l*v `""$($env:TEMP)\$($packageName).$($env:chocolateyPackageVersion).MsiInstall.log`""";
            AutomaticPackageNotesNuspec = "";
            Checksum = "";
            ChecksumType = "sha256";
            Checksum64 = "";
            ChecksumType64 = "sha256";
        }

        public void set_auto()
        {
            PackageName = "{{PackageName}}";
            PackageVersion = "{{PackageVersion}}";
            AutomaticPackageNotesInstaller = ChocolateyInstallTemplate.AutomaticPackageNotes;
            AutomaticPackageNotesNuspec = NuspecTemplate.AutomaticPackageNotes;
            Url = "{{DownloadUrl}}";
            Url64 = "{{DownloadUrlx64}}";
            Checksum = "{{Checksum}}";
            Checksum64 = "{{Checksumx64}}";
            ChecksumType = "{{ChecksumType}}";
            ChecksumType64 = "{{ChecksumTypex64}}";
        }

        public string PackageName { get; set; }

        public string PackageNameLower
        {
            get { return PackageName.to_lower(); }
        }

        public string PackageVersion { get; set; }
        public string MaintainerName { get; set; }
        public string MaintainerRepo { get; set; }
        public string AutomaticPackageNotesInstaller { get; set; }
        public string AutomaticPackageNotesNuspec { get; set; }
        public string InstallerType { get; set; }
        public string Url { get; set; }
        public string Url64 { get; set; }
        public string SilentArgs { get; set; }
        public string Checksum { get; set; }
        public string ChecksumType { get; set; }
        public string Checksum64 { get; set; }
        public string ChecksumType64 { get; set; }
        public IDictionary<string, string> AdditionalProperties { get; private set; }

        public static readonly string NamePropertyName = "PackageName";
        public static readonly string VersionPropertyName = "PackageVersion";
        public static readonly string MaintainerPropertyName = "MaintainerName";
    }
}
