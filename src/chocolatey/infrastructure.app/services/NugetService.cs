// Copyright © 2017 - 2018 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
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
    using chocolatey.infrastructure.app.utility;

    //todo - this monolith is too large. Refactor once test coverage is up.

    public class NugetService : INugetService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _nugetLogger;
        private readonly IChocolateyPackageInformationService _packageInfoService;
        private readonly IFilesService _filesService;
        private readonly IPackageDownloader _packageDownloader;
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
        /// <param name="packageDownloader">The downloader used to download packages</param>
        public NugetService(IFileSystem fileSystem, ILogger nugetLogger, IChocolateyPackageInformationService packageInfoService, IFilesService filesService, IPackageDownloader packageDownloader)
        {
            _fileSystem = fileSystem;
            _nugetLogger = nugetLogger;
            _packageInfoService = packageInfoService;
            _filesService = filesService;
            _packageDownloader = packageDownloader;
        }

        public SourceType SourceType
        {
            get { return SourceType.normal; }
        }

        public void ensure_source_app_installed(ChocolateyConfiguration config, Action<PackageResult> ensureAction)
        {
            // nothing to do. Nuget.Core is already part of Chocolatey
        }

        public virtual int count_run(ChocolateyConfiguration config)
        {
            if (config.ListCommand.LocalOnly)
            {
                config.Sources = ApplicationParameters.PackagesLocation;
                config.Prerelease = true;
            }

            int? pageValue = config.ListCommand.Page;
            try
            {
                return NugetList.GetCount(config, _nugetLogger);
            }
            finally
            {
                config.ListCommand.Page = pageValue;
            }
        }

        public virtual void list_noop(ChocolateyConfiguration config)
        {
            this.Log().Info("{0} would have searched for '{1}' against the following source(s) :\"{2}\"".format_with(
                ApplicationParameters.Name,
                config.Input,
                config.Sources
                                ));
        }

        public virtual IEnumerable<PackageResult> list_run(ChocolateyConfiguration config)
        {
            int count = 0;

            var sources = config.Sources;
            var prerelease = config.Prerelease;
            var includeVersionOverrides = config.ListCommand.IncludeVersionOverrides;

            if (config.ListCommand.LocalOnly)
            {
                config.Sources = ApplicationParameters.PackagesLocation;
                config.Prerelease = true;
                config.ListCommand.IncludeVersionOverrides = true;
            }

            if (config.RegularOutput) this.Log().Debug(() => "Running list with the following filter = '{0}'".format_with(config.Input));
            if (config.RegularOutput) this.Log().Debug(ChocolateyLoggers.Verbose, () => "--- Start of List ---");
            foreach (var pkg in NugetList.GetPackages(config, _nugetLogger))
            {
                var package = pkg; // for lamda access

                if (!string.IsNullOrWhiteSpace(config.Version))
                {
                    if (!pkg.Version.to_string().is_equal_to(config.Version)) continue;
                }

                if (config.ListCommand.LocalOnly)
                {
                    var packageInfo = _packageInfoService.get_package_information(package);
                    if (config.ListCommand.IncludeVersionOverrides)
                    {
                        if (packageInfo.VersionOverride != null)
                        {
                            package.OverrideOriginalVersion(packageInfo.VersionOverride);
                        }
                    }
                }

                if (!config.QuietOutput)
                {
                    var logger = config.Verbose ? ChocolateyLoggers.Important : ChocolateyLoggers.Normal;

                    if (config.RegularOutput)
                    {
                        this.Log().Info(logger, () => "{0}{1}".format_with(package.Id, config.ListCommand.IdOnly ? string.Empty : " {0}{1}{2}{3}".format_with(
                                package.Version.to_string(),
                                package.IsApproved ? " [Approved]" : string.Empty,
                                package.IsDownloadCacheAvailable ? " Downloads cached for licensed users" : string.Empty,
                                package.PackageTestResultStatus == "Failing" && package.IsDownloadCacheAvailable ? " - Possibly broken for FOSS users (due to original download location changes by vendor)" : package.PackageTestResultStatus == "Failing" ? " - Possibly broken" : string.Empty
                            ))
                        );

                        if (config.Verbose && !config.ListCommand.IdOnly) this.Log().Info(() =>
                            @" Title: {0} | Published: {1}{2}{3}
 Number of Downloads: {4} | Downloads for this version: {5}
 Package url
 Chocolatey Package Source: {6}{7}
 Tags: {8}
 Software Site: {9}
 Software License: {10}{11}{12}{13}{14}{15}
 Description: {16}{17}
".format_with(
                                package.Title.escape_curly_braces(),
                                package.Published.GetValueOrDefault().UtcDateTime.ToShortDateString(),
                                package.IsApproved ? "{0} Package approved {1} on {2}.".format_with(
                                        Environment.NewLine,
                                        string.IsNullOrWhiteSpace(package.PackageReviewer) ? "as a trusted package" : "by " + package.PackageReviewer,
                                        package.PackageApprovedDate.GetValueOrDefault().ToString("MMM dd yyyy HH:mm:ss")
                                    ) : string.Empty,
                                string.IsNullOrWhiteSpace(package.PackageTestResultStatus) || package.PackageTestResultStatus.is_equal_to("unknown") ? string.Empty : "{0} Package testing status: {1} on {2}.".format_with(
                                        Environment.NewLine,
                                        package.PackageTestResultStatus,
                                        package.PackageValidationResultDate.GetValueOrDefault().ToString("MMM dd yyyy HH:mm:ss")
                                    ),
                                package.DownloadCount <= 0 ? "n/a" : package.DownloadCount.to_string(),
                                package.VersionDownloadCount <= 0 ? "n/a" : package.VersionDownloadCount.to_string(),
                                package.PackageSourceUrl != null && !string.IsNullOrWhiteSpace(package.PackageSourceUrl.to_string()) ? package.PackageSourceUrl.to_string() : "n/a",
                                string.IsNullOrWhiteSpace(package.PackageHash) ? string.Empty : "{0} Package Checksum: '{1}' ({2})".format_with(
                                        Environment.NewLine,
                                        package.PackageHash,
                                        package.PackageHashAlgorithm
                                        ),
                                package.Tags.trim_safe().escape_curly_braces(),
                                package.ProjectUrl != null ? package.ProjectUrl.to_string() : "n/a",
                                package.LicenseUrl != null && !string.IsNullOrWhiteSpace(package.LicenseUrl.to_string()) ? package.LicenseUrl.to_string() : "n/a",
                                package.ProjectSourceUrl != null && !string.IsNullOrWhiteSpace(package.ProjectSourceUrl.to_string()) ? "{0} Software Source: {1}".format_with(Environment.NewLine, package.ProjectSourceUrl.to_string()) : string.Empty,
                                package.DocsUrl != null && !string.IsNullOrWhiteSpace(package.DocsUrl.to_string()) ? "{0} Documentation: {1}".format_with(Environment.NewLine, package.DocsUrl.to_string()) : string.Empty,
                                package.MailingListUrl != null && !string.IsNullOrWhiteSpace(package.MailingListUrl.to_string()) ? "{0} Mailing List: {1}".format_with(Environment.NewLine, package.MailingListUrl.to_string()) : string.Empty,
                                package.BugTrackerUrl != null && !string.IsNullOrWhiteSpace(package.BugTrackerUrl.to_string()) ? "{0} Issues: {1}".format_with(Environment.NewLine, package.BugTrackerUrl.to_string()) : string.Empty,
                                package.Summary != null && !string.IsNullOrWhiteSpace(package.Summary.to_string()) ? "{0} Summary: {1}".format_with(Environment.NewLine, package.Summary.escape_curly_braces().to_string()) : string.Empty,
                                package.Description.escape_curly_braces().Replace("\n    ", "\n").Replace("\n", "\n  "),
                                package.ReleaseNotes != null && !string.IsNullOrWhiteSpace(package.ReleaseNotes.to_string()) ? "{0} Release Notes: {1}".format_with(Environment.NewLine, package.ReleaseNotes.escape_curly_braces().Replace("\n    ", "\n").Replace("\n", "\n  ")) : string.Empty
                        ));
                    }
                    else
                    {
                        this.Log().Info(logger, () => "{0}{1}".format_with(package.Id, config.ListCommand.IdOnly ? string.Empty : "|{0}".format_with(package.Version.to_string())));
                    }
                }
                else
                {
                    this.Log().Debug(() => "{0}{1}".format_with(package.Id, config.ListCommand.IdOnly ? string.Empty : " {0}".format_with(package.Version.to_string())));
                }
                count++;

                yield return new PackageResult(package, null, config.Sources);
            }

            if (config.RegularOutput) this.Log().Debug(ChocolateyLoggers.Verbose, () => "--- End of List ---");
            if (config.RegularOutput && !config.QuietOutput)
            {
                this.Log().Warn(() => @"{0} packages {1}.".format_with(count, config.ListCommand.LocalOnly ? "installed" : "found"));
            }

            config.Sources = sources;
            config.Prerelease = prerelease;
            config.ListCommand.IncludeVersionOverrides = includeVersionOverrides;
        }

        public void pack_noop(ChocolateyConfiguration config)
        {
            this.Log().Info("{0} would have searched for a nuspec file in \"{1}\" and attempted to compile it.".format_with(
                ApplicationParameters.Name,
                config.OutputDirectory ?? _fileSystem.get_current_directory()
                                ));
        }

        public virtual string validate_and_return_package_file(ChocolateyConfiguration config, string extension)
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

        public virtual void pack_run(ChocolateyConfiguration config)
        {
            var nuspecFilePath = validate_and_return_package_file(config, Constants.ManifestExtension);
            var nuspecDirectory = _fileSystem.get_full_path(_fileSystem.get_directory_name(nuspecFilePath));
            if (string.IsNullOrWhiteSpace(nuspecDirectory)) nuspecDirectory = _fileSystem.get_current_directory();

            // Use case-insensitive properties like "nuget pack".
            var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Add any other properties passed to the pack command overriding any present.
            foreach (var property in config.PackCommand.Properties)
            {
                this.Log().Debug(() => "Setting property '{0}': {1}".format_with(
                    property.Key,
                    property.Value));

                properties[property.Key] = property.Value;
            }

            // Set the version property if the flag is set
            if (!string.IsNullOrWhiteSpace(config.Version))
            {
                this.Log().Debug(() => "Setting property 'version': {0}".format_with(
                    config.Version));

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
            string outputFolder = config.OutputDirectory ?? _fileSystem.get_current_directory();
            string outputPath = _fileSystem.combine_paths(outputFolder, outputFile);

            config.Sources = outputFolder;

            this.Log().Info(config.QuietOutput ? ChocolateyLoggers.LogFileOnly : ChocolateyLoggers.Normal, () => "Attempting to build package from '{0}'.".format_with(_fileSystem.get_file_name(nuspecFilePath)));

            IPackage package = NugetPack.BuildPackage(builder, _fileSystem, outputPath);
            // package.Validate().Any(v => v.Level == PackageIssueLevel.Error)
            if (package == null)
            {
                throw new ApplicationException("Unable to create nupkg. See the log for error details.");
            }
            //todo: v1 analyze package
            //if (package != null)
            //{
            //    AnalyzePackage(package);
            //}

            this.Log().Info(config.QuietOutput ? ChocolateyLoggers.LogFileOnly : ChocolateyLoggers.Important, () => "Successfully created package '{0}'".format_with(outputPath));
        }

        public void push_noop(ChocolateyConfiguration config)
        {
            string nupkgFilePath = validate_and_return_package_file(config, Constants.PackageExtension);
            this.Log().Info(() => "Would have attempted to push '{0}' to source '{1}'.".format_with(_fileSystem.get_file_name(nupkgFilePath), config.Sources));
        }

        public virtual void push_run(ChocolateyConfiguration config)
        {
            string nupkgFilePath = validate_and_return_package_file(config, Constants.PackageExtension);
            if (config.RegularOutput) this.Log().Info(() => "Attempting to push {0} to {1}".format_with(_fileSystem.get_file_name(nupkgFilePath), config.Sources));

            NugetPush.push_package(config, _fileSystem.get_full_path(nupkgFilePath));


            if (config.RegularOutput && (config.Sources.is_equal_to(ApplicationParameters.ChocolateyCommunityFeedPushSource) || config.Sources.is_equal_to(ApplicationParameters.ChocolateyCommunityFeedPushSourceOld)))
            {
                this.Log().Warn(ChocolateyLoggers.Important, () => @"

Your package may be subject to moderation. A moderator will review the
package prior to acceptance. You should have received an email. If you
don't hear back from moderators within 1-3 business days, please reply
to the email and ask for status or use contact site admins on the
package page to contact moderators.

Please ensure your registered email address is correct and emails from
noreply at chocolatey dot org are not being sent to your spam/junk 
folder.");
            }
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

        public virtual ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.PackagesLocation);
            var packageInstalls = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            //todo: handle all

            SemanticVersion version = !string.IsNullOrWhiteSpace(config.Version) ? new SemanticVersion(config.Version) : null;
            if (config.Force) config.AllowDowngrade = true;

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
                        version = packageFile.Version;
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
                config, _nugetLogger, _packageDownloader,
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

            var originalConfig = config;

            foreach (string packageName in packageNames.or_empty_list_if_null())
            {
                // reset config each time through
                config = originalConfig.deep_copy();

                //todo: get smarter about realizing multiple versions have been installed before and allowing that
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
                    this.Log().Warn(ChocolateyLoggers.Important, () => @"{0} v{1} already installed. Forcing reinstall of version '{1}'. 
 Please use upgrade if you meant to upgrade to a new version.".format_with(installedPackage.Id, installedPackage.Version));
                    version = installedPackage.Version;
                }

                if (installedPackage != null && version != null && version < installedPackage.Version && !config.AllowMultipleVersions && !config.AllowDowngrade)
                {
                    string logMessage = "A newer version of {0} (v{1}) is already installed.{2} Use --allow-downgrade or --force to attempt to install older versions, or use side by side to allow multiple versions.".format_with(installedPackage.Id, installedPackage.Version, Environment.NewLine);
                    var nullResult = packageInstalls.GetOrAdd(packageName, new PackageResult(installedPackage, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, installedPackage.Id)));
                    nullResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                    this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    continue;
                }

                IPackage availablePackage = config.Features.UsePackageRepositoryOptimizations ? 
                    find_package(packageName, version, config, packageManager.SourceRepository) 
                    : packageManager.SourceRepository.FindPackage(packageName, version, config.Prerelease, allowUnlisted: false);

                if (availablePackage == null)
                {
                    var logMessage = @"{0} not installed. The package was not found with the source(s) listed.
 Source(s): '{1}'
 NOTE: When you specify explicit sources, it overrides default sources.
If the package version is a prerelease and you didn't specify `--pre`,
 the package may not be found.{2}{3}".format_with(packageName, config.Sources, string.IsNullOrWhiteSpace(config.Version) ? String.Empty :
@"
Version was specified as '{0}'. It is possible that version 
 does not exist for '{1}' at the source specified.".format_with(config.Version.to_string(), packageName),
@"
Please see https://chocolatey.org/docs/troubleshooting for more 
 assistance.");
                    this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    var noPkgResult = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, version.to_string(), null));
                    noPkgResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                    continue;
                }

                if (installedPackage != null && (installedPackage.Version == availablePackage.Version) && config.Force)
                {
                    var forcedResult = packageInstalls.GetOrAdd(packageName, new PackageResult(availablePackage, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, availablePackage.Id)));
                    forcedResult.Messages.Add(new ResultMessage(ResultType.Note, "Backing up and removing old version"));

                    remove_rollback_directory_if_exists(packageName);
                    backup_existing_version(config, installedPackage, _packageInfoService.get_package_information(installedPackage));

                    try
                    {
                        packageManager.UninstallPackage(installedPackage, forceRemove: config.Force, removeDependencies: config.ForceDependencies);
                        if (!forcedResult.InstallLocation.is_equal_to(ApplicationParameters.PackagesLocation))
                        {
                            _fileSystem.delete_directory_if_exists(forcedResult.InstallLocation, recursive: true);
                        }
                        remove_cache_for_package(config, installedPackage);
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
                        remove_nuget_cache_for_package(availablePackage);
                    }
                }
                catch (Exception ex)
                {
                    var message = ex.Message;
                    var webException = ex as System.Net.WebException;
                    if (webException != null)
                    {
                        var response = webException.Response as HttpWebResponse;
                        if (response != null && !string.IsNullOrWhiteSpace(response.StatusDescription)) message += " {0}".format_with(response.StatusDescription);
                    }

                    var logMessage = "{0} not installed. An error occurred during installation:{1} {2}".format_with(packageName, Environment.NewLine, message);
                    this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    var errorResult = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, version.to_string(), null));
                    errorResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                    if (errorResult.ExitCode == 0) errorResult.ExitCode = 1;
                    if (continueAction != null) continueAction.Invoke(errorResult);
                }
            }

            return packageInstalls;
        }

        public virtual void remove_rollback_directory_if_exists(string packageName)
        {
            var rollbackDirectory = _fileSystem.get_full_path(_fileSystem.combine_paths(ApplicationParameters.PackageBackupLocation, packageName));
            if (!_fileSystem.directory_exists(rollbackDirectory))
            {
                //search for folder
                var possibleRollbacks = _fileSystem.get_directories(ApplicationParameters.PackageBackupLocation, packageName + "*");
                if (possibleRollbacks != null && possibleRollbacks.Count() != 0)
                {
                    rollbackDirectory = possibleRollbacks.OrderByDescending(p => p).DefaultIfEmpty(string.Empty).FirstOrDefault();
                }

                rollbackDirectory = _fileSystem.get_full_path(rollbackDirectory);
            }

            if (string.IsNullOrWhiteSpace(rollbackDirectory) || !_fileSystem.directory_exists(rollbackDirectory)) return;
            if (!rollbackDirectory.StartsWith(ApplicationParameters.PackageBackupLocation) || rollbackDirectory.is_equal_to(ApplicationParameters.PackageBackupLocation)) return;

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

        public ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config, Action<PackageResult> continueAction, Action<PackageResult> beforeUpgradeAction = null)
        {
            return upgrade_run(config, continueAction, performAction: true, beforeUpgradeAction: beforeUpgradeAction);
        }

        public virtual ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config, Action<PackageResult> continueAction, bool performAction, Action<PackageResult> beforeUpgradeAction = null)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.PackagesLocation);
            var packageInstalls = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            SemanticVersion version = !string.IsNullOrWhiteSpace(config.Version) ? new SemanticVersion(config.Version) : null;
            if (config.Force) config.AllowDowngrade = true;

            var packageManager = NugetCommon.GetPackageManager(
                config,
                _nugetLogger,
                _packageDownloader,
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

            var originalConfig = config;

            foreach (string packageName in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null())
            {
                // reset config each time through
                config = originalConfig.deep_copy();

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

                    if (config.Features.SkipPackageUpgradesWhenNotInstalled)
                    {
                        string warnLogMessage = "{0} is not installed and skip non-installed option selected. Skipping...".format_with(packageName);
                        var result = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                        result.Messages.Add(new ResultMessage(ResultType.Warn, warnLogMessage));
                        if (config.RegularOutput) this.Log().Warn(ChocolateyLoggers.Important, warnLogMessage);

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

                if (isPinned && config.OutdatedCommand.IgnorePinned)
                {
                    continue;
                }

                if (version != null && version < installedPackage.Version && !config.AllowMultipleVersions && !config.AllowDowngrade)
                {
                    string logMessage = "A newer version of {0} (v{1}) is already installed.{2} Use --allow-downgrade or --force to attempt to upgrade to older versions, or use side by side to allow multiple versions.".format_with(installedPackage.Id, installedPackage.Version, Environment.NewLine);
                    var nullResult = packageInstalls.GetOrAdd(packageName, new PackageResult(installedPackage, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, installedPackage.Id)));
                    nullResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                    this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    continue;
                }

                // if we have a prerelease installed, we want to have it upgrade based on newer prereleases
                var originalPrerelease = config.Prerelease;
                if (!string.IsNullOrWhiteSpace(installedPackage.Version.SpecialVersion) && !config.UpgradeCommand.ExcludePrerelease)
                {
                    // this is a prerelease - opt in for newer prereleases.
                    config.Prerelease = true;
                }

                IPackage availablePackage = config.Features.UsePackageRepositoryOptimizations ? 
                    find_package(packageName, version, config, packageManager.SourceRepository) 
                    : packageManager.SourceRepository.FindPackage(packageName, version, config.Prerelease, allowUnlisted: false);

                config.Prerelease = originalPrerelease;

                if (availablePackage == null)
                {
                    if (config.Features.IgnoreUnfoundPackagesOnUpgradeOutdated) continue;

                    string logMessage = "{0} was not found with the source(s) listed.{1} If you specified a particular version and are receiving this message, it is possible that the package name exists but the version does not.{1} Version: \"{2}\"; Source(s): \"{3}\"".format_with(packageName, Environment.NewLine, config.Version, config.Sources);
                    var unfoundResult = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, version.to_string(), null));

                    if (config.UpgradeCommand.FailOnUnfound)
                    {
                        unfoundResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                        if (config.RegularOutput) this.Log().Error(ChocolateyLoggers.Important, "{0}{1}".format_with(Environment.NewLine, logMessage));
                    }
                    else
                    {
                        unfoundResult.Messages.Add(new ResultMessage(ResultType.Warn, "{0} was not found with the source(s) listed.".format_with(packageName)));
                        unfoundResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                        if (config.RegularOutput)
                        {
                            this.Log().Warn(ChocolateyLoggers.Important, "{0}{1}".format_with(Environment.NewLine, logMessage));
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

                if (installedPackage.Version > availablePackage.Version && (!config.AllowDowngrade || (config.AllowDowngrade && version == null)))
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

                if ((availablePackage.Version > installedPackage.Version) || config.Force || (availablePackage.Version < installedPackage.Version && config.AllowDowngrade))
                {
                    if (availablePackage.Version > installedPackage.Version)
                    {
                        string logMessage = "You have {0} v{1} installed. Version {2} is available based on your source(s).".format_with(installedPackage.Id, installedPackage.Version, availablePackage.Version);
                        packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));

                        if (config.RegularOutput)
                        {
                            this.Log().Warn("{0}{1}".format_with(Environment.NewLine, logMessage));
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

                    set_package_config_for_upgrade(config, pkgInfo);

                    if (performAction)
                    {
                        try
                        {
                            using (packageManager.SourceRepository.StartOperation(
                                RepositoryOperationNames.Update,
                                packageName,
                                version == null ? null : version.ToString()))
                            {
                                if (beforeUpgradeAction != null)
                                {
                                    var currentPackageResult = new PackageResult(installedPackage, get_install_directory(config, installedPackage));
                                    beforeUpgradeAction(currentPackageResult);
                                }

                                remove_rollback_directory_if_exists(packageName);
                                ensure_package_files_have_compatible_attributes(config, installedPackage, pkgInfo);
                                rename_legacy_package_version(config, installedPackage, pkgInfo);
                                backup_existing_version(config, installedPackage, pkgInfo);
                                remove_shim_directors(config, installedPackage, pkgInfo);
                                if (config.Force && (installedPackage.Version == availablePackage.Version))
                                {
                                    FaultTolerance.try_catch_with_logging_exception(
                                        () =>
                                        {
                                            _fileSystem.delete_directory_if_exists(_fileSystem.combine_paths(ApplicationParameters.PackagesLocation, installedPackage.Id), recursive: true);
                                            remove_cache_for_package(config, installedPackage);
                                        },
                                        "Error during force upgrade");
                                    packageManager.InstallPackage(availablePackage, config.IgnoreDependencies, config.Prerelease);
                                }
                                else
                                {
                                    packageManager.UpdatePackage(availablePackage, updateDependencies: !config.IgnoreDependencies, allowPrereleaseVersions: config.Prerelease);
                                }
                                remove_nuget_cache_for_package(availablePackage);
                            }
                        }
                        catch (Exception ex)
                        {
                            var message = ex.Message;
                            var webException = ex as System.Net.WebException;
                            if (webException != null)
                            {
                                var response = webException.Response as HttpWebResponse;
                                if (response != null && !string.IsNullOrWhiteSpace(response.StatusDescription)) message += " {0}".format_with(response.StatusDescription);
                            }

                            var logMessage = "{0} not upgraded. An error occurred during installation:{1} {2}".format_with(packageName, Environment.NewLine, message);
                            this.Log().Error(ChocolateyLoggers.Important, logMessage);
                            packageResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                            if (packageResult.ExitCode == 0) packageResult.ExitCode = 1;
                            if (continueAction != null) continueAction.Invoke(packageResult);
                        }
                    }
                }
            }

            return packageInstalls;
        }

        public virtual ConcurrentDictionary<string, PackageResult> get_outdated(ChocolateyConfiguration config)
        {
            var packageManager = NugetCommon.GetPackageManager(
              config,
              _nugetLogger,
              _packageDownloader,
              installSuccessAction: null,
              uninstallSuccessAction: null,
              addUninstallHandler: false);

            var repository = packageManager.SourceRepository;
            var outdatedPackages = new ConcurrentDictionary<string, PackageResult>();

            set_package_names_if_all_is_specified(config, () => { config.IgnoreDependencies = true; });
            var packageNames = config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null().ToList();

            var originalConfig = config;

            foreach (var packageName in packageNames)
            {
                // reset config each time through
                config = originalConfig.deep_copy();

                var installedPackage = packageManager.LocalRepository.FindPackage(packageName);
                var pkgInfo = _packageInfoService.get_package_information(installedPackage);
                bool isPinned = pkgInfo.IsPinned;

                // if the package is pinned and we are skipping pinned,
                // move on quickly
                if (isPinned && config.OutdatedCommand.IgnorePinned)
                {
                    string pinnedLogMessage = "{0} is pinned. Skipping pinned package.".format_with(packageName);
                    var pinnedPackageResult = outdatedPackages.GetOrAdd(packageName, new PackageResult(installedPackage, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, installedPackage.Id)));
                    pinnedPackageResult.Messages.Add(new ResultMessage(ResultType.Debug, pinnedLogMessage));
                    pinnedPackageResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, pinnedLogMessage));

                    continue;
                }

                if (installedPackage != null && !string.IsNullOrWhiteSpace(installedPackage.Version.SpecialVersion) && !config.UpgradeCommand.ExcludePrerelease)
                {
                    // this is a prerelease - opt in for newer prereleases.
                    config.Prerelease = true;
                }

                SemanticVersion version =  null;
                var latestPackage = config.Features.UsePackageRepositoryOptimizations ? 
                    find_package(packageName, null, config, packageManager.SourceRepository) 
                    : packageManager.SourceRepository.FindPackage(packageName, version, config.Prerelease, allowUnlisted: false);

                if (latestPackage == null)
                {
                    if (config.Features.IgnoreUnfoundPackagesOnUpgradeOutdated) continue;

                    string unfoundLogMessage = "{0} was not found with the source(s) listed.{1} Source(s): \"{2}\"".format_with(packageName, Environment.NewLine, config.Sources);
                    var unfoundResult = outdatedPackages.GetOrAdd(packageName, new PackageResult(installedPackage, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, installedPackage.Id)));
                    unfoundResult.Messages.Add(new ResultMessage(ResultType.Warn, unfoundLogMessage));
                    unfoundResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, unfoundLogMessage));

                    this.Log().Warn("{0}|{1}|{1}|{2}".format_with(installedPackage.Id, installedPackage.Version, isPinned.to_string().to_lower()));
                    continue;
                }

                if (latestPackage.Version <= installedPackage.Version) continue;

                var packageResult = outdatedPackages.GetOrAdd(packageName, new PackageResult(latestPackage, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, latestPackage.Id)));

                string logMessage = "You have {0} v{1} installed. Version {2} is available based on your source(s).{3} Source(s): \"{4}\"".format_with(installedPackage.Id, installedPackage.Version, latestPackage.Version, Environment.NewLine, config.Sources);
                packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));

                this.Log().Info("{0}|{1}|{2}|{3}".format_with(installedPackage.Id, installedPackage.Version, latestPackage.Version, isPinned.to_string().to_lower()));
            }

            return outdatedPackages;
        }

        private IPackage find_package(string packageName, SemanticVersion version, ChocolateyConfiguration config, IPackageRepository repository)
        {
            packageName = packageName.to_string().ToLower(CultureInfo.CurrentCulture);
            // find the package based on version
            if (version != null) return repository.FindPackage(packageName, version, config.Prerelease, allowUnlisted: false);

            // we should always be using an aggregate repository
            var aggregateRepository = repository as AggregateRepository;
            if (aggregateRepository != null)
            {
                var packageResults = new List<IPackage>();

                foreach (var packageRepository in aggregateRepository.Repositories.or_empty_list_if_null())
                {
                    try
                    {
                        this.Log().Debug("Using '" + packageRepository.Source + "'.");
                        this.Log().Debug("- Supports prereleases? '" + packageRepository.SupportsPrereleasePackages + "'.");
                        this.Log().Debug("- Is ServiceBased? '" + (packageRepository is IServiceBasedRepository) + "'.");

                        // search based on lower case id - similar to PackageRepositoryExtensions.FindPackagesByIdCore()
                        IQueryable<IPackage> combinedResults = packageRepository.GetPackages().Where(x => x.Id.ToLower() == packageName);

                        if (config.Prerelease && packageRepository.SupportsPrereleasePackages)
                        {
                            combinedResults = combinedResults.Where(p => p.IsAbsoluteLatestVersion);
                        }
                        else
                        {
                            combinedResults = combinedResults.Where(p => p.IsLatestVersion);
                        }

                        if (!(packageRepository is IServiceBasedRepository))
                        {
                            combinedResults = combinedResults
                                .Where(PackageExtensions.IsListed)
                                .Where(p => config.Prerelease || p.IsReleaseVersion())
                                .distinct_last(PackageEqualityComparer.Id, PackageComparer.Version)
                                .AsQueryable();
                        }

                        var packageRepositoryResults = combinedResults.ToList();
                        if (packageRepositoryResults.Count() != 0)
                        {
                            this.Log().Debug("Package '{0}' found on source '{1}'".format_with(packageName, packageRepository.Source));
                            packageResults.AddRange(packageRepositoryResults);
                        }
                    }
                    catch (Exception e)
                    {
                       this.Log().Warn("Error retrieving packages from source '{0}':{1} {2}".format_with(packageRepository.Source, Environment.NewLine, e.Message));
                    }
                }

                // get only one result, should be the latest - similar to TryFindLatestPackageById
                return packageResults.OrderByDescending(x => x.Version).FirstOrDefault();
            }

            // search based on lower case id - similar to PackageRepositoryExtensions.FindPackagesByIdCore()
            IQueryable<IPackage> results = repository.GetPackages().Where(x => x.Id.ToLower() == packageName);

            if (config.Prerelease && repository.SupportsPrereleasePackages)
            {
                results = results.Where(p => p.IsAbsoluteLatestVersion);
            }
            else
            {
                results = results.Where(p => p.IsLatestVersion);
            }

            if (!(repository is IServiceBasedRepository))
            {
                results = results
                    .Where(PackageExtensions.IsListed)
                    .Where(p => config.Prerelease || p.IsReleaseVersion())
                    .distinct_last(PackageEqualityComparer.Id, PackageComparer.Version)
                    .AsQueryable();
            }

            // get only one result, should be the latest - similar to TryFindLatestPackageById
            return results.ToList().OrderByDescending(x => x.Version).FirstOrDefault();
        }

        /// <summary>
        /// Sets the configuration for the package upgrade
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="packageInfo">The package information.</param>
        /// <returns>The original unmodified configuration, so it can be reset after upgrade</returns>
        protected virtual ChocolateyConfiguration set_package_config_for_upgrade(ChocolateyConfiguration config, ChocolateyPackageInformation packageInfo)
        {
            if (!config.Features.UseRememberedArgumentsForUpgrades || string.IsNullOrWhiteSpace(packageInfo.Arguments)) return config;

            var packageArgumentsUnencrypted = packageInfo.Arguments.contains(" --") && packageInfo.Arguments.to_string().Length > 4 ? packageInfo.Arguments : NugetEncryptionUtility.DecryptString(packageInfo.Arguments);

            var sensitiveArgs = true;
            if (!ArgumentsUtility.arguments_contain_sensitive_information(packageArgumentsUnencrypted))
            {
                sensitiveArgs = false;
                this.Log().Debug(ChocolateyLoggers.Verbose, "{0} - Adding remembered arguments for upgrade: {1}".format_with(packageInfo.Package.Id, packageArgumentsUnencrypted.escape_curly_braces()));
            }

            var packageArgumentsSplit = packageArgumentsUnencrypted.Split(new[] { " --" }, StringSplitOptions.RemoveEmptyEntries);
            var packageArguments = new List<string>();
            foreach (var packageArgument in packageArgumentsSplit.or_empty_list_if_null())
            {
                var packageArgumentSplit = packageArgument.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                var optionName = packageArgumentSplit[0].to_string();
                var optionValue = string.Empty;
                if (packageArgumentSplit.Length == 2)
                {
                    optionValue = packageArgumentSplit[1].to_string().remove_surrounding_quotes();
                    if (optionValue.StartsWith("'")) optionValue.remove_surrounding_quotes();
                }

                if (sensitiveArgs)
                {
                    this.Log().Debug(ChocolateyLoggers.Verbose, "{0} - Adding '{1}' to upgrade arguments. Values not shown due to detected sensitive arguments".format_with(packageInfo.Package.Id, optionName.escape_curly_braces()));
                }
                packageArguments.Add("--{0}{1}".format_with(optionName, string.IsNullOrWhiteSpace(optionValue) ? string.Empty : "=" + optionValue));
            }

            var originalConfig = config.deep_copy();
            // this changes config globally
            ConfigurationOptions.OptionSet.Parse(packageArguments);

            // there may be overrides from the user running upgrade
            if (!string.IsNullOrWhiteSpace(originalConfig.SourceCommand.Username)) config.SourceCommand.Username = originalConfig.SourceCommand.Username;
            if (!string.IsNullOrWhiteSpace(originalConfig.SourceCommand.Password)) config.SourceCommand.Username = originalConfig.SourceCommand.Password;
            if (!string.IsNullOrWhiteSpace(originalConfig.SourceCommand.Certificate)) config.SourceCommand.Username = originalConfig.SourceCommand.Certificate;
            if (!string.IsNullOrWhiteSpace(originalConfig.SourceCommand.CertificatePassword)) config.SourceCommand.Username = originalConfig.SourceCommand.CertificatePassword;

            return originalConfig;
        }

        private string get_install_directory(ChocolateyConfiguration config, IPackage installedPackage)
        {
            var pathResolver = NugetCommon.GetPathResolver(config, NugetCommon.GetNuGetFileSystem(config, _nugetLogger));
            var installDirectory = pathResolver.GetInstallPath(installedPackage);
            if (!_fileSystem.directory_exists(installDirectory))
            {
                var chocoPathResolver = pathResolver as ChocolateyPackagePathResolver;
                if (chocoPathResolver != null)
                {
                    chocoPathResolver.UseSideBySidePaths = !chocoPathResolver.UseSideBySidePaths;
                    installDirectory = chocoPathResolver.GetInstallPath(installedPackage);
                }

                if (!_fileSystem.directory_exists(installDirectory)) return null;
            }

            return installDirectory;
        }

        public virtual void ensure_package_files_have_compatible_attributes(ChocolateyConfiguration config, IPackage installedPackage, ChocolateyPackageInformation pkgInfo)
        {
            var installDirectory = get_install_directory(config, installedPackage);
            if (!_fileSystem.directory_exists(installDirectory)) return;

            _filesService.ensure_compatible_file_attributes(installDirectory, config);
        }

        public virtual void rename_legacy_package_version(ChocolateyConfiguration config, IPackage installedPackage, ChocolateyPackageInformation pkgInfo)
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

        public virtual void backup_existing_version(ChocolateyConfiguration config, IPackage installedPackage, ChocolateyPackageInformation packageInfo)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.PackageBackupLocation);

            var pkgInstallPath = get_install_directory(config, installedPackage);

            if (_fileSystem.directory_exists(pkgInstallPath))
            {
                this.Log().Debug("Backing up existing {0} prior to operation.".format_with(installedPackage.Id));

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

                        remove_packaging_files_prior_to_upgrade(pkgInstallPath, config.CommandName);
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

        public virtual void remove_packaging_files_prior_to_upgrade(string directoryPath, string commandName)
        {
            if (commandName.to_lower() == "upgrade")
            {
                // Due to the way that Package Reducer works, there is a potential that a Chocolatey Packaging
                // script could be incorrectly left in place during an upgrade operation.  To guard against this,
                // remove any Chocolatey Packaging scripts, which will then be restored by the new package, if
                // they are still required
                var filesToDelete = new List<string> {"chocolateyinstall", "chocolateyuninstall", "chocolateybeforemodify"};
                var packagingScripts = _fileSystem.get_files(directoryPath, "*.ps1", SearchOption.AllDirectories)
                    .Where(p => filesToDelete.Contains(_fileSystem.get_file_name_without_extension(p).to_lower()));

                foreach (var packagingScript in packagingScripts)
                {
                    if (_fileSystem.file_exists(packagingScript))
                    {
                        this.Log().Debug("Deleting file {0}".format_with(packagingScript));
                        _fileSystem.delete_file(packagingScript);
                    }
                }
            }
        }

        public virtual void backup_changed_files(string packageInstallPath, ChocolateyConfiguration config, ChocolateyPackageInformation packageInfo)
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
                            // skip nupkgs if they are different
                            if (_fileSystem.get_file_extension(currentFile.Path).is_equal_to(".nupkg")) continue;

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
            var pkgInstallPath = get_install_directory(config, installedPackage);

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

        private void remove_cache_for_package(ChocolateyConfiguration config, IPackage installedPackage)
        {
            this.Log().Debug(ChocolateyLoggers.Verbose, "Ensuring removal of package cache files.");
            var cacheDirectory = _fileSystem.combine_paths(config.CacheLocation, installedPackage.Id, installedPackage.Version.to_string());

            if (!_fileSystem.directory_exists(cacheDirectory)) return;

            FaultTolerance.try_catch_with_logging_exception(
                                       () => _fileSystem.delete_directory_if_exists(cacheDirectory, recursive: true),
                                       "Unable to removed cached files");
        }

        /// <summary>
        /// Remove NuGet cache of the package.
        /// Whether we use the cached file or not, NuGet always caches the package.
        /// This is annoying with choco, but if you use both choco and NuGet, it can
        /// cause hard to detect issues in NuGet when there is a NuGet package of the
        /// same name with different contents.
        /// </summary>
        /// <param name="installedPackage">The installed package.</param>
        private void remove_nuget_cache_for_package(IPackage installedPackage)
        {
            var localAppData = Environment.GetEnvironmentVariable("LocalAppData");
            if (string.IsNullOrWhiteSpace(localAppData)) return;

            FaultTolerance.try_catch_with_logging_exception(
                () =>
                {
                    var nugetCachedFile = _fileSystem.combine_paths(localAppData, "NuGet", "Cache", "{0}.{1}.nupkg".format_with(installedPackage.Id, installedPackage.Version.to_string()));
                    if (_fileSystem.file_exists(nugetCachedFile))
                    {

                        _fileSystem.delete_file(nugetCachedFile);
                    }
                },
                "Unable to removed cached NuGet package file");
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

        public ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config, Action<PackageResult> continueAction, Action<PackageResult> beforeUninstallAction = null)
        {
            return uninstall_run(config, continueAction, performAction: true, beforeUninstallAction: beforeUninstallAction);
        }

        public virtual ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config, Action<PackageResult> continueAction, bool performAction, Action<PackageResult> beforeUninstallAction = null)
        {
            var packageUninstalls = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            SemanticVersion version = config.Version != null ? new SemanticVersion(config.Version) : null;
            var packageManager = NugetCommon.GetPackageManager(config, _nugetLogger, _packageDownloader,
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

                    // is this the latest version, have you passed --sxs, or is this a side-by-side install? This is the only way you get through to the continue action.
                    var latestVersion = packageManager.LocalRepository.FindPackage(e.Package.Id);
                    var pkgInfo = _packageInfoService.get_package_information(e.Package);
                    if (latestVersion.Version == pkg.Version || config.AllowMultipleVersions || (pkgInfo != null && pkgInfo.IsSideBySide))
                    {
                        packageResult.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));
                        if (continueAction != null) continueAction.Invoke(packageResult);
                    }
                    else
                    {
                        //todo:allow cleaning of pkgstore files
                    }
                };

            // if we are uninstalling a package and not forcing dependencies,
            // look to see if the user is missing the actual package they meant
            // to uninstall.
            if (!config.ForceDependencies)
            {
                // if you find an install of an .install / .portable / .commandline, allow adding it to the list
                var installedPackages = get_all_installed_packages(config).Select(p => p.Name).ToList().@join(ApplicationParameters.PackageNamesSeparator);
                foreach (var packageName in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null())
                {
                    var installerExists = installedPackages.contains("{0}.install".format_with(packageName));
                    var portableExists = installedPackages.contains("{0}.portable".format_with(packageName));
                    var cmdLineExists = installedPackages.contains("{0}.commandline".format_with(packageName));
                    if ((!config.PackageNames.contains("{0}.install".format_with(packageName))
                            && !config.PackageNames.contains("{0}.portable".format_with(packageName))
                            && !config.PackageNames.contains("{0}.commandline".format_with(packageName))
                            )
                        && (installerExists || portableExists || cmdLineExists)
                        )
                    {
                        var actualPackageName = installerExists ?
                            "{0}.install".format_with(packageName)
                            : portableExists ?
                                "{0}.portable".format_with(packageName)
                                : "{0}.commandline".format_with(packageName);

                        var timeoutInSeconds = config.PromptForConfirmation ? 0 : 20;
                        this.Log().Warn(@"You are uninstalling {0}, which is likely a metapackage for an 
 *.install/*.portable package that it installed 
 ({0} represents discoverability).".format_with(packageName));
                        var selection = InteractivePrompt.prompt_for_confirmation(
                            "Would you like to uninstall {0} as well?".format_with(actualPackageName),
                            new[] { "yes", "no" },
                            defaultChoice: null,
                            requireAnswer: false,
                            allowShortAnswer: true,
                            shortPrompt: true,
                            timeoutInSeconds: timeoutInSeconds
                        );

                        if (selection.is_equal_to("yes"))
                        {
                            config.PackageNames += ";{0}".format_with(actualPackageName);
                        }
                        else
                        {
                            var logMessage = "To finish removing {0}, please also run the command: `choco uninstall {1}`.".format_with(packageName, actualPackageName);
                            var actualPackageResult = packageUninstalls.GetOrAdd(actualPackageName, new PackageResult(actualPackageName, null, null));
                            actualPackageResult.Messages.Add(new ResultMessage(ResultType.Warn, logMessage));
                            actualPackageResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                        }
                    }
                }
            }

            set_package_names_if_all_is_specified(config, () =>
                {
                    // force remove the item, ignore the dependencies
                    // as those are going to be picked up anyway
                    config.Force = true;
                    config.ForceDependencies = false;
                });

            var originalConfig = config;

            foreach (string packageName in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null())
            {
                // reset config each time through
                config = originalConfig.deep_copy();

                IList<IPackage> installedPackageVersions = new List<IPackage>();
                if (string.IsNullOrWhiteSpace(config.Version))
                {
                    installedPackageVersions = packageManager.LocalRepository.FindPackagesById(packageName).OrderBy((p) => p.Version).ToList();
                }
                else
                {
                    var semanticVersion = new SemanticVersion(config.Version);
                    installedPackageVersions = packageManager.LocalRepository.FindPackagesById(packageName).Where((p) => p.Version.Equals(semanticVersion)).ToList();
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

                        var selection = InteractivePrompt.prompt_for_confirmation("Which version of {0} would you like to uninstall?".format_with(packageName),
                            choices,
                            defaultChoice: null,
                            requireAnswer: true,
                            allowShortAnswer: false);

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
                                packageVersion.Id, packageVersion.Version.to_string())
                                )
                            {
                                if (beforeUninstallAction != null)
                                {
                                    // guessing this is not added so that it doesn't fail the action if an error is recorded?
                                    //var currentPackageResult = packageUninstalls.GetOrAdd(packageName, new PackageResult(packageVersion, get_install_directory(config, packageVersion)));
                                    var currentPackageResult = new PackageResult(packageVersion, get_install_directory(config, packageVersion));
                                    beforeUninstallAction(currentPackageResult);
                                }
                                ensure_package_files_have_compatible_attributes(config, packageVersion, pkgInfo);
                                rename_legacy_package_version(config, packageVersion, pkgInfo);
                                remove_rollback_directory_if_exists(packageName);
                                backup_existing_version(config, packageVersion, pkgInfo);
                                packageManager.UninstallPackage(packageVersion.Id.to_lower(), forceRemove: config.Force, removeDependencies: config.ForceDependencies, version: packageVersion.Version);
                                ensure_nupkg_is_removed(packageVersion, pkgInfo);
                                remove_installation_files(packageVersion, pkgInfo);
                                remove_cache_for_package(config, packageVersion);
                            }
                        }
                        catch (Exception ex)
                        {
                            var logMessage = "{0} not uninstalled. An error occurred during uninstall:{1} {2}".format_with(packageName, Environment.NewLine, ex.Message);
                            this.Log().Error(ChocolateyLoggers.Important, logMessage);
                            var result = packageUninstalls.GetOrAdd(packageVersion.Id.to_lower() + "." + packageVersion.Version.to_string(), new PackageResult(packageVersion, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, packageVersion.Id)));
                            result.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                            if (result.ExitCode == 0) result.ExitCode = 1;
                            if (config.Features.StopOnFirstPackageFailure)
                            {
                                throw new ApplicationException("Stopping further execution as {0} has failed uninstallation".format_with(packageVersion.Id.to_lower()));
                            }
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
            this.Log().Debug(ChocolateyLoggers.Verbose, "Removing nupkg if it still exists.");
            var isSideBySide = pkgInfo != null && pkgInfo.IsSideBySide;

            var nupkgFile = "{0}{1}.nupkg".format_with(removedPackage.Id, isSideBySide ? "." + removedPackage.Version.to_string() : string.Empty);
            var installDir = _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, "{0}{1}".format_with(removedPackage.Id, isSideBySide ? "." + removedPackage.Version.to_string() : string.Empty));
            var nupkg = _fileSystem.combine_paths(installDir, nupkgFile);

            if (!_fileSystem.file_exists(nupkg)) return;

            FaultTolerance.try_catch_with_logging_exception(
                () => _fileSystem.delete_file(nupkg),
                "Error deleting nupkg file",
                throwError: true);
        }

        public virtual void remove_installation_files(IPackage removedPackage, ChocolateyPackageInformation pkgInfo)
        {
            this.Log().Debug(ChocolateyLoggers.Verbose, "Ensuring removal of installation files.");
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
                        if (!_fileSystem.file_exists(file)) continue;

                        FaultTolerance.try_catch_with_logging_exception(
                            () => _fileSystem.delete_file(file),
                            "Error deleting file");
                    }

                    if (fileSnapshot.Checksum == ApplicationParameters.HashProviderFileLocked)
                    {
                        this.Log().Warn(() => "Snapshot for '{0}' was attempted when file was locked.{1} Please inspect and manually remove file{1} at '{2}'".format_with(_fileSystem.get_file_name(file), Environment.NewLine, _fileSystem.get_directory_name(file)));
                    }
                }
            }

            if (_fileSystem.directory_exists(installDir) && !_fileSystem.get_files(installDir, "*.*", SearchOption.AllDirectories).or_empty_list_if_null().Any())
            {
                _fileSystem.delete_directory_if_exists(installDir, recursive: true);
            }
        }

        private IEnumerable<PackageResult> get_all_installed_packages(ChocolateyConfiguration config)
        {
            //todo : move to deep copy for get all installed
            //var listConfig = config.deep_copy();
            //listConfig.ListCommand.LocalOnly = true;
            //listConfig.Noop = false;
            //listConfig.PackageNames = string.Empty;
            //listConfig.Input = string.Empty;
            //listConfig.QuietOutput = true;

            //return list_run(listConfig).ToList();

            config.ListCommand.LocalOnly = true;
            var sources = config.Sources;
            //changed by the command automatically when LocalOnly = true
            config.Sources = ApplicationParameters.PackagesLocation;
            var pre = config.Prerelease;
            //changed by the command automatically when LocalOnly = true
            config.Prerelease = true;
            var noop = config.Noop;
            config.Noop = false;
            var packageNames = config.PackageNames;
            config.PackageNames = string.Empty;
            var input = config.Input;
            config.Input = string.Empty;
            var quiet = config.QuietOutput;
            config.QuietOutput = true;
            //changed by the command automatically when LocalOnly = true
            var includeVersionOverrides = config.ListCommand.IncludeVersionOverrides;

            var installedPackages = list_run(config).ToList();

            config.ListCommand.IncludeVersionOverrides = includeVersionOverrides;
            config.QuietOutput = quiet;
            config.Input = input;
            config.PackageNames = packageNames;
            config.Noop = noop;
            config.Prerelease = pre;
            config.Sources = sources;

            return installedPackages;
        }

        private void set_package_names_if_all_is_specified(ChocolateyConfiguration config, Action customAction)
        {
            if (config.PackageNames.is_equal_to(ApplicationParameters.AllPackages))
            {
                var packagesToUpdate = get_all_installed_packages(config).Select(p => p.Name).ToList();

                if (!string.IsNullOrWhiteSpace(config.UpgradeCommand.PackageNamesToSkip))
                {
                    var packagesToSkip = config.UpgradeCommand.PackageNamesToSkip
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Select(p => p.trim_safe())
                        .ToList();

                    var unknownPackagesToSkip = packagesToSkip
                        .Where(p => !packagesToUpdate.Contains(p, StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    if (unknownPackagesToSkip.Any())
                    {
                        this.Log().Warn(() => "Some packages specified in the 'except' list were not found in the local packages: '{0}'".format_with(string.Join(",", unknownPackagesToSkip)));

                        packagesToSkip = packagesToSkip
                            .Where(p => !unknownPackagesToSkip.Contains(p, StringComparer.OrdinalIgnoreCase))
                            .ToList();
                    }

                    if (packagesToSkip.Any())
                    {
                        packagesToUpdate = packagesToUpdate
                            .Where(p => !packagesToSkip.Contains(p, StringComparer.OrdinalIgnoreCase))
                            .ToList();

                        this.Log().Info(() => "These packages will not be upgraded because they were specified in the 'except' list: {0}".format_with(string.Join(",", packagesToSkip)));
                    }
                }

                config.PackageNames = packagesToUpdate.@join(ApplicationParameters.PackageNamesSeparator);

                if (customAction != null) customAction.Invoke();
            }
        }
    }
}