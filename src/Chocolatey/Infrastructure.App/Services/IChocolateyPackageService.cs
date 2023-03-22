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

namespace Chocolatey.Infrastructure.App.Services
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Configuration;
    using Results;

    /// <summary>
    ///   The packaging service
    /// </summary>
    public interface IChocolateyPackageService
    {

        /// <summary>
        ///   Ensures the application that controls a source is installed
        /// </summary>
        /// <param name="config">The configuration.</param>
        void EnsureSourceAppInstalled(ChocolateyConfiguration config);

        /// <summary>
        ///   Retrieves the count of items that meet the search criteria.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        int Count(ChocolateyConfiguration config);

        /// <summary>
        ///   Run list in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        void ListDryRun(ChocolateyConfiguration config);

        /// <summary>
        ///   Lists/searches for packages that meet a search criteria
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        IEnumerable<PackageResult> List(ChocolateyConfiguration config);

        /// <summary>
        ///   Run pack in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        void PackDryRun(ChocolateyConfiguration config);

        /// <summary>
        ///   Compiles a package
        /// </summary>
        /// <param name="config">The configuration.</param>
        void Pack(ChocolateyConfiguration config);

        /// <summary>
        ///   Run push in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        void PushDryRun(ChocolateyConfiguration config);

        /// <summary>
        ///   Pushes packages to remote feeds.
        /// </summary>
        /// <param name="config">The configuration.</param>
        void Push(ChocolateyConfiguration config);

        /// <summary>
        ///   Run install in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        void InstallDryRun(ChocolateyConfiguration config);

        /// <summary>
        ///   Installs packages
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>results of installs</returns>
        ConcurrentDictionary<string, PackageResult> Install(ChocolateyConfiguration config);

        /// <summary>
        ///  Run outdated in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        void OutdatedDryRun(ChocolateyConfiguration config);

        /// <summary>
        /// Determines all packages that are out of date
        /// </summary>
        /// <param name="config">The configuration.</param>
        void Outdated(ChocolateyConfiguration config);

        /// <summary>
        ///   Run upgrade in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        void UpgradeDryRun(ChocolateyConfiguration config);

        /// <summary>
        ///   Upgrades packages
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>results of upgrades</returns>
        ConcurrentDictionary<string, PackageResult> Upgrade(ChocolateyConfiguration config);

        /// <summary>
        ///   Run uninstall in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        void UninstallDryRun(ChocolateyConfiguration config);

        /// <summary>
        ///   Uninstalls packages
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>results of upgrades</returns>
        ConcurrentDictionary<string, PackageResult> Uninstall(ChocolateyConfiguration config);
    }
}
