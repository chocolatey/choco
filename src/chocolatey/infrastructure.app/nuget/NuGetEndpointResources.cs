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
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using chocolatey.infrastructure.logging;
    using NuGet.Protocol.Core.Types;

    public sealed class NuGetEndpointResources
    {
        private static readonly ConcurrentDictionary<SourceRepository, NuGetEndpointResources> _cachedResources = new ConcurrentDictionary<SourceRepository, NuGetEndpointResources>();

        private bool _resolvingFailed;
        private readonly ChocolateySourceCacheContext _cacheContext;
        private readonly Lazy<DependencyInfoResource> _dependencyInfoResource;
        private readonly Lazy<DownloadResource> _downloadResource;
        private readonly Lazy<FindPackageByIdResource> _findPackageResource;
        private readonly Lazy<ListResource> _listResource;
        private readonly Lazy<PackageMetadataResource> _packageMetadataResource;
        private readonly Lazy<PackageUpdateResource> _packageUpdateResource;
        private readonly Lazy<PackageSearchResource> _searchResource;

        private NuGetEndpointResources(SourceRepository _sourceRepository, ChocolateySourceCacheContext cacheContext)
        {
            Source = _sourceRepository;

            _cacheContext = cacheContext;
            _dependencyInfoResource = new Lazy<DependencyInfoResource>(() => ResolveResource<DependencyInfoResource>());
            _downloadResource = new Lazy<DownloadResource>(() => ResolveResource<DownloadResource>());
            _findPackageResource = new Lazy<FindPackageByIdResource>(() => ResolveResource<FindPackageByIdResource>());
            _listResource = new Lazy<ListResource>(() => ResolveResource<ListResource>());
            _packageMetadataResource = new Lazy<PackageMetadataResource>(() => ResolveResource<PackageMetadataResource>());
            _packageUpdateResource = new Lazy<PackageUpdateResource>(() => ResolveResource<PackageUpdateResource>());
            _searchResource = new Lazy<PackageSearchResource>(() => ResolveResource<PackageSearchResource>());
        }

        public DependencyInfoResource DependencyInfoResource
        {
            get
            {
                return _dependencyInfoResource.Value;
            }
        }

        public DownloadResource DownloadResource
        {
            get
            {
                return _downloadResource.Value;
            }
        }

        public FindPackageByIdResource FindPackageResource
        {
            get
            {
                return _findPackageResource.Value;
            }
        }

        public ListResource ListResource
        {
            get
            {
                return _listResource.Value;
            }
        }

        public PackageMetadataResource PackageMetadataResource
        {
            get
            {
                return _packageMetadataResource.Value;
            }
        }

        public PackageUpdateResource PackageUpdateResource
        {
            get
            {
                return _packageUpdateResource.Value;
            }
        }

        public PackageSearchResource SearchResource
        {
            get
            {
                return _searchResource.Value;
            }
        }

        public SourceRepository Source { get; private set; }

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static NuGetEndpointResources GetResourcesBySource(SourceRepository source)
        {
            return GetResourcesBySource(source, cacheContext: null);
        }

        public static NuGetEndpointResources GetResourcesBySource(SourceRepository source, ChocolateySourceCacheContext cacheContext)
        {
            return _cachedResources.GetOrAdd(source, (key) =>
            {
                var endpointResource = new NuGetEndpointResources(key, cacheContext);

                return endpointResource;
            });
        }

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static IEnumerable<NuGetEndpointResources> GetResourcesBySource(IEnumerable<SourceRepository> sources)
        {
            return GetResourcesBySource(sources, cacheContext: null);
        }

        public static IEnumerable<NuGetEndpointResources> GetResourcesBySource(IEnumerable<SourceRepository> sources, ChocolateySourceCacheContext cacheContext)
        {
            foreach (SourceRepository source in sources)
            {
                yield return GetResourcesBySource(source, cacheContext);
            }
        }

        private T ResolveResource<T>()
            where T : class, INuGetResource
        {
            T resource = default;

            try
            {
                this.Log().Debug("Resolving resource {0} for source {1}", typeof(T).Name, Source.PackageSource.Source);
#pragma warning disable RS0030 // Do not used banned APIs
                resource = Source.GetResource<T>(_cacheContext);
#pragma warning restore RS0030 // Do not used banned APIs
            }
            catch (AggregateException ex) when (!(ex.InnerException is null))
            {
                if (!_resolvingFailed)
                {
                    this.Log().Warn(ex.InnerException.Message);
                    _resolvingFailed = true;
                }
            }

            if (resource == default)
            {
                this.Log().Warn(ChocolateyLoggers.LogFileOnly, "The source {0} failed to get a {1} resource".FormatWith(Source.PackageSource.Source, typeof(T)));
            }

            return resource;
        }
    }
}
