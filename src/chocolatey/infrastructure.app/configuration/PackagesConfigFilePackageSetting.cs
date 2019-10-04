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

namespace chocolatey.infrastructure.app.configuration
{
    using System;
    using System.Xml.Serialization;

    /// <summary>
    ///   XML packages.config file package element
    /// </summary>
    [Serializable]
    //[XmlType("package")]
    public sealed class PackagesConfigFilePackageSetting
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "source")]
        public string Source { get; set; }

        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; }

        [XmlAttribute(AttributeName = "installArguments")]
        public string InstallArguments { get; set; }

        [XmlAttribute(AttributeName = "packageParameters")]
        public string PackageParameters { get; set; }

        [XmlAttribute(AttributeName = "applyPackageParametersToDependencies")]
        public bool ApplyPackageParametersToDependencies { get; set; }

        [XmlAttribute(AttributeName = "applyInstallArgumentsToDependencies")]
        public bool ApplyInstallArgumentsToDependencies { get; set; }

        [XmlAttribute(AttributeName = "forceX86")]
        public bool ForceX86 { get; set; }

        [XmlAttribute(AttributeName = "allowMultipleVersions")]
        public bool AllowMultipleVersions { get; set; }

        [XmlAttribute(AttributeName = "ignoreDependencies")]
        public bool IgnoreDependencies { get; set; }

        [XmlAttribute(AttributeName = "disabled")]
        public bool Disabled { get; set; }
    }
}
