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

namespace chocolatey.infrastructure.app.services
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using configuration;
    using results;

    /// <summary>
    ///   The packaging service
    /// </summary>
    public interface IChocolateyPackageService
    {

        /// <summary>
        ///   Ensures the application that controls a source is installed
        /// </summary>
        /// <param name="config">The configuration.</param>
        void ensure_source_app_installed(ChocolateyConfiguration config);

        /// <summary>
        ///   Retrieves the count of items that meet the search criteria.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        int count_run(ChocolateyConfiguration config);

        /// <summary>
        ///   Run list in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        void list_noop(ChocolateyConfiguration config);

        /// <summary>
        ///   Lists/searches for packages that meet a search criteria
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        IEnumerable<PackageResult> list_run(ChocolateyConfiguration config);

        /// <summary>
        ///   Run pack in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        void pack_noop(ChocolateyConfiguration config);

        /// <summary>
        ///   Compiles a package
        /// </summary>
        /// <param name="config">The configuration.</param>
        void pack_run(ChocolateyConfiguration config);

        /// <summary>
        ///   Run push in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        void push_noop(ChocolateyConfiguration config);

        /// <summary>
        ///   Pushes packages to remote feeds.
        /// </summary>
        /// <param name="config">The configuration.</param>
        void push_run(ChocolateyConfiguration config);

        /// <summary>
        ///   Run install in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        void install_noop(ChocolateyConfiguration config);

        /// <summary>
        ///   Installs packages
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>results of installs</returns>
        ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config);

        /// <summary>
        ///  Run outdated in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        void outdated_noop(ChocolateyConfiguration config);

        /// <summary>
        /// Determines all packages that are out of date
        /// </summary>
        /// <param name="config">The configuration.</param>
        void outdated_run(ChocolateyConfiguration config);

        /// <summary>
        ///   Run upgrade in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        void upgrade_noop(ChocolateyConfiguration config);

        /// <summary>
        ///   Upgrades packages
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>results of upgrades</returns>
        ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config);

        /// <summary>
        ///   Run uninstall in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        void uninstall_noop(ChocolateyConfiguration config);

        /// <summary>
        ///   Uninstalls packages
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>results of upgrades</returns>
        ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config);
    }
}