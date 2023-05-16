// Copyright © 2017 - 2022 Chocolatey Software, Inc
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
    using chocolatey.infrastructure.app.attributes;
    using configuration;
    using domain;
    using results;

    [MultiService]
    [Obsolete("This interface is deprecated and will be removed in v3.")]
    public interface ISourceRunner : IBootstrappableSourceRunner, ICountSourceRunner, IListSourceRunner, IInstallSourceRunner, IUpgradeSourceRunner, IUninstallSourceRunner
    {
#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void ensure_source_app_installed(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> ensureAction);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        int count_run(ChocolateyConfiguration config);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void list_noop(ChocolateyConfiguration config);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        IEnumerable<PackageResult> list_run(ChocolateyConfiguration config);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void install_noop(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeModifyAction);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        ConcurrentDictionary<string, PackageResult> upgrade_noop(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeUpgradeAction = null);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void uninstall_noop(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeUninstallAction = null);
#pragma warning restore IDE1006
    }

    public interface IAlternativeSourceRunner
    {
        /// <summary>
        ///   Gets the source type the source runner implements.
        /// </summary>
        /// <value>
        ///   The type of the source.
        /// </value>
        string SourceType { get; }
    }

    public interface IBootstrappableSourceRunner : IAlternativeSourceRunner
    {
        /// <summary>
        ///   Ensures the application that controls a source is installed.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="ensureAction">The action to continue with as part of the install.</param>
        void EnsureSourceAppInstalled(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> ensureAction);
    }

    public interface ICountSourceRunner : IAlternativeSourceRunner
    {
        /// <summary>
        ///   Retrieve the count of the listed packages from the source.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>Packages count.</returns>
        int Count(ChocolateyConfiguration config);
    }

    public interface IListSourceRunner : IAlternativeSourceRunner
    {
        /// <summary>
        ///   Run list in noop mode.
        /// </summary>
        /// <param name="config">The configuration.</param>
        void ListDryRun(ChocolateyConfiguration config);

        /// <summary>
        ///   Lists for packages from the source feed.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        IEnumerable<PackageResult> List(ChocolateyConfiguration config);
    }

    /// <summary>
    /// This interface is a 'marker' type that indicates that the List action on the current source runner
    /// supports searching for specific packages.
    /// </summary>
    public interface ISearchableSourceRunner : IAlternativeSourceRunner
    {
        /// <summary>
        ///   Run search in noop mode.
        /// </summary>
        /// <param name="config">The configuration.</param>
        void SearchDryRun(ChocolateyConfiguration config);

        /// <summary>
        ///   Searches for packages from the source feed.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        IEnumerable<PackageResult> Search(ChocolateyConfiguration config);
    }

    public interface IInstallSourceRunner : IAlternativeSourceRunner
    {
        /// <summary>
        ///   Run install in noop mode.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with for each noop test install.</param>
        void InstallDryRun(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction);

        /// <summary>
        ///   Installs packages from the source feed.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with when install is successful.</param>
        /// <returns>Results of installs.</returns>
        ConcurrentDictionary<string, PackageResult> Install(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction);

        /// <summary>
        ///   Installs packages from the source feed.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with when install is successful.</param>
        /// <param name="beforeModifyAction">The action (if any) to run on any currently installed package dependencies before triggering the install or updating those dependencies.</param>
        /// <returns>Results of installs.</returns>
        ConcurrentDictionary<string, PackageResult> Install(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeModifyAction);
    }

    public interface IUpgradeSourceRunner : IAlternativeSourceRunner
    {
        /// <summary>
        ///   Run upgrade in noop mode.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with for each noop test upgrade.</param>
        ConcurrentDictionary<string, PackageResult> UpgradeDryRun(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction);

        /// <summary>
        ///   Upgrades packages from NuGet related feeds.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with when upgrade is successful.</param>
        /// <param name="beforeUpgradeAction">The action (if any) to run on any currently installed package or its dependencies before triggering the upgrade.</param>
        /// <returns>Results of upgrades.</returns>
        ConcurrentDictionary<string, PackageResult> Upgrade(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeUpgradeAction = null);
    }

    public interface IUninstallSourceRunner : IAlternativeSourceRunner
    {
        /// <summary>
        ///   Run uninstall in noop mode.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with for each noop test upgrade.</param>
        void UninstallDryRun(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction);

        /// <summary>
        ///   Uninstalls packages from NuGet related feeds.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with when upgrade is successful.</param>
        /// <param name="beforeUninstallAction">The action (if any) to run on any currently installed package before triggering the uninstall.</param>
        /// <returns>Results of uninstalls.</returns>
        ConcurrentDictionary<string, PackageResult> Uninstall(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeUninstallAction = null);
    }
}
