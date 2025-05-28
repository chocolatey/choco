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
using System.ComponentModel;
using System.Xml.Serialization;

namespace chocolatey.infrastructure.app.configuration
{
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

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool ApplyPackageParametersToDependenciesSpecified
        {
            get { return ApplyPackageParametersToDependencies; }
        }

        [XmlAttribute(AttributeName = "applyInstallArgumentsToDependencies")]
        public bool ApplyInstallArgumentsToDependencies { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool ApplyInstallArgumentsToDependenciesSpecified
        {
            get { return ApplyInstallArgumentsToDependencies; }
        }

        [XmlAttribute(AttributeName = "forceX86")]
        public bool ForceX86 { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool ForceX86Specified
        {
            get { return ForceX86; }
        }

        [XmlAttribute(AttributeName = "ignoreDependencies")]
        public bool IgnoreDependencies { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool IgnoreDependenciesSpecified
        {
            get { return IgnoreDependencies; }
        }

        [XmlAttribute(AttributeName = "disabled")]
        public bool Disabled { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool DisabledSpecified
        {
            get { return Disabled; }
        }

        [XmlAttribute(AttributeName = "pinPackage")]
        public bool PinPackage { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool PinPackageSpecified
        {
            get { return PinPackage; }
        }

        [System.ComponentModel.DefaultValue(-1)]
        [XmlAttribute(AttributeName = "executionTimeout")]
        public int ExecutionTimeout { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool ExecutionTimeoutSpecified
        {
            get { return ExecutionTimeout != 0; }
        }

        [XmlAttribute(AttributeName = "force")]
        public bool Force { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool ForceSpecified
        {
            get { return Force; }
        }

        [XmlAttribute(AttributeName = "prerelease")]
        public bool Prerelease { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool PrereleaseSpecified
        {
            get { return Prerelease; }
        }

        [XmlAttribute(AttributeName = "overrideArguments")]
        public bool OverrideArguments { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool OverrideArgumentsSpecified
        {
            get { return OverrideArguments; }
        }

        [XmlAttribute(AttributeName = "notSilent")]
        public bool NotSilent { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool NotSilentSpecified
        {
            get { return NotSilent; }
        }

        [XmlAttribute(AttributeName = "allowDowngrade")]
        public bool AllowDowngrade { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool AllowDowngradeSpecified
        {
            get { return AllowDowngrade; }
        }

        [XmlAttribute(AttributeName = "forceDependencies")]
        public bool ForceDependencies { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool ForceDependenciesSpecified
        {
            get { return ForceDependencies; }
        }

        [XmlAttribute(AttributeName = "skipAutomationScripts")]
        public bool SkipAutomationScripts { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool SkipAutomationScriptsSpecified
        {
            get { return SkipAutomationScripts; }
        }

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

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool IgnoreChecksumsSpecified
        {
            get { return IgnoreChecksums; }
        }

        [XmlAttribute(AttributeName = "allowEmptyChecksums")]
        public bool AllowEmptyChecksums { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool AllowEmptyChecksumsSpecified
        {
            get { return AllowEmptyChecksums; }
        }

        [XmlAttribute(AttributeName = "allowEmptyChecksumsSecure")]
        public bool AllowEmptyChecksumsSecure { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool AllowEmptyChecksumsSecureSpecified
        {
            get { return AllowEmptyChecksumsSecure; }
        }

        [XmlAttribute(AttributeName = "requireChecksums")]
        public bool RequireChecksums { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool RequireChecksumsSpecified
        {
            get { return RequireChecksums; }
        }

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

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool IgnorePackageExitCodesSpecified
        {
            get { return IgnorePackageExitCodes; }
        }

        [XmlAttribute(AttributeName = "usePackageExitCodes")]
        public bool UsePackageExitCodes { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool UsePackageExitCodesSpecified
        {
            get { return UsePackageExitCodes; }
        }

        [XmlAttribute(AttributeName = "stopOnFirstFailure")]
        public bool StopOnFirstFailure { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool StopOnFirstFailureSpecified
        {
            get { return StopOnFirstFailure; }
        }

        [XmlAttribute(AttributeName = "exitWhenRebootDetected")]
        public bool ExitWhenRebootDetected { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool ExitWhenRebootDetectedSpecified
        {
            get { return ExitWhenRebootDetected; }
        }

        [XmlAttribute(AttributeName = "ignoreDetectedReboot")]
        public bool IgnoreDetectedReboot { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool IgnoreDetectedRebootSpecified
        {
            get { return IgnoreDetectedReboot; }
        }

        [XmlAttribute(AttributeName = "disableRepositoryOptimizations")]
        public bool DisableRepositoryOptimizations { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool DisableRepositoryOptimizationsSpecified
        {
            get { return DisableRepositoryOptimizations; }
        }

        [XmlAttribute(AttributeName = "acceptLicense")]
        public bool AcceptLicense { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool AcceptLicenseSpecified
        {
            get { return AcceptLicense; }
        }

        [XmlAttribute(AttributeName = "confirm")]
        public bool Confirm { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool ConfirmSpecified
        {
            get { return Confirm; }
        }

        [XmlAttribute(AttributeName = "limitOutput")]
        public bool LimitOutput { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool LimitOutputSpecified
        {
            get { return LimitOutput; }
        }

        [XmlAttribute(AttributeName = "cacheLocation")]
        public string CacheLocation { get; set; }

        [XmlAttribute(AttributeName = "failOnStderr")]
        public bool FailOnStderr { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool FailOnStderrSpecified
        {
            get { return FailOnStderr; }
        }

        [XmlAttribute(AttributeName = "useSystemPowershell")]
        public bool UseSystemPowershell { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool UseSystemPowershellSpecified
        {
            get { return UseSystemPowershell; }
        }

        [XmlAttribute(AttributeName = "noProgress")]
        public bool NoProgress { get; set; }

        [XmlIgnore, EditorBrowsable(EditorBrowsableState.Never)]
        public bool NoProgressSpecified
        {
            get { return NoProgress; }
        }
    }
}
