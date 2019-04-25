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
    using System.Linq;

    using NuGet;
    using configuration;
    using chocolatey.infrastructure.app.domain;

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

            if (configuration.ListCommand.Exact)
            {
                results = packageRepository.FindPackagesById(searchTermLower).AsQueryable();
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


            switch (configuration.ListCommand.OrderBy)
            {
                case PackageOrder.name:
                    results = results.OrderBy(p => p.Id);
                    break;

                case PackageOrder.title:
                    results = results.OrderBy(p => p.Title).ThenBy(p => p.Id);
                    break;

                case PackageOrder.popularity:
                    results = results.OrderByDescending(p => p.DownloadCount).ThenBy(p => p.Id);
                    break;

                case PackageOrder.lastpublished:
                    results = results.OrderByDescending(p => p.Published).ThenBy(p => p.Id);
                    break;

                case PackageOrder.unsorted:
                    break;

                default:
                    break;
            }

            return results;
        } 


    }

    // ReSharper restore InconsistentNaming
}