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

namespace chocolatey.infrastructure.app.nuget
{
    using System.IO;
    using NuGet;

    // ReSharper disable InconsistentNaming

    public sealed class ChocolateyPackagePathResolver : DefaultPackagePathResolver
    {
        private readonly IFileSystem _nugetFileSystem;
        public bool UseSideBySidePaths { get; set; }

        public ChocolateyPackagePathResolver(IFileSystem nugetFileSystem, bool useSideBySidePaths)
            : base(nugetFileSystem, useSideBySidePaths)
        {
            _nugetFileSystem = nugetFileSystem;
            UseSideBySidePaths = useSideBySidePaths;
        }

        public override string GetInstallPath(IPackage package)
        {
            return Path.Combine(_nugetFileSystem.Root, GetPackageDirectory(package));
        }

        public override string GetPackageDirectory(string packageId, SemanticVersion version)
        {
            string directory = packageId;
            if (UseSideBySidePaths)
            {
                directory += "." + version.to_string();
            }
            return directory;
        }

        public override string GetPackageFileName(string packageId, SemanticVersion version)
        {
            string fileNameBase = packageId;
            if (UseSideBySidePaths)
            {
                fileNameBase += "." + version.to_string();
            }
            return fileNameBase + Constants.PackageExtension;
        }
    }

    // ReSharper restore InconsistentNaming
}