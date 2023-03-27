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

namespace chocolatey.infrastructure.app.services
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using configuration;
    using results;

    public interface INugetService : ISourceRunner
    {
        /// <summary>
        ///   Get outdated packages
        /// </summary>
        /// <param name="config">The configuration.</param>
        ConcurrentDictionary<string, PackageResult> GetOutdated(ChocolateyConfiguration config);

        /// <summary>
        ///   Run pack in noop mode.
        /// </summary>
        /// <param name="config">The configuration.</param>
        void PackDryRun(ChocolateyConfiguration config);

        /// <summary>
        ///   Packages up a nuspec into a compiled nupkg.
        /// </summary>
        /// <param name="config">The configuration.</param>
        void Pack(ChocolateyConfiguration config);

        /// <summary>
        ///   Push_noops the specified configuration.
        /// </summary>
        /// <param name="config">The configuration.</param>
        void PushDryRun(ChocolateyConfiguration config);

        /// <summary>
        ///   Push_runs the specified configuration.
        /// </summary>
        /// <param name="config">The configuration.</param>
        void Push(ChocolateyConfiguration config);

        /// <summary>
        ///   Remove the rollback directory for a package if it exists
        /// </summary>
        /// <param name="packageName">Name of the package.</param>
        void EnsureBackupDirectoryRemoved(string packageName);


        /// <summary>
        ///   Get all installed packages
        /// </summary>
        /// <param name="config">The configuration</param>
        IEnumerable<PackageResult> GetInstalledPackages(ChocolateyConfiguration config);
    }
}
