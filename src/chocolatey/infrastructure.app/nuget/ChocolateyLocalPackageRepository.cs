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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Versioning;
    using System.Threading;
    using NuGet;

    // ReSharper disable InconsistentNaming

    public class ChocolateyLocalPackageRepository : LocalPackageRepository
    {
        public ChocolateyLocalPackageRepository(string physicalPath)
            : base(physicalPath)
        {
        }

        public ChocolateyLocalPackageRepository(string physicalPath, bool enableCaching)
            : base(physicalPath, enableCaching)
        {
        }

        public ChocolateyLocalPackageRepository(IPackagePathResolver pathResolver, IFileSystem fileSystem)
            : base(pathResolver, fileSystem)
        {
        }

        public ChocolateyLocalPackageRepository(IPackagePathResolver pathResolver, IFileSystem fileSystem, bool enableCaching)
            : base(pathResolver, fileSystem, enableCaching)
        {
        }

        public override void AddPackage(IPackage package)
        {
            string packageFilePath = GetPackageFilePath(package);
            FileSystem.AddFileWithCheck(packageFilePath, package.GetStream);
            // allow the file to finish being written 
            Thread.Sleep(200);
            if (PackageSaveMode.HasFlag(PackageSaveModes.Nuspec))
            {
                // don't trust the package metadata to be complete - extract from the downloaded nupkg
                var zipPackage = new OptimizedZipPackage(FileSystem, packageFilePath);
                string manifestFilePath = GetManifestFilePath(package.Id, package.Version);
                Manifest manifest = Manifest.Create(zipPackage);
                manifest.Metadata.ReferenceSets = Enumerable.ToList<ManifestReferenceSet>(Enumerable.Select<IGrouping<FrameworkName, IPackageAssemblyReference>, ManifestReferenceSet>(Enumerable.GroupBy<IPackageAssemblyReference, FrameworkName>(package.AssemblyReferences, (Func<IPackageAssemblyReference, FrameworkName>)(f => f.TargetFramework)), (Func<IGrouping<FrameworkName, IPackageAssemblyReference>, ManifestReferenceSet>)(g => new ManifestReferenceSet()
                {
                    TargetFramework = g.Key == (FrameworkName)null ? (string)null : VersionUtility.GetFrameworkString(g.Key),
                    References = Enumerable.ToList<ManifestReference>(Enumerable.Select<IPackageAssemblyReference, ManifestReference>((IEnumerable<IPackageAssemblyReference>)g, (Func<IPackageAssemblyReference, ManifestReference>)(p => new ManifestReference()
                    {
                        File = p.Name
                    })))
                })));
                FileSystem.AddFileWithCheck(manifestFilePath, manifest.Save);
            }
        }

        private string GetManifestFilePath(string packageId, SemanticVersion version)
        {
            string packageDirectory = PathResolver.GetPackageDirectory(packageId, version);
            string path2 = packageDirectory + Constants.ManifestExtension;
            return Path.Combine(packageDirectory, path2);
        }
    }

    // ReSharper restore InconsistentNaming
}