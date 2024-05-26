﻿// Copyright © 2017 - 2021 Chocolatey Software, Inc
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
using System.Diagnostics;
using chocolatey.infrastructure.adapters;

namespace chocolatey.infrastructure.information
{
    public sealed class VersionInformation
    {
        public static string GetCurrentAssemblyVersion(IAssembly assembly = null)
        {
            string version = null;
            if (assembly == null)
            {
                assembly = Assembly.GetExecutingAssembly();
            }

            var location = assembly != null ? assembly.Location : string.Empty;

            if (!string.IsNullOrEmpty(location))
            {
                version = FileVersionInfo.GetVersionInfo(location).FileVersion;
            }

            if (string.IsNullOrEmpty(version))
            {
                var attributes = assembly.UnderlyingType.GetCustomAttributesData();
                foreach (var attribute in attributes)
                {
                    if (attribute.ToStringSafe().Contains("AssemblyFileVersion"))
                    {
                        version = attribute.ConstructorArguments[0].Value.ToStringSafe();
                        break;
                    }
                }
            }

            return version;
        }

        public static string GetCurrentInformationalVersion(IAssembly assembly = null)
        {
            string version = null;
            if (assembly == null)
            {
                assembly = Assembly.GetExecutingAssembly();
            }

            var location = assembly != null ? assembly.Location : string.Empty;

            if (!string.IsNullOrEmpty(location))
            {
                version = FileVersionInfo.GetVersionInfo(location).ProductVersion;
            }

            if (string.IsNullOrEmpty(version))
            {
                var attributes = assembly.UnderlyingType.GetCustomAttributesData();
                foreach (var attribute in attributes)
                {
                    if (attribute.ToStringSafe().Contains("AssemblyInformationalVersion"))
                    {
                        version = attribute.ConstructorArguments[0].Value.ToStringSafe();
                        break;
                    }
                }
            }

            return version;
        }

        public static string GetMinimumChocolateyVersion(IAssembly assembly = null)
        {
            if (assembly == null)
            {
                assembly = Assembly.GetExecutingAssembly();
            }

            var attributeData = assembly.UnderlyingType.GetCustomAttributesData();
            foreach (var attribute in attributeData)
            {
                if (attribute.ToStringSafe().Contains("MinimumChocolateyVersion"))
                {
                    return attribute.ConstructorArguments[0].Value.ToStringSafe();
                }
            }

            // It was in version 1.0.0 of Chocolatey where we started to worry about compatible versions
            // so it makes sense to start with this as the default value, when there isn't a custom
            // attribute on the assembly to say what the minimum required Chocolatey version is.
            return "1.0.0";
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static string get_current_assembly_version(IAssembly assembly = null)
            => GetCurrentAssemblyVersion(assembly = null);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static string get_current_informational_version(IAssembly assembly = null)
            => GetCurrentInformationalVersion(assembly);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static string get_minimum_chocolatey_version(IAssembly assembly = null)
            => GetMinimumChocolateyVersion(assembly);
#pragma warning restore IDE0022, IDE1006
    }
}
