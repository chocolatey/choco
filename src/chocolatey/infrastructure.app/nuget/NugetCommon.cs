// Copyright © 2011 - Present RealDimensions Software, LLC
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
    using System.Linq;
    using System.Net;
    using System.Text;
    using infrastructure.configuration;
    using NuGet;
    using configuration;
    using logging;

    // ReSharper disable InconsistentNaming

    public sealed class NugetCommon
    {
        public static IFileSystem GetNuGetFileSystem(ChocolateyConfiguration configuration, ILogger nugetLogger)
        {
            var fileSystem = new ChocolateyPhysicalFileSystem(ApplicationParameters.PackagesLocation);
            if (configuration.Debug)
            {
                fileSystem.Logger = nugetLogger;
            }

            return fileSystem;
        }

        public static IPackagePathResolver GetPathResolver(ChocolateyConfiguration configuration, IFileSystem nugetPackagesFileSystem)
        {
            return new ChocolateyPackagePathResolver(nugetPackagesFileSystem, configuration.AllowMultipleVersions);
        }

        public static IPackageRepository GetLocalRepository(IPackagePathResolver pathResolver, IFileSystem nugetPackagesFileSystem)
        {
            IPackageRepository localRepository = new ChocolateyLocalPackageRepository(pathResolver, nugetPackagesFileSystem);
            localRepository.PackageSaveMode = PackageSaveModes.Nupkg | PackageSaveModes.Nuspec;

            return localRepository;
        }

        public static IPackageRepository GetRemoteRepository(ChocolateyConfiguration configuration, ILogger nugetLogger)
        {
            IEnumerable<string> sources = configuration.Sources.Split(new[] {";", ","}, StringSplitOptions.RemoveEmptyEntries);

            IList<IPackageRepository> repositories = new List<IPackageRepository>();

            // ensure credentials can be grabbed from configuration
            HttpClient.DefaultCredentialProvider = new ChocolateyNugetCredentialProvider(configuration);
            HttpClient.DefaultCertificateProvider = new ChocolateyClientCertificateProvider(configuration);
            if (!string.IsNullOrWhiteSpace(configuration.Proxy.Location))
            {
                "chocolatey".Log().Debug("Using proxy server '{0}'.".format_with(configuration.Proxy.Location));
                var proxy = new WebProxy(configuration.Proxy.Location, true);

                if (!String.IsNullOrWhiteSpace(configuration.Proxy.User) && !String.IsNullOrWhiteSpace(configuration.Proxy.EncryptedPassword))
                {
                    proxy.Credentials = new NetworkCredential(configuration.Proxy.User, NugetEncryptionUtility.DecryptString(configuration.Proxy.EncryptedPassword));
                }

                if (!string.IsNullOrWhiteSpace(configuration.Proxy.BypassList))
                {
                    proxy.BypassList = configuration.Proxy.BypassList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                }

                ProxyCache.Instance.Override(proxy);
            }

            var updatedSources = new StringBuilder();
            foreach (var sourceValue in sources.or_empty_list_if_null())
            {

                var source = sourceValue;
                var bypassProxy = false;
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

                        if (machineSource != null) bypassProxy = machineSource.BypassProxy;
                    }
                    catch (Exception ex)
                    {
                        "chocolatey".Log().Warn("Attempted to use a source name {0} to get default source but failed:{1} {2}".format_with(sourceValue, Environment.NewLine, ex.Message));
                    }
                }

                updatedSources.AppendFormat("{0};", source);

                try
                {
                    var uri = new Uri(source);
                    if (uri.IsFile || uri.IsUnc)
                    {
                        repositories.Add(new ChocolateyLocalPackageRepository(uri.LocalPath));
                    }
                    else
                    {
                        repositories.Add(new DataServicePackageRepository(new RedirectedHttpClient(uri, bypassProxy)));
                    }
                }
                catch (Exception)
                {
                    repositories.Add(new ChocolateyLocalPackageRepository(source));
                }
            }

            if (updatedSources.Length != 0)
            {
                configuration.Sources = updatedSources.Remove(updatedSources.Length - 1, 1).to_string();
            }

            //todo well that didn't work on failing repos... grrr
            var repository = new AggregateRepository(repositories) {IgnoreFailingRepositories = true};
            repository.ResolveDependenciesVertically = true;
            if (configuration.Debug)
            {
                repository.Logger = nugetLogger;
            }

            return repository;
        }

        public static IPackageManager GetPackageManager(ChocolateyConfiguration configuration, ILogger nugetLogger, Action<PackageOperationEventArgs> installSuccessAction, Action<PackageOperationEventArgs> uninstallSuccessAction, bool addUninstallHandler)
        {
            IFileSystem nugetPackagesFileSystem = GetNuGetFileSystem(configuration, nugetLogger);
            IPackagePathResolver pathResolver = GetPathResolver(configuration, nugetPackagesFileSystem);
            var packageManager = new PackageManager(GetRemoteRepository(configuration, nugetLogger), pathResolver, nugetPackagesFileSystem, GetLocalRepository(pathResolver, nugetPackagesFileSystem))
                {
                    DependencyVersion = DependencyVersion.Highest,
                };

            if (configuration.Debug)
            {
                packageManager.Logger = nugetLogger;
            }

            //NOTE DO NOT EVER use this method - packageManager.PackageInstalling += (s, e) =>
            packageManager.PackageInstalled += (s, e) =>
                {
                    var pkg = e.Package;
                    "chocolatey".Log().Info(ChocolateyLoggers.Important, "{0}{1} v{2}{3}{4}{5}".format_with(
                        Environment.NewLine, 
                        pkg.Id, 
                        pkg.Version.to_string(), 
                        configuration.Force ? " (forced)" : string.Empty,
                        pkg.IsApproved ? " [Approved]" : string.Empty,
                        pkg.PackageTestResultStatus == "Failing" && pkg.IsDownloadCacheAvailable ? " - Likely broken for FOSS users (due to download location changes)" : pkg.PackageTestResultStatus == "Failing" ? " - Possibly broken" : string.Empty
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
                    };
            }

            return packageManager;
        }
    }

    // ReSharper restore InconsistentNaming
}