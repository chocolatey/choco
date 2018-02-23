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
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using configuration;
    using domain;
    using results;

    public interface ISourceRunner
    {
        /// <summary>
        ///   Gets the source type the source runner implements
        /// </summary>
        /// <value>
        ///   The type of the source.
        /// </value>
        SourceType SourceType { get; }

        /// <summary>
        ///   Ensures the application that controls a source is installed
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="ensureAction">The action to continue with as part of the install</param>
        void ensure_source_app_installed(ChocolateyConfiguration config, Action<PackageResult> ensureAction);

        /// <summary>
        ///     Retrieve the listed packages from the source feed cout
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>Packages count</returns>
        int count_run(ChocolateyConfiguration config);

        /// <summary>
        ///   Run list in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        void list_noop(ChocolateyConfiguration config);

        /// <summary>
        ///   Lists/searches for packages from the source feed
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        IEnumerable<PackageResult> list_run(ChocolateyConfiguration config);

        /// <summary>
        ///   Run install in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with for each noop test install.</param>
        void install_noop(ChocolateyConfiguration config, Action<PackageResult> continueAction);

        /// <summary>
        ///   Installs packages from the source feed
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with when install is successful.</param>
        /// <returns>results of installs</returns>
        ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config, Action<PackageResult> continueAction);

        /// <summary>
        ///   Run upgrade in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with for each noop test upgrade.</param>
        ConcurrentDictionary<string, PackageResult> upgrade_noop(ChocolateyConfiguration config, Action<PackageResult> continueAction);

        /// <summary>
        ///   Upgrades packages from NuGet related feeds
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with when upgrade is successful.</param>
        /// <param name="beforeUpgradeAction">The action (if any) to run on any currently installed package before triggering the upgrade.</param>
        /// <returns>results of installs</returns>
        ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config, Action<PackageResult> continueAction, Action<PackageResult> beforeUpgradeAction = null);

        /// <summary>
        ///   Run uninstall in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with for each noop test upgrade.</param>
        void uninstall_noop(ChocolateyConfiguration config, Action<PackageResult> continueAction);

        /// <summary>
        ///   Uninstalls packages from NuGet related feeds
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with when upgrade is successful.</param>
        /// <param name="beforeUninstallAction">The action (if any) to run on any currently installed package before triggering the uninstall.</param>
        /// <returns>results of installs</returns>
        ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config, Action<PackageResult> continueAction, Action<PackageResult> beforeUninstallAction = null);
    }
}