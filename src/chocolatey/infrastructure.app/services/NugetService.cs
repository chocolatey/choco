namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NuGet;
    using configuration;
    using guards;
    using logging;
    using nuget;
    using results;
    using IFileSystem = filesystem.IFileSystem;

    public class NugetService : INugetService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _nugetLogger;

        /// <summary>
        ///   Initializes a new instance of the <see cref="NugetService" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="nugetLogger">The nuget logger</param>
        public NugetService(IFileSystem fileSystem, ILogger nugetLogger)
        {
            _fileSystem = fileSystem;
            _nugetLogger = nugetLogger;
        }

        public void list_noop(ChocolateyConfiguration configuration)
        {
            this.Log().Info("{0} would have searched for '{1}' against the following source(s) :\"{2}\"".format_with(
                ApplicationParameters.Name,
                configuration.Input,
                configuration.Source
                                ));
        }

        public ConcurrentDictionary<string, PackageResult> list_run(ChocolateyConfiguration configuration, bool logResults = true)
        {
            var packageResults = new ConcurrentDictionary<string, PackageResult>();

            foreach (var package in NugetList.GetPackages(configuration, _nugetLogger).or_empty_list_if_null())
            {
                if (logResults)
                {
                    this.Log().Info(configuration.Verbose ? ChocolateyLoggers.Important : ChocolateyLoggers.Normal,() =>"{0} {1}".format_with(package.Id, package.Version.to_string()));
                    if (configuration.Verbose) this.Log().Info(()=>" {0}{1} Tags:{2}{1}".format_with(package.Description, Environment.NewLine, package.Tags));
                }
                else
                {
                    this.Log().Debug(() => "[Nuget] {0} {1}".format_with(package.Id, package.Version.to_string()));
                }
                packageResults.GetOrAdd(package.Id, new PackageResult(package, null));
            }

            return packageResults;
        }

        public void pack_noop(ChocolateyConfiguration configuration)
        {
            this.Log().Info("{0} would have searched for a nuspec file in \"{1}\" and attempted to compile it.".format_with(
                ApplicationParameters.Name,
                _fileSystem.get_current_directory()
                                ));
        }

        public void pack_run(ChocolateyConfiguration configuration)
        {
            Func<IFileSystem, string> getLocalNuspecFiles = (fileSystem) =>
                {
                    var nuspecFiles = fileSystem.get_files(fileSystem.get_current_directory(), "*" + Constants.ManifestExtension).or_empty_list_if_null();
                    Ensure.that(() => nuspecFiles).meets((files) => files.Count() == 1,
                                                         (name, value) => { throw new FileNotFoundException("No nuspec files (or more than 1) were found to build in '{0}'. Please specify the nuspec file or try in a different directory.".format_with(_fileSystem.get_current_directory())); });

                    return nuspecFiles.FirstOrDefault();
                };

            string nuspecFilePath = !string.IsNullOrWhiteSpace(configuration.Input) ? configuration.Input : getLocalNuspecFiles.Invoke(_fileSystem);
            Ensure.that(() => nuspecFilePath).meets((nuspec) => _fileSystem.get_file_extension(nuspec).is_equal_to(Constants.ManifestExtension), (name, value) => { throw new ArgumentException("File specified or found is not a nuspec file. '{0}'".format_with(value)); });

            var nuspecDirectory = _fileSystem.get_full_path(_fileSystem.get_directory_name(nuspecFilePath));

            IDictionary<string, string> properties = new Dictionary<string, string>();
            // Set the version property if the flag is set
            if (!string.IsNullOrWhiteSpace(configuration.Version))
            {
                properties["version"] = configuration.Version;
            }

            // Initialize the property provider based on what was passed in using the properties flag
            var propertyProvider = new DictionaryPropertyProvider(properties);

            var builder = new PackageBuilder(nuspecFilePath, propertyProvider, includeEmptyDirectories: true);
            if (!string.IsNullOrWhiteSpace(configuration.Version))
            {
                builder.Version = new SemanticVersion(configuration.Version);
            }

            string outputFile = builder.Id + "." + builder.Version + Constants.PackageExtension;
            string outputPath = _fileSystem.combine_paths(nuspecDirectory, outputFile);

            IPackage package = NugetPack.BuildPackage(builder, _fileSystem, outputPath);
            //todo: v1 analyze package
            //if (package != null)
            //{
            //    AnalyzePackage(package);
            //}
        }

        public void install_noop(ChocolateyConfiguration configuration, Action<PackageResult> continueAction)
        {
            //todo: noop should see if packages are already installed and adjust message, amiright?!

            this.Log().Info("{0} would have used NuGet to install packages (if they are not already installed):{1}{2}".format_with(
                ApplicationParameters.Name,
                Environment.NewLine,
                configuration.PackageNames
                                ));

            var tempInstallsLocation = _fileSystem.combine_paths(_fileSystem.get_temp_path(), ApplicationParameters.Name, "TempInstalls_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_ffff"));
            _fileSystem.create_directory_if_not_exists(tempInstallsLocation);
            ApplicationParameters.PackagesLocation = tempInstallsLocation;

            install_run(configuration, continueAction);

            _fileSystem.delete_directory(tempInstallsLocation, recursive: true);
        }

        public ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration configuration, Action<PackageResult> continueAction)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.PackagesLocation);
            var packageInstalls = new ConcurrentDictionary<string, PackageResult>();

            //todo: handle all

            SemanticVersion version = configuration.Version != null ? new SemanticVersion(configuration.Version) : null;
            var packageManager = NugetCommon.GetPackageManager(configuration, _nugetLogger,
                installSuccessAction: (e) =>
                    {
                        var pkg = e.Package;
                        var results = packageInstalls.GetOrAdd(pkg.Id.to_lower(), new PackageResult(pkg, e.InstallPath));
                        results.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));

                        if (continueAction != null) continueAction.Invoke(results);
                    },
                uninstallSuccessAction: null);

            foreach (string packageName in configuration.PackageNames.Split(new[] {ApplicationParameters.PackageNamesSeparator}, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null())
            {
                if (packageName.to_lower().EndsWith(".config"))
                {
                    //todo: determine if .config file for packages .config
                    //todo: determine if config file exists
                }
                else
                {
                    //todo: get smarter about realizing multiple versions have been installed before and allowing that

                    IPackage installedPackage = packageManager.LocalRepository.FindPackage(packageName);
                    if (installedPackage != null && (version == null || version == installedPackage.Version) && !configuration.Force)
                    {
                        string logMessage = "{0} v{1} already installed.{2} Use --force to reinstall, specify a version to install, or try upgrade.".format_with(installedPackage.Id, installedPackage.Version, Environment.NewLine);
                        var results = packageInstalls.GetOrAdd(packageName, new PackageResult(installedPackage, ApplicationParameters.PackagesLocation));
                        results.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                        this.Log().Warn(ChocolateyLoggers.Important, logMessage);
                        continue;
                    }

                    if (installedPackage != null && (version == null || version == installedPackage.Version) && configuration.Force)
                    {
                        this.Log().Debug(() => "{0} v{1} already installed. Forcing reinstall.".format_with(installedPackage.Id, installedPackage.Version));
                        version = installedPackage.Version;
                    }

                    IPackage availablePackage = packageManager.SourceRepository.FindPackage(packageName, version, configuration.Prerelease, allowUnlisted: false);
                    if (availablePackage == null)
                    {
                        var logMessage = "{0} not installed. The package was not found with the source(s) listed.{1} If you specified a particular version and are receiving this message, it is possible that the package name exists but the version does not.{1} Version: \"{2}\"{1} Source(s): \"{3}\"".format_with(packageName, Environment.NewLine, configuration.Version, configuration.Source);
                        this.Log().Error(ChocolateyLoggers.Important, logMessage);
                        var results = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, version.to_string(), null));
                        results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                        continue;
                    }

                    if (installedPackage != null && (installedPackage.Version == availablePackage.Version))
                    {
                        packageManager.UninstallPackage(installedPackage, forceRemove: configuration.Force, removeDependencies: configuration.ForceDependencies);
                    }

                    using (packageManager.SourceRepository.StartOperation(
                        RepositoryOperationNames.Install,
                        packageName,
                        version == null ? null : version.ToString()))
                    {
                        packageManager.InstallPackage(availablePackage, configuration.IgnoreDependencies, configuration.Prerelease);
                        //packageManager.InstallPackage(packageName, version, configuration.IgnoreDependencies, configuration.Prerelease);
                    }
                }
            }

            return packageInstalls;
        }

        public void upgrade_noop(ChocolateyConfiguration configuration, Action<PackageResult> continueAction)
        {
            configuration.Force = false;
            upgrade_run(configuration, continueAction, performAction: false);
        }

        public ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration configuration, Action<PackageResult> continueAction)
        {
            return upgrade_run(configuration, continueAction, performAction: true);
        }

        public ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration configuration, Action<PackageResult> continueAction, bool performAction)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.PackagesLocation);
            var packageInstalls = new ConcurrentDictionary<string, PackageResult>();

            SemanticVersion version = configuration.Version != null ? new SemanticVersion(configuration.Version) : null;
            var packageManager = NugetCommon.GetPackageManager(configuration, _nugetLogger,
                installSuccessAction: (e) =>
                    {
                        var pkg = e.Package;
                        var results = packageInstalls.GetOrAdd(pkg.Id.to_lower(), new PackageResult(pkg, e.InstallPath));
                        results.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));

                        if (continueAction != null) continueAction.Invoke(results);
                    },
                uninstallSuccessAction: null);

            set_package_names_if_all_is_specified(configuration, () => { configuration.IgnoreDependencies = true; });

            foreach (string packageName in configuration.PackageNames.Split(new[] {ApplicationParameters.PackageNamesSeparator}, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null())
            {
                if (packageName.to_lower().EndsWith(".config"))
                {
                    //todo: determine if .config file for packages .config
                    //todo: determine if config file exists
                }
                else
                {
                    //todo: get smarter about realizing multiple versions have been installed before and allowing that

                    IPackage installedPackage = packageManager.LocalRepository.FindPackage(packageName);
                    if (installedPackage == null)
                    {
                        string logMessage = "{0} is not installed. Cannot upgrade a non-existent package.".format_with(packageName);
                        var results = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                        results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));

                        if (configuration.RegularOuptut) this.Log().Error(ChocolateyLoggers.Important, logMessage);
                        continue;
                    }

                    IPackage availablePackage = packageManager.SourceRepository.FindPackage(packageName, version, configuration.Prerelease, allowUnlisted: false);
                    if (availablePackage == null)
                    {
                        string logMessage = "{0} was not found with the source(s) listed.{1} If you specified a particular version and are receiving this message, it is possible that the package name exists but the version does not.{1} Version: \"{2}\"{1} Source(s): \"{3}\"".format_with(packageName, Environment.NewLine, configuration.Version, configuration.Source);
                        var results = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, version.to_string(), null));
                        results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));

                        if (configuration.RegularOuptut) this.Log().Error(ChocolateyLoggers.Important, logMessage);
                        continue;
                    }

                    if ((installedPackage.Version > availablePackage.Version))
                    {
                        string logMessage = "{0} v{1} is newer than the most recent.{2} You must be smarter than the average bear...".format_with(installedPackage.Id, installedPackage.Version, Environment.NewLine);
                        var results = packageInstalls.GetOrAdd(packageName, new PackageResult(installedPackage, ApplicationParameters.PackagesLocation));
                        results.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));

                        if (configuration.RegularOuptut) this.Log().Info(ChocolateyLoggers.Important, logMessage);
                        continue;
                    }

                    if ((installedPackage.Version == availablePackage.Version))
                    {
                        string logMessage = "{0} v{1} is the latest version available based on your source(s).".format_with(installedPackage.Id, installedPackage.Version);
                        var results = packageInstalls.GetOrAdd(packageName, new PackageResult(installedPackage, ApplicationParameters.PackagesLocation));
                        if (results.Messages.Count((p) => p.Message == ApplicationParameters.Messages.ContinueChocolateyAction) == 0)
                        {
                            results.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                        }

                        if (configuration.RegularOuptut) this.Log().Info(logMessage);
                        if (!configuration.Force) continue;
                    }

                    if (availablePackage.Version > installedPackage.Version || configuration.Force)
                    {
                        if (availablePackage.Version > installedPackage.Version)
                        {
                            if (configuration.RegularOuptut)
                            {
                                this.Log().Warn("You have {0} v{1} installed. Version {2} is available based on your source(s)".format_with(installedPackage.Id, installedPackage.Version, availablePackage.Version));
                            }
                            else
                            {
                                //last one is whether this package is pinned or not
                                this.Log().Info("{0}|{1}|{2}|{3}".format_with(installedPackage.Id, installedPackage.Version, availablePackage.Version, "false"));
                            }
                        }

                        if (performAction)
                        {
                            using (packageManager.SourceRepository.StartOperation(
                                RepositoryOperationNames.Install,
                                packageName,
                                version == null ? null : version.ToString()))
                            {
                                packageManager.UpdatePackage(availablePackage, updateDependencies: !configuration.IgnoreDependencies, allowPrereleaseVersions: configuration.Prerelease);
                            }
                        }
                    }
                }
            }

            return packageInstalls;
        }

        public void uninstall_noop(ChocolateyConfiguration configuration, Action<PackageResult> continueAction)
        {
            var results = uninstall_run(configuration, continueAction, performAction: false);
            foreach (var packageResult in results.or_empty_list_if_null())
            {
                var package = packageResult.Value.Package;
                if (package != null) this.Log().Warn("Would have uninstalled {0} v{1}.".format_with(package.Id, package.Version.to_string()));
            }
        }

        public ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration configuration, Action<PackageResult> continueAction)
        {
            return uninstall_run(configuration, continueAction, performAction: true);
        }

        public ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration configuration, Action<PackageResult> continueAction, bool performAction)
        {
            var packageUninstalls = new ConcurrentDictionary<string, PackageResult>();

            SemanticVersion version = configuration.Version != null ? new SemanticVersion(configuration.Version) : null;
            var packageManager = NugetCommon.GetPackageManager(configuration, _nugetLogger,
                                                               installSuccessAction: null,
                                                               uninstallSuccessAction: (e) =>
                                                                   {
                                                                       var pkg = e.Package;
                                                                       "chocolatey".Log().Info(ChocolateyLoggers.Important, " {0} has been successfully uninstalled.".format_with(pkg.Id));
                                                                   });

            packageManager.PackageUninstalling += (s, e) =>
                {
                    var pkg = e.Package;

                    // this section fires twice sometimes, like for older packages in a sxs install... 
                    var results = packageUninstalls.GetOrAdd(pkg.Id.to_lower() + "." + pkg.Version.to_string(), new PackageResult(pkg, e.InstallPath));
                    string logMessage = "{0}{1} v{2}{3}".format_with(Environment.NewLine, pkg.Id, pkg.Version.to_string(), configuration.Force ? " (forced)" : string.Empty);
                    if (results.Messages.Count((p) => p.Message == ApplicationParameters.Messages.NugetEventActionHeader) == 0)
                    {
                        results.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.NugetEventActionHeader));
                        "chocolatey".Log().Info(ChocolateyLoggers.Important, logMessage);
                    }
                    else
                    {
                        "chocolatey".Log().Debug(ChocolateyLoggers.Important, "Another time through!{0}{1}".format_with(Environment.NewLine, logMessage));
                    }

                    // is this the latest version or have you passed --sxs? This is the only way you get through to the continue action.
                    var latestVersion = packageManager.LocalRepository.FindPackage(e.Package.Id);
                    if (latestVersion.Version == pkg.Version || configuration.AllowMultipleVersions)
                    {
                        results.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));
                        if (continueAction != null) continueAction.Invoke(results);
                    }
                    else
                    {
                        //todo:allow cleaning of pkgstore files      
                    }
                };

            set_package_names_if_all_is_specified(configuration, () =>
                {
                    // force remove the item, ignore the dependencies 
                    // as those are going to be picked up anyway
                    configuration.Force = true;
                    configuration.ForceDependencies = false;
                });

            foreach (string packageName in configuration.PackageNames.Split(new[] {ApplicationParameters.PackageNamesSeparator}, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null())
            {
                if (packageName.to_lower().EndsWith(".config"))
                {
                    //todo: determine if .config file for packages .config
                    //todo: determine if config file exists
                }
                else
                {
                    IList<IPackage> installedPackageVersions = new List<IPackage>();
                    if (string.IsNullOrWhiteSpace(configuration.Version))
                    {
                        installedPackageVersions = packageManager.LocalRepository.FindPackagesById(packageName).OrderBy((p) => p.Version).ToList();
                    }
                    else
                    {
                        IPackage installedPackage = packageManager.LocalRepository.FindPackage(packageName);
                        if (installedPackage != null) installedPackageVersions.Add(installedPackage);
                    }

                    if (installedPackageVersions.Count == 0)
                    {
                        string logMessage = "{0} is not installed. Cannot uninstall a non-existent package.".format_with(packageName);
                        var results = packageUninstalls.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                        results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));

                        if (configuration.RegularOuptut) this.Log().Error(ChocolateyLoggers.Important, logMessage);
                        continue;
                    }

                    var packageVersionsToRemove = installedPackageVersions.ToList();
                    if (installedPackageVersions.Count != 1 && !configuration.AllVersions)
                    {
                        packageVersionsToRemove.Clear();
                        this.Log().Info(ChocolateyLoggers.Important, "Which version of {0} would you like to uninstall?".format_with(packageName));

                        int counter = 1;
                        IDictionary<int, IPackage> choices = new Dictionary<int, IPackage>();
                        foreach (var installedVersion in installedPackageVersions.or_empty_list_if_null())
                        {
                            choices.Add(counter, installedVersion);
                            this.Log().Info(" {0}) {1}".format_with(counter, installedVersion.Version.to_string()));

                            counter++;
                        }

                        this.Log().Info(" {0}) All versions".format_with(counter));
                        var selection = Console.ReadLine();

                        int selected = -1;
                        if (!int.TryParse(selection, out selected) || selected <= 0 || selected > counter)
                        {
                            string logMessage = "{0} was not a valid selection.".format_with(selection);
                            var results = packageUninstalls.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                            results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));

                            if (configuration.RegularOuptut) this.Log().Error(ChocolateyLoggers.Important, logMessage);
                            continue;
                        }


                        if (selection == counter.to_string())
                        {
                            packageVersionsToRemove = installedPackageVersions.ToList();
                            if (configuration.RegularOuptut) this.Log().Info(() => "You selected to remove all versions of {0}".format_with(packageName));
                        }
                        else
                        {
                            packageVersionsToRemove.Add(choices[selected]);
                            if (configuration.RegularOuptut) this.Log().Info(() => "You selected {0} v{1}".format_with(choices[selected].Id, choices[selected].Version.to_string()));
                        }
                    }


                    foreach (var packageVersion in packageVersionsToRemove)
                    {
                        if (performAction)
                        {
                            using (packageManager.SourceRepository.StartOperation(
                                RepositoryOperationNames.Install,
                                packageName,
                                version == null ? null : version.ToString()))
                            {
                                packageManager.UninstallPackage(packageVersion, forceRemove: configuration.Force, removeDependencies: configuration.ForceDependencies);
                            }
                        }
                        else
                        {
                            packageUninstalls.GetOrAdd(packageVersion.Id.to_lower() + "." + packageVersion.Version.to_string(), new PackageResult(packageVersion, ApplicationParameters.PackagesLocation));
                        }
                    }
                }
            }

            return packageUninstalls;
        }

        private void set_package_names_if_all_is_specified(ChocolateyConfiguration configuration, Action customAction)
        {
            if (configuration.PackageNames.is_equal_to("all"))
            {
                configuration.LocalOnly = true;
                var sources = configuration.Source;
                configuration.Source = ApplicationParameters.PackagesLocation;
                var localPackages = list_run(configuration, logResults: false);
                configuration.Source = sources;
                configuration.PackageNames = string.Join(ApplicationParameters.PackageNamesSeparator, localPackages.Select((p) => p.Key).or_empty_list_if_null());

                if (customAction != null) customAction.Invoke();
            }
        }
    }
}