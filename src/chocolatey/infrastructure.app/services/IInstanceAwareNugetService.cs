// Copyright (c) 2017 - 2025 Chocolatey Software, Inc
// Copyright (c) 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.results;

namespace chocolatey.infrastructure.app.services
{
    /// <summary>
    ///   Enumeration of installed package instances. Kept as a separate interface
    ///   (rather than adding members to <see cref="INugetService"/>) so that
    ///   existing external implementations of that interface are not broken.
    /// </summary>
    public interface IInstanceAwareNugetService
    {
        /// <summary>
        ///   Get a single installed package instance by id and instance name.
        /// </summary>
        /// <param name="id">The package id.</param>
        /// <param name="instance">The instance name (the name the instance was installed with).</param>
        /// <param name="config">The configuration.</param>
        /// <returns>The matching installed instance, or <c>null</c> when it is not installed.</returns>
        PackageResult GetInstalledPackageInstance(string id, string instance, ChocolateyConfiguration config);

        /// <summary>
        ///   Get every installed instance of a package id.
        /// </summary>
        /// <param name="id">The package id.</param>
        /// <param name="config">The configuration.</param>
        /// <returns>All installed instances of the id, ordered by instance name; empty when none.</returns>
        IEnumerable<PackageResult> GetInstalledPackageInstances(string id, ChocolateyConfiguration config);

        /// <summary>
        ///   Get every installed package instance across all ids.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>All installed instances, ordered by id then instance name; empty when none.</returns>
        IEnumerable<PackageResult> GetInstalledPackageInstances(ChocolateyConfiguration config);
    }
}
