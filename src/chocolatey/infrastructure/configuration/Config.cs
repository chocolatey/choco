﻿// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

using System;
using System.ComponentModel;
using chocolatey.infrastructure.app.configuration;

namespace chocolatey.infrastructure.configuration
{
    /// <summary>
    ///   Configuration initialization
    /// </summary>
    public sealed class Config
    {
        private static ChocolateyConfiguration _configuration = new ChocolateyConfiguration();

        /// <summary>
        ///   Initializes application configuration with a configuration instance.
        ///   DO NOT USE with API. Use `GetChocolatey` methods - accessing this directly in API can cause very bad side effects.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void InitializeWith(ChocolateyConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        ///   Gets the configuration settings.
        ///   DO NOT USE with API. Use `GetChocolatey` methods - accessing this directly in API can cause very bad side effects.
        /// </summary>
        /// <returns>
        ///   An instance of <see cref="ChocolateyConfiguration" /> if one has been initialized; defaults to new instance of
        ///   <see
        ///     cref="ChocolateyConfiguration" />
        ///   if one has not been.
        /// </returns>
        public static ChocolateyConfiguration GetConfigurationSettings()
        {
            return _configuration;
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static void initialize_with(ChocolateyConfiguration configuration)
            => InitializeWith(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static ChocolateyConfiguration get_configuration_settings()
            => GetConfigurationSettings();
#pragma warning restore IDE0022, IDE1006
    }
}
