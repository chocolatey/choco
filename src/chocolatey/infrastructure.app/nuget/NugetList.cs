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
using System.Threading;
using System.Threading.Tasks;
using chocolatey.infrastructure.tolerance;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.filesystem;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace chocolatey.infrastructure.app.nuget
{
    public static class NugetList
    {
        public static int LastPackageLimitUsed { get; private set; }
        public static bool ThresholdHit { get; private set; }
        public static bool LowerThresholdHit { get; private set; }

        public static IEnumerable<IPackageSearchMetadata> GetPackages(ChocolateyConfiguration configuration, ILogger nugetLogger, IFileSystem filesystem)
        {
            return SearchPackagesAsync(configuration, nugetLogger, filesystem).GetAwaiter().GetResult();
        }

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static int GetCount(ChocolateyConfiguration configuration, ILogger nugetLogger, IFileSystem filesystem)
        {
            return GetCount(configuration, nugetLogger, filesystem, new ChocolateySourceCacheContext(configuration));
        }

        public static int GetCount(ChocolateyConfiguration configuration, ILogger nugetLogger, IFileSystem filesystem, ChocolateySourceCacheContext cacheContext)
        {
            var packageRepositoriesResources = NugetCommon.GetRepositoryResources(configuration, nugetLogger, filesystem, cacheContext);
            var searchTermLower = configuration.Input.ToLowerSafe();

            var searchFilter = new SearchFilter(configuration.Prerelease)
            {
                IncludeDelisted = configuration.ListCommand.LocalOnly,
                OrderBy = GetSortOrder(configuration.ListCommand.OrderBy, configuration.AllVersions)
            };

            var totalCount = 0;
            foreach (var searchResource in packageRepositoriesResources.SearchResources())
            {
                totalCount += searchResource.SearchCountAsync(searchTermLower, searchFilter, nugetLogger, CancellationToken.None).GetAwaiter().GetResult();
            }

            return totalCount;
        }

        private async static Task<IQueryable<IPackageSearchMetadata>> SearchPackagesAsync(ChocolateyConfiguration configuration, ILogger nugetLogger, IFileSystem filesystem)
        {
            ThresholdHit = false;
            LowerThresholdHit = false;

            var cacheContext = new ChocolateySourceCacheContext(configuration);
            var packageRepositoryResources = NugetCommon.GetRepositoryResources(configuration, nugetLogger, filesystem, cacheContext);
            var searchTermLower = configuration.Input.ToLowerSafe();

            NuGetVersion version = !string.IsNullOrWhiteSpace(configuration.Version) ? NuGetVersion.Parse(configuration.Version) : null;

            var searchFilter = new SearchFilter(configuration.Prerelease)
            {
                IncludeDelisted = configuration.ListCommand.LocalOnly,
                OrderBy = GetSortOrder(configuration.ListCommand.OrderBy, configuration.AllVersions || !(version is null))
            };

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
                foreach (var repositoryResources in packageRepositoryResources)
                {
                    var skipNumber = 0;

                    if (configuration.ListCommand.Page.HasValue)
                    {
                        skipNumber = configuration.ListCommand.PageSize * configuration.ListCommand.Page.GetValueOrDefault(0);
                    }

                    if ((version == null || repositoryResources.ListResource == null) && repositoryResources.SearchResource != null)
                    {
                        var takeNumber = GetTakeAmount(configuration);

                        if (takeNumber != 30)
                        {
                            var warning = "The page size has been specified to be {0:N0} packages. There are known issues with some repositories when you use a page size other than 30.".FormatWith(takeNumber);
                            if (configuration.RegularOutput)
                            {
                                "chocolatey".Log().Warn(warning);
                            }
                            else
                            {
                                "chocolatey".Log().Debug(warning);
                            }
                        }

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
                            partResults.AddRange(await repositoryResources.SearchResource.SearchAsync(searchTermLower, searchFilter, skipNumber, takeNumber, nugetLogger, cacheContext, CancellationToken.None));
                            skipNumber += takeNumber;
                            perSourceThresholdLimit -= partResults.Count;
                            perSourceThresholdMinLimit -= partResults.Count;
                            latestResults.AddRange(partResults);
                        } while (partResults.Count >= takeNumber && skipNumber < totalToGet);

                        ThresholdHit = ThresholdHit || perSourceThresholdLimit <= 0;
                        LowerThresholdHit = LowerThresholdHit || perSourceThresholdMinLimit <= 0;

                        if (configuration.AllVersions)
                        {
                            foreach (var result in latestResults)
                            {
                                foreach (var versionInfo in await result.GetVersionsAsync())
                                {
                                    if (versionInfo.PackageSearchMetadata == null)
                                    {
                                        //This is horribly inefficient, having to get the metadata again but that is the NuGet resources for you
                                        results.Add(await repositoryResources.PackageMetadataResource.GetMetadataAsync(new PackageIdentity(result.Identity.Id, versionInfo.Version), cacheContext, nugetLogger, CancellationToken.None));
                                    }
                                    else
                                    {
                                        results.Add(versionInfo.PackageSearchMetadata);
                                    }
                                }
                            }
                        }
                        else if (version != null)
                        {
                            // We need to look up any packages that do not have a matching version number.

                            foreach (var package in latestResults)
                            {
                                if (package.Identity.Version != version)
                                {
                                    var result = FindPackage(package.Identity.Id, configuration, nugetLogger, (SourceCacheContext)cacheContext, new[] { repositoryResources }, version);

                                    if (result != null)
                                    {
                                        results.Add(result);
                                    }
                                }
                                else
                                {
                                    results.Add(package);
                                }
                            }
                        }
                        else
                        {
                            results.AddRange(latestResults);
                        }
                    }
                    else if (repositoryResources.ListResource != null)
                    {
                        configuration.Prerelease = configuration.Prerelease || (version != null && version.IsPrerelease);
                        configuration.AllVersions = configuration.AllVersions || (version != null);

                        var tempResults = await repositoryResources.ListResource.ListAsync(searchTermLower, configuration.Prerelease, configuration.AllVersions, false, nugetLogger, cacheContext, CancellationToken.None);
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
                    foreach (var repositoryResources in packageRepositoryResources)
                    {
                        results.AddRange(await repositoryResources.PackageMetadataResource.GetMetadataAsync(
                            searchTermLower, configuration.Prerelease, false, cacheContext, nugetLogger, CancellationToken.None));
                    }
                }
                else
                {
                    var exactPackage = FindPackage(searchTermLower, configuration, nugetLogger, (SourceCacheContext)cacheContext, packageRepositoryResources, version);

                    if (exactPackage == null)
                    {
                        return new List<IPackageSearchMetadata>().AsQueryable();
                    }

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

            results = ApplyPackageSort(results, configuration.ListCommand.OrderBy).ToHashSet();

            return results.AsQueryable();
        }

        private static int GetTakeAmount(ChocolateyConfiguration configuration)
        {

            if (configuration.ListCommand.Page.HasValue || configuration.ListCommand.ExplicitPageSize)
            {
                return configuration.ListCommand.PageSize;
            }

            return 30;
        }

        [Obsolete("Will be removed in v3, use overload with NuGetEndpointResources instead!")]
        public static ISet<IPackageSearchMetadata> FindAllPackageVersions(string packageName, ChocolateyConfiguration config, ILogger nugetLogger, ChocolateySourceCacheContext cacheContext, IEnumerable<PackageMetadataResource> resources)
        {
            var metadataList = new HashSet<IPackageSearchMetadata>();
            foreach (var resource in resources)
            {
                metadataList.AddRange(resource.GetMetadataAsync(packageName, config.Prerelease, false, cacheContext, nugetLogger, CancellationToken.None).GetAwaiter().GetResult());
            }
            return metadataList;
        }

        public static ISet<IPackageSearchMetadata> FindAllPackageVersions(string packageName, ChocolateyConfiguration config, ILogger nugetLogger, ChocolateySourceCacheContext cacheContext, IEnumerable<NuGetEndpointResources> resources)
        {
            // Currently this method is a duplicate of its overload,
            // but using NuGetEndpointResources here gives us more flexibility in the future
            // if we need to call one of the other methods if it is possible.
            var metadataList = new HashSet<IPackageSearchMetadata>();

            foreach (PackageMetadataResource resource in resources.MetadataResources())
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
        /// <param name="resources">The resources that should be queried</param>
        /// <param name="version">Version to search for</param>
        /// <param name="cacheContext">Settings for caching of results from sources</param>
        /// <returns>One result or nothing</returns>
        [Obsolete("Use the overload that uses the base source cache context instead.")]
        public static IPackageSearchMetadata FindPackage(
            string packageName,
            ChocolateyConfiguration config,
            ILogger nugetLogger,
            ChocolateySourceCacheContext cacheContext,
            IEnumerable<NuGetEndpointResources> resources,
            NuGetVersion version)
        {
            return FindPackage(packageName, config, nugetLogger, (SourceCacheContext)cacheContext, resources, version);
        }

        /// <summary>
        ///   Searches for packages that are available based on name and other options
        /// </summary>
        /// <param name="packageName">Name of package to search for</param>
        /// <param name="config">Chocolatey configuration used to help supply the search parameters</param>
        /// <param name="nugetLogger">The nuget logger</param>
        /// <param name="resources">The resources that should be queried</param>
        /// <param name="version">Version to search for</param>
        /// <param name="cacheContext">Settings for caching of results from sources</param>
        /// <returns>One result or nothing</returns>
        public static IPackageSearchMetadata FindPackage(
            string packageName,
            ChocolateyConfiguration config,
            ILogger nugetLogger,
            SourceCacheContext cacheContext,
            IEnumerable<NuGetEndpointResources> resources,
            NuGetVersion version = null)
        {
            var packagesList = new HashSet<IPackageSearchMetadata>();
            var packageNameLower = packageName.ToLowerSafe();

            foreach (var resource in resources)
            {
                if (version is null)
                {
                    // We can only use the optimized ListResource query when the user has asked us to, via the UsePackageRepositoryOptimizations
                    // feature, as well as when a ListResource exists for the feed in question.  Some technologies, such as Sleet or Baget, only
                    // offer V3 feeds, not V2, and as a result, no ListResource is available.
                    if (config.Features.UsePackageRepositoryOptimizations && resource.ListResource != null)
                    {
                        var package = FaultTolerance.TryCatchWithLoggingException(
                            () => resource.ListResource.PackageAsync(packageNameLower, config.Prerelease, nugetLogger, cacheContext, CancellationToken.None).GetAwaiter().GetResult(),
                            errorMessage: "Unable to connect to source '{0}'".FormatWith(resource.Source.PackageSource.Source),
                            throwError: false,
                            logWarningInsteadOfError: true);

                        if (!(package is null))
                        {
                            packagesList.Add(package);
                        }
                    }
                    else
                    {
                        var packages = FaultTolerance.TryCatchWithLoggingException(
                            () => resource.PackageMetadataResource.GetMetadataAsync(packageNameLower, config.Prerelease, includeUnlisted: false, sourceCacheContext: cacheContext, log: nugetLogger, token: CancellationToken.None).GetAwaiter().GetResult(),
                            errorMessage: "Unable to connect to source '{0}'".FormatWith(resource.Source.PackageSource.Source),
                            throwError: false,
                            logWarningInsteadOfError: true).OrEmpty();

                        packagesList.AddRange(packages);
                    }
                }
                else
                {
                    var package = FaultTolerance.TryCatchWithLoggingException(
                        () => resource.PackageMetadataResource.GetMetadataAsync(new PackageIdentity(packageNameLower, version), cacheContext, nugetLogger, CancellationToken.None).GetAwaiter().GetResult(),
                        errorMessage: "Unable to connect to source '{0}'".FormatWith(resource.Source.PackageSource.Source),
                        throwError: false,
                        logWarningInsteadOfError: true);

                    if (!(package is null))
                    {
                        packagesList.Add(package);
                    }
                }
            }

            return packagesList.OrderByDescending(p => p.Identity.Version).FirstOrDefault();
        }

        private static IOrderedEnumerable<IPackageSearchMetadata> ApplyPackageSort(IEnumerable<IPackageSearchMetadata> query, domain.PackageOrder orderBy)
        {
            switch (orderBy)
            {
                case domain.PackageOrder.LastPublished:
                    return query
                        .OrderByDescending(q => q.Published)
                        .ThenBy(q => q.Identity.Id)
                        .ThenByDescending(q => q.Identity.Version);

                case domain.PackageOrder.Id:
                    return query.OrderBy(q => q.Identity.Id)
                        .ThenBy(q => q.Title)
                        .ThenByDescending(q => q.Identity.Version);

                case domain.PackageOrder.Popularity:
                    return query
                        .OrderByDescending(q => q.DownloadCount)
                        .ThenByDescending(q => q.VersionDownloadCount)
                        .ThenBy(q => q.Identity.Id);

                case domain.PackageOrder.Title:
                    return query
                        // Fallback to Id if Title is missing
                        .OrderBy(q => q.Title ?? q.Identity.Id)
                        .ThenBy(q => q.Identity.Id)
                        .ThenByDescending(q => q.Identity.Version);

                default:
                    // Since we return an IOrderedEnumerable, some form of ordering must be applied,
                    // even when the user has not explicitly requested a sort order.
                    //
                    // This fallback also applies when the user has explicitly set the package order to 'Unsorted'.
                    // In both cases, we apply a default order to satisfy the contract of the return type.

                    return query.OrderBy(_ => 0);
            }
        }

        private static SearchOrderBy? GetSortOrder(domain.PackageOrder orderBy, bool useMultiVersionOrdering)
        {
            switch (orderBy)
            {
                case domain.PackageOrder.Popularity:
                    if (useMultiVersionOrdering)
                    {
                        return SearchOrderBy.DownloadCountAndVersion;
                    }
                    else
                    {
                        return SearchOrderBy.DownloadCount;
                    }

                case domain.PackageOrder.Id:
                // Ideally, we would order by the package title,
                // but this is not currently supported by the NuGet client libraries.
                // Since ordering by Id typically produces a similar result,
                // we explicitly sort by Id here and defer title-based sorting
                // to the client side later.
                case domain.PackageOrder.Title:
                    if (useMultiVersionOrdering)
                    {
                        return SearchOrderBy.Version;
                    }
                    else
                    {
                        return SearchOrderBy.Id;
                    }

                default:
                    if (orderBy != domain.PackageOrder.Unsorted)
                    {
                        // Inform the user that ordering is performed on the client side,
                        // which may result in inconsistent ordering due to server-side paging.
                        //
                        // Although the user may not explicitly request paging, Chocolatey
                        // performs paging automatically. Since we only receive a limited
                        // subset of packages from the server, client-side sorting may not
                        // produce fully consistent results across pages.
                        //
                        // Ideally, server-side ordering would be supported, but the current
                        // NuGet client libraries do not yet provide this capability.


                        "chocolatey".Log().Warn(
                            @"OrderBy '{0}' is applied on the client side. Because results are paged by the
 server, this may lead to inconsistent ordering.",
                            orderBy);

                        if (useMultiVersionOrdering)
                        {
                            // We will be explicit about ordering by version
                            // in this case. This has to do with historical
                            // reasons to prevent inconsistent results being
                            // returned when a version is specified.
                            return SearchOrderBy.Version;
                        }
                    }

                    // Anything else, we are currently not able to tell the server
                    // what sorting method to use, so let us fall back to either
                    // defaults in the NuGet library, or unsorted on the server side.
                    return null;
            }
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static ISet<IPackageSearchMetadata> find_all_package_versions(string packageName, ChocolateyConfiguration config, ILogger nugetLogger, ChocolateySourceCacheContext cacheContext, IEnumerable<PackageMetadataResource> resources)
            => FindAllPackageVersions(packageName, config, nugetLogger, cacheContext, resources);
#pragma warning restore IDE0022, IDE1006
    }

    public class ComparePackageSearchMetadataIdOnly : IEqualityComparer<IPackageSearchMetadata>
    {
        public bool Equals(IPackageSearchMetadata x, IPackageSearchMetadata y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }
            return x.Identity.Id.Equals(y.Identity.Id, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(IPackageSearchMetadata obj)
        {
            if (obj is null)
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

            if (x is null || y is null)
            {
                return false;
            }

            return x.Identity.Equals(y.Identity);
        }

        public int GetHashCode(IPackageSearchMetadata obj)
        {
            if (obj is null)
            {
                return 0;
            }
            return obj.Identity.GetHashCode();
        }
    }
}
