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

namespace chocolatey.infrastructure.app.nuget
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using adapters;
    using Alphaleonis.Win32.Filesystem;
    using Chocolatey.NuGet.Frameworks;
    using infrastructure.configuration;
    using configuration;
    using domain;
    using filesystem;
    using logging;
    using NuGet;
    using NuGet.Common;
    using NuGet.Configuration;
    using NuGet.Credentials;
    using NuGet.PackageManagement;
    using NuGet.Packaging;
    using NuGet.Packaging.Core;
    using NuGet.ProjectManagement;
    using NuGet.Protocol;
    using NuGet.Protocol.Core.Types;
    using NuGet.Versioning;
    using results;
    using Console = adapters.Console;
    using Environment = adapters.Environment;

    // ReSharper disable InconsistentNaming

    public sealed class NugetCommon
    {
        private static Lazy<IConsole> _console = new Lazy<IConsole>(() => new Console());

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void initialize_with(Lazy<IConsole> console)
        {
            _console = console;
        }

        private static IConsole Console
        {
            get { return _console.Value; }
        }

        /*
        public static IFileSystem GetNuGetFileSystem(ChocolateyConfiguration configuration, ILogger nugetLogger)
        {
            return new ChocolateyPhysicalFileSystem(ApplicationParameters.PackagesLocation) { Logger = nugetLogger };
        }*/

        public static ChocolateyPackagePathResolver GetPathResolver(ChocolateyConfiguration configuration, IFileSystem nugetPackagesFileSystem)
        {
            return new ChocolateyPackagePathResolver(ApplicationParameters.PackagesLocation, nugetPackagesFileSystem, configuration.AllowMultipleVersions);
        }


        public static SourceRepository GetLocalRepository()
        {
            var nugetSource = new PackageSource(ApplicationParameters.PackagesLocation);
            return Repository.Factory.GetCoreV3(nugetSource);
        }

        /*
        public static IPackageRepository GetLocalRepository(IPackagePathResolver pathResolver, IFileSystem nugetPackagesFileSystem, ILogger nugetLogger)
        {
            return new ChocolateyLocalPackageRepository(pathResolver, nugetPackagesFileSystem) { Logger = nugetLogger, PackageSaveMode = PackageSaveModes.Nupkg | PackageSaveModes.Nuspec };
        }
        */

        public static IEnumerable<SourceRepository> GetRemoteRepositories(ChocolateyConfiguration configuration, ILogger nugetLogger)
        {

            //TODO, fix
            /*
            if (configuration.Features.ShowDownloadProgress)
            {
                PackageDownloader.
                PackageDownloader.ProgressAvailable += (sender, e) =>
                {
                    // http://stackoverflow.com/a/888569/18475
                    Console.Write("\rProgress: {0} {1}%".format_with(e.Operation, e.PercentComplete.to_string()).PadRight(Console.WindowWidth));
                    if (e.PercentComplete == 100)
                    {
                        Console.WriteLine("");
                    }
                };
            }
            */


            // ensure credentials can be grabbed from configuration
            SetHttpHandlerCredentialService(configuration);

            if (!string.IsNullOrWhiteSpace(configuration.Proxy.Location))
            {
                "chocolatey".Log().Debug("Using proxy server '{0}'.".format_with(configuration.Proxy.Location));
                var proxy = new System.Net.WebProxy(configuration.Proxy.Location, true);

                if (!String.IsNullOrWhiteSpace(configuration.Proxy.User) && !String.IsNullOrWhiteSpace(configuration.Proxy.EncryptedPassword))
                {
                    proxy.Credentials = new NetworkCredential(configuration.Proxy.User, NugetEncryptionUtility.DecryptString(configuration.Proxy.EncryptedPassword));
                }

                if (!string.IsNullOrWhiteSpace(configuration.Proxy.BypassList))
                {
                    "chocolatey".Log().Debug("Proxy has a bypass list of '{0}'.".format_with(configuration.Proxy.BypassList.escape_curly_braces()));
                    proxy.BypassList = configuration.Proxy.BypassList.Replace("*",".*").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                }

                proxy.BypassProxyOnLocal = configuration.Proxy.BypassOnLocal;

                ProxyCache.Instance.Override(proxy);
            }

            IEnumerable<string> sources = configuration.Sources.to_string().Split(new[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);

            IList<SourceRepository> repositories = new List<SourceRepository>();

            var updatedSources = new StringBuilder();
            foreach (var sourceValue in sources.or_empty_list_if_null())
            {

                var source = sourceValue;
                var bypassProxy = false;

                var sourceClientCertificates = new List<X509Certificate>();
                if (!string.IsNullOrWhiteSpace(configuration.SourceCommand.Certificate))
                {
                    "chocolatey".Log().Debug("Using passed in certificate for source {0}".format_with(source));
                    sourceClientCertificates.Add(new X509Certificate2(configuration.SourceCommand.Certificate, configuration.SourceCommand.CertificatePassword));
                }

                if (configuration.MachineSources.Any(m => m.Name.is_equal_to(source) || m.Key.is_equal_to(source)))
                {
                    try
                    {
                        var machineSource = configuration.MachineSources.FirstOrDefault(m => m.Key.is_equal_to(source));
                        if (machineSource == null)
                        {
                            machineSource = configuration.MachineSources.FirstOrDefault(m => m.Name.is_equal_to(source));
                            "chocolatey".Log().Debug("Switching source name {0} to actual source value '{1}'.".format_with(sourceValue, machineSource.Key.to_string()));
                            source = machineSource.Key;
                        }

                        if (machineSource != null)
                        {
                            bypassProxy = machineSource.BypassProxy;
                            if (bypassProxy) "chocolatey".Log().Debug("Source '{0}' is configured to bypass proxies.".format_with(source));

                            if (!string.IsNullOrWhiteSpace(machineSource.Certificate))
                            {
                                "chocolatey".Log().Debug("Using configured certificate for source {0}".format_with(source));
                                sourceClientCertificates.Add(new X509Certificate2(machineSource.Certificate, NugetEncryptionUtility.DecryptString(machineSource.EncryptedCertificatePassword)));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        "chocolatey".Log().Warn("Attempted to use a source name {0} to get default source but failed:{1} {2}".format_with(sourceValue, System.Environment.NewLine, ex.Message));
                    }
                }

                updatedSources.AppendFormat("{0};", source);

                var nugetSource = new PackageSource(source);
                nugetSource.ClientCertificates = sourceClientCertificates;
                var repo = Repository.Factory.GetCoreV3(nugetSource);
                repositories.Add(repo);
            }

            if (updatedSources.Length != 0)
            {
                configuration.Sources = updatedSources.Remove(updatedSources.Length - 1, 1).to_string();
            }

            return repositories;
        }

        /*
        // keep this here for the licensed edition for now
        public static NuGetPackageManager GetPackageManager(ChocolateyConfiguration configuration, ILogger nugetLogger, Action<ChocolateyPackageOperationEventArgs> installSuccessAction, Action<ChocolateyPackageOperationEventArgs> uninstallSuccessAction, bool addUninstallHandler)
        {
            return GetPackageManager(configuration, nugetLogger, new PackageDownloader(), installSuccessAction, uninstallSuccessAction, addUninstallHandler);
        }
        */

        /*
        // keep this here for the licensed edition for now
        public static NuGetPackageManager GetPackageManager(ChocolateyConfiguration configuration, ILogger nugetLogger, Action<ChocolateyPackageOperationEventArgs> installSuccessAction, Action<ChocolateyPackageOperationEventArgs> uninstallSuccessAction, bool addUninstallHandler)
        //public static NuGetPackageManager GetPackageManager(ChocolateyConfiguration configuration, ILogger nugetLogger, bool addUninstallHandler)
        {
            //IFileSystem nugetPackagesFileSystem = GetNuGetFileSystem(configuration, nugetLogger);
            //IPackagePathResolver pathResolver = GetPathResolver(configuration, nugetPackagesFileSystem);


            var packageManager = new NuGetPackageManager(GetRemoteRepositories(configuration, nugetLogger, packageDownloader), pathResolver, nugetPackagesFileSystem, GetLocalRepository(pathResolver, nugetPackagesFileSystem, nugetLogger))
                {
                    DependencyVersion = DependencyVersion.Highest,
                    Logger = nugetLogger,
                };


        //TODO - see if wee need to implement ISettings to set something instead of nullsettings
        //TODO - properly implement everything for ChocolateySourceRepositoryProvider
        var repositoryProvider = new ChocolateySourceRepositoryProvider(NugetCommon.GetRemoteRepositories(configuration, nugetLogger));
            var packageManager = new NuGetPackageManager(repositoryProvider, new NullSettings(), ApplicationParameters.PackagesLocation);


            // GH-1548
            //note: is this a good time to capture a backup (for dependencies) / maybe grab remembered arguments here instead / and somehow get out of the endless loop!
            //NOTE DO NOT EVER use this method - packageManager.PackageInstalling += (s, e) => { };
            packageManager.PackageInstalled += (s, e) =>
                {
                    var pkg = e.Package;
                    "chocolatey".Log().Info(ChocolateyLoggers.Important, "{0}{1} v{2}{3}{4}{5}".format_with(
                        System.Environment.NewLine,
                        pkg.Id,
                        pkg.Version.to_string(),
                        configuration.Force ? " (forced)" : string.Empty,
                        string.Empty, string.Empty
                        //pkg.IsApproved ? " [Approved]" : string.Empty,
                        //pkg.PackageTestResultStatus == "Failing" && pkg.IsDownloadCacheAvailable ? " - Likely broken for FOSS users (due to download location changes)" : pkg.PackageTestResultStatus == "Failing" ? " - Possibly broken" : string.Empty
                        ));

                    if (installSuccessAction != null) installSuccessAction.Invoke(e);
                };



            if (addUninstallHandler)
            {
                // NOTE DO NOT EVER use this method, or endless loop - packageManager.PackageUninstalling += (s, e) =>

                packageManager.PackageUninstalled += (s, e) =>
                    {


                        IPackage pkg = packageManager.LocalRepository.FindPackage(e.Package.Id, e.Package.Version);
                        if (pkg != null)
                        {
                            // install not actually removed, let's clean it up. This is a bug with nuget, where it reports it removed some package and did NOTHING
                            // this is what happens when you are switching from AllowMultiple to just one and back
                            var chocoPathResolver = packageManager.PathResolver as ChocolateyPackagePathResolver;
                            if (chocoPathResolver != null)
                            {
                                chocoPathResolver.UseSideBySidePaths = !chocoPathResolver.UseSideBySidePaths;

                                // an unfound package folder can cause an endless loop.
                                // look for it and ignore it if doesn't line up with versioning
                                if (nugetPackagesFileSystem.DirectoryExists(chocoPathResolver.GetInstallPath(pkg)))
                                {
                                    //todo: This causes an issue with upgrades.
                                    // this causes this to be called again, which should then call the uninstallSuccessAction below
                                    packageManager.UninstallPackage(pkg, forceRemove: configuration.Force, removeDependencies: false);
                                }

                                chocoPathResolver.UseSideBySidePaths = configuration.AllowMultipleVersions;
                            }
                        }
                        else
                        {
                            if (uninstallSuccessAction != null) uninstallSuccessAction.Invoke(e);
                        }
                        if (uninstallSuccessAction != null) uninstallSuccessAction.Invoke(e);
                    };
            }

            return packageManager;

        }
        */

        public static IEnumerable<T> GetRepositoryResource<T>(IEnumerable<SourceRepository> packageRepositories) where T : class, INuGetResource
        {
            foreach (var repository in packageRepositories)
            {
                var resource = repository.GetResource<T>();
                if (resource is null)
                {
                    "chocolatey".Log().Warn("The source {0} failed to get a {1} resource".format_with(repository.PackageSource.Source, typeof(T)));
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

        private static async Task<IEnumerable<ICredentialProvider>> GetCredentialProvidersAsync(ChocolateyConfiguration configuration)
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
            ISet<PackageDependency> dependencyCache)
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
                        dependency.Id, framework, cacheContext, logger, dependencyInfoResources, availablePackages, dependencyCache);
                }
            }
        }

        public static async Task GetPackageDependencies(string packageId,
            NuGetFramework framework,
            SourceCacheContext cacheContext,
            ILogger logger,
            IEnumerable<DependencyInfoResource> dependencyInfoResources,
            ISet<SourcePackageDependencyInfo> availablePackages,
            ISet<PackageDependency> dependencyCache)
        {
            //if (availablePackages.Contains(packageID)) return;

            foreach (var dependencyInfoResource in dependencyInfoResources)
            {
                var dependencyInfos = await dependencyInfoResource.ResolvePackages(
                    packageId, framework, cacheContext, logger, CancellationToken.None);

                if (!dependencyInfos.Any()) continue;

                availablePackages.AddRange(dependencyInfos);
                foreach (var dependency in dependencyInfos.SelectMany(p => p.Dependencies))
                {
                    if (dependencyCache.Contains(dependency)) continue;
                    dependencyCache.Add(dependency);

                    // Recursion is fun, kids
                    await GetPackageDependencies(
                        dependency.Id, framework, cacheContext, logger, dependencyInfoResources, availablePackages, dependencyCache);
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

    // ReSharper restore InconsistentNaming
}
