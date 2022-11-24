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

namespace chocolatey.infrastructure.results
{
	using System;
	using System.Collections.Generic;
    using System.Linq;
    using app.domain;
    using NuGet.Packaging;
    using NuGet.Packaging.Core;
    using NuGet.Protocol.Core.Types;
    using NuGet.Protocol;
    using NuGet.Versioning;

    /// <summary>
    ///   Outcome of package installation
    /// </summary>
    public sealed class PackageResult : Result
    {
        public bool Inconclusive
        {
            get { return _messages.Value.Any(x => x.MessageType == ResultType.Inconclusive); }
        }

        public bool Warning
        {
            get { return _messages.Value.Any(x => x.MessageType == ResultType.Warn); }
        }

        public string Name { get; private set; }
        public string Version { get; private set; }
        public IPackageMetadata PackageMetadata { get; private set; }
        public IPackageSearchMetadata SearchMetadata { get; private set; }
        public string InstallLocation { get; set; }
        public string Source { get; set; }
        public string SourceUri { get; set; }
        public int ExitCode { get; set; }

        public void ResetMetadata(IPackageMetadata metadata, IPackageSearchMetadata search)
        {
            PackageMetadata = metadata;
            SearchMetadata = search;
            Name = metadata.Id.to_lower();
            Version = metadata.Version.to_string();
        }

        public PackageResult(IPackageMetadata packageMetadata, string installLocation, string source = null) : this(packageMetadata.Id.to_lower(), packageMetadata.Version.to_string(), installLocation)
        {
            PackageMetadata = packageMetadata;
            Source = source;
        }

        public PackageResult(IPackageSearchMetadata packageSearch, string installLocation, string source = null) : this(packageSearch.Identity.Id.to_lower(), packageSearch.Identity.Version.to_string(), installLocation)
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
                    this.Log().Debug("Unable to determine sources from '{0}'. Using value as is.{1} {2}".format_with(source, Environment.NewLine, ex.to_string()));
                    // source is already set above
                    return;
                }
            }

            Source = sources.FirstOrDefault(uri => uri.IsFile || uri.IsUnc).to_string();
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

        public PackageResult(IPackageMetadata packageMetadata, IPackageSearchMetadata packageSearch, string installLocation, string source = null) : this(packageMetadata.Id.to_lower(), packageMetadata.Version.to_string(), installLocation)
        {
            SearchMetadata = packageSearch;
            PackageMetadata = packageMetadata;
            var sources = new List<Uri>();
            if (!string.IsNullOrEmpty(source))
            {
                try
                {
                    sources.AddRange(source.Split(new[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries).Select(s => new Uri(s)));
                }
                catch (Exception ex)
                {
                    this.Log().Debug("Unable to determine sources from '{0}'. Using value as is.{1} {2}".format_with(source, Environment.NewLine, ex.to_string()));
                    // source is already set above
                    return;
                }
            }

            Source = sources.FirstOrDefault(uri => uri.IsFile || uri.IsUnc).to_string();
        }

        public PackageResult(string name, string version, string installLocation, string source = null)
        {
            Name = name;
            Version = version;
            InstallLocation = installLocation;
            Source = source;
        }

        public PackageIdentity Identity
        {
            get { return new PackageIdentity(Name, NuGetVersion.Parse(Version));  }
        }
    }
}
