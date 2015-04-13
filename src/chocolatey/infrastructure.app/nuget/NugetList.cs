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
    using System.Collections.Generic;
    using System.Linq;
    using NuGet;
    using configuration;

    // ReSharper disable InconsistentNaming

    public static class NugetList
    {
        public static IEnumerable<IPackage> GetPackages(ChocolateyConfiguration configuration, ILogger nugetLogger)
        {
            var packageRepository = NugetCommon.GetRemoteRepository(configuration, nugetLogger);
            IQueryable<IPackage> results = packageRepository.Search(configuration.Input, configuration.Prerelease);

            if (configuration.AllVersions)
            {
                // just trust the server order, don't sort because that's a blocking operation
                return results.Where(PackageExtensions.IsListed);
            }

            if (configuration.Prerelease && packageRepository.SupportsPrereleasePackages)
            {
                results = results.Where(p => p.IsAbsoluteLatestVersion);
            }
            else
            {
                results = results.Where(p => p.IsLatestVersion);
            }

            // just trust the server order, don't sort because that's a blocking operation
            // also don't worry about multiple versions, considering Is*LatestVersion only applies to one
            return results.Where(PackageExtensions.IsListed)
                          .Where(p => configuration.Prerelease || p.IsReleaseVersion());
                       // .OrderBy(p => p.Id).ThenByDescending(p => p.Version)
                       // .GroupBy(p => p.Id).Select(g => g.First());
        }
    }

    // ReSharper restore InconsistentNaming
}