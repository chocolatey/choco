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
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO.Packaging;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using configuration;
    using filesystem;
    using NuGet.Common;
    using NuGet.Configuration;
    using NuGet.PackageManagement;
    using NuGet.Packaging;
    using NuGet.Packaging.Core;
    using NuGet.Protocol;
    using NuGet.Protocol.Core.Types;
    using NuGet.Versioning;

    public static class NugetList
    {
        public static int LastPackageLimitUsed { get; private set; }
        public static bool ThresholdHit { get; private set; }
        public static bool LowerThresholdHit { get; private set; }

        public static IEnumerable<IPackageSearchMetadata> GetPackages(ChocolateyConfiguration configuration, ILogger nugetLogger, IFileSystem filesystem)
        {
            return SearchPackagesAsync(configuration, nugetLogger, filesystem).GetAwaiter().GetResult();
        }

        public static int GetCount(ChocolateyConfiguration configuration, ILogger nugetLogger, IFileSystem filesystem)
        {
            var packageRepositories = NugetCommon.GetRemoteRepositories(configuration, nugetLogger, filesystem);
            var packageRepositoriesResources = NugetCommon.GetRepositoryResources(packageRepositories);
            string searchTermLower = configuration.Input.ToLowerSafe();

            SearchFilter searchFilter = new SearchFilter(configuration.Prerelease);
            searchFilter.IncludeDelisted = configuration.ListCommand.LocalOnly;


            int totalCount = 0;
            foreach (var repositoryResources in packageRepositoriesResources)
            {
                if (repositoryResources.searchResource == null)
                {
                    continue;
                }

                totalCount += repositoryResources.searchResource.SearchCountAsync(searchTermLower, searchFilter, nugetLogger, CancellationToken.None).GetAwaiter().GetResult();
            }

            return totalCount;
        }

        private async static Task<IQueryable<IPackageSearchMetadata>> SearchPackagesAsync(ChocolateyConfiguration configuration, ILogger nugetLogger, IFileSystem filesystem)
        {
            ThresholdHit = false;
            LowerThresholdHit = false;

            var packageRepositories = NugetCommon.GetRemoteRepositories(configuration, nugetLogger, filesystem);
            var packageRepositoriesResources = NugetCommon.GetRepositoryResources(packageRepositories);
            string searchTermLower = configuration.Input.ToLowerSafe();

            SearchFilter searchFilter = new SearchFilter(configuration.Prerelease);
            searchFilter.IncludeDelisted = configuration.ListCommand.LocalOnly;
            searchFilter.OrderBy = SearchOrderBy.Id;

            if (configuration.ListCommand.OrderByPopularity)
            {
                searchFilter.OrderBy = SearchOrderBy.DownloadCount;
            }

            if (configuration.ListCommand.ByIdOnly)
            {
                searchFilter.ByIdOnly = true;
            }

            if (configuration.ListCommand.ByTagOnly)
            {
                searchFilter.ByTagOnly = true;
            }

            if (configuration.ListCommand.IdStartsWith)
            {
                searchFilter.IdStartsWith = true;
            }

            var cacheContext = new ChocolateySourceCacheContext(configuration);

            NuGetVersion version = !string.IsNullOrWhiteSpace(configuration.Version) ? NuGetVersion.Parse(configuration.Version) : null;

            if (version != null)
            {
                searchFilter.OrderBy = SearchOrderBy.Version;
            }

            var results = new HashSet<IPackageSearchMetadata>(new ComparePackageSearchMetadata());
            var thresholdLimit = 0;

            if (configuration.ListCommand.ExplicitPageSize)
            {
                LastPackageLimitUsed = configuration.ListCommand.PageSize;
                thresholdLimit = LastPackageLimitUsed;
            }
            else
            {
                LastPackageLimitUsed = configuration.ListCommand.LocalOnly ? 10000 : 1000;
                thresholdLimit = LastPackageLimitUsed;
            }

            if (configuration.ListCommand.Page.HasValue)
            {
                LastPackageLimitUsed = (configuration.ListCommand.Page.Value + 1) * configuration.ListCommand.PageSize;
                thresholdLimit = configuration.ListCommand.PageSize;
            }

            var lowerThresholdLimit = (int)(thresholdLimit * 0.9);

            var totalToGet = LastPackageLimitUsed;

            if (!configuration.ListCommand.Exact)
            {
                foreach (var repositoryResources in packageRepositoriesResources)
                {
                    var skipNumber = 0;

                    if (configuration.ListCommand.Page.HasValue)
                    {
                        skipNumber = configuration.ListCommand.PageSize * configuration.ListCommand.Page.GetValueOrDefault(0);
                    }

                    if (repositoryResources.searchResource != null && version == null)
                    {
                        var takeNumber = GetTakeAmount(configuration);

                        var partResults = new HashSet<IPackageSearchMetadata>(new ComparePackageSearchMetadataIdOnly());
                        var latestResults = new List<IPackageSearchMetadata>();

                        var perSourceThresholdLimit = thresholdLimit;
                        var perSourceThresholdMinLimit = lowerThresholdLimit;

                        do
                        {
                            if (perSourceThresholdLimit < takeNumber)
                            {
                                takeNumber = perSourceThresholdLimit;
                            }

                            partResults.Clear();
                            partResults.AddRange(await repositoryResources.searchResource.SearchAsync(searchTermLower, searchFilter, skipNumber, takeNumber, nugetLogger, CancellationToken.None));
                            skipNumber += takeNumber;
                            perSourceThresholdLimit -= partResults.Count;
                            perSourceThresholdMinLimit -= partResults.Count;
                            latestResults.AddRange(partResults);
                        } while (partResults.Count >= takeNumber && skipNumber < totalToGet);

                        ThresholdHit = perSourceThresholdLimit <= 0;
                        LowerThresholdHit = perSourceThresholdMinLimit <= 0;

                        if (configuration.AllVersions)
                        {
                            foreach (var result in latestResults)
                            {
                                foreach (var versionInfo in await result.GetVersionsAsync())
                                {
                                    if (versionInfo.PackageSearchMetadata == null)
                                    {
                                        //This is horribly inefficient, having to get the metadata again but that is the NuGet resources for you
                                        results.Add(await repositoryResources.packageMetadataResource.GetMetadataAsync(new PackageIdentity(result.Identity.Id, versionInfo.Version), cacheContext, nugetLogger, CancellationToken.None));
                                    }
                                    else
                                    {
                                        results.Add(versionInfo.PackageSearchMetadata);
                                    }
                                }
                            }
                        }
                        else
                        {
                            results.AddRange(latestResults);
                        }
                    }
                    else
                    {
                        configuration.Prerelease = configuration.Prerelease || (version != null && version.IsPrerelease);
                        configuration.AllVersions = configuration.AllVersions || (version != null);

                        var tempResults = await repositoryResources.listResource.ListAsync(searchTermLower, configuration.Prerelease, configuration.AllVersions, false, nugetLogger, CancellationToken.None);
                        var enumerator = tempResults.GetEnumeratorAsync();

                        var perSourceThresholdLimit = thresholdLimit;
                        var perSourceThresholdMinLimit = lowerThresholdLimit;

                        while (await enumerator.MoveNextAsync())
                        {
                            if (version != null && enumerator.Current.Identity.Version != version)
                            {
                                continue;
                            }

                            if (skipNumber > 0)
                            {
                                skipNumber--;
                                continue;
                            }

                            results.Add(enumerator.Current);
                            perSourceThresholdLimit--;
                            perSourceThresholdMinLimit--;

                            if (results.Count >= totalToGet)
                            {
                                break;
                            }
                        }

                        ThresholdHit = ThresholdHit || perSourceThresholdLimit <= 0;
                        LowerThresholdHit = LowerThresholdHit || perSourceThresholdMinLimit <= 0;
                    }
                }

                //TODO - deduplicate package ids
            }
            else
            {
                if (configuration.AllVersions)
                {
                    foreach (var repositoryResources in packageRepositoriesResources)
                    {
                        results.AddRange(await repositoryResources.packageMetadataResource.GetMetadataAsync(
                            searchTermLower, configuration.Prerelease, false, cacheContext, nugetLogger, CancellationToken.None));
                    }
                }
                else
                {
                    var exactPackage = FindPackage(searchTermLower, configuration, nugetLogger, cacheContext, packageRepositoriesResources.Select(x => x.packageMetadataResource), packageRepositoriesResources.Select(x => x.listResource), version);

                    if (exactPackage == null) return new List<IPackageSearchMetadata>().AsQueryable();

                    return new List<IPackageSearchMetadata>()
                    {
                        exactPackage
                    }.AsQueryable();
                }
            }

            if (version != null)
            {
                results = results.Where(p => p.Identity.Version.Equals(version)).ToHashSet();
            }

            if (configuration.ListCommand.IdStartsWith)
            {
                results = results.Where(p => p.Identity.Id.ToLower().StartsWith(searchTermLower)).ToHashSet();
            }
            else if (configuration.ListCommand.ByIdOnly)
            {
                results = results.Where(p => p.Identity.Id.ToLower().Contains(searchTermLower)).ToHashSet();
            }

            if (configuration.ListCommand.ByTagOnly)
            {
                results = results.Where(p => p.Tags.ContainsSafe(searchTermLower, StringComparison.InvariantCultureIgnoreCase)).ToHashSet();
            }

            if (configuration.ListCommand.ApprovedOnly)
            {
                results = results.Where(p => p.IsApproved).ToHashSet();
            }

            if (configuration.ListCommand.DownloadCacheAvailable)
            {
                results = results.Where(p => p.IsDownloadCacheAvailable).ToHashSet();
            }

            if (configuration.ListCommand.NotBroken)
            {
                results = results.Where(p => (p.IsDownloadCacheAvailable && configuration.Information.IsLicensedVersion) || p.PackageTestResultStatus != "Failing").ToHashSet();
            }

            results = configuration.ListCommand.OrderByPopularity ?
                 results.OrderByDescending(p => p.DownloadCount).ThenBy(p => p.Identity.Id).ToHashSet()
                 : results.OrderBy(p => p.Identity.Id).ThenByDescending(p => p.Identity.Version).ToHashSet();

            return results.AsQueryable();
        }

        private static int GetTakeAmount(ChocolateyConfiguration configuration)
        {
            // We calculate the amount of items we take at the same time, while making
            // sure to take the minimum value of 30 packages which we know is a safe value
            // to take from CCR and other feeds to minimize any issues that can happen.

            if (configuration.ListCommand.Page.HasValue || configuration.ListCommand.ExplicitPageSize)
            {
                return Math.Min(configuration.ListCommand.PageSize, 30);
            }

            return 30;
        }

        public static ISet<IPackageSearchMetadata> FindAllPackageVersions(string packageName, ChocolateyConfiguration config, ILogger nugetLogger, ChocolateySourceCacheContext cacheContext, IEnumerable<PackageMetadataResource> resources)
        {
            var metadataList = new HashSet<IPackageSearchMetadata>();
            foreach (var resource in resources)
            {
                metadataList.AddRange(resource.GetMetadataAsync(packageName, config.Prerelease, false, cacheContext, nugetLogger, CancellationToken.None).GetAwaiter().GetResult());
            }
            return metadataList;
        }

        /// <summary>
        ///   Searches for packages that are available based on name and other options
        /// </summary>
        /// <param name="packageName">Name of package to search for</param>
        /// <param name="config">Chocolatey configuration used to help supply the search parameters</param>
        /// <param name="nugetLogger">The nuget logger</param>
        /// <param name="packageMetadataResources">The PackageMetaDataResources that should be queried</param>
        /// <param name="listResources">The ListResources that should be queried</param>
        /// <param name="version">Version to search for</param>
        /// <param name="cacheContext">Settings for caching of results from sources</param>
        /// <returns>One result or nothing</returns>
        public static IPackageSearchMetadata FindPackage(
            string packageName,
            ChocolateyConfiguration config,
            ILogger nugetLogger,
            ChocolateySourceCacheContext cacheContext,
            IEnumerable<PackageMetadataResource> packageMetadataResources,
            IEnumerable<ListResource> listResources,
            NuGetVersion version = null)
        {
            // We can only use the optimized ListResource query when the user has asked us to, via the UsePackageRepositoryOptimizations
            // feature, as well as when a ListResource exists for the feed in question.  Some technologies, such as Sleet or Baget, only
            // offer V3 feeds, not V2, and as a result, no ListResource is available.
            if (config.Features.UsePackageRepositoryOptimizations && listResources.Any())
            {
                if (version is null)
                {
                    // We only need to force a lower case package name when we
                    // are using our own repository optimization queries.
                    var packageNameLower = packageName.ToLowerSafe();
                    var metadataList = new HashSet<IPackageSearchMetadata>();

                    foreach (var listResource in listResources)
                    {
                        var package = listResource.PackageAsync(packageNameLower, config.Prerelease, nugetLogger, CancellationToken.None).GetAwaiter().GetResult();
                        if (package != null)
                        {
                            metadataList.Add(package);
                        }
                    }

                    return metadataList.OrderByDescending(p => p.Identity.Version).FirstOrDefault();
                }
            }
            else
            {
                if (version is null)
                {
                    var metadataList = new HashSet<IPackageSearchMetadata>();

                    foreach (var packageMetadataResource in packageMetadataResources)
                    {
                        metadataList.AddRange(packageMetadataResource.GetMetadataAsync(packageName, config.Prerelease, false, cacheContext, nugetLogger, CancellationToken.None).GetAwaiter().GetResult());
                    }

                    return metadataList.OrderByDescending(p => p.Identity.Version).FirstOrDefault();
                }
            }

            foreach (var resource in packageMetadataResources)
            {
                var metadata = resource.GetMetadataAsync(new PackageIdentity(packageName, version), cacheContext, nugetLogger, CancellationToken.None).GetAwaiter().GetResult();

                if (metadata != null)
                {
                    return metadata;
                }
            }

            //If no packages found, then return nothing
            return null;
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static ISet<IPackageSearchMetadata> find_all_package_versions(string packageName, ChocolateyConfiguration config, ILogger nugetLogger, ChocolateySourceCacheContext cacheContext, IEnumerable<PackageMetadataResource> resources)
            => FindAllPackageVersions(packageName, config, nugetLogger, cacheContext, resources);
#pragma warning restore IDE1006
    }

    public class ComparePackageSearchMetadataIdOnly: IEqualityComparer<IPackageSearchMetadata>
    {
        public bool Equals(IPackageSearchMetadata x, IPackageSearchMetadata y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
            {
                return false;
            }
            return x.Identity.Id.Equals(y.Identity.Id, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(IPackageSearchMetadata obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return 0;
            }
            return obj.Identity.Id.GetHashCode();
        }
    }

    public class ComparePackageSearchMetadata : IEqualityComparer<IPackageSearchMetadata>
    {
        public bool Equals(IPackageSearchMetadata x, IPackageSearchMetadata y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
            {
                return false;
            }

            return x.Identity.Equals(y.Identity);
        }

        public int GetHashCode(IPackageSearchMetadata obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return 0;
            }
            return obj.Identity.GetHashCode();
        }
    }
}
