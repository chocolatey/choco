// Copyright © 2017 - 2022 Chocolatey Software, Inc
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
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Chocolatey.NuGet.Frameworks;
    using adapters;
    using chocolatey.infrastructure.app.utility;
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
    using NuGet.Common;
    using NuGet.Configuration;
    using NuGet.PackageManagement;
    using NuGet.Packaging;
    using NuGet.Packaging.Core;
    using NuGet.Protocol;
    using NuGet.Packaging.Signing;
    using NuGet.ProjectManagement;
    using NuGet.Protocol.Core.Types;
    using NuGet.Resolver;
    using NuGet.Versioning;

    //todo: #2575 - this monolith is too large. Refactor once test coverage is up.

    public class NugetService : INugetService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _nugetLogger;
        private readonly IChocolateyPackageInformationService _packageInfoService;
        private readonly IFilesService _filesService;
        //private readonly PackageDownloader _packageDownloader;
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
        public NugetService(IFileSystem fileSystem, ILogger nugetLogger, IChocolateyPackageInformationService packageInfoService, IFilesService filesService)
        {
            _fileSystem = fileSystem;
            _nugetLogger = nugetLogger;
            _packageInfoService = packageInfoService;
            _filesService = filesService;
        }

        public string SourceType
        {
            get { return SourceTypes.NORMAL; }
        }

        public void ensure_source_app_installed(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> ensureAction)
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
                    if (!pkg.Identity.Version.to_string().is_equal_to(config.Version)) continue;
                }

                ChocolateyPackageMetadata packageLocalMetadata;
                string packageInstallLocation = null;
                if (package.PackagePath != null && !string.IsNullOrWhiteSpace(package.PackagePath))
                {
                    packageLocalMetadata = new ChocolateyPackageMetadata(package.PackagePath, _fileSystem);
                    packageInstallLocation = _fileSystem.get_directory_name(package.PackagePath);
                }
                else
                {
                    packageLocalMetadata = null;
                }

                if (config.ListCommand.LocalOnly && packageLocalMetadata != null)
                {
                    var packageInfo = _packageInfoService.get_package_information(packageLocalMetadata);
                    if (config.ListCommand.IncludeVersionOverrides)
                    {
                        if (packageInfo.VersionOverride != null)
                        {
                            packageLocalMetadata.OverrideOriginalVersion(packageInfo.VersionOverride);
                        }
                    }

                }

                if (!config.QuietOutput)
                {
                    var logger = config.Verbose ? ChocolateyLoggers.Important : ChocolateyLoggers.Normal;

                    if (config.RegularOutput)
                    {
                        this.Log().Info(logger, () => "{0}{1}".format_with(package.Identity.Id, config.ListCommand.IdOnly ? string.Empty : " {0}{1}{2}{3}".format_with(
                                packageLocalMetadata != null ? packageLocalMetadata.Version.to_full_string() : package.Identity.Version.to_full_string(),
                                package.IsApproved ? " [Approved]" : string.Empty,
                                package.IsDownloadCacheAvailable ? " Downloads cached for licensed users" : string.Empty,
                                package.PackageTestResultStatus == "Failing" && package.IsDownloadCacheAvailable ? " - Possibly broken for FOSS users (due to original download location changes by vendor)" : package.PackageTestResultStatus == "Failing" ? " - Possibly broken" : string.Empty
                            ))
                        );

                        if (config.Verbose && !config.ListCommand.IdOnly) this.Log().Info(() =>
                            @" Title: {0} | Published: {1}{2}{3}
 Number of Downloads: {4} | Downloads for this version: {5}
 Package url: {6}
 Chocolatey Package Source: {7}{8}
 Tags: {9}
 Software Site: {10}
 Software License: {11}{12}{13}{14}{15}{16}
 Description: {17}{18}
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
                                package.PackageDetailsUrl == null || string.IsNullOrWhiteSpace(package.PackageDetailsUrl.AbsoluteUri) ? "N/A" : package.PackageDetailsUrl.AbsoluteUri,
                                packageLocalMetadata != null && packageLocalMetadata.PackageSourceUrl != null && !string.IsNullOrWhiteSpace(packageLocalMetadata.PackageSourceUrl.to_string())
                                    ? packageLocalMetadata.PackageSourceUrl.to_string()
                                    : "N/A",
                                string.IsNullOrWhiteSpace(package.PackageHash) ? string.Empty : "{0} Package Checksum: '{1}' ({2})".format_with(
                                        Environment.NewLine,
                                        package.PackageHash,
                                        package.PackageHashAlgorithm
                                        ),
                                package.Tags.trim_safe().escape_curly_braces(),
                                package.ProjectUrl != null ? package.ProjectUrl.to_string() : "n/a",
                                package.LicenseUrl != null && !string.IsNullOrWhiteSpace(package.LicenseUrl.to_string()) ? package.LicenseUrl.to_string() : "n/a",
                                packageLocalMetadata != null && packageLocalMetadata.ProjectSourceUrl != null && !string.IsNullOrWhiteSpace(packageLocalMetadata.ProjectSourceUrl.to_string()) ? "{0} Software Source: {1}".format_with(Environment.NewLine, packageLocalMetadata.ProjectSourceUrl.to_string()) : string.Empty,
                                packageLocalMetadata != null && packageLocalMetadata.DocsUrl != null && !string.IsNullOrWhiteSpace(packageLocalMetadata.DocsUrl.to_string()) ? "{0} Documentation: {1}".format_with(Environment.NewLine, packageLocalMetadata.DocsUrl.to_string()) : string.Empty,
                                packageLocalMetadata != null && packageLocalMetadata.MailingListUrl != null && !string.IsNullOrWhiteSpace(packageLocalMetadata.MailingListUrl.to_string()) ? "{0} Mailing List: {1}".format_with(Environment.NewLine, packageLocalMetadata.MailingListUrl.to_string()) : string.Empty,
                                packageLocalMetadata != null && packageLocalMetadata.BugTrackerUrl != null && !string.IsNullOrWhiteSpace(packageLocalMetadata.BugTrackerUrl.to_string()) ? "{0} Issues: {1}".format_with(Environment.NewLine, packageLocalMetadata.BugTrackerUrl.to_string()) : string.Empty,
                                package.Summary != null && !string.IsNullOrWhiteSpace(package.Summary.to_string()) ? "{0} Summary: {1}".format_with(Environment.NewLine, package.Summary.escape_curly_braces().to_string()) : string.Empty,
                                package.Description.escape_curly_braces().Replace("\n    ", "\n").Replace("\n", "\n  "),
                                packageLocalMetadata != null && packageLocalMetadata.ReleaseNotes != null && !string.IsNullOrWhiteSpace(packageLocalMetadata.ReleaseNotes.to_string()) ? "{0} Release Notes: {1}".format_with(Environment.NewLine, packageLocalMetadata.ReleaseNotes.escape_curly_braces().Replace("\n    ", "\n").Replace("\n", "\n  ")) : string.Empty
                            ));


                    }
                    else
                    {
                        this.Log().Info(logger, () => "{0}{1}".format_with(package.Identity.Id, config.ListCommand.IdOnly ? string.Empty : "|{0}".format_with(package.Identity.Version.to_full_string())));
                    }
                }
                else
                {
                    this.Log().Debug(() => "{0}{1}".format_with(package.Identity.Id, config.ListCommand.IdOnly ? string.Empty : " {0}".format_with(package.Identity.Version.to_full_string())));
                }
                count++;

                if (packageLocalMetadata is null)
                {
                    yield return new PackageResult(package, null, config.Sources);
                }
                else
                {
                    yield return new PackageResult(packageLocalMetadata, package, packageInstallLocation, config.Sources);
                }
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
            var nuspecFilePath = validate_and_return_package_file(config, PackagingConstants.ManifestExtension);
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

            //Allows empty directories to be distributed in templates via .template packages, issue #1003
            bool includeEmptyDirectories = true;
            //No need to be deterministic, it's ok to include timestamps
            bool deterministic = false;
            var builder = new PackageBuilder(nuspecFilePath, nuspecDirectory, propertyProvider.GetPropertyValue, includeEmptyDirectories, deterministic, _nugetLogger);
            if (!string.IsNullOrWhiteSpace(config.Version))
            {
                builder.Version = new NuGetVersion(config.Version);
            }

            string outputFile = builder.Id + "." + builder.Version.to_normalized_string() + NuGetConstants.PackageExtension;
            string outputFolder = config.OutputDirectory ?? _fileSystem.get_current_directory();
            string outputPath = _fileSystem.combine_paths(outputFolder, outputFile);

            config.Sources = outputFolder;

            this.Log().Info(config.QuietOutput ? ChocolateyLoggers.LogFileOnly : ChocolateyLoggers.Normal, () => "Attempting to build package from '{0}'.".format_with(_fileSystem.get_file_name(nuspecFilePath)));
            _fileSystem.create_directory_if_not_exists(outputFolder);

            var createdPackage = NugetPack.BuildPackage(builder, _fileSystem, outputPath);
            // package.Validate().Any(v => v.Level == PackageIssueLevel.Error)
            if (!createdPackage)
            {
                throw new ApplicationException("Unable to create nupkg. See the log for error details.");
            }
            //todo: #602 analyze package
            //if (package != null)
            //{
            //    AnalyzePackage(package);
            //}

            this.Log().Info(config.QuietOutput ? ChocolateyLoggers.LogFileOnly : ChocolateyLoggers.Important, () => "Successfully created package '{0}'".format_with(outputPath));
        }

        public void push_noop(ChocolateyConfiguration config)
        {
            string nupkgFilePath = validate_and_return_package_file(config, NuGetConstants.PackageExtension);
            this.Log().Info(() => "Would have attempted to push '{0}' to source '{1}'.".format_with(_fileSystem.get_file_name(nupkgFilePath), config.Sources));
        }

        public virtual void push_run(ChocolateyConfiguration config)
        {
            string nupkgFilePath = validate_and_return_package_file(config, NuGetConstants.PackageExtension);
            string nupkgFileName = _fileSystem.get_file_name(nupkgFilePath);
            if (config.RegularOutput) this.Log().Info(() => "Attempting to push {0} to {1}".format_with(nupkgFileName, config.Sources));

            NugetPush.push_package(config, _fileSystem.get_full_path(nupkgFilePath), _nugetLogger, nupkgFileName);

            if (config.RegularOutput && (config.Sources.is_equal_to(ApplicationParameters.ChocolateyCommunityFeedPushSource) || config.Sources.is_equal_to(ApplicationParameters.ChocolateyCommunityFeedPushSourceOld)))
            {
                this.Log().Warn(ChocolateyLoggers.Important, () => @"

Your package will go through automated checks and may be subject to
human moderation. You should receive email(s) with the automated
testing results. If you don't receive them within 1-3 business days,
please use the 'Contact Site Admins' on the package page to contact the
moderators. If your package is subject to human moderation there is no
guarantee on the length of time that this can take to complete. This
depends on the availability of moderators, number of packages in the
queue at this time, as well as many other factors.

You can check where your package is in the moderation queue here:
https://ch0.co/moderation

For more information about the moderation process, see the docs:
https://docs.chocolatey.org/en-us/community-repository/moderation/

Please ensure your registered email address is correct and emails from
moderation at chocolatey dot org are not being sent to your spam/junk
folder.");
            }
        }

        public void install_noop(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
        {
            //todo: #2576 noop should see if packages are already installed and adjust message, amiright?!

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

        public virtual ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.PackagesLocation);
            var packageResultsToReturn = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            //todo: #23 handle all

            NuGetVersion version = !string.IsNullOrWhiteSpace(config.Version) ? NuGetVersion.Parse(config.Version) : null;
            if (config.Force) config.AllowDowngrade = true;

            var sourceCacheContext = new ChocolateySourceCacheContext(config);
            var remoteRepositories = NugetCommon.GetRemoteRepositories(config, _nugetLogger);
            var localRepositorySource = NugetCommon.GetLocalRepository();
            var pathResolver = NugetCommon.GetPathResolver(config, _fileSystem);
            var nugetProject = new FolderNuGetProject(ApplicationParameters.PackagesLocation, pathResolver, NuGetFramework.AnyFramework);
            var projectContext = new ChocolateyNuGetProjectContext(config, _nugetLogger);

            IList<string> packageNames = config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null().ToList();
            if (packageNames.Count == 1)
            {
                var packageName = packageNames.DefaultIfEmpty(string.Empty).FirstOrDefault();
                if (packageName.EndsWith(NuGetConstants.PackageExtension) || packageName.EndsWith(PackagingConstants.ManifestExtension))
                {
                    this.Log().Debug("Updating source and package name to handle *.nupkg or *.nuspec file.");
                    packageNames.Clear();

                    config.Sources = _fileSystem.get_directory_name(_fileSystem.get_full_path(packageName));

                    if (packageName.EndsWith(PackagingConstants.ManifestExtension))
                    {
                        packageNames.Add(_fileSystem.get_file_name_without_extension(packageName));

                        this.Log().Debug("Building nuspec file prior to install.");
                        config.Input = packageName;
                        // build package
                        pack_run(config);
                    }
                    else
                    {
                        using (var packageFile = new PackageArchiveReader(_fileSystem.get_full_path(packageName)))
                        {
                            version = packageFile.NuspecReader.GetVersion();
                            packageNames.Add(packageFile.NuspecReader.GetId());
                            packageFile.Dispose();
                        }
                    }
                }
            }

            // this is when someone points the source directly at a nupkg
            // e.g. -source c:\somelocation\somewhere\packagename.nupkg
            if (config.Sources.to_string().EndsWith(NuGetConstants.PackageExtension))
            {
                config.Sources = _fileSystem.get_directory_name(_fileSystem.get_full_path(config.Sources));
            }

            config.start_backup();

            foreach (string packageName in packageNames.or_empty_list_if_null())
            {
                // We need to ensure we are using a clean configuration file
                // before we start reading it.
                config.reset_config();

                var allLocalPackages = get_all_installed_packages(config).ToList();
                var packagesToInstall = new List<IPackageSearchMetadata>();
                var packagesToUninstall = new HashSet<PackageResult>();
                var sourcePackageDependencyInfos = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
                var localPackageToRemoveDependencyInfos = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);

                //todo: #2577 get smarter about realizing multiple versions have been installed before and allowing that
                var installedPackage = allLocalPackages.FirstOrDefault(p => p.Name.Equals(packageName));

                if (Platform.get_platform() != PlatformType.Windows && !packageName.EndsWith(".template"))
                {
                    string logMessage = "{0} is not a supported package on non-Windows systems.{1}Only template packages are currently supported.".format_with(packageName, Environment.NewLine);
                    this.Log().Warn(ChocolateyLoggers.Important, logMessage);
                }

                if (installedPackage != null && (version == null || version == installedPackage.PackageMetadata.Version) && !config.Force)
                {
                    string logMessage = "{0} v{1} already installed.{2} Use --force to reinstall, specify a version to install, or try upgrade.".format_with(installedPackage.Name, installedPackage.Version, Environment.NewLine);
                    var nullResult = packageResultsToReturn.GetOrAdd(packageName, installedPackage);
                    nullResult.Messages.Add(new ResultMessage(ResultType.Warn, logMessage));
                    nullResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                    this.Log().Warn(ChocolateyLoggers.Important, logMessage);
                    continue;
                }

                NuGetVersion latestPackageVersion = null;

                if (installedPackage != null && (version == null || version == installedPackage.PackageMetadata.Version) && config.Force)
                {
                    this.Log().Warn(ChocolateyLoggers.Important, () => @"{0} v{1} already installed. Forcing reinstall of version '{1}'.
 Please use upgrade if you meant to upgrade to a new version.".format_with(installedPackage.Name, installedPackage.Version));

                    //This is set to ensure the same package version is reinstalled
                    latestPackageVersion = installedPackage.PackageMetadata.Version;
                }

                if (installedPackage != null && version != null && version < installedPackage.PackageMetadata.Version && !config.AllowMultipleVersions && !config.AllowDowngrade)
                {
                    string logMessage = "A newer version of {0} (v{1}) is already installed.{2} Use --allow-downgrade or --force to attempt to install older versions, or use --side-by-side to allow multiple versions.".format_with(installedPackage.Name, installedPackage.Version, Environment.NewLine);
                    var nullResult = packageResultsToReturn.GetOrAdd(packageName, installedPackage);
                    nullResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                    this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    continue;
                }

                if (latestPackageVersion is null && version != null)
                {
                    latestPackageVersion = version;
                }

                var availablePackage = NugetList.find_package(packageName, config, _nugetLogger, sourceCacheContext, NugetCommon.GetRepositoryResource<PackageMetadataResource>(remoteRepositories).ToList(), latestPackageVersion);

                if (availablePackage == null)
                {
                    var logMessage = @"{0} not installed. The package was not found with the source(s) listed.
 Source(s): '{1}'
 NOTE: When you specify explicit sources, it overrides default sources.
If the package version is a prerelease and you didn't specify `--pre`,
 the package may not be found.{2}{3}".format_with(packageName, config.Sources, string.IsNullOrWhiteSpace(config.Version)
                            ? String.Empty
                            : @"
Version was specified as '{0}'. It is possible that version
 does not exist for '{1}' at the source specified.".format_with(config.Version, packageName),
                        @"
Please see https://docs.chocolatey.org/en-us/troubleshooting for more
 assistance.");
                    this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    var noPkgResult = packageResultsToReturn.GetOrAdd(packageName, new PackageResult(packageName, version.to_full_string(), null));
                    noPkgResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                    continue;
                }

                var dependencyResources = NugetCommon.GetRepositoryResource<DependencyInfoResource>(remoteRepositories).ToList();
                NugetCommon.GetPackageDependencies(availablePackage.Identity, NuGetFramework.AnyFramework, sourceCacheContext, _nugetLogger, dependencyResources, sourcePackageDependencyInfos, new HashSet<PackageDependency>()).GetAwaiter().GetResult();

                if (installedPackage != null && (installedPackage.PackageMetadata.Version == availablePackage.Identity.Version) && config.Force)
                {
                    packagesToUninstall.Add(installedPackage);
                }

                if (config.ForceDependencies && installedPackage != null)
                {
                    NugetCommon.GetLocalPackageDependencies(installedPackage.Identity, NuGetFramework.AnyFramework, allLocalPackages, localPackageToRemoveDependencyInfos);

                    foreach (var dependencyInfo in localPackageToRemoveDependencyInfos)
                    {
                        packagesToUninstall.Add(allLocalPackages.FirstOrDefault(p => p.Identity.Equals(dependencyInfo)));
                    }
                }

                packagesToInstall.Add(availablePackage);
                var targetIdsToInstall = packagesToInstall.Select(p => p.Identity.Id);

                var localPackagesDependencyInfos = allLocalPackages
                    .Where(p => !targetIdsToInstall.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                    .Select(
                        p => new SourcePackageDependencyInfo(
                            p.SearchMetadata.Identity,
                            p.PackageMetadata.DependencyGroups.SelectMany(x => x.Packages).ToList(),
                            true,
                            localRepositorySource,
                            null,
                            null));
                sourcePackageDependencyInfos.AddRange(localPackagesDependencyInfos);

                var dependencyResolver = new PackageResolver();

                var allPackagesIdentities = allLocalPackages.Select(p => p.SearchMetadata.Identity).Where(p => !targetIdsToInstall.Contains(p.Id, StringComparer.OrdinalIgnoreCase)).ToList();
                var allPackagesReferences = allPackagesIdentities.Select(p => new PackageReference(p, NuGetFramework.AnyFramework));

                var resolverContext = new PackageResolverContext(
                    dependencyBehavior: DependencyBehavior.Highest,
                    targetIds: targetIdsToInstall,
                    requiredPackageIds: allPackagesIdentities.Select(p => p.Id),
                    packagesConfig: allPackagesReferences,
                    preferredVersions: allPackagesIdentities.Where(p => !p.Id.Equals(packageName, StringComparison.OrdinalIgnoreCase)),
                    availablePackages: sourcePackageDependencyInfos,
                    packageSources: remoteRepositories.Select(s => s.PackageSource),
                    log: _nugetLogger
                );

                IEnumerable<SourcePackageDependencyInfo> resolvedPackages = new List<SourcePackageDependencyInfo>();
                if (config.IgnoreDependencies)
                {
                    resolvedPackages = packagesToInstall.Select(p => sourcePackageDependencyInfos.Single(x => p.Identity.Equals(new PackageIdentity(x.Id, x.Version))));

                    if (config.ForceDependencies)
                    {
                        //TODO Log warning here about dependencies being removed and not being reinstalled?
                        foreach (var packageToUninstall in packagesToUninstall.Where(p => !resolvedPackages.Contains(p.Identity)))
                        {
                            try
                            {
                                nugetProject.DeletePackage(packageToUninstall.Identity, projectContext, CancellationToken.None).GetAwaiter().GetResult();
                                remove_cache_for_package(config, packageToUninstall.PackageMetadata);
                            }
                            catch (Exception ex)
                            {
                                var forcedResult = packageResultsToReturn.GetOrAdd(packageToUninstall.Identity.Id, packageToUninstall);
                                forcedResult.Messages.Add(new ResultMessage(ResultType.Note, "Removing old version"));
                                string logMessage = "{0}:{1} {2}".format_with("Unable to remove existing package", Environment.NewLine, ex.Message);
                                this.Log().Warn(logMessage);
                                forcedResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                            }
                        }
                    }
                }
                else
                {
                    try
                    {
                        resolvedPackages = dependencyResolver.Resolve(resolverContext, CancellationToken.None)
                            .Select(p => sourcePackageDependencyInfos.Single(x => PackageIdentityComparer.Default.Equals(x, p)));


                        if (!config.ForceDependencies)
                        {
                            var identitiesToUninstall = packagesToUninstall.Select(x => x.Identity);
                            resolvedPackages = resolvedPackages.Where(p => !(localPackagesDependencyInfos.Contains(p) && !identitiesToUninstall.Contains(p)));

                            if (!config.AllowMultipleVersions)
                            {
                                // If forcing dependencies, then dependencies already added to packages to remove
                                // If allow multiple is added, then new version of dependency will be added side by side
                                // If neither, then package needs to be removed so it can be upgraded to the new version required by the parent

                                packagesToUninstall.AddRange(allLocalPackages.Where(p => resolvedPackages.Select(x => x.Id).Contains(p.Name, StringComparer.OrdinalIgnoreCase)));
                            }
                        }
                    }
                    catch (NuGetResolverConstraintException ex)
                    {
                        this.Log().Warn(ex.Message);

                        string constraintPattern = @"constraint: (?<packageId>\w+)\s\(";
                        var invalidDependencyMatch = Regex.Match(ex.Message, constraintPattern, RegexOptions.IgnoreCase);
                        var invalidDependencyName = invalidDependencyMatch.Groups["packageId"].Success ? invalidDependencyMatch.Groups["packageId"].Value : string.Empty;

                        if (!invalidDependencyMatch.Groups["packageId"].Success)
                        {
                            string resolvePattern = @"Unable to resolve dependency \'(?<packageId>\w+)\'";
                            var resolveDependencyMatch = Regex.Match(ex.Message, resolvePattern, RegexOptions.IgnoreCase);
                            invalidDependencyName = resolveDependencyMatch.Groups["packageId"].Success ? resolveDependencyMatch.Groups["packageId"].Value : string.Empty;

                            if (!resolveDependencyMatch.Success)
                            {
                                this.Log().Warn("Unable to match dependency resolution message, add another type");
                            }
                        }

                        foreach (var pkgMetadata in packagesToInstall)
                        {
                            var logMessage = "Unable to resolve dependency '{0}'".format_with(invalidDependencyName);
                            this.Log().Error(ChocolateyLoggers.Important, logMessage);
                            var errorResult = packageResultsToReturn.GetOrAdd(pkgMetadata.Identity.Id, new PackageResult(pkgMetadata, pathResolver.GetInstallPath(pkgMetadata.Identity)));
                            errorResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Log().Warn("Need to add specific handling for exception type {0}".format_with(nameof(ex)));
                        this.Log().Warn(ex.Message);
                    }
                }

                foreach (SourcePackageDependencyInfo packageDependencyInfo in resolvedPackages)
                {

                    var packageRemoteMetadata = packagesToInstall.FirstOrDefault(p => p.Identity.Equals(packageDependencyInfo));

                    if (packageRemoteMetadata is null)
                    {
                        packageRemoteMetadata = packageDependencyInfo
                            .Source
                            .GetResource<PackageMetadataResource>()
                            .GetMetadataAsync(packageDependencyInfo, sourceCacheContext, _nugetLogger, CancellationToken.None)
                            .GetAwaiter().GetResult();

                        var resource = packageDependencyInfo.Source.GetResource<PackageMetadataResource>();
                    }

                    bool shouldAddForcedResultMessage = false;

                    var packageToUninstall = packagesToUninstall.FirstOrDefault(p => p.PackageMetadata.Id.Equals(packageDependencyInfo.Id, StringComparison.OrdinalIgnoreCase));
                    if (packageToUninstall != null)
                    {
                        shouldAddForcedResultMessage = true;
                        remove_rollback_directory_if_exists(packageRemoteMetadata.Identity.Id);
                        backup_existing_version(config, packageToUninstall.PackageMetadata, _packageInfoService.get_package_information(packageToUninstall.PackageMetadata));
                        packageToUninstall.InstallLocation = pathResolver.GetInstallPath(packageToUninstall.Identity);
                        try
                        {
                            // This deletes satellite files and stuff
                            //But it does not throw or return false if it fails to delete something...
                            var ableToDelete = nugetProject.DeletePackage(packageToUninstall.Identity, projectContext, CancellationToken.None, shouldDeleteDirectory: false).GetAwaiter().GetResult();
                            //So removing directly manually so as to throw if needed.
                            _fileSystem.delete_directory_if_exists(packageToUninstall.InstallLocation, true, true, true);
                            remove_cache_for_package(config, packageToUninstall.PackageMetadata);
                        }
                        catch (Exception ex)
                        {
                            var forcedResult = packageResultsToReturn.GetOrAdd(packageToUninstall.Name, packageToUninstall);
                            forcedResult.Messages.Add(new ResultMessage(ResultType.Note, "Backing up and removing old version"));
                            string logMessage = "{0}:{1} {2}".format_with("Unable to remove existing package prior to forced reinstall", Environment.NewLine, ex.Message);
                            this.Log().Warn(logMessage);
                            forcedResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                            forcedResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                            if (continueAction != null) continueAction.Invoke(forcedResult, config);

                            continue;
                        }
                    }


                    try
                    {
                        //TODO, do sanity check here.

                        var downloadResource = packageDependencyInfo.Source.GetResource<DownloadResource>();

                        _fileSystem.delete_file(pathResolver.GetInstalledPackageFilePath(packageDependencyInfo));

                        using (var downloadResult = downloadResource.GetDownloadResourceResultAsync(
                                   packageDependencyInfo,
                                   new PackageDownloadContext(sourceCacheContext),
                                   config.CacheLocation,
                                   _nugetLogger, CancellationToken.None).GetAwaiter().GetResult())
                        {
                            //TODO, do check on downloadResult

                            nugetProject.InstallPackageAsync(
                                packageDependencyInfo,
                                downloadResult,
                                projectContext,
                                CancellationToken.None).GetAwaiter().GetResult();

                        }

                        remove_nuget_cache_for_package(availablePackage);

                        var manifestPath = nugetProject.GetInstalledManifestFilePath(packageDependencyInfo);
                        var packageMetadata = new ChocolateyPackageMetadata(manifestPath, _fileSystem);

                        var installedPath = nugetProject.GetInstalledPath(packageDependencyInfo);

                        this.Log().Info(ChocolateyLoggers.Important, "{0}{1} v{2}{3}{4}{5}".format_with(
                            System.Environment.NewLine,
                            packageMetadata.Id,
                            packageMetadata.Version.to_full_string(),
                            config.Force ? " (forced)" : string.Empty,
                            packageRemoteMetadata.IsApproved ? " [Approved]" : string.Empty,
                            packageRemoteMetadata.PackageTestResultStatus == "Failing" && packageRemoteMetadata.IsDownloadCacheAvailable ? " - Likely broken for FOSS users (due to download location changes)" : packageRemoteMetadata.PackageTestResultStatus == "Failing" ? " - Possibly broken" : string.Empty
                        ));

                        var packageResult = packageResultsToReturn.GetOrAdd(packageDependencyInfo.Id.to_lower(), new PackageResult(packageMetadata, packageRemoteMetadata, installedPath));
                        if (shouldAddForcedResultMessage) packageResult.Messages.Add(new ResultMessage(ResultType.Note, "Backing up and removing old version"));
                        packageResult.InstallLocation = installedPath;
                        packageResult.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));

                        if (continueAction != null) continueAction.Invoke(packageResult, config);

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

                        var logMessage = "{0} not installed. An error occurred during installation:{1} {2}".format_with(packageDependencyInfo.Id, Environment.NewLine, message);
                        this.Log().Error(ChocolateyLoggers.Important, logMessage);
                        var errorResult = packageResultsToReturn.GetOrAdd(packageDependencyInfo.Id, new PackageResult(packageDependencyInfo.Id, version.to_full_string(), null));
                        errorResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                        if (errorResult.ExitCode == 0) errorResult.ExitCode = 1;
                        if (continueAction != null) continueAction.Invoke(errorResult, config);
                    }
                }
            }

            // Reset the configuration again once we are completely done with the processing of
            // configurations, and make sure that we are removing any backup that was created
            // as part of this run.
            config.reset_config(removeBackup: true);

            return packageResultsToReturn;
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

        public ConcurrentDictionary<string, PackageResult> upgrade_noop(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
        {
            config.Force = false;
            return upgrade_run(config, continueAction, performAction: false);
        }

        public ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeUpgradeAction = null)
        {
            return upgrade_run(config, continueAction, performAction: true, beforeUpgradeAction: beforeUpgradeAction);
        }

        public virtual ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, bool performAction, Action<PackageResult, ChocolateyConfiguration> beforeUpgradeAction = null)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.PackagesLocation);
            var packageResultsToReturn = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            NuGetVersion version = !string.IsNullOrWhiteSpace(config.Version) ? NuGetVersion.Parse(config.Version) : null;

            if (config.Force) config.AllowDowngrade = true;

            var sourceCacheContext = new ChocolateySourceCacheContext(config);
            var remoteRepositories = NugetCommon.GetRemoteRepositories(config, _nugetLogger);
            var localRepositorySource = NugetCommon.GetLocalRepository();
            var projectContext = new ChocolateyNuGetProjectContext(config, _nugetLogger);

            var configIgnoreDependencies = config.IgnoreDependencies;
            set_package_names_if_all_is_specified(config, () => { config.IgnoreDependencies = true; });
            config.IgnoreDependencies = configIgnoreDependencies;

            config.start_backup();

            foreach (string packageName in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null())
            {
                // We need to ensure we are using a clean configuration file
                // before we start reading it.
                config.reset_config();

                var allLocalPackages = get_all_installed_packages(config);
                var installedPackage = allLocalPackages.FirstOrDefault(p => p.Name.Equals(packageName));
                var packagesToInstall = new List<IPackageSearchMetadata>();
                var packagesToUninstall = new HashSet<PackageResult>();
                var sourcePackageDependencyInfos = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
                var localPackageToRemoveDependencyInfos = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
                var dependencyResources = NugetCommon.GetRepositoryResource<DependencyInfoResource>(remoteRepositories).ToList();
                var sourceDependencyCache = new HashSet<PackageDependency>();

                if (installedPackage == null)
                {
                    if (config.UpgradeCommand.FailOnNotInstalled)
                    {
                        string failLogMessage = "{0} is not installed. Cannot upgrade a non-existent package.".format_with(packageName);
                        var result = packageResultsToReturn.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                        result.Messages.Add(new ResultMessage(ResultType.Error, failLogMessage));
                        if (config.RegularOutput) this.Log().Error(ChocolateyLoggers.Important, failLogMessage);

                        continue;
                    }

                    if (config.Features.SkipPackageUpgradesWhenNotInstalled)
                    {
                        string warnLogMessage = "{0} is not installed and skip non-installed option selected. Skipping...".format_with(packageName);
                        var result = packageResultsToReturn.GetOrAdd(packageName, new PackageResult(packageName, null, null));
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
                            packageResultsToReturn.GetOrAdd(result.Key, result.Value);
                        }
                    }

                    config.PackageNames = packageNames;
                    continue;
                }

                var pkgInfo = _packageInfoService.get_package_information(installedPackage.PackageMetadata);
                bool isPinned = pkgInfo != null && pkgInfo.IsPinned;

                if (isPinned && config.OutdatedCommand.IgnorePinned)
                {
                    continue;
                }

                //Needs to be set here to ensure that the path resolver has the side by side option set correctly.
                set_package_config_for_upgrade(config, pkgInfo);
                var pathResolver = NugetCommon.GetPathResolver(config, _fileSystem);
                var nugetProject = new FolderNuGetProject(ApplicationParameters.PackagesLocation, pathResolver, NuGetFramework.AnyFramework);

                if (version != null && version < installedPackage.PackageMetadata.Version && !config.AllowMultipleVersions && !config.AllowDowngrade)
                {
                    string logMessage = "A newer version of {0} (v{1}) is already installed.{2} Use --allow-downgrade or --force to attempt to upgrade to older versions, or use side by side to allow multiple versions.".format_with(installedPackage.PackageMetadata.Id, installedPackage.Version, Environment.NewLine);
                    var nullResult = packageResultsToReturn.GetOrAdd(packageName, new PackageResult(installedPackage.PackageMetadata, pathResolver.GetInstallPath(installedPackage.PackageMetadata.Id, installedPackage.PackageMetadata.Version)));
                    nullResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                    this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    continue;
                }

                // if we have a prerelease installed, we want to have it upgrade based on newer prereleases
                var originalPrerelease = config.Prerelease;
                if (installedPackage.PackageMetadata.Version.IsPrerelease && !config.UpgradeCommand.ExcludePrerelease)
                {
                    // this is a prerelease - opt in for newer prereleases.
                    config.Prerelease = true;
                }

                var availablePackage = NugetList.find_package(packageName, config, _nugetLogger, sourceCacheContext, NugetCommon.GetRepositoryResource<PackageMetadataResource>(remoteRepositories).ToList(), version);

                config.Prerelease = originalPrerelease;

                if (availablePackage == null)
                {
                    if (config.Features.IgnoreUnfoundPackagesOnUpgradeOutdated) continue;

                    string logMessage = "{0} was not found with the source(s) listed.{1} If you specified a particular version and are receiving this message, it is possible that the package name exists but the version does not.{1} Version: \"{2}\"; Source(s): \"{3}\"".format_with(packageName, Environment.NewLine, config.Version, config.Sources);
                    var unfoundResult = packageResultsToReturn.GetOrAdd(packageName, new PackageResult(packageName, version.to_full_string(), null));

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
                            this.Log().Info("{0}|{1}|{1}|{2}".format_with(installedPackage.PackageMetadata.Id, installedPackage.Version, isPinned.to_string().to_lower()));
                        }
                    }

                    continue;
                }

                if (pkgInfo != null && pkgInfo.IsSideBySide)
                {
                    //todo: #103 get smarter about realizing multiple versions have been installed before and allowing that
                }

                var packageResult = packageResultsToReturn.GetOrAdd(packageName, new PackageResult(availablePackage, pathResolver.GetInstallPath(availablePackage.Identity)));
                if (installedPackage.PackageMetadata.Version > availablePackage.Identity.Version && (!config.AllowDowngrade || (config.AllowDowngrade && version == null)))
                {
                    string logMessage = "{0} v{1} is newer than the most recent.{2} You must be smarter than the average bear...".format_with(installedPackage.PackageMetadata.Id, installedPackage.Version, Environment.NewLine);
                    packageResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));

                    if (!config.UpgradeCommand.NotifyOnlyAvailableUpgrades)
                    {
                        if (config.RegularOutput)
                        {
                            this.Log().Info(ChocolateyLoggers.Important, logMessage);
                        }
                        else
                        {
                            this.Log().Info("{0}|{1}|{1}|{2}".format_with(installedPackage.PackageMetadata.Id, installedPackage.Version, isPinned.to_string().to_lower()));
                        }
                    }

                    continue;
                }

                if (installedPackage.PackageMetadata.Version == availablePackage.Identity.Version)
                {
                    string logMessage = "{0} v{1} is the latest version available based on your source(s).".format_with(installedPackage.PackageMetadata.Id, installedPackage.Version);

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
                                this.Log().Info("{0}|{1}|{2}|{3}".format_with(installedPackage.PackageMetadata.Id, installedPackage.Version, availablePackage.Identity.Version, isPinned.to_string().to_lower()));
                            }
                        }

                        continue;
                    }

                    packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));
                    if (config.RegularOutput) this.Log().Info(logMessage);
                }

                if ((availablePackage.Identity.Version > installedPackage.PackageMetadata.Version) || config.Force || (availablePackage.Identity.Version < installedPackage.PackageMetadata.Version && config.AllowDowngrade))
                {
                    if (availablePackage.Identity.Version > installedPackage.PackageMetadata.Version)
                    {
                        string logMessage = "You have {0} v{1} installed. Version {2} is available based on your source(s).".format_with(installedPackage.PackageMetadata.Id, installedPackage.Version, availablePackage.Identity.Version);
                        packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));

                        if (config.RegularOutput)
                        {
                            this.Log().Warn("{0}{1}".format_with(Environment.NewLine, logMessage));
                        }
                        else
                        {
                            this.Log().Info("{0}|{1}|{2}|{3}".format_with(installedPackage.PackageMetadata.Id, installedPackage.Version, availablePackage.Identity.Version, isPinned.to_string().to_lower()));
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

                        NugetCommon.GetPackageDependencies(availablePackage.Identity, NuGetFramework.AnyFramework, sourceCacheContext, _nugetLogger, dependencyResources, sourcePackageDependencyInfos, sourceDependencyCache).GetAwaiter().GetResult();


                        packagesToUninstall.Add(installedPackage);

                        if (config.ForceDependencies && installedPackage != null)
                        {
                            NugetCommon.GetLocalPackageDependencies(installedPackage.Identity, NuGetFramework.AnyFramework, allLocalPackages, localPackageToRemoveDependencyInfos);

                            foreach (var dependencyInfo in localPackageToRemoveDependencyInfos)
                            {
                                packagesToUninstall.Add(allLocalPackages.FirstOrDefault(p => p.Identity.Equals(dependencyInfo)));
                            }
                        }


                        packagesToInstall.Add(availablePackage);

                        var localPackagesDependencyInfos = allLocalPackages
                            .Where(p => !p.Name.Equals(availablePackage.Identity.Id, StringComparison.OrdinalIgnoreCase))
                            .Select(
                                p => new SourcePackageDependencyInfo(
                                    p.SearchMetadata.Identity,
                                    p.PackageMetadata.DependencyGroups.SelectMany(x => x.Packages).ToList(),
                                    true,
                                    localRepositorySource,
                                    null,
                                    null));
                        sourcePackageDependencyInfos.AddRange(localPackagesDependencyInfos);

                        var parentInfos = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
                        NugetCommon.GetPackageParents(availablePackage.Identity.Id, parentInfos, localPackagesDependencyInfos).GetAwaiter().GetResult();
                        foreach (var parentPackage in parentInfos)
                        {
                            foreach (var packageVersion in NugetList.find_all_package_versions(parentPackage.Id, config, _nugetLogger, sourceCacheContext, NugetCommon.GetRepositoryResource<PackageMetadataResource>(remoteRepositories).ToList()))
                            {
                                NugetCommon.GetPackageDependencies(packageVersion.Identity, NuGetFramework.AnyFramework, sourceCacheContext, _nugetLogger, dependencyResources, sourcePackageDependencyInfos, sourceDependencyCache).GetAwaiter().GetResult();
                            }
                        }

                        sourcePackageDependencyInfos.RemoveWhere(p => p.Id.Equals(availablePackage.Identity.Id, StringComparison.OrdinalIgnoreCase) && !p.Version.Equals(availablePackage.Identity.Version));

                        var dependencyResolver = new PackageResolver();

                        var targetIdsToInstall = packagesToInstall.Select(p => p.Identity.Id);
                        var allPackagesIdentities = allLocalPackages.Where(x => !targetIdsToInstall.Contains(x.Identity.Id, StringComparer.OrdinalIgnoreCase)).Select(p => p.SearchMetadata.Identity).ToList();
                        //var allPackagesIdentities = allLocalPackages.Select(p => p.SearchMetadata.Identity).ToList();
                        var allPackagesReferences = allPackagesIdentities.Select(p => new PackageReference(p, NuGetFramework.AnyFramework));

                        var resolverContext = new PackageResolverContext(
                            dependencyBehavior: DependencyBehavior.Highest,
                            targetIds: targetIdsToInstall,
                            requiredPackageIds: allPackagesIdentities.Select(p => p.Id),
                            packagesConfig: allPackagesReferences,
                            preferredVersions: allPackagesIdentities,
                            availablePackages: sourcePackageDependencyInfos,
                            packageSources: remoteRepositories.Select(s => s.PackageSource),
                            log: _nugetLogger
                        );

                        IEnumerable<SourcePackageDependencyInfo> resolvedPackages = new List<SourcePackageDependencyInfo>();
                        if (config.IgnoreDependencies)
                        {
                            resolvedPackages = packagesToInstall.Select(p => sourcePackageDependencyInfos.Single(x => p.Identity.Equals(new PackageIdentity(x.Id, x.Version))));


                            if (config.ForceDependencies)
                            {
                                //TODO Log warning here about dependencies being removed and not being reinstalled?
                                foreach (var packageToUninstall in packagesToUninstall.Where(p => !resolvedPackages.Contains(p.Identity)))
                                {
                                    try
                                    {
                                        nugetProject.DeletePackage(packageToUninstall.Identity, projectContext, CancellationToken.None).GetAwaiter().GetResult();
                                        remove_cache_for_package(config, packageToUninstall.PackageMetadata);
                                    }
                                    catch (Exception ex)
                                    {
                                        var forcedResult = packageResultsToReturn.GetOrAdd(packageToUninstall.Identity.Id, packageToUninstall);
                                        forcedResult.Messages.Add(new ResultMessage(ResultType.Note, "Removing old version"));
                                        string logMessage = "{0}:{1} {2}".format_with("Unable to remove existing package", Environment.NewLine, ex.Message);
                                        this.Log().Warn(logMessage);
                                        forcedResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                                    }
                                }
                            }

                        }
                        else
                        {
                            try
                            {
                                resolvedPackages = dependencyResolver.Resolve(resolverContext, CancellationToken.None)
                                    .Select(p => sourcePackageDependencyInfos.Single(x => PackageIdentityComparer.Default.Equals(x, p)));


                                if (!config.ForceDependencies)
                                {
                                    var identitiesToUninstall = packagesToUninstall.Select(x => x.Identity);
                                    resolvedPackages = resolvedPackages.Where(p => !(localPackagesDependencyInfos.Contains(p) && !identitiesToUninstall.Contains(p)));

                                    if (!config.AllowMultipleVersions)
                                    {
                                        // If forcing dependencies, then dependencies already added to packages to remove
                                        // If allow multiple is added, then new version of dependency will be added side by side
                                        // If neither, then package needs to be removed so it can be upgraded to the new version required by the parent

                                        packagesToUninstall.AddRange(allLocalPackages.Where(p => resolvedPackages.Select(x => x.Id).Contains(p.Name, StringComparer.OrdinalIgnoreCase)));
                                    }
                                }
                            }
                            catch (NuGetResolverConstraintException ex)
                            {
                                this.Log().Warn(ex.Message);

                                string constraintPattern = @"constraint: (?<packageId>\w+)\s\(";
                                var invalidDependencyMatch = Regex.Match(ex.Message, constraintPattern, RegexOptions.IgnoreCase);
                                var invalidDependencyName = invalidDependencyMatch.Groups["packageId"].Success ? invalidDependencyMatch.Groups["packageId"].Value : string.Empty;

                                if (!invalidDependencyMatch.Groups["packageId"].Success)
                                {
                                    string resolvePattern = @"Unable to resolve dependency \'(?<packageId>\w+)\'";
                                    var resolveDependencyMatch = Regex.Match(ex.Message, resolvePattern, RegexOptions.IgnoreCase);
                                    invalidDependencyName = resolveDependencyMatch.Groups["packageId"].Success ? resolveDependencyMatch.Groups["packageId"].Value : string.Empty;

                                    if (!resolveDependencyMatch.Success)
                                    {
                                        this.Log().Warn("Unable to match dependency resolution message, add another type");
                                    }
                                }

                                foreach (var pkgMetadata in packagesToInstall)
                                {
                                    var logMessage = "Unable to resolve dependency '{0}'".format_with(invalidDependencyName);
                                    this.Log().Error(ChocolateyLoggers.Important, logMessage);
                                    var errorResult = packageResultsToReturn.GetOrAdd(pkgMetadata.Identity.Id, new PackageResult(pkgMetadata, pathResolver.GetInstallPath(pkgMetadata.Identity)));
                                    errorResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                                }
                            }
                            catch (Exception ex)
                            {
                                this.Log().Warn("Need to add specific handling for exception type {0}".format_with(nameof(ex)));
                                this.Log().Warn(ex.Message);
                            }
                        }

                        foreach (SourcePackageDependencyInfo packageDependencyInfo in resolvedPackages)
                        {
                            var packageRemoteMetadata = packagesToInstall.FirstOrDefault(p => p.Identity.Equals(packageDependencyInfo));

                            if (packageRemoteMetadata is null)
                            {
                                packageRemoteMetadata = packageDependencyInfo
                                    .Source
                                    .GetResource<PackageMetadataResource>()
                                    .GetMetadataAsync(packageDependencyInfo, sourceCacheContext, _nugetLogger, CancellationToken.None)
                                    .GetAwaiter().GetResult();
                            }

                            var packageToUninstall = packagesToUninstall.FirstOrDefault(p => p.PackageMetadata.Id.Equals(packageDependencyInfo.Id, StringComparison.OrdinalIgnoreCase));
                            var oldPkgInfo = _packageInfoService.get_package_information(packageToUninstall.PackageMetadata);

                            if (beforeUpgradeAction != null && packageToUninstall.PackageMetadata != null)
                            {
                                beforeUpgradeAction(packageToUninstall, config);
                            }

                            try
                            {

                                remove_rollback_directory_if_exists(packageName);
                                ensure_package_files_have_compatible_attributes(config, packageToUninstall.PackageMetadata, oldPkgInfo);
                                rename_legacy_package_version(config, packageToUninstall.PackageMetadata, oldPkgInfo);
                                backup_existing_version(config, packageToUninstall.PackageMetadata, oldPkgInfo);
                                remove_shim_directors(config, packageToUninstall.PackageMetadata, pkgInfo);

                                if (packageToUninstall != null)
                                {
                                    packageToUninstall.InstallLocation = pathResolver.GetInstallPath(packageToUninstall.Identity);
                                    try
                                    {
                                        //It does not throw or return false if it fails to delete something...
                                        //var ableToDelete = nugetProject.DeletePackage(packageToUninstall.Identity, projectContext, CancellationToken.None, shouldDeleteDirectory: false).GetAwaiter().GetResult();
                                        remove_installation_files_unsafe(packageToUninstall.PackageMetadata, oldPkgInfo);
                                    }
                                    catch (Exception ex)
                                    {
                                        var forcedResult = packageResultsToReturn.GetOrAdd(packageToUninstall.Name, packageToUninstall);
                                        forcedResult.Messages.Add(new ResultMessage(ResultType.Note, "Backing up and removing old version"));
                                        string logMessage = "{0}:{1} {2}".format_with("Unable to remove existing package prior to upgrade", Environment.NewLine, ex.Message);
                                        this.Log().Warn(logMessage);
                                        //forcedResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                                        forcedResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                                        if (continueAction != null) continueAction.Invoke(forcedResult, config);

                                        continue;
                                    }
                                }

                                var downloadResource = packageDependencyInfo.Source.GetResource<DownloadResource>();

                                _fileSystem.delete_file(pathResolver.GetInstalledPackageFilePath(packageDependencyInfo));

                                using (var downloadResult = downloadResource.GetDownloadResourceResultAsync(
                                           packageDependencyInfo,
                                           new PackageDownloadContext(sourceCacheContext),
                                           config.CacheLocation,
                                           _nugetLogger, CancellationToken.None).GetAwaiter().GetResult())
                                {
                                    //TODO, do check on downloadResult

                                    nugetProject.InstallPackageAsync(
                                        packageDependencyInfo,
                                        downloadResult,
                                        projectContext,
                                        CancellationToken.None).GetAwaiter().GetResult();

                                }

                                var manifestPath = nugetProject.GetInstalledManifestFilePath(packageDependencyInfo);
                                var packageMetadata = new ChocolateyPackageMetadata(manifestPath, _fileSystem);

                                var installedPath = nugetProject.GetInstalledPath(packageDependencyInfo);

                                remove_nuget_cache_for_package(availablePackage);

                                this.Log().Info(ChocolateyLoggers.Important, "{0}{1} v{2}{3}{4}{5}".format_with(
                                    System.Environment.NewLine,
                                    packageMetadata.Id,
                                    packageMetadata.Version.to_full_string(),
                                    config.Force ? " (forced)" : string.Empty,
                                    packageRemoteMetadata.IsApproved ? " [Approved]" : string.Empty,
                                    packageRemoteMetadata.PackageTestResultStatus == "Failing" && packageRemoteMetadata.IsDownloadCacheAvailable ? " - Likely broken for FOSS users (due to download location changes)" : packageRemoteMetadata.PackageTestResultStatus == "Failing" ? " - Possibly broken" : string.Empty
                                ));

                                var upgradePackageResult = packageResultsToReturn.GetOrAdd(packageDependencyInfo.Id.to_lower(), new PackageResult(packageMetadata, packageRemoteMetadata, installedPath));
                                upgradePackageResult.ResetMetadata(packageMetadata, packageRemoteMetadata);
                                upgradePackageResult.InstallLocation = installedPath;
                                upgradePackageResult.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));

                                if (continueAction != null) continueAction.Invoke(upgradePackageResult, config);
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
                                var errorResult = packageResultsToReturn.GetOrAdd(packageDependencyInfo.Id, new PackageResult(packageDependencyInfo.Id, version.to_full_string(), null));
                                errorResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                                if (errorResult.ExitCode == 0) errorResult.ExitCode = 1;
                                if (continueAction != null) continueAction.Invoke(errorResult, config);
                            }
                        }
                    }
                }
            }

            // Reset the configuration again once we are completely done with the processing of
            // configurations, and make sure that we are removing any backup that was created
            // as part of this run.
            config.reset_config(removeBackup: true);

            return packageResultsToReturn;
        }

        public virtual ConcurrentDictionary<string, PackageResult> get_outdated(ChocolateyConfiguration config)
        {

            var remoteRepositories = NugetCommon.GetRemoteRepositories(config, _nugetLogger);
            var pathResolver = NugetCommon.GetPathResolver(config, _fileSystem);

            var outdatedPackages = new ConcurrentDictionary<string, PackageResult>();

            var sourceCacheContext = new ChocolateySourceCacheContext(config);
            var allPackages = set_package_names_if_all_is_specified(config, () => { config.IgnoreDependencies = true; });
            var packageNames = config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null().ToList();

            config.start_backup();

            foreach (var packageName in packageNames)
            {
                // We need to ensure we are using a clean configuration file
                // before we start reading it.
                config.reset_config();

                var installedPackage = allPackages.FirstOrDefault(p => string.Equals(p.Name, packageName, StringComparison.OrdinalIgnoreCase));

                var pkgInfo = _packageInfoService.get_package_information(installedPackage.PackageMetadata);
                bool isPinned = pkgInfo.IsPinned;

                // if the package is pinned and we are skipping pinned,
                // move on quickly
                if (isPinned && config.OutdatedCommand.IgnorePinned)
                {
                    string pinnedLogMessage = "{0} is pinned. Skipping pinned package.".format_with(packageName);
                    var pinnedPackageResult = outdatedPackages.GetOrAdd(packageName, new PackageResult(installedPackage.PackageMetadata, pathResolver.GetInstallPath(installedPackage.PackageMetadata.Id, installedPackage.PackageMetadata.Version)));
                    pinnedPackageResult.Messages.Add(new ResultMessage(ResultType.Debug, pinnedLogMessage));
                    pinnedPackageResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, pinnedLogMessage));

                    continue;
                }

                if (installedPackage != null && installedPackage.PackageMetadata.Version.IsPrerelease && !config.UpgradeCommand.ExcludePrerelease)
                {
                    // this is a prerelease - opt in for newer prereleases.
                    config.Prerelease = true;
                }

                var latestPackage = NugetList.find_package(packageName, config, _nugetLogger, sourceCacheContext, NugetCommon.GetRepositoryResource<PackageMetadataResource>(remoteRepositories).ToList());

                if (latestPackage == null)
                {
                    if (config.Features.IgnoreUnfoundPackagesOnUpgradeOutdated) continue;

                    string unfoundLogMessage = "{0} was not found with the source(s) listed.{1} Source(s): \"{2}\"".format_with(packageName, Environment.NewLine, config.Sources);
                    var unfoundResult = outdatedPackages.GetOrAdd(packageName, new PackageResult(installedPackage.PackageMetadata, pathResolver.GetInstallPath(installedPackage.PackageMetadata.Id, installedPackage.PackageMetadata.Version)));
                    unfoundResult.Messages.Add(new ResultMessage(ResultType.Warn, unfoundLogMessage));
                    unfoundResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, unfoundLogMessage));

                    this.Log().Warn("{0}|{1}|{1}|{2}".format_with(installedPackage.Name, installedPackage.Version, isPinned.to_string().to_lower()));
                    continue;
                }

                if (latestPackage.Identity.Version <= installedPackage.PackageMetadata.Version) continue;

                var packageResult = outdatedPackages.GetOrAdd(packageName, new PackageResult(latestPackage, pathResolver.GetInstallPath(latestPackage.Identity)));

                string logMessage = "You have {0} v{1} installed. Version {2} is available based on your source(s).{3} Source(s): \"{4}\"".format_with(installedPackage.Name, installedPackage.Version, latestPackage.Identity.Version, Environment.NewLine, config.Sources);
                packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));

                this.Log().Info("{0}|{1}|{2}|{3}".format_with(installedPackage.Name, installedPackage.Version, latestPackage.Identity.Version, isPinned.to_string().to_lower()));

                if (pkgInfo.IsSideBySide)
                {
                    var deprecationMessage = @"
{0} v{1} has been installed as a side by side installation.
Side by side installations are deprecated and is pending removal in v2.0.0".format_with(installedPackage.Name, installedPackage.Version);

                    packageResult.Messages.Add(new ResultMessage(ResultType.Warn, deprecationMessage));
                }
            }

            // Reset the configuration again once we are completely done with the processing of
            // configurations, and make sure that we are removing any backup that was created
            // as part of this run.
            config.reset_config(removeBackup: true);

            return outdatedPackages;
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
            if (!string.IsNullOrWhiteSpace(originalConfig.SourceCommand.Password)) config.SourceCommand.Password = originalConfig.SourceCommand.Password;
            if (!string.IsNullOrWhiteSpace(originalConfig.SourceCommand.Certificate)) config.SourceCommand.Certificate = originalConfig.SourceCommand.Certificate;
            if (!string.IsNullOrWhiteSpace(originalConfig.SourceCommand.CertificatePassword)) config.SourceCommand.CertificatePassword = originalConfig.SourceCommand.CertificatePassword;

            return originalConfig;
        }

        private string get_install_directory(ChocolateyConfiguration config, IPackageMetadata installedPackage)
        {

            var pathResolver = NugetCommon.GetPathResolver(config, _fileSystem);
            var installDirectory = pathResolver.GetInstallPath(new PackageIdentity(installedPackage.Id, installedPackage.Version));
            if (!_fileSystem.directory_exists(installDirectory))
            {
                var chocoPathResolver = pathResolver as ChocolateyPackagePathResolver;
                if (chocoPathResolver != null)
                {
                    chocoPathResolver.UseSideBySidePaths = !chocoPathResolver.UseSideBySidePaths;
                    installDirectory = chocoPathResolver.GetInstallPath(new PackageIdentity(installedPackage.Id, installedPackage.Version));
                }

                if (!_fileSystem.directory_exists(installDirectory)) return null;
            }

            return installDirectory;
        }

        public virtual void ensure_package_files_have_compatible_attributes(ChocolateyConfiguration config, IPackageMetadata installedPackage, ChocolateyPackageInformation pkgInfo)
        {
            var installDirectory = get_install_directory(config, installedPackage);
            if (!_fileSystem.directory_exists(installDirectory)) return;

            _filesService.ensure_compatible_file_attributes(installDirectory, config);
        }

        public virtual void rename_legacy_package_version(ChocolateyConfiguration config, IPackageMetadata installedPackage, ChocolateyPackageInformation pkgInfo)
        {
            if (pkgInfo != null && pkgInfo.IsSideBySide) return;

            var installDirectory = _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, installedPackage.Id);
            if (!_fileSystem.directory_exists(installDirectory))
            {
                // if the folder has a version on it, we need to rename the folder first.
                var pathResolver = new ChocolateyPackagePathResolver(ApplicationParameters.PackagesLocation, _fileSystem, useSideBySidePaths: true);
                installDirectory = pathResolver.GetInstallPath(new PackageIdentity(installedPackage.Id, installedPackage.Version));
                if (_fileSystem.directory_exists(installDirectory))
                {
                    FaultTolerance.try_catch_with_logging_exception(
                        () => _fileSystem.move_directory(installDirectory, _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, installedPackage.Id)),
                        "Error during old package rename");
                }
            }

        }

        public virtual void backup_existing_version(ChocolateyConfiguration config, IPackageMetadata installedPackage, ChocolateyPackageInformation packageInfo)
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
                var filesToDelete = new List<string> { "chocolateyinstall", "chocolateyuninstall", "chocolateybeforemodify" };
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
        private void remove_shim_directors(ChocolateyConfiguration config, IPackageMetadata installedPackage, ChocolateyPackageInformation pkgInfo)
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

        private void remove_cache_for_package(ChocolateyConfiguration config, IPackageMetadata installedPackage)
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
        private void remove_nuget_cache_for_package(IPackageSearchMetadata installedPackage)
        {
            var localAppData = Environment.GetEnvironmentVariable("LocalAppData");
            if (string.IsNullOrWhiteSpace(localAppData)) return;

            FaultTolerance.try_catch_with_logging_exception(
                () =>
                {
                    var nugetCachedFile = _fileSystem.combine_paths(localAppData, "NuGet", "Cache", "{0}.{1}.nupkg".format_with(installedPackage.Identity.Id, installedPackage.Identity.Version.to_string()));
                    if (_fileSystem.file_exists(nugetCachedFile))
                    {
                        _fileSystem.delete_file(nugetCachedFile);
                    }
                },
                "Unable to removed cached NuGet package file");
        }

        public void uninstall_noop(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
        {
            var results = uninstall_run(config, continueAction, performAction: false);
            foreach (var packageResult in results.or_empty_list_if_null())
            {
                var package = packageResult.Value.PackageMetadata;
                if (package != null) this.Log().Warn("Would have uninstalled {0} v{1}.".format_with(package.Id, package.Version.to_full_string()));
            }
        }

        public ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeUninstallAction = null)
        {
            return uninstall_run(config, continueAction, performAction: true, beforeUninstallAction: beforeUninstallAction);
        }

        public virtual ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, bool performAction, Action<PackageResult, ChocolateyConfiguration> beforeUninstallAction = null)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.PackagesLocation);
            var packageResultsToReturn = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            NuGetVersion version = config.Version != null ? NuGetVersion.Parse(config.Version) : null;

            var sourceCacheContext = new ChocolateySourceCacheContext(config);
            var localRepositorySource = NugetCommon.GetLocalRepository();
            var projectContext = new ChocolateyNuGetProjectContext(config, _nugetLogger);
            var allLocalPackages = get_all_installed_packages(config);

            // if we are uninstalling a package and not forcing dependencies,
            // look to see if the user is missing the actual package they meant
            // to uninstall.
            if (!config.ForceDependencies)
            {
                // if you find an install of an .install / .portable / .commandline, allow adding it to the list
                var installedPackages = allLocalPackages.Select(p => p.Name).ToList().@join(ApplicationParameters.PackageNamesSeparator);
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
                            var actualPackageResult = packageResultsToReturn.GetOrAdd(actualPackageName, new PackageResult(actualPackageName, null, null));
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

            config.start_backup();

            foreach (string packageName in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null())
            {
                // We need to ensure we are using a clean configuration file
                // before we start reading it.
                config.reset_config();

                IList<PackageResult> installedPackageVersions = new List<PackageResult>();
                if (string.IsNullOrWhiteSpace(config.Version))
                {
                    installedPackageVersions = allLocalPackages.Where(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase)).OrderBy((p) => p.Version).ToList();
                }
                else
                {
                    var nugetVersion = NuGetVersion.Parse(config.Version);
                    installedPackageVersions = allLocalPackages.Where(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase) && p.PackageMetadata.Version.Equals(nugetVersion)).ToList();
                }

                if (installedPackageVersions.Count == 0)
                {
                    string logMessage = "{0} is not installed. Cannot uninstall a non-existent package.".format_with(packageName);
                    var missingResult = packageResultsToReturn.GetOrAdd(packageName, new PackageResult(packageName, null, null));
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
                            choices.Add(installedVersion.Version);
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
                            PackageResult pkg = installedPackageVersions.FirstOrDefault((p) => p.Version.is_equal_to(selection));
                            packageVersionsToRemove.Add(pkg);
                            if (config.RegularOutput) this.Log().Info(() => "You selected {0} v{1}".format_with(pkg.Name, pkg.Version));
                        }
                    }
                }

                foreach (var installedPackage in packageVersionsToRemove)
                {
                    //Need to get this again for dependency resolution
                    allLocalPackages = get_all_installed_packages(config);
                    var packagesToUninstall = new HashSet<PackageResult>();
                    var localPackagesDependencyInfos = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
                    var pathResolver = NugetCommon.GetPathResolver(config, _fileSystem);
                    var nugetProject = new FolderNuGetProject(ApplicationParameters.PackagesLocation, pathResolver, NuGetFramework.AnyFramework);


                    var pkgInfo = _packageInfoService.get_package_information(installedPackage.PackageMetadata);
                    if (pkgInfo != null && pkgInfo.IsPinned)
                    {
                        string logMessage = "{0} is pinned. Skipping pinned package.".format_with(packageName);
                        var pinnedResult = packageResultsToReturn.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                        pinnedResult.Messages.Add(new ResultMessage(ResultType.Warn, logMessage));
                        pinnedResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                        if (config.RegularOutput) this.Log().Warn(ChocolateyLoggers.Important, logMessage);
                        continue;
                    }

                    if (performAction)
                    {
                        var allPackagesIdentities = allLocalPackages.Where(p => !p.Identity.Equals(installedPackage)).Select(p => p.SearchMetadata.Identity).ToList();
                        localPackagesDependencyInfos.AddRange(allLocalPackages
                            .Select(
                                p => new SourcePackageDependencyInfo(
                                    p.SearchMetadata.Identity,
                                    p.PackageMetadata.DependencyGroups.SelectMany(x => x.Packages).ToList(),
                                    true,
                                    localRepositorySource,
                                    null,
                                    null)));
                        var uninstallationContext = new UninstallationContext(removeDependencies: true, forceRemove: config.ForceDependencies);
                        var resolvedPackages = UninstallResolver.GetPackagesToBeUninstalled(installedPackage.Identity, localPackagesDependencyInfos, allPackagesIdentities, uninstallationContext);
                        packagesToUninstall.AddRange(allLocalPackages.Where(p => resolvedPackages.Contains(p.Identity)));

                        foreach (var packageToUninstall in packagesToUninstall)
                        {
                            if (beforeUninstallAction != null)
                            {
                                // guessing this is not added so that it doesn't fail the action if an error is recorded?
                                //var currentPackageResult = packageUninstalls.GetOrAdd(packageName, new PackageResult(packageVersion, get_install_directory(config, packageVersion)));
                                beforeUninstallAction(packageToUninstall, config);
                            }

                            var uninstallPkgInfo = _packageInfoService.get_package_information(packageToUninstall.PackageMetadata);

                            try
                            {
                                ensure_package_files_have_compatible_attributes(config, packageToUninstall.PackageMetadata, uninstallPkgInfo);
                                rename_legacy_package_version(config, packageToUninstall.PackageMetadata, uninstallPkgInfo);
                                remove_rollback_directory_if_exists(packageName);
                                backup_existing_version(config, packageToUninstall.PackageMetadata, uninstallPkgInfo);

                                var packageResult = packageResultsToReturn.GetOrAdd(packageToUninstall.Name.to_lower() + "." + packageToUninstall.Version.to_string(), packageToUninstall);
                                packageResult.InstallLocation = packageToUninstall.InstallLocation;
                                string logMessage = "{0}{1} v{2}{3}".format_with(Environment.NewLine, packageToUninstall.Name, packageToUninstall.Version.to_string(), config.Force ? " (forced)" : string.Empty);
                                packageResult.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));

                                if (continueAction != null) continueAction.Invoke(packageResult, config);

                                if (packageToUninstall != null)
                                {
                                    packageToUninstall.InstallLocation = pathResolver.GetInstallPath(packageToUninstall.Identity);
                                    try
                                    {
                                        //It does not throw or return false if it fails to delete something...
                                        //var ableToDelete = nugetProject.DeletePackage(packageToUninstall.Identity, projectContext, CancellationToken.None, shouldDeleteDirectory: false).GetAwaiter().GetResult();
                                        remove_installation_files_unsafe(packageToUninstall.PackageMetadata, pkgInfo);
                                    }
                                    catch (Exception ex)
                                    {
                                        string errorlogMessage = "{0}:{1} {2}".format_with("Unable to remove existing package", Environment.NewLine, ex.Message);
                                        this.Log().Warn(logMessage);
                                        packageResult.Messages.Add(new ResultMessage(ResultType.Error, errorlogMessage));
                                        if (continueAction != null) continueAction.Invoke(packageResult, config);
                                        continue;
                                    }
                                }

                                this.Log().Info(ChocolateyLoggers.Important, " {0} has been successfully uninstalled.".format_with(packageToUninstall.Name));

                                ensure_nupkg_is_removed(packageToUninstall.PackageMetadata, uninstallPkgInfo);
                                remove_installation_files(packageToUninstall.PackageMetadata, uninstallPkgInfo);
                            }
                            catch (Exception ex)
                            {
                                var logMessage = "{0} not uninstalled. An error occurred during uninstall:{1} {2}".format_with(packageName, Environment.NewLine, ex.Message);
                                this.Log().Error(ChocolateyLoggers.Important, logMessage);
                                var result = packageResultsToReturn.GetOrAdd(packageToUninstall.Name.to_lower() + "." + packageToUninstall.Version.to_string(), new PackageResult(packageToUninstall.PackageMetadata, pathResolver.GetInstallPath(packageToUninstall.PackageMetadata.Id, packageToUninstall.PackageMetadata.Version)));
                                result.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                                if (result.ExitCode == 0) result.ExitCode = 1;
                                if (config.Features.StopOnFirstPackageFailure)
                                {
                                    throw new ApplicationException("Stopping further execution as {0} has failed uninstallation".format_with(packageToUninstall.Name.to_lower()));
                                }
                                // do not call continueAction - will result in multiple passes
                            }
                        }
                    }
                    else
                    {
                        // continue action won't be found b/c we are not actually uninstalling (this is noop)
                        var result = packageResultsToReturn.GetOrAdd(installedPackage.Name.to_lower() + "." + installedPackage.Version.to_string(), new PackageResult(installedPackage.PackageMetadata, pathResolver.GetInstallPath(installedPackage.PackageMetadata.Id, installedPackage.PackageMetadata.Version)));
                        if (continueAction != null) continueAction.Invoke(result, config);
                    }
                }
            }

            // Reset the configuration again once we are completely done with the processing of
            // configurations, and make sure that we are removing any backup that was created
            // as part of this run.
            config.reset_config(removeBackup: true);

            return packageResultsToReturn;
        }

        /// <summary>
        /// NuGet will happily report a package has been uninstalled, even if it doesn't always remove the nupkg.
        /// Ensure that the package is deleted or throw an error.
        /// </summary>
        /// <param name="removedPackage">The installed package.</param>
        /// <param name="pkgInfo">The package information.</param>
        private void ensure_nupkg_is_removed(IPackageMetadata removedPackage, ChocolateyPackageInformation pkgInfo)
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

        public virtual void remove_installation_files_unsafe(IPackageMetadata removedPackage, ChocolateyPackageInformation pkgInfo)
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

                    var filesystemFileChecksum = _filesService.get_package_file(file).Checksum;

                    if (filesystemFileChecksum == ApplicationParameters.HashProviderFileLocked)
                    {
                        throw new IOException("File {0} is locked".format_with(file));
                    }

                    if (fileSnapshot.Checksum == filesystemFileChecksum)
                    {
                        if (!_fileSystem.file_exists(file)) continue;

                        _fileSystem.delete_file(file);
                    }
                }
            }

            if (_fileSystem.directory_exists(installDir) && !_fileSystem.get_files(installDir, "*.*", SearchOption.AllDirectories).or_empty_list_if_null().Any())
            {
                _fileSystem.delete_directory_if_exists(installDir, recursive: true);
            }
        }

        public virtual void remove_installation_files(IPackageMetadata removedPackage, ChocolateyPackageInformation pkgInfo)
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

        public IEnumerable<PackageResult> get_all_installed_packages(ChocolateyConfiguration config)
        {
            //todo: #2579 move to deep copy for get all installed
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
            var version = config.Version;
            config.Version = string.Empty;

            var installedPackages = list_run(config).ToList();

            config.ListCommand.IncludeVersionOverrides = includeVersionOverrides;
            config.QuietOutput = quiet;
            config.Input = input;
            config.PackageNames = packageNames;
            config.Noop = noop;
            config.Prerelease = pre;
            config.Sources = sources;
            config.Version = version;

            return installedPackages;
        }

        private IEnumerable<PackageResult> set_package_names_if_all_is_specified(ChocolateyConfiguration config, Action customAction)
        {
            var allPackages = get_all_installed_packages(config);
            if (config.PackageNames.is_equal_to(ApplicationParameters.AllPackages))
            {
                var packagesToUpdate= allPackages.Select(p => p.Name).ToList();

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

                        allPackages = allPackages.Where(p => !packagesToSkip.Contains(p.Name, StringComparer.OrdinalIgnoreCase));

                        this.Log().Info(() => "These packages will not be upgraded because they were specified in the 'except' list: {0}".format_with(string.Join(",", packagesToSkip)));
                    }
                }

                config.PackageNames = packagesToUpdate.@join(ApplicationParameters.PackageNamesSeparator);

                if (customAction != null) customAction.Invoke();
            }

            return allPackages;
        }
    }
}
