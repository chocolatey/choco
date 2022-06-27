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

namespace chocolatey.infrastructure.app.nuget
{
    using System;
    using System.IO;
    using filesystem;
    using NuGet.Configuration;
    using NuGet.Packaging;
    using NuGet.Packaging.Core;
    using NuGet.ProjectManagement;
    using NuGet.Versioning;

    // ReSharper disable InconsistentNaming

    public sealed class ChocolateyPackagePathResolver : PackagePathResolver
    {
        public string RootDirectory { get; set; }
        public new bool UseSideBySidePaths { get; set; }
        private IFileSystem _filesystem;

        public ChocolateyPackagePathResolver(string rootDirectory, IFileSystem filesystem, bool useSideBySidePaths)
            : base(rootDirectory, useSideBySidePaths)
        {
            RootDirectory = rootDirectory;
            UseSideBySidePaths = useSideBySidePaths;
            _filesystem = filesystem;
        }

        public override string GetInstallPath(PackageIdentity packageIdentity)
        {
            if (UseSideBySidePaths)
            {
                return Path.Combine(RootDirectory, GetPackageDirectory(packageIdentity, useVersionInPath: true));
            }
            else
            {
                var packageVersionPath = Path.Combine(RootDirectory, GetPackageDirectory(packageIdentity, useVersionInPath: true));
                if (_filesystem.directory_exists(packageVersionPath)) return packageVersionPath;


                return Path.Combine(RootDirectory, GetPackageDirectory(packageIdentity, false));
            }
        }

        public string GetInstallPath(string id, NuGetVersion version)
        {
            return GetInstallPath(new PackageIdentity(id, version));
        }

        [Obsolete("Side by Side installations are deprecated, and is pending removal in v2.0.0")]
        public override string GetPackageDirectoryName(PackageIdentity packageIdentity)
        {
            return GetPackageDirectory(packageIdentity, UseSideBySidePaths);
        }

        public string GetPackageDirectory(PackageIdentity packageIdentity, bool useVersionInPath)
        {
            string directory = packageIdentity.Id;
            if (useVersionInPath)
            {
                directory += "." + packageIdentity.Version.to_string();
            }

            return directory;
        }

        public override string GetPackageFileName(PackageIdentity packageIdentity)
        {
            string fileNameBase = packageIdentity.Id;
            if (UseSideBySidePaths)
            {
                fileNameBase += "." + packageIdentity.Version.to_string();
            }
            return fileNameBase + NuGetConstants.PackageExtension;
        }
    }

    // ReSharper restore InconsistentNaming
}
