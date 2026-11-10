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
using System.IO;
using chocolatey.infrastructure.filesystem;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Versioning;

namespace chocolatey.infrastructure.app.nuget
{
    public sealed class ChocolateyPackagePathResolver : PackagePathResolver
    {
        public string RootDirectory { get; set; }

        /// <summary>
        ///   The package instance name, or null/empty for a default
        ///   (non-instanced) package. When set, install paths gain an extra
        ///   segment: &lt;root&gt;\&lt;id&gt;\&lt;instance&gt;.
        /// </summary>
        public string Instance { get; }

        /// <summary>
        ///   The user-requested (parent) package id for an instanced install. The parent resolves
        ///   under &lt;root&gt;\&lt;parentId&gt;\&lt;instance&gt;; every other id resolves under the
        ///   dependencies root. Settable because one resolver is reused across a multi-package install.
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        ///   Root the dependencies of an instanced package are installed under. Only consulted
        ///   when ParentId is set and the requested id is a dependency.
        /// </summary>
        public string DependenciesRootDirectory { get; set; }

        private IFileSystem _filesystem;

        public ChocolateyPackagePathResolver(string rootDirectory, IFileSystem filesystem)
            : this(rootDirectory, filesystem, instance: null)
        {
        }

        public ChocolateyPackagePathResolver(string rootDirectory, IFileSystem filesystem, string instance)
             : base(rootDirectory, useSideBySidePaths: false)
        {
            RootDirectory = rootDirectory;
            _filesystem = filesystem;
            Instance = instance;
        }

        public override string GetInstallPath(PackageIdentity packageIdentity)
        {
            return GetInstallPath(packageIdentity.Id);
        }

        public string GetInstallPath(string packageId)
        {
            // Default packages stay at <root>\<id> (one folder per id, as before).
            if (string.IsNullOrWhiteSpace(Instance))
            {
                return _filesystem.CombinePaths(RootDirectory, packageId);
            }

            // Instanced install: the requested package lives at <root>\<id>\<instance>,
            // any other id is a dependency and lives at <depsRoot>\<depId>.
            var isDependency = !string.IsNullOrWhiteSpace(ParentId)
                && !string.IsNullOrWhiteSpace(DependenciesRootDirectory)
                && !string.Equals(packageId, ParentId, StringComparison.OrdinalIgnoreCase);

            return isDependency
                ? _filesystem.CombinePaths(DependenciesRootDirectory, packageId)
                : _filesystem.CombinePaths(RootDirectory, packageId, Instance);
        }

        [Obsolete("This overload will be removed in a future version.")]
        public string GetInstallPath(string id, NuGetVersion version)
        {
            return GetInstallPath(id);
        }

        public override string GetPackageFileName(PackageIdentity packageIdentity)
        {
            return packageIdentity.Id + NuGetConstants.PackageExtension;
        }
    }
}
