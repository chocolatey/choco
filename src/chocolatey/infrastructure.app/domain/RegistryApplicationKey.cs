﻿// Copyright © 2017 - 2025 Chocolatey Software, Inc
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
using System.Xml.Serialization;
using Microsoft.Win32;
using chocolatey.infrastructure.xml;

namespace chocolatey.infrastructure.app.domain
{
    [Serializable]
    [XmlType("key")]
    public class RegistryApplicationKey : IEquatable<RegistryApplicationKey>
    {
        public RegistryView RegistryView { get; set; }

        public string KeyPath { get; set; }

        [XmlAttribute(AttributeName = "installerType")]
        public InstallerType InstallerType { get; set; }

        public string DefaultValue { get; set; }

        [XmlAttribute(AttributeName = "displayName")]
        public string DisplayName { get; set; }

        public XmlCData InstallLocation { get; set; }
        public XmlCData UninstallString { get; set; }
        public bool HasQuietUninstall { get; set; }

        // informational
        public XmlCData Publisher { get; set; }
        public string InstallDate { get; set; }
        public XmlCData InstallSource { get; set; }
        public string Language { get; set; } //uint

        // version stuff
        [XmlAttribute(AttributeName = "displayVersion")]
        public string DisplayVersion { get; set; }

        public string Version { get; set; } //uint
        public string VersionMajor { get; set; } //uint
        public string VersionMinor { get; set; } //uint
        public string VersionRevision { get; set; } //uint
        public string VersionBuild { get; set; } //uint

        // install information
        public bool SystemComponent { get; set; }
        public bool WindowsInstaller { get; set; }
        public bool NoRemove { get; set; }
        public bool NoModify { get; set; }
        public bool NoRepair { get; set; }
        public string ReleaseType { get; set; } //hotfix, securityupdate, update rollup, servicepack
        public string ParentKeyName { get; set; }
        public XmlCData LocalPackage { get; set; }

        /// <summary>
        ///   Is an application listed in ARP (Programs and Features)?
        /// </summary>
        /// <returns>true if the key should be listed as a program</returns>
        /// <remarks>
        ///   http://community.spiceworks.com/how_to/show/2238-how-add-remove-programs-works
        /// </remarks>
        public bool IsInProgramsAndFeatures()
        {
            return !string.IsNullOrWhiteSpace(DisplayName)
                   && !string.IsNullOrWhiteSpace(UninstallString.ToStringSafe())
                   && InstallerType != InstallerType.HotfixOrSecurityUpdate
                   && InstallerType != InstallerType.ServicePack
                   && string.IsNullOrWhiteSpace(ParentKeyName)
                   && !NoRemove
                   && !SystemComponent
                ;
        }

        public override string ToString()
        {
            return "{0}|{1}|{2}|{3}|{4}".FormatWith(
                DisplayName,
                DisplayVersion,
                InstallerType,
                UninstallString,
                KeyPath
            );
        }

        public override int GetHashCode()
        {
            return DisplayName.GetHashCode()
                   & DisplayVersion.GetHashCode()
                   & UninstallString.ToStringSafe().GetHashCode()
                   & KeyPath.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as RegistryApplicationKey);
        }

        bool IEquatable<RegistryApplicationKey>.Equals(RegistryApplicationKey other)
        {
            if (other is null)
            {
                return false;
            }

            return DisplayName.ToStringSafe().IsEqualTo(other.DisplayName)
                   && DisplayVersion.IsEqualTo(other.DisplayVersion)
                   && UninstallString.ToStringSafe().IsEqualTo(other.UninstallString.ToStringSafe())
                   && KeyPath.IsEqualTo(other.KeyPath)
                ;
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public bool is_in_programs_and_features()
            => IsInProgramsAndFeatures();
#pragma warning restore IDE0022, IDE1006
    }
}
