﻿// Copyright © 2017 - 2025 Chocolatey Software, Inc
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

using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.domain;
using chocolatey.infrastructure.results;
using System;

namespace chocolatey.infrastructure.app.services
{
    /// <summary>
    /// The automagic uninstaller service
    /// </summary>
    public interface IAutomaticUninstallerService
    {
        /// <summary>
        /// Runs to remove an application from the registry.
        /// </summary>
        /// <param name="packageResult">The package result.</param>
        /// <param name="config">The configuration.</param>
        void Run(PackageResult packageResult, ChocolateyConfiguration config);

        /// <summary>
        /// Removes one app (registry value) based on config and records any messaging in a package result.
        /// </summary>
        /// <param name="key">The registry key to remove.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="packageResult">The package result.</param>
        /// <param name="packageCacheLocation">The package cache location.</param>
        void Remove(RegistryApplicationKey key, ChocolateyConfiguration config, PackageResult packageResult, string packageCacheLocation);

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void run(PackageResult packageResult, ChocolateyConfiguration config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void remove(RegistryApplicationKey key, ChocolateyConfiguration config, PackageResult packageResult, string packageCacheLocation);
#pragma warning restore IDE0022, IDE1006
    }
}
