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
    using platforms;
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

        public void list_noop(ChocolateyConfiguration config)
        {
            this.Log().Info("{0} would have searched for '{1}' against the following source(s) :\"{2}\"".format_with(
                ApplicationParameters.Name,
                config.Input,
                config.Source
                                ));
        }

        public ConcurrentDictionary<string, PackageResult> list_run(ChocolateyConfiguration config, bool logResults = true)
        {
            var packageResults = new ConcurrentDictionary<string, PackageResult>();

            foreach (var package in NugetList.GetPackages(config, _nugetLogger).or_empty_list_if_null())
            {
                if (logResults)
                {
                    this.Log().Info(config.Verbose ? ChocolateyLoggers.Important : ChocolateyLoggers.Normal, () => "{0} {1}".format_with(package.Id, package.Version.to_string()));
                    if (config.Verbose) this.Log().Info(() => " {0}{1} Description: {2}{1} Tags: {3}{1} Number of Downloads: {4}{1}".format_with(package.Title, Environment.NewLine, package.Description, package.Tags, package.DownloadCount <= 0 ? "n/a" : package.DownloadCount.to_string()));
                    // Maintainer(s):{3}{1} | package.Owners.join(", ") - null at the moment
                }
                else
                {
                    this.Log().Debug(() => "[Nuget] {0} {1}".format_with(package.Id, package.Version.to_string()));
                }
                packageResults.GetOrAdd(package.Id, new PackageResult(package, null));
            }

            return packageResults;
        }

        public void pack_noop(ChocolateyConfiguration config)
        {
            this.Log().Info("{0} would have searched for a nuspec file in \"{1}\" and attempted to compile it.".format_with(
                ApplicationParameters.Name,
                _fileSystem.get_current_directory()
                                ));
        }

        public string validate_and_return_package_file(ChocolateyConfiguration config, string extension)
        {
            Func<IFileSystem, string> getLocalFiles = (fileSystem) =>
                {
                    var filesFound = fileSystem.get_files(fileSystem.get_current_directory(), "*" + extension).or_empty_list_if_null();
                    Ensure.that(() => filesFound)
                          .meets((files) => files.Count() == 1,
                                 (name, value) =>
                                 {
                                     throw new FileNotFoundException("No {0} files (or more than 1) were found to build in '{1}'. Please specify the {0} file or try in a different directory.".format_with(extension, _fileSystem.get_current_directory()));
                                 });

                    return filesFound.FirstOrDefault();
                };

            string filePath = !string.IsNullOrWhiteSpace(config.Input) ? config.Input : getLocalFiles.Invoke(_fileSystem);
            Ensure.that(() => filePath).meets((file) => _fileSystem.get_file_extension(file).is_equal_to(extension) && _fileSystem.file_exists(file),
                                              (name, value) => { throw new ArgumentException("File specified is either not found or not a {0} file. '{1}'".format_with(extension, value)); });

            return filePath;
        }

        public void pack_run(ChocolateyConfiguration config)
        {
            string nuspecFilePath = validate_and_return_package_file(config, Constants.ManifestExtension);
            var nuspecDirectory = _fileSystem.get_full_path(_fileSystem.get_directory_name(nuspecFilePath));

            IDictionary<string, string> properties = new Dictionary<string, string>();
            // Set the version property if the flag is set
            if (!string.IsNullOrWhiteSpace(config.Version))
            {
                properties["version"] = config.Version;
            }

            // Initialize the property provider based on what was passed in using the properties flag
            var propertyProvider = new DictionaryPropertyProvider(properties);

            //if (config.PlatformType != PlatformType.Windows)
            //{
            //}

            var builder = new PackageBuilder(nuspecFilePath,_fileSystem.get_current_directory(), propertyProvider, includeEmptyDirectories: true);
            if (!string.IsNullOrWhiteSpace(config.Version))
            {
                builder.Version = new SemanticVersion(config.Version);
            }

            string outputFile = builder.Id + "." + builder.Version + Constants.PackageExtension;
            string outputPath = _fileSystem.combine_paths(nuspecDirectory, outputFile);

            this.Log().Info(() => "Attempting to build package from '{0}'.".format_with(_fileSystem.get_file_name(nuspecFilePath)));

            //IPackage package = 
            NugetPack.BuildPackage(builder, _fileSystem, outputPath);
            //todo: v1 analyze package
            //if (package != null)
            //{
            //    AnalyzePackage(package);
            //}

            this.Log().Info(ChocolateyLoggers.Important, () => "Successfully created package '{0}'".format_with(outputFile));
        }

        public void push_noop(ChocolateyConfiguration config)
        {
            string nupkgFilePath = validate_and_return_package_file(config, Constants.PackageExtension);
            this.Log().Info(() => "Would have attempted to push '{0}' to source '{1}'.".format_with(_fileSystem.get_file_name(nupkgFilePath), config.Source));
        }

        public void push_run(ChocolateyConfiguration config)
        {
            string nupkgFilePath = validate_and_return_package_file(config, Constants.PackageExtension);
            if (config.RegularOuptut) this.Log().Info(() => "Attempting to push {0} to {1}".format_with(_fileSystem.get_file_name(nupkgFilePath), config.Source));

            NugetPush.push_package(config, nupkgFilePath);
        }

        public void install_noop(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            //todo: noop should see if packages are already installed and adjust message, amiright?!

            this.Log().Info("{0} would have used NuGet to install packages (if they are not already installed):{1}{2}".format_with(
                ApplicationParameters.Name,
                Environment.NewLine,
                config.PackageNames
                                ));

            var tempInstallsLocation = _fileSystem.combine_paths(_fileSystem.get_temp_path(), ApplicationParameters.Name, "TempInstalls_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_ffff"));
            _fileSystem.create_directory_if_not_exists(tempInstallsLocation);
            ApplicationParameters.PackagesLocation = tempInstallsLocation;

            install_run(config, continueAction);

            _fileSystem.delete_directory(tempInstallsLocation, recursive: true);
        }

        public ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.PackagesLocation);
            var packageInstalls = new ConcurrentDictionary<string, PackageResult>();

            //todo: handle all

            SemanticVersion version = config.Version != null ? new SemanticVersion(config.Version) : null;
            var packageManager = NugetCommon.GetPackageManager(config, _nugetLogger,
                                                               installSuccessAction: (e) =>
                                                                   {
                                                                       var pkg = e.Package;
                                                                       var results = packageInstalls.GetOrAdd(pkg.Id.to_lower(), new PackageResult(pkg, e.InstallPath));
                                                                       results.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));

                                                                       if (continueAction != null) continueAction.Invoke(results);
                                                                   },
                                                               uninstallSuccessAction: null);

            foreach (string packageName in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null())
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
                    if (installedPackage != null && (version == null || version == installedPackage.Version) && !config.Force)
                    {
                        string logMessage = "{0} v{1} already installed.{2} Use --force to reinstall, specify a version to install, or try upgrade.".format_with(installedPackage.Id, installedPackage.Version, Environment.NewLine);
                        var results = packageInstalls.GetOrAdd(packageName, new PackageResult(installedPackage, ApplicationParameters.PackagesLocation));
                        results.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                        this.Log().Warn(ChocolateyLoggers.Important, logMessage);
                        continue;
                    }

                    if (installedPackage != null && (version == null || version == installedPackage.Version) && config.Force)
                    {
                        this.Log().Debug(() => "{0} v{1} already installed. Forcing reinstall.".format_with(installedPackage.Id, installedPackage.Version));
                        version = installedPackage.Version;
                    }

                    IPackage availablePackage = packageManager.SourceRepository.FindPackage(packageName, version, config.Prerelease, allowUnlisted: false);
                    if (availablePackage == null)
                    {
                        var logMessage = "{0} not installed. The package was not found with the source(s) listed.{1} If you specified a particular version and are receiving this message, it is possible that the package name exists but the version does not.{1} Version: \"{2}\"{1} Source(s): \"{3}\"".format_with(packageName, Environment.NewLine, config.Version, config.Source);
                        this.Log().Error(ChocolateyLoggers.Important, logMessage);
                        var results = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, version.to_string(), null));
                        results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                        continue;
                    }

                    if (installedPackage != null && (installedPackage.Version == availablePackage.Version))
                    {
                        packageManager.UninstallPackage(installedPackage, forceRemove: config.Force, removeDependencies: config.ForceDependencies);
                    }

                    using (packageManager.SourceRepository.StartOperation(
                        RepositoryOperationNames.Install,
                        packageName,
                        version == null ? null : version.ToString()))
                    {
                        packageManager.InstallPackage(availablePackage, config.IgnoreDependencies, config.Prerelease);
                        //packageManager.InstallPackage(packageName, version, configuration.IgnoreDependencies, configuration.Prerelease);
                    }
                }
            }

            return packageInstalls;
        }

        public void upgrade_noop(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            config.Force = false;
            upgrade_run(config, continueAction, performAction: false);
        }

        public ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            return upgrade_run(config, continueAction, performAction: true);
        }

        public ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config, Action<PackageResult> continueAction, bool performAction)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.PackagesLocation);
            var packageInstalls = new ConcurrentDictionary<string, PackageResult>();

            SemanticVersion version = config.Version != null ? new SemanticVersion(config.Version) : null;
            var packageManager = NugetCommon.GetPackageManager(config, _nugetLogger,
                                                               installSuccessAction: (e) =>
                                                                   {
                                                                       var pkg = e.Package;
                                                                       var results = packageInstalls.GetOrAdd(pkg.Id.to_lower(), new PackageResult(pkg, e.InstallPath));
                                                                       results.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));

                                                                       if (continueAction != null) continueAction.Invoke(results);
                                                                   },
                                                               uninstallSuccessAction: null);

            set_package_names_if_all_is_specified(config, () => { config.IgnoreDependencies = true; });

            foreach (string packageName in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null())
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

                        if (config.RegularOuptut) this.Log().Error(ChocolateyLoggers.Important, logMessage);
                        continue;
                    }

                    IPackage availablePackage = packageManager.SourceRepository.FindPackage(packageName, version, config.Prerelease, allowUnlisted: false);
                    if (availablePackage == null)
                    {
                        string logMessage = "{0} was not found with the source(s) listed.{1} If you specified a particular version and are receiving this message, it is possible that the package name exists but the version does not.{1} Version: \"{2}\"{1} Source(s): \"{3}\"".format_with(packageName, Environment.NewLine, config.Version, config.Source);
                        var results = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, version.to_string(), null));
                        results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));

                        if (config.RegularOuptut) this.Log().Error(ChocolateyLoggers.Important, logMessage);
                        continue;
                    }

                    if ((installedPackage.Version > availablePackage.Version))
                    {
                        string logMessage = "{0} v{1} is newer than the most recent.{2} You must be smarter than the average bear...".format_with(installedPackage.Id, installedPackage.Version, Environment.NewLine);
                        var results = packageInstalls.GetOrAdd(packageName, new PackageResult(installedPackage, ApplicationParameters.PackagesLocation));
                        results.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));

                        if (config.RegularOuptut) this.Log().Info(ChocolateyLoggers.Important, logMessage);
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

                        if (config.RegularOuptut) this.Log().Info(logMessage);
                        if (!config.Force) continue;
                    }

                    if (availablePackage.Version > installedPackage.Version || config.Force)
                    {
                        if (availablePackage.Version > installedPackage.Version)
                        {
                            if (config.RegularOuptut)
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
                                packageManager.UpdatePackage(availablePackage, updateDependencies: !config.IgnoreDependencies, allowPrereleaseVersions: config.Prerelease);
                            }
                        }
                    }
                }
            }

            return packageInstalls;
        }

        public void uninstall_noop(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            var results = uninstall_run(config, continueAction, performAction: false);
            foreach (var packageResult in results.or_empty_list_if_null())
            {
                var package = packageResult.Value.Package;
                if (package != null) this.Log().Warn("Would have uninstalled {0} v{1}.".format_with(package.Id, package.Version.to_string()));
            }
        }

        public ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            return uninstall_run(config, continueAction, performAction: true);
        }

        public ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config, Action<PackageResult> continueAction, bool performAction)
        {
            var packageUninstalls = new ConcurrentDictionary<string, PackageResult>();

            SemanticVersion version = config.Version != null ? new SemanticVersion(config.Version) : null;
            var packageManager = NugetCommon.GetPackageManager(config, _nugetLogger,
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
                    string logMessage = "{0}{1} v{2}{3}".format_with(Environment.NewLine, pkg.Id, pkg.Version.to_string(), config.Force ? " (forced)" : string.Empty);
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
                    if (latestVersion.Version == pkg.Version || config.AllowMultipleVersions)
                    {
                        results.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));
                        if (continueAction != null) continueAction.Invoke(results);
                    }
                    else
                    {
                        //todo:allow cleaning of pkgstore files      
                    }
                };

            set_package_names_if_all_is_specified(config, () =>
                {
                    // force remove the item, ignore the dependencies 
                    // as those are going to be picked up anyway
                    config.Force = true;
                    config.ForceDependencies = false;
                });

            foreach (string packageName in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null())
            {
                if (packageName.to_lower().EndsWith(".config"))
                {
                    //todo: determine if .config file for packages .config
                    //todo: determine if config file exists
                }
                else
                {
                    IList<IPackage> installedPackageVersions = new List<IPackage>();
                    if (string.IsNullOrWhiteSpace(config.Version))
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

                        if (config.RegularOuptut) this.Log().Error(ChocolateyLoggers.Important, logMessage);
                        continue;
                    }

                    var packageVersionsToRemove = installedPackageVersions.ToList();
                    if (installedPackageVersions.Count != 1 && !config.AllVersions)
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

                            if (config.RegularOuptut) this.Log().Error(ChocolateyLoggers.Important, logMessage);
                            continue;
                        }


                        if (selection == counter.to_string())
                        {
                            packageVersionsToRemove = installedPackageVersions.ToList();
                            if (config.RegularOuptut) this.Log().Info(() => "You selected to remove all versions of {0}".format_with(packageName));
                        }
                        else
                        {
                            packageVersionsToRemove.Add(choices[selected]);
                            if (config.RegularOuptut) this.Log().Info(() => "You selected {0} v{1}".format_with(choices[selected].Id, choices[selected].Version.to_string()));
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
                                packageManager.UninstallPackage(packageVersion, forceRemove: config.Force, removeDependencies: config.ForceDependencies);
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

        private void set_package_names_if_all_is_specified(ChocolateyConfiguration config, Action customAction)
        {
            if (config.PackageNames.is_equal_to("all"))
            {
                config.LocalOnly = true;
                var sources = config.Source;
                config.Source = ApplicationParameters.PackagesLocation;
                var localPackages = list_run(config, logResults: false);
                config.Source = sources;
                config.PackageNames = string.Join(ApplicationParameters.PackageNamesSeparator, localPackages.Select((p) => p.Key).or_empty_list_if_null());

                if (customAction != null) customAction.Invoke();
            }
        }
    }
}