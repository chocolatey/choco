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

namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NuGet;
    using adapters;
    using commandline;
    using configuration;
    using domain;
    using guards;
    using logging;
    using nuget;
    using platforms;
    using results;
    using tolerance;
    using DateTime = adapters.DateTime;
    using Environment = System.Environment;
    using IFileSystem = filesystem.IFileSystem;

    //todo - this monolith is too large. Refactor once test coverage is up.

    public class NugetService : INugetService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _nugetLogger;
        private readonly IChocolateyPackageInformationService _packageInfoService;
        private readonly IFilesService _filesService;
        private readonly Lazy<IDateTime> datetime_initializer = new Lazy<IDateTime>(() => new DateTime());

        private IDateTime DateTime
        {
            get { return datetime_initializer.Value; }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="NugetService" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="nugetLogger">The nuget logger</param>
        /// <param name="packageInfoService">Package information service</param>
        /// <param name="filesService">The files service</param>
        public NugetService(IFileSystem fileSystem, ILogger nugetLogger, IChocolateyPackageInformationService packageInfoService, IFilesService filesService)
        {
            _fileSystem = fileSystem;
            _nugetLogger = nugetLogger;
            _packageInfoService = packageInfoService;
            _filesService = filesService;
        }

        public SourceType SourceType
        {
            get { return SourceType.normal; }
        }

        public void ensure_source_app_installed(ChocolateyConfiguration config, Action<PackageResult> ensureAction)
        {
            // nothing to do. Nuget.Core is already part of Chocolatey
        }

        public void list_noop(ChocolateyConfiguration config)
        {
            this.Log().Info("{0} would have searched for '{1}' against the following source(s) :\"{2}\"".format_with(
                ApplicationParameters.Name,
                config.Input,
                config.Sources
                                ));
        }

        public IEnumerable<PackageResult> list_run(ChocolateyConfiguration config)
        {
            int count = 0;

            if (config.ListCommand.LocalOnly)
            {
                config.Sources = ApplicationParameters.PackagesLocation;
                config.Prerelease = true;
            }

            if (config.RegularOutput) this.Log().Debug(() => "Running list with the following filter = '{0}'".format_with(config.Input));
            if (config.RegularOutput) this.Log().Debug(() => "--- Start of List ---");
            foreach (var pkg in NugetList.GetPackages(config, _nugetLogger))

            {
                var package = pkg; // for lamda access
                if (!config.QuietOutput)
                {
                    this.Log().Info(config.Verbose ? ChocolateyLoggers.Important : ChocolateyLoggers.Normal, () => "{0} {1}".format_with(package.Id, package.Version.to_string()));
                    if (config.RegularOutput && config.Verbose) this.Log().Info(() => " {0}{1} Description: {2}{1} Tags: {3}{1} Number of Downloads: {4}{1}".format_with(package.Title.escape_curly_braces(), Environment.NewLine, package.Description.escape_curly_braces(), package.Tags.escape_curly_braces(), package.DownloadCount <= 0 ? "n/a" : package.DownloadCount.to_string()));
                }
                else
                {
                    this.Log().Debug(() => "{0} {1}".format_with(package.Id, package.Version.to_string()));
                }
                count++;

                yield return new PackageResult(package, null, config.Sources);
            }

            if (config.RegularOutput) this.Log().Debug(() => "--- End of List ---");
            if (config.RegularOutput)
            {
                this.Log().Warn(() => @"{0} packages {1}.".format_with(count, config.ListCommand.LocalOnly ? "installed" : "found"));
            }
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
                    var filesFound = fileSystem.get_files(fileSystem.get_current_directory(), "*" + extension).ToList().or_empty_list_if_null();
                    Ensure.that(() => filesFound)
                          .meets((files) => files.Count() == 1,
                                 (name, value) => { throw new FileNotFoundException("No {0} files (or more than 1) were found to build in '{1}'. Please specify the {0} file or try in a different directory.".format_with(extension, _fileSystem.get_current_directory())); });

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
            if (string.IsNullOrWhiteSpace(nuspecDirectory)) nuspecDirectory = _fileSystem.get_current_directory();

            IDictionary<string, string> properties = new Dictionary<string, string>();
            // Set the version property if the flag is set
            if (!string.IsNullOrWhiteSpace(config.Version))
            {
                properties["version"] = config.Version;
            }

            // Initialize the property provider based on what was passed in using the properties flag
            var propertyProvider = new DictionaryPropertyProvider(properties);

            var basePath = nuspecDirectory;
            if (config.Information.PlatformType != PlatformType.Windows)
            {
                //bug with nuspec and tools/** folder location on Windows.
                basePath = "./";
            }

            var builder = new PackageBuilder(nuspecFilePath, basePath, propertyProvider, includeEmptyDirectories: true);
            if (!string.IsNullOrWhiteSpace(config.Version))
            {
                builder.Version = new SemanticVersion(config.Version);
            }

            string outputFile = builder.Id + "." + builder.Version + Constants.PackageExtension;
            string outputPath = _fileSystem.combine_paths(_fileSystem.get_current_directory(), outputFile);

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
            this.Log().Info(() => "Would have attempted to push '{0}' to source '{1}'.".format_with(_fileSystem.get_file_name(nupkgFilePath), config.Sources));
        }

        public void push_run(ChocolateyConfiguration config)
        {
            string nupkgFilePath = validate_and_return_package_file(config, Constants.PackageExtension);
            if (config.RegularOutput) this.Log().Info(() => "Attempting to push {0} to {1}".format_with(_fileSystem.get_file_name(nupkgFilePath), config.Sources));

            NugetPush.push_package(config, _fileSystem.get_full_path(nupkgFilePath));

            if (config.RegularOutput) this.Log().Warn(ChocolateyLoggers.Important, () => @"

Your package may be subject to moderation. A moderator will review the
package prior to acceptance. You should have received an email. If you
don't hear back from moderators within 1-3 business days, please reply
to the email and ask for status or use contact site admins on the
package page to contact moderators.

Please ensure your registered email address is correct and emails from
chocolateywebadmin at googlegroups dot com are not being sent to your
spam/junk folder.");
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

            var installLocation = ApplicationParameters.PackagesLocation;
            ApplicationParameters.PackagesLocation = tempInstallsLocation;

            install_run(config, continueAction);

            _fileSystem.delete_directory(tempInstallsLocation, recursive: true);
            ApplicationParameters.PackagesLocation = installLocation;
        }

        public ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.PackagesLocation);
            var packageInstalls = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            //todo: handle all

            SemanticVersion version = config.Version != null ? new SemanticVersion(config.Version) : null;

            IList<string> packageNames = config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null().ToList();
            if (packageNames.Count == 1)
            {
                var packageName = packageNames.DefaultIfEmpty(string.Empty).FirstOrDefault();
                if (packageName.EndsWith(Constants.PackageExtension) || packageName.EndsWith(Constants.ManifestExtension))
                {
                    this.Log().Debug("Updating source and package name to handle *.nupkg or *.nuspec file.");
                    packageNames.Clear();

                    config.Sources = _fileSystem.get_directory_name(_fileSystem.get_full_path(packageName));

                    if (packageName.EndsWith(Constants.ManifestExtension))
                    {
                        packageNames.Add(_fileSystem.get_file_name_without_extension(packageName));

                        this.Log().Debug("Building nuspec file prior to install.");
                        config.Input = packageName;
                        // build package
                        pack_run(config);
                    }
                    else
                    {
                        var packageFile = new OptimizedZipPackage(_fileSystem.get_full_path(packageName));
                        packageNames.Add(packageFile.Id);
                    }
                }
            }

            // this is when someone points the source directly at a nupkg 
            // e.g. -source c:\somelocation\somewhere\packagename.nupkg
            if (config.Sources.to_string().EndsWith(Constants.PackageExtension))
            {
                config.Sources = _fileSystem.get_directory_name(_fileSystem.get_full_path(config.Sources));
            }

            var packageManager = NugetCommon.GetPackageManager(
                config, _nugetLogger,
                installSuccessAction: (e) =>
                    {
                        var pkg = e.Package;
                        var packageResult = packageInstalls.GetOrAdd(pkg.Id.to_lower(), new PackageResult(pkg, e.InstallPath));
                        packageResult.InstallLocation = e.InstallPath;
                        packageResult.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));

                        if (continueAction != null) continueAction.Invoke(packageResult);
                    },
                uninstallSuccessAction: null,
                addUninstallHandler: true);


            foreach (string packageName in packageNames.or_empty_list_if_null())
            {
                //todo: get smarter about realizing multiple versions have been installed before and allowing that

                remove_rollback_directory_if_exists(packageName);

                IPackage installedPackage = packageManager.LocalRepository.FindPackage(packageName);

                if (installedPackage != null && (version == null || version == installedPackage.Version) && !config.Force)
                {
                    string logMessage = "{0} v{1} already installed.{2} Use --force to reinstall, specify a version to install, or try upgrade.".format_with(installedPackage.Id, installedPackage.Version, Environment.NewLine);
                    var nullResult = packageInstalls.GetOrAdd(packageName, new PackageResult(installedPackage, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, installedPackage.Id)));
                    nullResult.Messages.Add(new ResultMessage(ResultType.Warn, logMessage));
                    nullResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
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
                    var logMessage = "{0} not installed. The package was not found with the source(s) listed.{1} If you specified a particular version and are receiving this message, it is possible that the package name exists but the version does not.{1} Version: \"{2}\"{1} Source(s): \"{3}\"".format_with(packageName, Environment.NewLine, config.Version, config.Sources);
                    this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    var noPkgResult = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, version.to_string(), null));
                    noPkgResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                    continue;
                }

                if (installedPackage != null && (installedPackage.Version == availablePackage.Version) && config.Force)
                {
                    var forcedResult = packageInstalls.GetOrAdd(packageName, new PackageResult(installedPackage, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, installedPackage.Id)));
                    forcedResult.Messages.Add(new ResultMessage(ResultType.Note, "Backing up and removing old version"));

                    backup_existing_version(config, installedPackage, _packageInfoService.get_package_information(installedPackage));

                    try
                    {
                        packageManager.UninstallPackage(installedPackage, forceRemove: config.Force, removeDependencies: config.ForceDependencies);
                        if (!forcedResult.InstallLocation.is_equal_to(ApplicationParameters.PackagesLocation))
                        {
                            _fileSystem.delete_directory_if_exists(forcedResult.InstallLocation, recursive: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        string logMessage = "{0}:{1} {2}".format_with("Unable to remove existing package prior to forced reinstall", Environment.NewLine, ex.Message);
                        this.Log().Warn(logMessage);
                        forcedResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                    }
                }

                try
                {
                    using (packageManager.SourceRepository.StartOperation(
                        RepositoryOperationNames.Install,
                        packageName,
                        version == null ? null : version.ToString()))
                    {
                        packageManager.InstallPackage(availablePackage, ignoreDependencies: config.IgnoreDependencies, allowPrereleaseVersions: config.Prerelease);
                        //packageManager.InstallPackage(packageName, version, configuration.IgnoreDependencies, configuration.Prerelease);
                    }
                }
                catch (Exception ex)
                {
                    var logMessage = "{0} not installed. An error occurred during installation:{1} {2}".format_with(packageName, Environment.NewLine, ex.Message);
                    this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    var errorResult = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, version.to_string(), null));
                    errorResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                    if (continueAction != null) continueAction.Invoke(errorResult);
                }
            }

            return packageInstalls;
        }

        public void remove_rollback_directory_if_exists(string packageName)
        {
            var rollbackDirectory = _fileSystem.combine_paths(ApplicationParameters.PackageBackupLocation, packageName);
            if (!_fileSystem.directory_exists(rollbackDirectory))
            {
                //search for folder
                var possibleRollbacks = _fileSystem.get_directories(ApplicationParameters.PackageBackupLocation, packageName + "*");
                if (possibleRollbacks != null && possibleRollbacks.Count() != 0)
                {
                    rollbackDirectory = possibleRollbacks.OrderByDescending(p => p).DefaultIfEmpty(string.Empty).FirstOrDefault();
                }
            }

            if (string.IsNullOrWhiteSpace(rollbackDirectory) || !_fileSystem.directory_exists(rollbackDirectory)) return;

            FaultTolerance.try_catch_with_logging_exception(
                () => _fileSystem.delete_directory_if_exists(rollbackDirectory, recursive: true),
                "Attempted to remove '{0}' but had an error:".format_with(rollbackDirectory),
                logWarningInsteadOfError: true);
        }

        public ConcurrentDictionary<string, PackageResult> upgrade_noop(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            config.Force = false;
            return upgrade_run(config, continueAction, performAction: false);
        }

        public ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            return upgrade_run(config, continueAction, performAction: true);
        }

        public ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config, Action<PackageResult> continueAction, bool performAction)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.PackagesLocation);
            var packageInstalls = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            SemanticVersion version = config.Version != null ? new SemanticVersion(config.Version) : null;
            var packageManager = NugetCommon.GetPackageManager(
                config,
                _nugetLogger,
                installSuccessAction: (e) =>
                    {
                        var pkg = e.Package;
                        var packageResult = packageInstalls.GetOrAdd(pkg.Id.to_lower(), new PackageResult(pkg, e.InstallPath));
                        packageResult.InstallLocation = e.InstallPath;
                        packageResult.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));

                        if (continueAction != null) continueAction.Invoke(packageResult);
                    },
                uninstallSuccessAction: null,
                addUninstallHandler: false);

            var configIgnoreDependencies = config.IgnoreDependencies;
            set_package_names_if_all_is_specified(config, () => { config.IgnoreDependencies = true; });
            config.IgnoreDependencies = configIgnoreDependencies;

            foreach (string packageName in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null())
            {
                remove_rollback_directory_if_exists(packageName);

                IPackage installedPackage = packageManager.LocalRepository.FindPackage(packageName);

                if (installedPackage == null)
                {
                    if (config.UpgradeCommand.FailOnNotInstalled)
                    {
                        string failLogMessage = "{0} is not installed. Cannot upgrade a non-existent package.".format_with(packageName);
                        var result = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                        result.Messages.Add(new ResultMessage(ResultType.Error, failLogMessage));
                        if (config.RegularOutput) this.Log().Error(ChocolateyLoggers.Important, failLogMessage);

                        continue;
                    }

                    string logMessage = @"{0} is not installed. Installing...".format_with(packageName);

                    if (config.RegularOutput) this.Log().Warn(ChocolateyLoggers.Important, logMessage);

                    var packageNames = config.PackageNames;
                    config.PackageNames = packageName;
                    if (config.Noop)
                    {
                        install_noop(config, continueAction);
                    }
                    else
                    {
                        var installResults = install_run(config, continueAction);
                        foreach (var result in installResults)
                        {
                            packageInstalls.GetOrAdd(result.Key, result.Value);
                        }
                    }

                    config.PackageNames = packageNames;
                    continue;
                }

                var pkgInfo = _packageInfoService.get_package_information(installedPackage);
                bool isPinned = pkgInfo != null && pkgInfo.IsPinned;


                IPackage availablePackage = packageManager.SourceRepository.FindPackage(packageName, version, config.Prerelease, allowUnlisted: false);
                if (availablePackage == null)
                {
                    string logMessage = "{0} was not found with the source(s) listed.{1} If you specified a particular version and are receiving this message, it is possible that the package name exists but the version does not.{1} Version: \"{2}\"{1} Source(s): \"{3}\"".format_with(packageName, Environment.NewLine, config.Version, config.Sources);
                    var unfoundResult = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, version.to_string(), null));

                    if (config.UpgradeCommand.FailOnUnfound)
                    {
                        unfoundResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                        if (config.RegularOutput) this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    }
                    else
                    {
                        unfoundResult.Messages.Add(new ResultMessage(ResultType.Warn, logMessage));
                        unfoundResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                        if (config.RegularOutput)
                        {
                            this.Log().Warn(ChocolateyLoggers.Important, logMessage);
                        }
                        else
                        {
                            //last one is whether this package is pinned or not
                            this.Log().Info("{0}|{1}|{1}|{2}".format_with(installedPackage.Id, installedPackage.Version, isPinned.to_string().to_lower()));
                        }
                    }

                    continue;
                }

                if (pkgInfo != null && pkgInfo.IsSideBySide)
                {
                    //todo: get smarter about realizing multiple versions have been installed before and allowing that
                }

                var packageResult = packageInstalls.GetOrAdd(packageName, new PackageResult(availablePackage, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, availablePackage.Id)));

                if ((installedPackage.Version > availablePackage.Version))
                {
                    string logMessage = "{0} v{1} is newer than the most recent.{2} You must be smarter than the average bear...".format_with(installedPackage.Id, installedPackage.Version, Environment.NewLine);
                    packageResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));

                    if (!config.UpgradeCommand.NotifyOnlyAvailableUpgrades)
                    {
                        if (config.RegularOutput)
                        {
                            this.Log().Info(ChocolateyLoggers.Important, logMessage);
                        }
                        else
                        {
                            this.Log().Info("{0}|{1}|{1}|{2}".format_with(installedPackage.Id, installedPackage.Version, isPinned.to_string().to_lower()));
                        }
                    }

                    continue;
                }

                if (installedPackage.Version == availablePackage.Version)
                {
                    string logMessage = "{0} v{1} is the latest version available based on your source(s).".format_with(installedPackage.Id, installedPackage.Version);

                    if (!config.Force)
                    {
                        if (packageResult.Messages.Count((p) => p.Message == ApplicationParameters.Messages.ContinueChocolateyAction) == 0)
                        {
                            packageResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                        }

                        if (!config.UpgradeCommand.NotifyOnlyAvailableUpgrades)
                        {
                            if (config.RegularOutput)
                            {
                                this.Log().Info(logMessage);
                            }
                            else
                            {
                                this.Log().Info("{0}|{1}|{2}|{3}".format_with(installedPackage.Id, installedPackage.Version, availablePackage.Version, isPinned.to_string().to_lower()));
                            }
                        }
                        
                        continue;
                    }

                    packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));
                    if (config.RegularOutput) this.Log().Info(logMessage);
                }

                if ((availablePackage.Version > installedPackage.Version) || config.Force)
                {
                    if (availablePackage.Version > installedPackage.Version)
                    {
                        string logMessage = "You have {0} v{1} installed. Version {2} is available based on your source(s).".format_with(installedPackage.Id, installedPackage.Version, availablePackage.Version);
                        packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));

                        if (config.RegularOutput)
                        {
                            this.Log().Warn(logMessage);
                        }
                        else
                        {
                            this.Log().Info("{0}|{1}|{2}|{3}".format_with(installedPackage.Id, installedPackage.Version, availablePackage.Version, isPinned.to_string().to_lower()));
                        }
                    }

                    if (isPinned)
                    {
                        string logMessage = "{0} is pinned. Skipping pinned package.".format_with(packageName);
                        packageResult.Messages.Add(new ResultMessage(ResultType.Warn, logMessage));
                        packageResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                        if (config.RegularOutput) this.Log().Warn(ChocolateyLoggers.Important, logMessage);

                        continue;
                    }

                    if (performAction)
                    {
                        try
                        {
                            using (packageManager.SourceRepository.StartOperation(
                                RepositoryOperationNames.Update,
                                packageName,
                                version == null ? null : version.ToString()))
                            {
                                rename_legacy_package_version(config, installedPackage, pkgInfo);
                                backup_existing_version(config, installedPackage, pkgInfo);
                                remove_shim_directors(config, installedPackage, pkgInfo);
                                if (config.Force && (installedPackage.Version == availablePackage.Version))
                                {
                                    FaultTolerance.try_catch_with_logging_exception(
                                        () => _fileSystem.delete_directory_if_exists(_fileSystem.combine_paths(ApplicationParameters.PackagesLocation, installedPackage.Id), recursive: true),
                                        "Error during force upgrade");
                                    packageManager.InstallPackage(availablePackage, config.IgnoreDependencies, config.Prerelease);
                                }
                                else
                                {
                                    packageManager.UpdatePackage(availablePackage, updateDependencies: !config.IgnoreDependencies, allowPrereleaseVersions: config.Prerelease);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var logMessage = "{0} not upgraded. An error occurred during installation:{1} {2}".format_with(packageName, Environment.NewLine, ex.Message);
                            this.Log().Error(ChocolateyLoggers.Important, logMessage);
                            packageResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                            if (continueAction != null) continueAction.Invoke(packageResult);
                        }
                    }
                }
            }

            return packageInstalls;
        }

        public void rename_legacy_package_version(ChocolateyConfiguration config, IPackage installedPackage, ChocolateyPackageInformation pkgInfo)
        {
            if (pkgInfo != null && pkgInfo.IsSideBySide) return;

            var installDirectory = _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, installedPackage.Id);
            if (!_fileSystem.directory_exists(installDirectory))
            {
                // if the folder has a version on it, we need to rename the folder first.
                var pathResolver = new ChocolateyPackagePathResolver(NugetCommon.GetNuGetFileSystem(config, _nugetLogger), useSideBySidePaths: true);
                installDirectory = pathResolver.GetInstallPath(installedPackage);
                if (_fileSystem.directory_exists(installDirectory))
                {
                    FaultTolerance.try_catch_with_logging_exception(
                        () => _fileSystem.move_directory(installDirectory, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, installedPackage.Id)),
                        "Error during old package rename");
                }
            }
        }

        public void backup_existing_version(ChocolateyConfiguration config, IPackage installedPackage, ChocolateyPackageInformation packageInfo)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.PackageBackupLocation);

            var pathResolver = NugetCommon.GetPathResolver(config, NugetCommon.GetNuGetFileSystem(config, _nugetLogger));
            var pkgInstallPath = pathResolver.GetInstallPath(installedPackage);
            if (!_fileSystem.directory_exists(pkgInstallPath))
            {
                var chocoPathResolver = pathResolver as ChocolateyPackagePathResolver;
                if (chocoPathResolver != null)
                {
                    chocoPathResolver.UseSideBySidePaths = !chocoPathResolver.UseSideBySidePaths;
                    pkgInstallPath = chocoPathResolver.GetInstallPath(installedPackage);
                }
            }

            if (_fileSystem.directory_exists(pkgInstallPath))
            {
                this.Log().Debug("Backing up existing {0} prior to upgrade.".format_with(installedPackage.Id));

                var backupLocation = pkgInstallPath.Replace(ApplicationParameters.PackagesLocation, ApplicationParameters.PackageBackupLocation);

                var errored = false;
                try
                {
                    _fileSystem.move_directory(pkgInstallPath, backupLocation);
                }
                catch (Exception ex)
                {
                    errored = true;
                    this.Log().Error("Error during backup (move phase):{0} {1}".format_with(Environment.NewLine, ex.Message));
                }
                finally
                {
                    try
                    {
                        _fileSystem.copy_directory(backupLocation, pkgInstallPath, overwriteExisting: true);
                    }
                    catch (Exception ex)
                    {
                        errored = true;
                        this.Log().Error("Error during backup (reset phase):{0} {1}".format_with(Environment.NewLine, ex.Message));
                    }
                }

                backup_changed_files(pkgInstallPath, config, packageInfo);

                if (errored)
                {
                    this.Log().Warn(ChocolateyLoggers.Important,
                                    @"There was an error accessing files. This could mean there is a 
 process locking the folder or files. Please make sure nothing is 
 running that would lock the files or folders in this directory prior 
 to upgrade. If the package fails to upgrade, this is likely the cause.");
                }
            }
        }

        public void backup_changed_files(string packageInstallPath, ChocolateyConfiguration config, ChocolateyPackageInformation packageInfo)
        {
            if (packageInfo == null || packageInfo.Package == null) return;

            var version = packageInfo.Package.Version.to_string();

            if (packageInfo.FilesSnapshot == null || packageInfo.FilesSnapshot.Files.Count == 0)
            {
                var configFiles = _fileSystem.get_files(packageInstallPath, ApplicationParameters.ConfigFileExtensions, SearchOption.AllDirectories);
                foreach (var file in configFiles.or_empty_list_if_null())
                {
                    var backupName = "{0}.{1}".format_with(_fileSystem.get_file_name(file), version);

                    FaultTolerance.try_catch_with_logging_exception(
                        () => _fileSystem.copy_file(file, _fileSystem.combine_paths(_fileSystem.get_directory_name(file), backupName), overwriteExisting: true),
                        "Error backing up configuration file");
                }
            }
            else
            {
                var currentFiles = _filesService.capture_package_files(packageInstallPath, config);
                foreach (var currentFile in currentFiles.Files.or_empty_list_if_null())
                {
                    var installedFile = packageInfo.FilesSnapshot.Files.FirstOrDefault(x => x.Path.is_equal_to(currentFile.Path));
                    if (installedFile != null)
                    {
                        if (!currentFile.Checksum.is_equal_to(installedFile.Checksum))
                        {
                            var backupName = "{0}.{1}".format_with(_fileSystem.get_file_name(currentFile.Path), version);
                            FaultTolerance.try_catch_with_logging_exception(
                                () => _fileSystem.copy_file(currentFile.Path, _fileSystem.combine_paths(_fileSystem.get_directory_name(currentFile.Path), backupName), overwriteExisting: true),
                                "Error backing up changed file");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove the shimgen director files from the package.
        /// These are .gui/.ignore files that may have been created during the installation 
        /// process and won't be pulled by the nuget package replacement.
        /// This usually happens when package maintainers haven't been very good about how 
        /// they create the files in the past (not using force with new-item typically throws 
        /// an error if the file exists).
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="installedPackage">The installed package.</param>
        /// <param name="pkgInfo">The package information.</param>
        private void remove_shim_directors(ChocolateyConfiguration config, IPackage installedPackage, ChocolateyPackageInformation pkgInfo)
        {
            var pathResolver = NugetCommon.GetPathResolver(config, NugetCommon.GetNuGetFileSystem(config, _nugetLogger));
            var pkgInstallPath = pathResolver.GetInstallPath(installedPackage);
            if (!_fileSystem.directory_exists(pkgInstallPath))
            {
                var chocoPathResolver = pathResolver as ChocolateyPackagePathResolver;
                if (chocoPathResolver != null)
                {
                    chocoPathResolver.UseSideBySidePaths = !chocoPathResolver.UseSideBySidePaths;
                    pkgInstallPath = chocoPathResolver.GetInstallPath(installedPackage);
                }
            }

            if (_fileSystem.directory_exists(pkgInstallPath))
            {
                var shimDirectorFiles = _fileSystem.get_files(pkgInstallPath, ApplicationParameters.ShimDirectorFileExtensions, SearchOption.AllDirectories);
                foreach (var file in shimDirectorFiles.or_empty_list_if_null())
                {
                    FaultTolerance.try_catch_with_logging_exception(
                        () => _fileSystem.delete_file(file),
                        "Error deleting shim director file");
                }
            }
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
            var packageUninstalls = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            SemanticVersion version = config.Version != null ? new SemanticVersion(config.Version) : null;
            var packageManager = NugetCommon.GetPackageManager(config, _nugetLogger,
                                                               installSuccessAction: null,
                                                               uninstallSuccessAction: (e) =>
                                                                   {
                                                                       var pkg = e.Package;
                                                                       "chocolatey".Log().Info(ChocolateyLoggers.Important, " {0} has been successfully uninstalled.".format_with(pkg.Id));
                                                                   },
                                                               addUninstallHandler: true);

            var loopCount = 0;
            packageManager.PackageUninstalling += (s, e) =>
                {
                    var pkg = e.Package;

                    // this section fires twice sometimes, like for older packages in a sxs install...
                    var packageResult = packageUninstalls.GetOrAdd(pkg.Id.to_lower() + "." + pkg.Version.to_string(), new PackageResult(pkg, e.InstallPath));
                    packageResult.InstallLocation = e.InstallPath;
                    string logMessage = "{0}{1} v{2}{3}".format_with(Environment.NewLine, pkg.Id, pkg.Version.to_string(), config.Force ? " (forced)" : string.Empty);
                    if (packageResult.Messages.Count((p) => p.Message == ApplicationParameters.Messages.NugetEventActionHeader) == 0)
                    {
                        packageResult.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.NugetEventActionHeader));
                        "chocolatey".Log().Info(ChocolateyLoggers.Important, logMessage);
                        loopCount = 0;
                    }
                    else
                    {
                        "chocolatey".Log().Debug(ChocolateyLoggers.Important, "Another time through!{0}{1}".format_with(Environment.NewLine, logMessage));
                        loopCount += 1;
                    }

                    if (loopCount == 10)
                    {
                        this.Log().Warn("Loop detected. Attempting to break out. Check for issues with {0}".format_with(pkg.Id));
                        return;
                    }

                    // is this the latest version or have you passed --sxs? This is the only way you get through to the continue action.
                    var latestVersion = packageManager.LocalRepository.FindPackage(e.Package.Id);
                    if (latestVersion.Version == pkg.Version || config.AllowMultipleVersions)
                    {
                        packageResult.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));
                        if (continueAction != null) continueAction.Invoke(packageResult);
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
                remove_rollback_directory_if_exists(packageName);

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
                    var missingResult = packageUninstalls.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                    missingResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));

                    if (config.RegularOutput) this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    continue;
                }

                var packageVersionsToRemove = installedPackageVersions.ToList();
                if (!config.AllVersions && installedPackageVersions.Count > 1)
                {
                    if (config.PromptForConfirmation)
                    {
                        packageVersionsToRemove.Clear();

                        IList<string> choices = new List<string>();
                        const string abortChoice = "None";
                        choices.Add(abortChoice);
                        foreach (var installedVersion in installedPackageVersions.or_empty_list_if_null())
                        {
                            choices.Add(installedVersion.Version.to_string());
                        }

                        const string allVersionsChoice = "All versions";
                        if (installedPackageVersions.Count != 1)
                        {
                            choices.Add(allVersionsChoice);
                        }

                        var selection = InteractivePrompt.prompt_for_confirmation("Which version of {0} would you like to uninstall?".format_with(packageName), choices, defaultChoice: null, requireAnswer: true);

                        if (string.IsNullOrWhiteSpace(selection)) continue;
                        if (selection.is_equal_to(abortChoice)) continue;
                        if (selection.is_equal_to(allVersionsChoice))
                        {
                            packageVersionsToRemove = installedPackageVersions.ToList();
                            if (config.RegularOutput) this.Log().Info(() => "You selected to remove all versions of {0}".format_with(packageName));
                        }
                        else
                        {
                            IPackage pkg = installedPackageVersions.FirstOrDefault((p) => p.Version.to_string().is_equal_to(selection));
                            packageVersionsToRemove.Add(pkg);
                            if (config.RegularOutput) this.Log().Info(() => "You selected {0} v{1}".format_with(pkg.Id, pkg.Version.to_string()));
                        }
                    }
                }

                foreach (var packageVersion in packageVersionsToRemove)
                {
                    var pkgInfo = _packageInfoService.get_package_information(packageVersion);
                    if (pkgInfo != null && pkgInfo.IsPinned)
                    {
                        string logMessage = "{0} is pinned. Skipping pinned package.".format_with(packageName);
                        var pinnedResult = packageUninstalls.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                        pinnedResult.Messages.Add(new ResultMessage(ResultType.Warn, logMessage));
                        pinnedResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                        if (config.RegularOutput) this.Log().Warn(ChocolateyLoggers.Important, logMessage);
                        continue;
                    }

                    if (performAction)
                    {
                        try
                        {
                            using (packageManager.SourceRepository.StartOperation(
                                RepositoryOperationNames.Install,
                                packageName,
                                version == null ? null : version.ToString()))
                            {
                                rename_legacy_package_version(config, packageVersion, pkgInfo);
                                backup_existing_version(config, packageVersion, pkgInfo);
                                packageManager.UninstallPackage(packageVersion, forceRemove: config.Force, removeDependencies: config.ForceDependencies);
                                ensure_nupkg_is_removed(packageVersion, pkgInfo);
                                remove_installation_files(packageVersion, pkgInfo);
                            }
                        }
                        catch (Exception ex)
                        {
                            var logMessage = "{0} not uninstalled. An error occurred during uninstall:{1} {2}".format_with(packageName, Environment.NewLine, ex.Message);
                            this.Log().Error(ChocolateyLoggers.Important, logMessage);
                            var result = packageUninstalls.GetOrAdd(packageVersion.Id.to_lower() + "." + packageVersion.Version.to_string(), new PackageResult(packageVersion, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, packageVersion.Id)));
                            result.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                            // do not call continueAction - will result in multiple passes
                        }
                    }
                    else
                    {
                        // continue action won't be found b/c we are not actually uninstalling (this is noop)
                        var result = packageUninstalls.GetOrAdd(packageVersion.Id.to_lower() + "." + packageVersion.Version.to_string(), new PackageResult(packageVersion, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, packageVersion.Id)));
                        if (continueAction != null) continueAction.Invoke(result);
                    }
                }
            }

            return packageUninstalls;
        }

        /// <summary>
        /// NuGet will happily report a package has been uninstalled, even if it doesn't always remove the nupkg.
        /// Ensure that the package is deleted or throw an error.
        /// </summary>
        /// <param name="removedPackage">The installed package.</param>
        /// <param name="pkgInfo">The package information.</param>
        private void ensure_nupkg_is_removed(IPackage removedPackage, ChocolateyPackageInformation pkgInfo)
        {
            var isSideBySide = pkgInfo != null && pkgInfo.IsSideBySide;

            var nupkgFile = "{0}{1}.nupkg".format_with(removedPackage.Id, isSideBySide ? "." + removedPackage.Version.to_string() : string.Empty);
            var installDir = _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, "{0}{1}".format_with(removedPackage.Id, isSideBySide ? "." + removedPackage.Version.to_string() : string.Empty));
            var nupkg = _fileSystem.combine_paths(installDir, nupkgFile);

            FaultTolerance.try_catch_with_logging_exception(
                () => _fileSystem.delete_file(nupkg),
                "Error deleting nupkg file",
                throwError: true);
        }

        public void remove_installation_files(IPackage removedPackage, ChocolateyPackageInformation pkgInfo)
        {
            var isSideBySide = pkgInfo != null && pkgInfo.IsSideBySide;
            var installDir = _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, "{0}{1}".format_with(removedPackage.Id, isSideBySide ? "." + removedPackage.Version.to_string() : string.Empty));

            if (_fileSystem.directory_exists(installDir) && pkgInfo != null && pkgInfo.FilesSnapshot != null)
            {
                foreach (var file in _fileSystem.get_files(installDir, "*.*", SearchOption.AllDirectories).or_empty_list_if_null())
                {
                    var fileSnapshot = pkgInfo.FilesSnapshot.Files.FirstOrDefault(f => f.Path.is_equal_to(file));
                    if (fileSnapshot == null) continue;

                    if (fileSnapshot.Checksum == _filesService.get_package_file(file).Checksum)
                    {
                        FaultTolerance.try_catch_with_logging_exception(
                            () => _fileSystem.delete_file(file),
                            "Error deleting file");
                    } 

                    if (fileSnapshot.Checksum == ApplicationParameters.HashProviderFileLocked)
                    {
                        this.Log().Warn(()=> "Snapshot for '{0}' was attempted when file was locked.{1} Please inspect and manually remove file{1} at '{2}'".format_with(_fileSystem.get_file_name(file), Environment.NewLine, _fileSystem.get_directory_name(file)));
                    }
                }
            }

            if (_fileSystem.directory_exists(installDir) && !_fileSystem.get_files(installDir, "*.*", SearchOption.AllDirectories).or_empty_list_if_null().Any())
            {
                _fileSystem.delete_directory_if_exists(installDir, recursive: true);
            }
        }

        private void set_package_names_if_all_is_specified(ChocolateyConfiguration config, Action customAction)
        {
            if (config.PackageNames.is_equal_to(ApplicationParameters.AllPackages))
            {
                config.ListCommand.LocalOnly = true;
                var sources = config.Sources;
                config.Sources = ApplicationParameters.PackagesLocation;
                var pre = config.Prerelease;
                config.Prerelease = true;
                var noop = config.Noop;
                config.Noop = false;
                config.PackageNames = string.Empty;
                var input = config.Input;
                config.Input = string.Empty;
                var quiet = config.QuietOutput;
                config.QuietOutput = true;

                config.PackageNames = list_run(config).Select(p => p.Name).@join(ApplicationParameters.PackageNamesSeparator);

                config.QuietOutput = quiet;
                config.Input = input;
                config.Noop = noop;
                config.Prerelease = pre;
                config.Sources = sources;

                if (customAction != null) customAction.Invoke();
            }
        }
    }
}