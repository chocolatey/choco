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
using System.Collections.Generic;
using System.Linq;
using chocolatey.infrastructure.app.domain;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol;
using NuGet.Versioning;

namespace chocolatey.infrastructure.results
{
    /// <summary>
    ///   Outcome of package installation
    /// </summary>
    public sealed class PackageResult : Result
    {
        public bool Inconclusive
        {
            get { return Messages.Any(x => x.MessageType == ResultType.Inconclusive); }
        }

        public bool Warning
        {
            get { return Messages.Any(x => x.MessageType == ResultType.Warn); }
        }

        /// <summary>
        /// Name of the package installed.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Version of the package installed.
        /// </summary>
        public string Version { get; private set; }
        /// <summary>
        /// Instance of <see cref="IPackageMetadata"/> representing the nuspec file in memory.
        /// </summary>
        public IPackageMetadata PackageMetadata { get; private set; }
        /// <summary>
        /// Instance of <see cref="IPackageSearchMetadata"/> representing the package data returned from a repository.
        /// </summary>
        public IPackageSearchMetadata SearchMetadata { get; private set; }
        /// <summary>
        /// Location on disk that the package has been installed to.
        /// </summary>
        public string InstallLocation { get; set; }
        /// <summary>
        /// Sources available during package installation.
        /// </summary>
        public string Source { get; set; }
        [Obsolete("This property is deprecated and will be removed in v3.")]
        public string SourceUri { get; set; }
        /// <summary>
        /// The package source used to install the package.
        /// </summary>
        public string SourceInstalledFrom { get; set; }

        /// <summary>
        /// When the packaage was last updated.
        /// </summary>
        public string LastUpdated { get; set; }
        public int ExitCode { get; set; }

        public void ResetMetadata(IPackageMetadata metadata, IPackageSearchMetadata search)
        {
            PackageMetadata = metadata;
            SearchMetadata = search;
            Name = metadata.Id;
            Version = metadata.Version.ToNormalizedStringChecked();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageResult"/> class.
        /// </summary>
        /// <param name="packageMetadata">Instance of <see cref="IPackageMetadata"/> representing the nuspec file in memory. Assigned to <see cref="PackageMetadata"/></param>
        /// <param name="installLocation">Assigned to <see cref="InstallLocation"/></param>
        /// <param name="source">Sources available during package installation. Assigned to <see cref="Source"/></param>
        public PackageResult(IPackageMetadata packageMetadata, string installLocation, string source = null) 
            : this(packageMetadata.Id, packageMetadata.Version.ToNormalizedStringChecked(), installLocation)
        {
            PackageMetadata = packageMetadata;
            Source = source;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageResult"/> class.
        /// </summary>
        /// <param name="packageSearch">Instance of <see cref="IPackageSearchMetadata"/> representing the package data returned from a repository. Assigned to <see cref="SearchMetadata"/></param>
        /// <param name="installLocation">Assigned to <see cref="InstallLocation"/></param>
        /// <param name="source">Sources available during package installation. Assigned to <see cref="Source"/></param>
        public PackageResult(IPackageSearchMetadata packageSearch, string installLocation, string source = null) 
            : this(packageSearch.Identity.Id, packageSearch.Identity.Version.ToNormalizedStringChecked(), installLocation)
        {
            SearchMetadata = packageSearch;
            Source = source;

            var sources = new List<Uri>();
            if (!string.IsNullOrEmpty(source))
            {
                try
                {
                    sources.AddRange(source.Split(new[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries).Select(s => new Uri(s)));
                }
                catch (Exception ex)
                {
                    this.Log().Debug("Unable to determine sources from '{0}'. Using value as is.{1} {2}".FormatWith(source, Environment.NewLine, ex.ToStringSafe()));
                    // source is already set above
                    return;
                }
            }

            Source = sources.FirstOrDefault(uri => uri.IsFile || uri.IsUnc).ToStringSafe();
            /*
            var rp = Package as DataServicePackage;
            if (rp != null && rp.DownloadUrl != null)
            {
                SourceUri = rp.DownloadUrl.ToString();
                Source = sources.FirstOrDefault(uri => uri.IsBaseOf(rp.DownloadUrl)).to_string();
                if (string.IsNullOrEmpty(Source))
                {
                    Source = sources.FirstOrDefault(uri => uri.DnsSafeHost == rp.DownloadUrl.DnsSafeHost).to_string();
                }
            }
            else
            {
                Source = sources.FirstOrDefault(uri => uri.IsFile || uri.IsUnc).to_string();
            }
            */
        }

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public PackageResult(IPackageMetadata packageMetadata, IPackageSearchMetadata packageSearch, string installLocation, string source = null, string lastUpdated = null)
            : this(packageMetadata, packageSearch, installLocation, source, null, lastUpdated) {  }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageResult"/> class.
        /// </summary>
        /// <param name="packageMetadata">Instance of <see cref="IPackageMetadata"/> representing the nuspec file in memory. Assigned to <see cref="PackageMetadata"/></param>
        /// <param name="packageSearch">Instance of <see cref="IPackageSearchMetadata"/> representing the package data returned from a repository. Assigned to <see cref="SearchMetadata"/></param>
        /// <param name="installLocation">Assigned to <see cref="InstallLocation"/></param>
        /// <param name="source">Sources available during package installation. Assigned to <see cref="Source"/></param>
        /// <param name="sourceInstalledFrom">The package source used to install the package. Assigned to <see cref="SourceInstalledFrom"/></param>
        /// <param name="lastUpdated">The last updated date of the package. Assigned to <see cref="LastUpdated"/></param>
        public PackageResult(IPackageMetadata packageMetadata, IPackageSearchMetadata packageSearch, string installLocation, string source, string sourceInstalledFrom, string lastUpdated)
            : this(packageMetadata.Id, packageMetadata.Version.ToNormalizedStringChecked(), installLocation, lastUpdated)
        {
            SearchMetadata = packageSearch;
            PackageMetadata = packageMetadata;
            SourceInstalledFrom = sourceInstalledFrom;
            LastUpdated = lastUpdated;
            var sources = new List<Uri>();
            if (!string.IsNullOrEmpty(source))
            {
                try
                {
                    sources.AddRange(source.Split(new[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries).Select(s => new Uri(s)));
                }
                catch (Exception ex)
                {
                    this.Log().Debug("Unable to determine sources from '{0}'. Using value as is.{1} {2}".FormatWith(source, Environment.NewLine, ex.ToStringSafe()));
                    // source is already set above
                    // Note: Where above? This seems to be copied/pasted from the overload above, but forgot to actually set the source.
                    // While this seems like it may be incorrect, we do not currently know if this is an issue at all.
                    return;
                }
            }

            Source = sources.FirstOrDefault(uri => uri.IsFile || uri.IsUnc).ToStringSafe();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageResult"/> class.
        /// </summary>
        /// <param name="name">Name of the package installed. Assigned to <see cref="Name"/></param>
        /// <param name="version">Version of the package installed. Assigned to <see cref="Version"/></param>
        /// <param name="installLocation">Location on disk the package was installed to. Assigned to <see cref="InstallLocation"/></param>
        /// <param name="source">Sources available during package installation. Assigned to <see cref="Source"/></param>
        public PackageResult(string name, string version, string installLocation, string source = null)
        {
            Name = name;
            Version = version;
            InstallLocation = installLocation;
            Source = source;
        }

        public PackageIdentity Identity
        {
            get { return new PackageIdentity(Name, NuGetVersion.Parse(Version)); }
        }
    }
}
