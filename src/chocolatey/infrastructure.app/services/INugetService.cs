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

namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Collections.Concurrent;
    using configuration;
    using results;

    public interface INugetService
    {
        /// <summary>
        ///   Run list in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        void list_noop(ChocolateyConfiguration config);

        /// <summary>
        ///   Lists/searches for pacakge against nuget related feeds.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="logResults">Should results be logged?</param>
        /// <returns></returns>
        ConcurrentDictionary<string, PackageResult> list_run(ChocolateyConfiguration config, bool logResults);

        /// <summary>
        ///   Run pack in noop mode.
        /// </summary>
        /// <param name="config">The configuration.</param>
        void pack_noop(ChocolateyConfiguration config);

        /// <summary>
        ///   Packages up a nuspec into a compiled nupkg.
        /// </summary>
        /// <param name="config">The configuration.</param>
        void pack_run(ChocolateyConfiguration config);

        /// <summary>
        ///   Push_noops the specified configuration.
        /// </summary>
        /// <param name="config">The configuration.</param>
        void push_noop(ChocolateyConfiguration config);

        /// <summary>
        ///   Push_runs the specified configuration.
        /// </summary>
        /// <param name="config">The configuration.</param>
        void push_run(ChocolateyConfiguration config);

        /// <summary>
        ///   Run install in noop mode
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with for each noop test install.</param>
        void install_noop(ChocolateyConfiguration config, Action<PackageResult> continueAction);

        /// <summary>
        ///   Installs packages from NuGet related feeds
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
        void upgrade_noop(ChocolateyConfiguration config, Action<PackageResult> continueAction);

        /// <summary>
        ///   Upgrades packages from NuGet related feeds
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="continueAction">The action to continue with when upgrade is successful.</param>
        /// <returns>results of installs</returns>
        ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config, Action<PackageResult> continueAction);

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
        /// <returns>results of installs</returns>
        ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config, Action<PackageResult> continueAction);
    }
}