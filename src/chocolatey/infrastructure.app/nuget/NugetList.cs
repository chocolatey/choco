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
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using NuGet;
    using configuration;

    // ReSharper disable InconsistentNaming

    public static class NugetList
    {
        public static IEnumerable<IPackage> GetPackages(ChocolateyConfiguration configuration, ILogger nugetLogger)
        {
            return execute_package_search(configuration, nugetLogger);
        }

        public static int GetCount(ChocolateyConfiguration configuration, ILogger nugetLogger)
        {
            return execute_package_search(configuration, nugetLogger).Count();
        }

        private static IQueryable<IPackage> execute_package_search(ChocolateyConfiguration configuration, ILogger nugetLogger)
        {
            var packageRepository = NugetCommon.GetRemoteRepository(configuration, nugetLogger, new PackageDownloader());
            var searchTermLower = configuration.Input.to_lower();

            // Whether or not the package is remote determines two things:
            // 1. Does the repository have a notion of "listed"?
            // 2. Does it support prerelease in a straight-forward way?
            // Choco previously dealt with this by taking the path of least resistance and manually filtering out and sort unwanted packages
            // This result in blocking operations that didn't let service based repositories, like OData, take care of heavy lifting on the server.
            bool isServiceBased;
            var aggregateRepo = packageRepository as AggregateRepository;
            if (aggregateRepo != null)
            {
                isServiceBased = aggregateRepo.Repositories.All(repo => repo is IServiceBasedRepository);
            }
            else
            {
                isServiceBased = packageRepository is IServiceBasedRepository;
            }

            IQueryable<IPackage> results = packageRepository.Search(searchTermLower, configuration.Prerelease);

            SemanticVersion version = !string.IsNullOrWhiteSpace(configuration.Version) ? new SemanticVersion(configuration.Version) : null;

            if (configuration.ListCommand.Exact)
            {
                if (configuration.AllVersions)
                {
                    // convert from a search to getting packages by id.
                    // search based on lower case id - similar to PackageRepositoryExtensions.FindPackagesByIdCore()
                    results = packageRepository.GetPackages().Where(x => x.Id.ToLower() == searchTermLower);
                }
                else
                {
                    var exactPackage = find_package(searchTermLower, version, configuration, packageRepository);

                    if (exactPackage == null) return new List<IPackage>().AsQueryable();

                    return new List<IPackage>()
                    {
                        exactPackage
                    }.AsQueryable();
                }
            }

            if (configuration.ListCommand.Page.HasValue)
            {
                results = results.Skip(configuration.ListCommand.PageSize * configuration.ListCommand.Page.Value).Take(configuration.ListCommand.PageSize);
            }

            if (configuration.ListCommand.ByIdOnly)
            {
                results = isServiceBased ?
                    results.Where(p => p.Id.ToLower().Contains(searchTermLower))
                  : results.Where(p => p.Id.contains(searchTermLower, StringComparison.OrdinalIgnoreCase));
            }

            if (configuration.ListCommand.ByTagOnly)
            {
                results = isServiceBased
                    ? results.Where(p => p.Tags.Contains(searchTermLower))
                    : results.Where(p => p.Tags.contains(searchTermLower, StringComparison.InvariantCultureIgnoreCase));
            }

            if (configuration.ListCommand.IdStartsWith)
            {
                results = isServiceBased ?
                    results.Where(p => p.Id.ToLower().StartsWith(searchTermLower))
                  : results.Where(p => p.Id.StartsWith(searchTermLower, StringComparison.OrdinalIgnoreCase));
            }

            if (configuration.ListCommand.ApprovedOnly)
            {
                results = results.Where(p => p.IsApproved);
            }

            if (configuration.ListCommand.DownloadCacheAvailable)
            {
                results = results.Where(p => p.IsDownloadCacheAvailable);
            }

            if (configuration.ListCommand.NotBroken)
            {
                results = results.Where(p => (p.IsDownloadCacheAvailable && configuration.Information.IsLicensedVersion) || p.PackageTestResultStatus != "Failing");
            }

            if (configuration.AllVersions || !string.IsNullOrWhiteSpace(configuration.Version))
            {
                if (isServiceBased)
                {
                    return results.OrderBy(p => p.Id).ThenByDescending(p => p.Version);
                }
                else
                {
                    return results.Where(PackageExtensions.IsListed).OrderBy(p => p.Id).ThenByDescending(p => p.Version).AsQueryable();
                }
            }

            if (configuration.Prerelease && packageRepository.SupportsPrereleasePackages)
            {
                results = results.Where(p => p.IsAbsoluteLatestVersion);
            }
            else
            {
                results = results.Where(p => p.IsLatestVersion);
            }

            if (!isServiceBased)
            {
                results =
                    results
                        .Where(PackageExtensions.IsListed)
                        .Where(p => configuration.Prerelease || p.IsReleaseVersion())
                        .distinct_last(PackageEqualityComparer.Id, PackageComparer.Version)
                        .AsQueryable();
            }

            results = configuration.ListCommand.OrderByPopularity ?
                 results.OrderByDescending(p => p.DownloadCount).ThenBy(p => p.Id)
                 : results;

            return results;
        }

        /// <summary>
        ///   Searches for packages that are available based on name and other options
        /// </summary>
        /// <param name="packageName">Name of package to search for</param>
        /// <param name="version">Optional version to search for</param>
        /// <param name="config">Chocolatey configuration used to help supply the search parameters</param>
        /// <param name="repository">Repository (aggregate for multiple) to search in</param>
        /// <returns>One result or nothing</returns>
        public static IPackage find_package(string packageName, SemanticVersion version, ChocolateyConfiguration config, IPackageRepository repository)
        {
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
        }

    }

    // ReSharper restore InconsistentNaming
}