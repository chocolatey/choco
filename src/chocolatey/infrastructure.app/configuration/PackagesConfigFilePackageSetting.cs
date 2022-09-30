// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

        [XmlAttribute(AttributeName = "pinPackage")]
        public bool PinPackage { get; set; }
        
        [System.ComponentModel.DefaultValue(-1)]
        [XmlAttribute(AttributeName = "executionTimeout")]
        public int ExecutionTimeout { get; set; }

        [XmlAttribute(AttributeName = "force")]
        public bool Force { get; set; }

        [XmlAttribute(AttributeName = "prerelease")]
        public bool Prerelease { get; set; }

        [XmlAttribute(AttributeName = "overrideArguments")]
        public bool OverrideArguments { get; set; }

        [XmlAttribute(AttributeName = "notSilent")]
        public bool NotSilent { get; set; }

        [XmlAttribute(AttributeName = "allowDowngrade")]
        public bool AllowDowngrade { get; set; }

        [XmlAttribute(AttributeName = "forceDependencies")]
        public bool ForceDependencies { get; set; }

        [XmlAttribute(AttributeName = "skipAutomationScripts")]
        public bool SkipAutomationScripts { get; set; }

        [XmlAttribute(AttributeName = "user")]
        public string User { get; set; }

        [XmlAttribute(AttributeName = "password")]
        public string Password { get; set; }

        [XmlAttribute(AttributeName = "cert")]
        public string Cert { get; set; }

        [XmlAttribute(AttributeName = "certPassword")]
        public string CertPassword { get; set; }

        [XmlAttribute(AttributeName = "ignoreChecksums")]
        public bool IgnoreChecksums { get; set; }

        [XmlAttribute(AttributeName = "allowEmptyChecksums")]
        public bool AllowEmptyChecksums { get; set; }

        [XmlAttribute(AttributeName = "allowEmptyChecksumsSecure")]
        public bool AllowEmptyChecksumsSecure { get; set; }

        [XmlAttribute(AttributeName = "requireChecksums")]
        public bool RequireChecksums { get; set; }

        [XmlAttribute(AttributeName = "downloadChecksum")]
        public string DownloadChecksum { get; set; }

        [XmlAttribute(AttributeName = "downloadChecksum64")]
        public string DownloadChecksum64 { get; set; }

        [XmlAttribute(AttributeName = "downloadChecksumType")]
        public string DownloadChecksumType { get; set; }

        [XmlAttribute(AttributeName = "downloadChecksumType64")]
        public string DownloadChecksumType64 { get; set; }

        [XmlAttribute(AttributeName = "ignorePackageExitCodes")]
        public bool IgnorePackageExitCodes { get; set; }

        [XmlAttribute(AttributeName = "usePackageExitCodes")]
        public bool UsePackageExitCodes { get; set; }

        [XmlAttribute(AttributeName = "stopOnFirstFailure")]
        public bool StopOnFirstFailure { get; set; }

        [XmlAttribute(AttributeName = "exitWhenRebootDetected")]
        public bool ExitWhenRebootDetected { get; set; }

        [XmlAttribute(AttributeName = "ignoreDetectedReboot")]
        public bool IgnoreDetectedReboot { get; set; }

        [XmlAttribute(AttributeName = "disableRepositoryOptimizations")]
        public bool DisableRepositoryOptimizations { get; set; }

        [XmlAttribute(AttributeName = "acceptLicense")]
        public bool AcceptLicense { get; set; }

        [XmlAttribute(AttributeName = "confirm")]
        public bool Confirm { get; set; }

        [XmlAttribute(AttributeName = "limitOutput")]
        public bool LimitOutput { get; set; }

        [XmlAttribute(AttributeName = "cacheLocation")]
        public string CacheLocation { get; set; }

        [XmlAttribute(AttributeName = "failOnStderr")]
        public bool FailOnStderr { get; set; }

        [XmlAttribute(AttributeName = "useSystemPowershell")]
        public bool UseSystemPowershell { get; set; }

        [XmlAttribute(AttributeName = "noProgress")]
        public bool NoProgress { get; set; }
    }
}
