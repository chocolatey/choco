// Copyright © 2023-Present Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.app.nuget
{
    using System.Collections.Generic;
    using System.Linq;
    using NuGet.Protocol.Core.Types;

    public static class NuGetEndpointResourcesExtensions
    {
        public static IEnumerable<PackageMetadataResource> MetadataResources(this IEnumerable<NuGetEndpointResources> resources)
        {
            return resources.Where(r => r.PackageMetadataResource != null).Select(r => r.PackageMetadataResource);
        }

        public static IEnumerable<DependencyInfoResource> DependencyInfoResources(this IEnumerable<NuGetEndpointResources> resources)
        {
            return resources.Where(r => r.DependencyInfoResource != null).Select(r => r.DependencyInfoResource);
        }

        public static IEnumerable<ListResource> ListResources(this IEnumerable<NuGetEndpointResources> resources)
        {
            return resources.Where(r => r.ListResource != null).Select(r => r.ListResource);
        }

        public static IEnumerable<PackageSearchResource> SearchResources(this IEnumerable<NuGetEndpointResources> resources)
        {
            return resources.Where(r => r.SearchResource != null).Select(r => r.SearchResource);
        }
    }
}
