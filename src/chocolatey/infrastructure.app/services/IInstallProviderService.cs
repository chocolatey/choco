﻿// Copyright © 2011 - Present RealDimensions Software, LLC
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
    using configuration;
    using results;

    public interface IInstallProviderService
    {
        /// <summary>
        ///   Noops the specified package install.
        /// </summary>
        /// <param name="packageResult">The package result.</param>
        void install_noop(PackageResult packageResult);

        /// <summary>
        ///   Installs the specified package.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="packageResult">The package result.</param>
        /// <returns>true if the chocolateyInstall.ps1 was found, even if it has failures</returns>
        bool install(ChocolateyConfiguration configuration, PackageResult packageResult);

        /// <summary>
        ///   Noops the specified package uninstall.
        /// </summary>
        /// <param name="packageResult">The package result.</param>
        void uninstall_noop(PackageResult packageResult);

        /// <summary>
        ///   Uninstalls the specified package.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="packageResult">The package result.</param>
        /// <returns>true if the chocolateyUninstall.ps1 was found, even if it has failures</returns>
        bool uninstall(ChocolateyConfiguration configuration, PackageResult packageResult);

        bool can_be_used(ChocolateyConfiguration configuration);
    }
}