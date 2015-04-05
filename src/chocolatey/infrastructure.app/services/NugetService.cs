﻿// Copyright © 2011 - Present RealDimensions Software, LLC
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
        public NugetService(IFileSystem fileSystem, ILogger nugetLogger, IChocolateyPackageInformationService packageInfoService)
        {
            _fileSystem = fileSystem;
            _nugetLogger = nugetLogger;
            _packageInfoService = packageInfoService;
        }

        public void list_noop(ChocolateyConfiguration config)
        {
            this.Log().Info("{0} would have searched for '{1}' against the following source(s) :\"{2}\"".format_with(
                ApplicationParameters.Name,
                config.Input,
                config.Sources
                                ));
        }

        public IEnumerable<PackageResult> list_run(ChocolateyConfiguration config, bool logResults)
        {
            foreach (var package in NugetList.GetPackages(config, _nugetLogger))
            {
                var pkg = package; // for lamda access
                if (logResults)
                {
                    if (config.RegularOutput)
                    {
                        this.Log().Info(config.Verbose ? ChocolateyLoggers.Important : ChocolateyLoggers.Normal, () => "{0} {1}".format_with(pkg.Id, pkg.Version.to_string()));
                        if (config.Verbose) this.Log().Info(() => " {0}{1} Description: {2}{1} Tags: {3}{1} Number of Downloads: {4}{1}".format_with(pkg.Title.escape_curly_braces(), Environment.NewLine, pkg.Description.escape_curly_braces(), pkg.Tags.escape_curly_braces(), pkg.DownloadCount <= 0 ? "n/a" : pkg.DownloadCount.to_string()));
                        // Maintainer(s):{3}{1} | pkg.Owners.join(", ") - null at the moment
                    }
                    else
                    {
                        this.Log().Info(config.Verbose ? ChocolateyLoggers.Important : ChocolateyLoggers.Normal, () => "{0}|{1}".format_with(pkg.Id, pkg.Version.to_string()));
                    }
                }
                else
                {
                    this.Log().Debug(() => "{0} {1}".format_with(pkg.Id, pkg.Version.to_string()));
                }

                yield return new PackageResult(pkg, null);
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

            IDictionary<string, string> properties = new Dictionary<string, string>();
            // Set the version property if the flag is set
            if (!string.IsNullOrWhiteSpace(config.Version))
            {
                properties["version"] = config.Version;
            }

            // Initialize the property provider based on what was passed in using the properties flag
            var propertyProvider = new DictionaryPropertyProvider(properties);

            var basePath = _fileSystem.get_current_directory();
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
            var packageInstalls = new ConcurrentDictionary<string, PackageResult>();

            //todo: handle all

            SemanticVersion version = config.Version != null ? new SemanticVersion(config.Version) : null;

            IList<string> packageNames = config.PackageNames.Split(new[] {ApplicationParameters.PackageNamesSeparator}, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null().ToList();
            if (packageNames.Count == 1)
            {
                var packageName = packageNames.DefaultIfEmpty(string.Empty).FirstOrDefault();
                if (packageName.EndsWith(Constants.PackageExtension) || packageName.EndsWith(Constants.ManifestExtension))
                {
                    this.Log().Debug("Updating source and package name to handle *.nupkg or *.nuspec file.");
                    packageNames.Clear();
                    packageNames.Add(_fileSystem.get_file_name_without_extension(packageName));
                    config.Sources = _fileSystem.get_directory_name(_fileSystem.get_full_path(packageName));

                    if (packageName.EndsWith(Constants.ManifestExtension))
                    {
                        this.Log().Debug("Building nuspec file prior to install.");
                        config.Input = packageName;
                        // build package
                        pack_run(config);
                    }
                }
            }

            // this is when someone points the source directly at a nupkg 
            // e.g. -s c:\somelocation\somewhere\packagename.nupkg
            if (config.Sources.to_string().EndsWith(Constants.PackageExtension))
            {
                config.Sources = _fileSystem.get_directory_name(_fileSystem.get_full_path(config.Sources));
            }

            var packageManager = NugetCommon.GetPackageManager(
                config, _nugetLogger,
                installSuccessAction: (e) =>
                    {
                        var pkg = e.Package;
                        var results = packageInstalls.GetOrAdd(pkg.Id.to_lower(), new PackageResult(pkg, e.InstallPath));
                        results.InstallLocation = e.InstallPath;
                        results.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));

                        if (continueAction != null) continueAction.Invoke(results);
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
                    var results = packageInstalls.GetOrAdd(packageName, new PackageResult(installedPackage, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, installedPackage.Id)));
                    results.Messages.Add(new ResultMessage(ResultType.Warn, logMessage));
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
                    var logMessage = "{0} not installed. The package was not found with the source(s) listed.{1} If you specified a particular version and are receiving this message, it is possible that the package name exists but the version does not.{1} Version: \"{2}\"{1} Source(s): \"{3}\"".format_with(packageName, Environment.NewLine, config.Version, config.Sources);
                    this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    var results = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, version.to_string(), null));
                    results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                    continue;
                }

                if (installedPackage != null && (installedPackage.Version == availablePackage.Version) && config.Force)
                {
                    var results = packageInstalls.GetOrAdd(packageName, new PackageResult(installedPackage, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, installedPackage.Id)));
                    results.Messages.Add(new ResultMessage(ResultType.Note, "Backing up and removing old version"));

                    backup_existing_version(config, installedPackage);

                    try
                    {
                        packageManager.UninstallPackage(installedPackage, forceRemove: config.Force, removeDependencies: config.ForceDependencies);
                        if (!results.InstallLocation.is_equal_to(ApplicationParameters.PackagesLocation))
                        {
                            _fileSystem.delete_directory_if_exists(results.InstallLocation, recursive: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        string logMessage = "{0}:{1} {2}".format_with("Unable to remove existing package prior to forced reinstall", Environment.NewLine, ex.Message);
                        this.Log().Warn(logMessage);
                        results.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                    }
                }

                try
                {
                    using (packageManager.SourceRepository.StartOperation(
                        RepositoryOperationNames.Install,
                        packageName,
                        version == null ? null : version.ToString()))
                    {
                        packageManager.InstallPackage(availablePackage,ignoreDependencies: config.IgnoreDependencies, allowPrereleaseVersions: config.Prerelease);
                        //packageManager.InstallPackage(packageName, version, configuration.IgnoreDependencies, configuration.Prerelease);
                    }
                }
                catch (Exception ex)
                {
                    var logMessage = "{0} not installed. An error occurred during installation:{1} {2}".format_with(packageName, Environment.NewLine, ex.Message);
                    this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    var results = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, version.to_string(), null));
                    results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                    if (continueAction != null) continueAction.Invoke(results);
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
            var packageInstalls = new ConcurrentDictionary<string, PackageResult>();

            SemanticVersion version = config.Version != null ? new SemanticVersion(config.Version) : null;
            var packageManager = NugetCommon.GetPackageManager(
                config,
                _nugetLogger,
                installSuccessAction: (e) =>
                    {
                        var pkg = e.Package;
                        var results = packageInstalls.GetOrAdd(pkg.Id.to_lower(), new PackageResult(pkg, e.InstallPath));
                        results.InstallLocation = e.InstallPath;
                        results.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));

                        if (continueAction != null) continueAction.Invoke(results);
                    },
                uninstallSuccessAction: null,
                addUninstallHandler: false);

            var configIgnoreDependencies = config.IgnoreDependencies;
            set_package_names_if_all_is_specified(config, () => { config.IgnoreDependencies = true; });
            config.IgnoreDependencies = configIgnoreDependencies;

            foreach (string packageName in config.PackageNames.Split(new[] {ApplicationParameters.PackageNamesSeparator}, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null())
            {
                remove_rollback_directory_if_exists(packageName);

                IPackage installedPackage = packageManager.LocalRepository.FindPackage(packageName);

                if (installedPackage == null)
                {
                    //todo v1 Deprecation - reimplement error
                    //string logMessage = "{0} is not installed. Cannot upgrade a non-existent package.".format_with(packageName);
                    //var results = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                    //results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));

                    //if (config.RegularOutput) this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    //continue;

                    string logMessage = @"
DEPRECATION NOTICE - Upgrade will no longer install non-installed 
packages as of version 1.0.0. That is what the install command is for.


{0} is not installed. Installing...".format_with(packageName);

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
                        foreach (var packageResult in installResults)
                        {
                            packageInstalls.GetOrAdd(packageResult.Key, packageResult.Value);
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
                    var unfoundResults = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, version.to_string(), null));

                    if (config.UpgradeCommand.FailOnUnfound)
                    {
                        unfoundResults.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                        if (config.RegularOutput) this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    }
                    else
                    {
                        unfoundResults.Messages.Add(new ResultMessage(ResultType.Warn, logMessage));
                        unfoundResults.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
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

                var results = packageInstalls.GetOrAdd(packageName, new PackageResult(availablePackage, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, availablePackage.Id)));

                if ((installedPackage.Version > availablePackage.Version))
                {
                    string logMessage = "{0} v{1} is newer than the most recent.{2} You must be smarter than the average bear...".format_with(installedPackage.Id, installedPackage.Version, Environment.NewLine);
                    results.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));

                    if (config.RegularOutput)
                    {
                        this.Log().Info(ChocolateyLoggers.Important, logMessage);
                    }
                    else
                    {
                        this.Log().Info("{0}|{1}|{1}|{2}".format_with(installedPackage.Id, installedPackage.Version, isPinned.to_string().to_lower()));
                    }
                    continue;
                }

                if (installedPackage.Version == availablePackage.Version)
                {
                    string logMessage = "{0} v{1} is the latest version available based on your source(s).".format_with(installedPackage.Id, installedPackage.Version);

                    if (!config.Force)
                    {
                        if (results.Messages.Count((p) => p.Message == ApplicationParameters.Messages.ContinueChocolateyAction) == 0)
                        {
                            results.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                        }

                        if (config.RegularOutput)
                        {
                            this.Log().Info(logMessage);
                        }
                        else
                        {
                            this.Log().Info("{0}|{1}|{2}|{3}".format_with(installedPackage.Id, installedPackage.Version, availablePackage.Version, isPinned.to_string().to_lower()));
                        }
                            
                        continue;
                    }

                    results.Messages.Add(new ResultMessage(ResultType.Note, logMessage));
                    if (config.RegularOutput) this.Log().Info(logMessage);
                }

                if ((availablePackage.Version > installedPackage.Version) || config.Force)
                {
                    if (availablePackage.Version > installedPackage.Version)
                    {
                        string logMessage = "You have {0} v{1} installed. Version {2} is available based on your source(s)".format_with(installedPackage.Id, installedPackage.Version, availablePackage.Version);
                        results.Messages.Add(new ResultMessage(ResultType.Note, logMessage));

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
                        results.Messages.Add(new ResultMessage(ResultType.Warn, logMessage));
                        results.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
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
                                backup_existing_version(config, installedPackage);
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
                            results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                            if (continueAction != null) continueAction.Invoke(results);
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

        public void backup_existing_version(ChocolateyConfiguration config, IPackage installedPackage)
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

                backup_configuration_files(pkgInstallPath, installedPackage.Version.to_string());

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

        private void backup_configuration_files(string packageInstallPath, string version)
        {
            var configFiles = _fileSystem.get_files(packageInstallPath, ApplicationParameters.ConfigFileExtensions, SearchOption.AllDirectories);
            foreach (var file in configFiles.or_empty_list_if_null())
            {
                var backupName = "{0}.{1}.{2}".format_with(_fileSystem.get_file_name_without_extension(file), version, _fileSystem.get_file_extension(file));

                FaultTolerance.try_catch_with_logging_exception(
                    () => _fileSystem.copy_file(file, _fileSystem.combine_paths(_fileSystem.get_directory_name(file), backupName), overwriteExisting: true),
                    "Error backing up configuration file");
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
            var packageUninstalls = new ConcurrentDictionary<string, PackageResult>();

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
                    var results = packageUninstalls.GetOrAdd(pkg.Id.to_lower() + "." + pkg.Version.to_string(), new PackageResult(pkg, e.InstallPath));
                    string logMessage = "{0}{1} v{2}{3}".format_with(Environment.NewLine, pkg.Id, pkg.Version.to_string(), config.Force ? " (forced)" : string.Empty);
                    if (results.Messages.Count((p) => p.Message == ApplicationParameters.Messages.NugetEventActionHeader) == 0)
                    {
                        results.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.NugetEventActionHeader));
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

            foreach (string packageName in config.PackageNames.Split(new[] {ApplicationParameters.PackageNamesSeparator}, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null())
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
                    var results = packageUninstalls.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                    results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));

                    if (config.RegularOutput) this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    continue;
                }

                var packageVersionsToRemove = installedPackageVersions.ToList();
                if (!config.AllVersions)
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

                        var selection = InteractivePrompt.prompt_for_confirmation("Which version of {0} would you like to uninstall?".format_with(packageName), choices, abortChoice, true);

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
                        var pinnedResults = packageUninstalls.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                        pinnedResults.Messages.Add(new ResultMessage(ResultType.Warn, logMessage));
                        pinnedResults.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
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
                                backup_existing_version(config, packageVersion);
                                packageManager.UninstallPackage(packageVersion, forceRemove: config.Force, removeDependencies: config.ForceDependencies);
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
                        var result = packageUninstalls.GetOrAdd(packageVersion.Id.to_lower() + "." + packageVersion.Version.to_string(), new PackageResult(packageVersion, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, packageVersion.Id)));
                        if (continueAction != null) continueAction.Invoke(result);
                    }
                }
            }

            return packageUninstalls;
        }

        private void set_package_names_if_all_is_specified(ChocolateyConfiguration config, Action customAction)
        {
            if (config.PackageNames.is_equal_to("all"))
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

                config.PackageNames = list_run(config, false).Select(p => p.Name).@join(ApplicationParameters.PackageNamesSeparator);
                config.Input = input;
                config.Noop = noop;
                config.Prerelease = pre;
                config.Sources = sources;

                if (customAction != null) customAction.Invoke();
            }
        }
    }
}