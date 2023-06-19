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

    public sealed class ChocolateyPackagePathResolver : PackagePathResolver
    {
        public string RootDirectory { get; set; }
        private IFileSystem _filesystem;

        public ChocolateyPackagePathResolver(string rootDirectory, IFileSystem filesystem)
             : base(rootDirectory, useSideBySidePaths: false)
        {
            RootDirectory = rootDirectory;
            _filesystem = filesystem;
        }

        public override string GetInstallPath(PackageIdentity packageIdentity)
            => GetInstallPath(packageIdentity.Id);

        public string GetInstallPath(string packageId)
            => _filesystem.CombinePaths(RootDirectory, packageId);

        [Obsolete("This overload will be removed in a future version.")]
        public string GetInstallPath(string id, NuGetVersion version)
            => GetInstallPath(id);

        public override string GetPackageFileName(PackageIdentity packageIdentity)
            => packageIdentity.Id + NuGetConstants.PackageExtension;
    }
}
