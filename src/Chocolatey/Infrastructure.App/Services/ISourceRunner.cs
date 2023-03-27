﻿// Copyright © 2017 - 2022 Chocolatey Software, Inc
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
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Chocolatey.Infrastructure.App.Attributes;
    using Configuration;
    using Domain;
    using Results;

    [MultiService]
    public interface ISourceRunner
    {
        /// <summary>
        ///   Gets the source type the source runner implements
        /// </summary>
        /// <value>
        ///   The type of the source.
        /// </value>
        string SourceType { get; }

        /// <summary>
        ///   Ensures the application that controls a source is installed
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="ensureAction">The action to continue with as part of the install</param>
        void EnsureSourceAppInstalled(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> ensureAction);

        /// <summary>
        ///     Retrieve the listed packages from the source feed cout
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>Packages count</returns>
        int Count(ChocolateyConfiguration config);

        /// <summary>
        ///   Run list in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        void ListDryRun(ChocolateyConfiguration config);

        /// <summary>
        ///   Lists/searches for packages from the source feed
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        IEnumerable<PackageResult> List(ChocolateyConfiguration config);

        /// <summary>
        ///   Run install in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with for each noop test install.</param>
        void InstallDryRun(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction);

        /// <summary>
        ///   Installs packages from the source feed
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with when install is successful.</param>
        /// <returns>results of installs</returns>
        ConcurrentDictionary<string, PackageResult> Install(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction);

        /// <summary>
        ///   Installs packages from the source feed
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with when install is successful.</param>
        /// <param name="beforeModifyAction">The action (if any) to run on any currently installed package dependencies before triggering the install or updating those dependencies.</param>
        /// <returns>results of installs</returns>
        ConcurrentDictionary<string, PackageResult> Install(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeModifyAction);

        /// <summary>
        ///   Run upgrade in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with for each noop test upgrade.</param>
        ConcurrentDictionary<string, PackageResult> UpgradeDryRun(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction);

        /// <summary>
        ///   Upgrades packages from NuGet related feeds
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with when upgrade is successful.</param>
        /// <param name="beforeUpgradeAction">The action (if any) to run on any currently installed package or its dependencies before triggering the upgrade.</param>
        /// <returns>results of installs</returns>
        ConcurrentDictionary<string, PackageResult> Upgrade(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeUpgradeAction = null);

        /// <summary>
        ///   Run uninstall in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with for each noop test upgrade.</param>
        void UninstallDryRun(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction);

        /// <summary>
        ///   Uninstalls packages from NuGet related feeds
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with when upgrade is successful.</param>
        /// <param name="beforeUninstallAction">The action (if any) to run on any currently installed package before triggering the uninstall.</param>
        /// <returns>results of installs</returns>
        ConcurrentDictionary<string, PackageResult> Uninstall(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeUninstallAction = null);
    }
}
