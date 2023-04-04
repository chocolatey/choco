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
    using configuration;
    using results;
    using System;

    public interface IShimGenerationService
    {
        /// <summary>
        ///   Installs shims for the package
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="packageResult">The package result.</param>
        void Install(ChocolateyConfiguration configuration, PackageResult packageResult);

        /// <summary>
        ///   Uninstalls shims for the package
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="packageResult">The package result.</param>
        void Uninstall(ChocolateyConfiguration configuration, PackageResult packageResult);

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void install(ChocolateyConfiguration configuration, PackageResult packageResult);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void uninstall(ChocolateyConfiguration configuration, PackageResult packageResult);
#pragma warning restore IDE1006
    }
}
