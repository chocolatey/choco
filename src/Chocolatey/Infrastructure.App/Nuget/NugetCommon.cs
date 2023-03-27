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

namespace Chocolatey.Infrastructure.App.Nuget
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Security;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Adapters;
    using Alphaleonis.Win32.Filesystem;
    using Chocolatey.NuGet.Frameworks;
    using Infrastructure.Configuration;
    using Configuration;
    using Domain;
    using FileSystem;
    using Logging;
    using global::NuGet;
    using global::NuGet.Common;
    using global::NuGet.Configuration;
    using global::NuGet.Credentials;
    using global::NuGet.PackageManagement;
    using global::NuGet.Packaging;
    using global::NuGet.Packaging.Core;
    using global::NuGet.ProjectManagement;
    using global::NuGet.Protocol;
    using global::NuGet.Protocol.Core.Types;
    using global::NuGet.Versioning;
    using Results;
    using Console = Adapters.Console;
    using Environment = Adapters.Environment;
    using System.Collections.Concurrent;

    public sealed class NugetCommon
    {
        private static readonly ConcurrentDictionary<string, SourceRepository> _repositories = new ConcurrentDictionary<string, SourceRepository>();

        private static Lazy<IConsole> _console = new Lazy<IConsole>(() => new Console());

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void InitializeWith(Lazy<IConsole> console)
        {
            _console = console;
        }

        [Obsolete("This member is unused and should probably be removed.")]
        private static IConsole Console
        {
            get { return _console.Value; }
        }

        [Obsolete("This overload is obsolete and will be removed in a future version.")]
        public static ChocolateyPackagePathResolver GetPathResolver(ChocolateyConfiguration configuration, IFileSystem nugetPackagesFileSystem)
            => GetPathResolver(nugetPackagesFileSystem);

        public static ChocolateyPackagePathResolver GetPathResolver(IFileSystem nugetPackagesFileSystem)
        {
            return new ChocolateyPackagePathResolver(ApplicationParameters.PackagesLocation, nugetPackagesFileSystem);
        }

        public static void ClearRepositoriesCache()
        {
            _repositories.Clear();
        }

        public static SourceRepository GetLocalRepository()
        {
            var nugetSource = new PackageSource(ApplicationParameters.PackagesLocation);
            return Repository.Factory.GetCoreV3(nugetSource);
        }

        public static IEnumerable<SourceRepository> GetRemoteRepositories(ChocolateyConfiguration configuration, ILogger nugetLogger, IFileSystem filesystem)
        {
            // Set user agent for all NuGet library calls. Should not affect any HTTP calls that Chocolatey itself would make.
            UserAgent.SetUserAgentString(new UserAgentStringBuilder("{0}/{1} via NuGet Client".FormatWith(ApplicationParameters.UserAgent, configuration.Information.ChocolateyProductVersion)));

            // ensure credentials can be grabbed from configuration
            SetHttpHandlerCredentialService(configuration);

            if (!string.IsNullOrWhiteSpace(configuration.Proxy.Location))
            {
                "chocolatey".Log().Debug("Using proxy server '{0}'.".FormatWith(configuration.Proxy.Location));
                var proxy = new System.Net.WebProxy(configuration.Proxy.Location, true);

                if (!String.IsNullOrWhiteSpace(configuration.Proxy.User) && !String.IsNullOrWhiteSpace(configuration.Proxy.EncryptedPassword))
                {
                    proxy.Credentials = new NetworkCredential(configuration.Proxy.User, NugetEncryptionUtility.DecryptString(configuration.Proxy.EncryptedPassword));
                }

                if (!string.IsNullOrWhiteSpace(configuration.Proxy.BypassList))
                {
                    "chocolatey".Log().Debug("Proxy has a bypass list of '{0}'.".FormatWith(configuration.Proxy.BypassList.EscapeCurlyBraces()));
                    proxy.BypassList = configuration.Proxy.BypassList.Replace("*",".*").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                }

                proxy.BypassProxyOnLocal = configuration.Proxy.BypassOnLocal;

                ProxyCache.Instance.Override(proxy);
            }
            else
            {
                ProxyCache.Instance.Override(new System.Net.WebProxy());
            }

            IEnumerable<string> sources = configuration.Sources.ToStringSafe().Split(new[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);

            IList<SourceRepository> repositories = new List<SourceRepository>();

            var updatedSources = new StringBuilder();
            foreach (var sourceValue in sources.OrEmpty())
            {
var source = sourceValue;
                var bypassProxy = false;

                var sourceClientCertificates = new List<X509Certificate>();
                if (!string.IsNullOrWhiteSpace(configuration.SourceCommand.Certificate))
                {
                    "chocolatey".Log().Debug("Using passed in certificate for source {0}".FormatWith(source));
                    sourceClientCertificates.Add(new X509Certificate2(configuration.SourceCommand.Certificate, configuration.SourceCommand.CertificatePassword));
                }

                if (configuration.MachineSources.Any(m => m.Name.IsEqualTo(source) || m.Key.IsEqualTo(source)))
                {
                    try
                    {
                        var machineSource = configuration.MachineSources.FirstOrDefault(m => m.Key.IsEqualTo(source));
                        if (machineSource == null)
                        {
                            machineSource = configuration.MachineSources.FirstOrDefault(m => m.Name.IsEqualTo(source));
                            "chocolatey".Log().Debug("Switching source name {0} to actual source value '{1}'.".FormatWith(sourceValue, machineSource.Key.ToStringSafe()));
                            source = machineSource.Key;
                        }

                        if (machineSource != null)
                        {
                            bypassProxy = machineSource.BypassProxy;
                            if (bypassProxy) "chocolatey".Log().Debug("Source '{0}' is configured to bypass proxies.".FormatWith(source));

                            if (!string.IsNullOrWhiteSpace(machineSource.Certificate))
                            {
                                "chocolatey".Log().Debug("Using configured certificate for source {0}".FormatWith(source));
                                sourceClientCertificates.Add(new X509Certificate2(machineSource.Certificate, NugetEncryptionUtility.DecryptString(machineSource.EncryptedCertificatePassword)));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        "chocolatey".Log().Warn("Attempted to use a source name {0} to get default source but failed:{1} {2}".FormatWith(sourceValue, System.Environment.NewLine, ex.Message));
                    }
                }

                if (_repositories.ContainsKey(source))
                {
                    repositories.Add(_repositories[source]);
                }
                else
                {
                    var nugetSource = new PackageSource(source);

                    // If not parsed as a http(s) or local source, let's try resolving the path
                    // Since NuGet.Client is not able to parse all relative paths
                    // Conversion to absolute paths is handled by clients, not by the libraries as per
                    // https://github.com/NuGet/NuGet.Client/pull/3783
                    if (nugetSource.TrySourceAsUri is null)
                    {
                        string fullsource;
                        try
                        {
                            fullsource = filesystem.GetFullPath(source);
                        }
                        catch
                        {
                            // If an invalid source was passed in, we don't care here, pass it along
                            fullsource = source;
                        }
                        nugetSource = new PackageSource(fullsource);

                        if (!nugetSource.IsLocal)
                        {
                            throw new ApplicationException("Source '{0}' is unable to be parsed".FormatWith(source));
                        }

                        "chocolatey".Log().Debug("Updating Source path from {0} to {1}".FormatWith(source, fullsource));
                        updatedSources.AppendFormat("{0};", fullsource);
                    }
                    else
                    {
                        updatedSources.AppendFormat("{0};", source);
                    }

                    nugetSource.ClientCertificates = sourceClientCertificates;
                    var repo = Repository.Factory.GetCoreV3(nugetSource);

                    if (nugetSource.IsHttp || nugetSource.IsHttps)
                    {
                        var httpSourceResource = repo.GetResource<HttpSourceResource>();
                        if (httpSourceResource != null)
                        {
                            httpSourceResource.HttpSource.HttpCacheDirectory = ApplicationParameters.HttpCacheLocation;
                        }
                    }

                    _repositories.TryAdd(source, repo);
                    repositories.Add(repo);
                }
            }

            if (updatedSources.Length != 0)
            {
                configuration.Sources = updatedSources.Remove(updatedSources.Length - 1, 1).ToStringChecked();
            }

            return repositories;
        }

        public static IEnumerable<T> GetRepositoryResource<T>(IEnumerable<SourceRepository> packageRepositories) where T : class, INuGetResource
        {
            foreach (var repository in packageRepositories)
            {
                var resource = repository.GetResource<T>();
                if (resource is null)
                {
                    "chocolatey".Log().Warn(ChocolateyLoggers.LogFileOnly, "The source {0} failed to get a {1} resource".FormatWith(repository.PackageSource.Source, typeof(T)));
                }
                else
                {
                    yield return resource;
                }
            }
        }

        // TODO: Refactor this to not use a tuple, or make private method.
        public static IEnumerable<(SourceRepository repository,
                PackageSearchResource searchResource,
                FindPackageByIdResource findPackageByIdResource,
                PackageMetadataResource packageMetadataResource,
                ListResource listResource
                )> GetRepositoryResources(IEnumerable<SourceRepository> packageRepositories)
        {
            foreach (var repository in packageRepositories)
            {
                yield return (
                    repository,
                    repository.GetResource<PackageSearchResource>(),
                    repository.GetResource<FindPackageByIdResource>(),
                    repository.GetResource<PackageMetadataResource>(),
                    repository.GetResource<ListResource>());
            }
        }

        public static void SetHttpHandlerCredentialService(ChocolateyConfiguration configuration)
        {
            HttpHandlerResourceV3.CredentialService = new Lazy<ICredentialService>(
                () => new CredentialService(
                    new AsyncLazy<IEnumerable<ICredentialProvider>>(
                        () => GetCredentialProvidersAsync(configuration)), false, true));
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously.
        // We don't care about this method being synchronous because this is just used to pass in the credential provider to Lazy<ICredentialService>
        private static async Task<IEnumerable<ICredentialProvider>> GetCredentialProvidersAsync(ChocolateyConfiguration configuration)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            return new List<ICredentialProvider>() { new ChocolateyNugetCredentialProvider(configuration) };
        }

        public static void GetLocalPackageDependencies(PackageIdentity package,
            NuGetFramework framework,
            IEnumerable<PackageResult> allLocalPackages,
            ISet<SourcePackageDependencyInfo> dependencyInfos
        )
        {
            if (dependencyInfos.Contains(package)) return;

            var metadata = allLocalPackages
                .FirstOrDefault(p => p.PackageMetadata.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase) && p.PackageMetadata.Version.Equals(package.Version))
                .PackageMetadata;

            var group = NuGetFrameworkUtility.GetNearest<PackageDependencyGroup>(metadata.DependencyGroups, framework);
            var dependencies = group?.Packages ?? Enumerable.Empty<PackageDependency>();

            var result = new SourcePackageDependencyInfo(
                package,
                dependencies,
                true,
                null,
                null,
                null);

            dependencyInfos.Add(result);

            foreach (var dependency in dependencies)
            {
                GetLocalPackageDependencies(dependency.Id, dependency.VersionRange, framework, allLocalPackages, dependencyInfos);
            }
        }

        public static void GetLocalPackageDependencies(string packageId,
            VersionRange versionRange,
            NuGetFramework framework,
            IEnumerable<PackageResult> allLocalPackages,
            ISet<SourcePackageDependencyInfo> dependencyInfos
        )
        {
            var versionsMetadata = allLocalPackages
                .Where(p => p.PackageMetadata.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase) && versionRange.Satisfies(p.PackageMetadata.Version))
                .Select(p => p.PackageMetadata);

            foreach (var metadata in versionsMetadata)
            {
                var group = NuGetFrameworkUtility.GetNearest<PackageDependencyGroup>(metadata.DependencyGroups, framework);
                var dependencies = group?.Packages ?? Enumerable.Empty<PackageDependency>();

                var result = new SourcePackageDependencyInfo(
                    metadata.Id,
                    metadata.Version,
                    dependencies,
                    true,
                    null,
                    null,
                    null);

                if (dependencyInfos.Contains(result)) return;
                dependencyInfos.Add(result);

                foreach (var dependency in dependencies)
                {
                    GetLocalPackageDependencies(dependency.Id, dependency.VersionRange, framework, allLocalPackages, dependencyInfos);
                }
            }
        }

        public static async Task GetPackageDependencies(PackageIdentity package,
            NuGetFramework framework,
            SourceCacheContext cacheContext,
            ILogger logger,
            IEnumerable<DependencyInfoResource> dependencyInfoResources,
            ISet<SourcePackageDependencyInfo> availablePackages,
            ISet<PackageDependency> dependencyCache,
            ChocolateyConfiguration configuration)
        {
            if (availablePackages.Contains(package)) return;

            foreach (var dependencyInfoResource in dependencyInfoResources)
            {
                var dependencyInfo = await dependencyInfoResource.ResolvePackage(
                    package, framework, cacheContext, logger, CancellationToken.None);

                if (dependencyInfo == null) continue;

                availablePackages.Add(dependencyInfo);
                foreach (var dependency in dependencyInfo.Dependencies)
                {
                    if (dependencyCache.Contains(dependency)) continue;
                    dependencyCache.Add(dependency);
                    await GetPackageDependencies(
                        dependency.Id, framework, cacheContext, logger, dependencyInfoResources, availablePackages, dependencyCache, configuration);
                }
            }
        }

        public static async Task GetPackageDependencies(string packageId,
            NuGetFramework framework,
            SourceCacheContext cacheContext,
            ILogger logger,
            IEnumerable<DependencyInfoResource> dependencyInfoResources,
            ISet<SourcePackageDependencyInfo> availablePackages,
            ISet<PackageDependency> dependencyCache,
            ChocolateyConfiguration configuration)
        {
            //if (availablePackages.Contains(packageID)) return;

            foreach (var dependencyInfoResource in dependencyInfoResources)
            {
                var dependencyInfos = await dependencyInfoResource.ResolvePackages(
                    packageId, configuration.Prerelease, framework, cacheContext, logger, CancellationToken.None);

                if (!dependencyInfos.Any()) continue;

                availablePackages.AddRange(dependencyInfos);
                foreach (var dependency in dependencyInfos.SelectMany(p => p.Dependencies))
                {
                    if (dependencyCache.Contains(dependency)) continue;
                    dependencyCache.Add(dependency);

                    // Recursion is fun, kids
                    await GetPackageDependencies(
                        dependency.Id, framework, cacheContext, logger, dependencyInfoResources, availablePackages, dependencyCache, configuration);
                }
            }
        }

        public static async Task GetPackageParents(string packageId,
            ISet<SourcePackageDependencyInfo> parentPackages,
            IEnumerable<SourcePackageDependencyInfo> locallyInstalledPackages)
        {
            foreach (var package in locallyInstalledPackages.Where(p => !parentPackages.Contains(p)))
            {
                if (parentPackages.Contains(package)) continue;
                if (package.Dependencies.Any(p => p.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase)))
                {
                    parentPackages.Add(package);
                    await GetPackageParents(package.Id, parentPackages, locallyInstalledPackages);
                }
            }
        }

    }
}
