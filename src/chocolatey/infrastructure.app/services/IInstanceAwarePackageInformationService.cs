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

using chocolatey.infrastructure.app.domain;
using NuGet.Packaging;

namespace chocolatey.infrastructure.app.services
{
    /// <summary>
    ///   Instance-aware reads/writes of the package information store. Kept as a
    ///   separate interface (rather than adding members to
    ///   <see cref="IChocolateyPackageInformationService"/>) so that existing
    ///   external implementations of that interface are not broken.
    ///   Implementations that support package instances implement both; callers
    ///   reach these overloads by casting the injected service to this interface,
    ///   and only do so on the instance code paths (the default path keeps using
    ///   the single-argument overloads).
    /// </summary>
    public interface IInstanceAwarePackageInformationService
    {
        /// <summary>
        ///   Get the information for a specific package instance (stored under
        ///   <c>.chocolatey\instances\&lt;id&gt;\&lt;instance&gt;</c>).
        /// </summary>
        ChocolateyPackageInformation Get(IPackageMetadata package, string instance);

        /// <summary>
        ///   Remove the information store for a specific package instance.
        /// </summary>
        void Remove(IPackageMetadata package, string instance);
    }
}
