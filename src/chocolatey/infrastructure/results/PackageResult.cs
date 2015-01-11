// Copyright © 2011 - Present RealDimensions Software, LLC
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

namespace chocolatey.infrastructure.results
{
    using System.Linq;
    using NuGet;

    /// <summary>
    ///   Outcome of package installation
    /// </summary>
    public sealed class PackageResult : Result
    {
        public bool Inconclusive
        {
            get { return _messages.Value.Any(x => x.MessageType == ResultType.Inconclusive); }
        }

        public string Name { get; private set; }
        public string Version { get; private set; }
        public IPackage Package { get; private set; }
        public string InstallLocation { get; set; }

        public PackageResult(IPackage package, string installLocation) : this(package.Id.to_lower(), package.Version.to_string(), installLocation)
        {
            Package = package;
        }

        public PackageResult(string name, string version, string installLocation)
        {
            Name = name;
            Version = version;
            InstallLocation = installLocation;
        }
    }
}