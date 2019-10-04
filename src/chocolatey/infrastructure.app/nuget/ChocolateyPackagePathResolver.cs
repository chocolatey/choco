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
            var packageVersionPath = Path.Combine(_nugetFileSystem.Root, GetPackageDirectory(package.Id,package.Version,useVersionInPath:true));
            if (_nugetFileSystem.DirectoryExists(packageVersionPath)) return packageVersionPath;


            return Path.Combine(_nugetFileSystem.Root, GetPackageDirectory(package.Id, package.Version));
        }

        public override string GetPackageDirectory(string packageId, SemanticVersion version)
        {
            return GetPackageDirectory(packageId, version, UseSideBySidePaths);
        }

        public string GetPackageDirectory(string packageId, SemanticVersion version, bool useVersionInPath)
        {
            string directory = packageId;
            if (useVersionInPath)
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