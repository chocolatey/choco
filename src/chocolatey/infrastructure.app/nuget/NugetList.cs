// Copyright © 2011 - Present RealDimensions Software, LLC
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
            var packageRepository = NugetCommon.GetRemoteRepository(configuration, nugetLogger);

            // Whether or not the package is remote determines two things:
            // 1. Does the repository have a notion of "listed"?
            // 2. Does it support prerelease in a straight-forward way?
            // Choco previously dealt with this by taking the path of least resistance and manually filtering out and sort unwanted packages
            // This result in blocking operations that didn't let service based repositories, like OData, take care of heavy lifting on the server.
            bool isRemote;
            var aggregateRepo = packageRepository as AggregateRepository;
            if (aggregateRepo != null)
            {
                isRemote = aggregateRepo.Repositories.All(repo => repo is IServiceBasedRepository);
            }
            else
            {
                isRemote = packageRepository is IServiceBasedRepository;
            }

            IQueryable<IPackage> results = packageRepository.Search(configuration.Input, configuration.Prerelease);


            if (configuration.ListCommand.Page.HasValue)
            {
                results = results.Skip(configuration.ListCommand.PageSize * configuration.ListCommand.Page.Value).Take(configuration.ListCommand.PageSize);
            }

            if (configuration.ListCommand.Exact)
            {
                results = results.Where(p => p.Id == configuration.Input);
            }

            if (configuration.ListCommand.ByIdOnly)
            {
                results = isRemote ?
                    results.Where(p => p.Id.Contains(configuration.Input))
                  : results.Where(p => p.Id.contains(configuration.Input, StringComparison.OrdinalIgnoreCase));
            }

            if (configuration.ListCommand.IdStartsWith)
            {
                results = isRemote ?
                    results.Where(p => p.Id.StartsWith(configuration.Input))
                  : results.Where(p => p.Id.StartsWith(configuration.Input, StringComparison.OrdinalIgnoreCase));
            }

            if (configuration.AllVersions)
            {
                if (isRemote)
                {
                    return results.OrderBy(p => p.Id);
                }
                else
                {
                    return results.Where(PackageExtensions.IsListed).OrderBy(p => p.Id).AsQueryable();
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

            if (!isRemote)
            {
                results =
                    results
                        .Where(PackageExtensions.IsListed)
                        .Where(p => configuration.Prerelease || p.IsReleaseVersion())
                        .distinct_last(PackageEqualityComparer.Id, PackageComparer.Version)
                        .AsQueryable();
            }

            return results.OrderBy(p => p.Id);
        } 


    }

    // ReSharper restore InconsistentNaming
}