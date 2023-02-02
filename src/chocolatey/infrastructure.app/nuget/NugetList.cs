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

    // ReSharper disable InconsistentNaming

    public static class NugetList
    {
        public static IEnumerable<IPackageSearchMetadata> GetPackages(ChocolateyConfiguration configuration, ILogger nugetLogger, IFileSystem filesystem)
        {
            return execute_package_search(configuration, nugetLogger, filesystem).GetAwaiter().GetResult();
        }

        public static int GetCount(ChocolateyConfiguration configuration, ILogger nugetLogger, IFileSystem filesystem)
        {
            var packageRepositories = NugetCommon.GetRemoteRepositories(configuration, nugetLogger, filesystem);
            var packageRepositoriesResources = NugetCommon.GetRepositoryResources(packageRepositories);
            string searchTermLower = configuration.Input.to_lower();

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

        private async static Task<IQueryable<IPackageSearchMetadata>> execute_package_search(ChocolateyConfiguration configuration, ILogger nugetLogger, IFileSystem filesystem)
        {
            var packageRepositories = NugetCommon.GetRemoteRepositories(configuration, nugetLogger, filesystem);
            var packageRepositoriesResources = NugetCommon.GetRepositoryResources(packageRepositories);
            string searchTermLower = configuration.Input.to_lower();

            SearchFilter searchFilter = new SearchFilter(configuration.Prerelease);
            searchFilter.IncludeDelisted = configuration.ListCommand.LocalOnly;
            searchFilter.OrderBy = SearchOrderBy.Id;

            if (configuration.ListCommand.OrderByPopularity)
            {
                searchFilter.OrderBy = SearchOrderBy.DownloadCount;
            }

            var cacheContext = new ChocolateySourceCacheContext(configuration);

            NuGetVersion version = !string.IsNullOrWhiteSpace(configuration.Version) ? NuGetVersion.Parse(configuration.Version) : null;
            var results = new HashSet<IPackageSearchMetadata>(new ComparePackageSearchMetadata());

            // TODO: maybe adjust number if no page specified? Or adjust check to have no max value
            var totalToGet = configuration.ListCommand.Page.HasValue ? (configuration.ListCommand.Page + 1) * configuration.ListCommand.PageSize : 100000;

            if (!configuration.ListCommand.Exact)
            {
                foreach (var repositoryResources in packageRepositoriesResources)
                {
                    if (repositoryResources.searchResource != null)
                    {
                        var skipNumber = 0;
                        var takeNumber = 30;

                        var partResults = new HashSet<IPackageSearchMetadata>(new ComparePackageSearchMetadataIdOnly());
                        var latestResults = new List<IPackageSearchMetadata>();

                        if (configuration.ListCommand.Page.HasValue)
                        {
                            skipNumber = configuration.ListCommand.PageSize * configuration.ListCommand.Page.GetValueOrDefault(0);
                            takeNumber = configuration.ListCommand.PageSize;

                            partResults.AddRange(await repositoryResources.searchResource.SearchAsync(searchTermLower, searchFilter, skipNumber, takeNumber, nugetLogger, CancellationToken.None));
                            latestResults.AddRange(partResults);
                        }
                        else
                        {
                            do
                            {
                                partResults.Clear();
                                partResults.AddRange(await repositoryResources.searchResource.SearchAsync(searchTermLower, searchFilter, skipNumber, takeNumber, nugetLogger, CancellationToken.None));
                                skipNumber += takeNumber;
                                latestResults.AddRange(partResults);
                            } while (partResults.Count >= takeNumber && skipNumber < totalToGet);
                        }

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
                        var tempResults = await repositoryResources.listResource.ListAsync(searchTermLower, configuration.Prerelease, configuration.AllVersions, false, nugetLogger, CancellationToken.None);
                        var enumerator = tempResults.GetEnumeratorAsync();

                        while (await enumerator.MoveNextAsync())
                        {
                            results.Add(enumerator.Current);
                            if (results.Count > totalToGet)
                            {
                                break;
                            }
                        }
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
                    if (version == null)
                    {
                        var versions = new SortedSet<NuGetVersion>(VersionComparer.Default);
                        foreach (var repositoryResources in packageRepositoriesResources)
                        {
                            //We want all versions available across all repositories
                            versions.AddRange((await repositoryResources.findPackageByIdResource.GetAllVersionsAsync(searchTermLower, cacheContext, nugetLogger, CancellationToken.None))
                                .Where(a => configuration.Prerelease || !a.IsPrerelease));
                        }
                        version = versions.Max();
                        if (version == null) return new List<IPackageSearchMetadata>().AsQueryable();
                    }

                    var exactPackage = find_package(searchTermLower, configuration, nugetLogger, cacheContext, packageRepositoriesResources.Select(x => x.packageMetadataResource), version);

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

            if (configuration.ListCommand.ByIdOnly)
            {
                results = results.Where(p => p.Identity.Id.ToLower().Contains(searchTermLower)).ToHashSet();
            }

            if (configuration.ListCommand.ByTagOnly)
            {
                results = results.Where(p => p.Tags.contains(searchTermLower, StringComparison.InvariantCultureIgnoreCase)).ToHashSet();
            }

            if (configuration.ListCommand.IdStartsWith)
            {
                results = results.Where(p => p.Identity.Id.ToLower().StartsWith(searchTermLower)).ToHashSet();
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

        public static ISet<IPackageSearchMetadata> find_all_package_versions(string packageName, ChocolateyConfiguration config, ILogger nugetLogger, ChocolateySourceCacheContext cacheContext, IEnumerable<PackageMetadataResource> resources)
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
        /// <param name="nugetLogger">fill me in</param>
        /// <param name="resources">fill me in</param>
        /// <param name="version">Version to search for</param>
        /// <param name="cacheContext">Settings for caching of results from sources</param>
        /// <returns>One result or nothing</returns>
        public static IPackageSearchMetadata find_package(string packageName, ChocolateyConfiguration config, ILogger nugetLogger, ChocolateySourceCacheContext cacheContext, IEnumerable<PackageMetadataResource> resources, NuGetVersion version = null)
        {
            if (version is null)
            {
                var metadataList = new HashSet<IPackageSearchMetadata>();
                foreach (var resource in resources)
                {
                    metadataList.AddRange(resource.GetMetadataAsync(packageName, config.Prerelease, false, cacheContext, nugetLogger, CancellationToken.None).GetAwaiter().GetResult());
                }
                return metadataList.OrderByDescending(p => p.Identity.Version).FirstOrDefault();
            }

            foreach (var resource in resources)
            {
                var metadata = resource.GetMetadataAsync(new PackageIdentity(packageName, version), cacheContext, nugetLogger, CancellationToken.None).GetAwaiter().GetResult();
                if (metadata != null)
                {
                    return metadata;
                }
            }

            //If no packages found, then return nothing
            return null;

            /*
            // use old method when newer method causes issues
            if (!config.Features.UsePackageRepositoryOptimizations) return repository.FindPackage(packageName, version, config.Prerelease, allowUnlisted: false);

            packageName = packageName.to_string().ToLower(CultureInfo.CurrentCulture);
            // find the package based on version using older method
            if (version != null) return repository.FindPackage(packageName, version, config.Prerelease, allowUnlisted: false);

            // we should always be using an aggregate repository
            var aggregateRepository = repository as AggregateRepository;
            if (aggregateRepository != null)
            {
                var packageResults = new List<IPackage>();

                foreach (var packageRepository in aggregateRepository.Repositories.or_empty_list_if_null())
                {
                    try
                    {
                        "chocolatey".Log().Debug("Using '" + packageRepository.Source + "'.");
                        "chocolatey".Log().Debug("- Supports prereleases? '" + packageRepository.SupportsPrereleasePackages + "'.");
                        "chocolatey".Log().Debug("- Is ServiceBased? '" + (packageRepository is IServiceBasedRepository) + "'.");

                        // search based on lower case id - similar to PackageRepositoryExtensions.FindPackagesByIdCore()
                        IQueryable<IPackage> combinedResults = packageRepository.GetPackages().Where(x => x.Id.ToLower() == packageName);

                        if (config.Prerelease && packageRepository.SupportsPrereleasePackages)
                        {
                            combinedResults = combinedResults.Where(p => p.IsAbsoluteLatestVersion);
                        }
                        else
                        {
                            combinedResults = combinedResults.Where(p => p.IsLatestVersion);
                        }

                        if (!(packageRepository is IServiceBasedRepository))
                        {
                            combinedResults = combinedResults
                                .Where(PackageExtensions.IsListed)
                                .Where(p => config.Prerelease || p.IsReleaseVersion())
                                .distinct_last(PackageEqualityComparer.Id, PackageComparer.Version)
                                .AsQueryable();
                        }

                        var packageRepositoryResults = combinedResults.ToList();
                        if (packageRepositoryResults.Count() != 0)
                        {
                            "chocolatey".Log().Debug("Package '{0}' found on source '{1}'".format_with(packageName, packageRepository.Source));
                            packageResults.AddRange(packageRepositoryResults);
                        }
                    }
                    catch (Exception e)
                    {
                        "chocolatey".Log().Warn("Error retrieving packages from source '{0}':{1} {2}".format_with(packageRepository.Source, Environment.NewLine, e.Message));
                    }
                }

                // get only one result, should be the latest - similar to TryFindLatestPackageById
                return packageResults.OrderByDescending(x => x.Version).FirstOrDefault();
            }

            // search based on lower case id - similar to PackageRepositoryExtensions.FindPackagesByIdCore()
            IQueryable<IPackage> results = repository.GetPackages().Where(x => x.Id.ToLower() == packageName);

            if (config.Prerelease && repository.SupportsPrereleasePackages)
            {
                results = results.Where(p => p.IsAbsoluteLatestVersion);
            }
            else
            {
                results = results.Where(p => p.IsLatestVersion);
            }

            if (!(repository is IServiceBasedRepository))
            {
                results = results
                    .Where(PackageExtensions.IsListed)
                    .Where(p => config.Prerelease || p.IsReleaseVersion())
                    .distinct_last(PackageEqualityComparer.Id, PackageComparer.Version)
                    .AsQueryable();
            }

            // get only one result, should be the latest - similar to TryFindLatestPackageById
            return results.ToList().OrderByDescending(x => x.Version).FirstOrDefault();
            */
        }
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


    // ReSharper restore InconsistentNaming
}
