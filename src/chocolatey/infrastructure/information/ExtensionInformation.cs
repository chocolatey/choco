// Copyright © 2017 - 2022 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.information
{
    using System.Collections.Generic;
    using chocolatey.infrastructure.adapters;

    public class ExtensionInformation
    {
        public ExtensionInformation(IAssembly assembly)
        {
            Name = assembly.GetName().Name;
            Version = VersionInformation.get_current_informational_version(assembly);
            Status = ExtensionStatus.Unknown;
        }

        public string Name { get; private set; }

        public string Version { get; private set; }

        public ExtensionStatus Status { get; internal set; }

        public override bool Equals(object obj)
        {
            ExtensionInformation information = obj as ExtensionInformation;
            return !ReferenceEquals(information, null) &&
                   Name == information.Name &&
                   Version == information.Version;
        }

        public override int GetHashCode()
        {
            // We do this in an uncheched statement so there won't be any arithmetic exceptions
            unchecked
            {
                int hashCode = 14;
                hashCode = (hashCode * 6)
                    + EqualityComparer<string>.Default.GetHashCode(Name)
                    + EqualityComparer<string>.Default.GetHashCode(Version);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return "{0} v{1}".format_with(Name, Version);
        }
    }

    public enum ExtensionStatus
    {
        Unknown = 0,
        Loaded,
        Enabled = Loaded,
        Disabled,
        Failed
    }
}
