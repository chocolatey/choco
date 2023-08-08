// Copyright © 2017 - 2023 Chocolatey Software, Inc
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
    using NuGet.Common;
    using NuGet.Configuration;
    using NuGet.PackageManagement;
    using NuGet.Packaging;
    using NuGet.Packaging.Core;
    using NuGet.Protocol;
    using NuGet.ProjectManagement;
    using NuGet.Protocol.Core.Types;
    using NuGet.Resolver;
    using NuGet.Versioning;
    using chocolatey.infrastructure.services;

    //todo: #2575 - this monolith is too large. Refactor once test coverage is up.

    public class NugetService : INugetService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _nugetLogger;
        private readonly IChocolateyPackageInformationService _packageInfoService;
        private readonly IFilesService _filesService;
        private readonly IRuleService _ruleService;
        //private readonly PackageDownloader _packageDownloader;
        private readonly Lazy<IDateTime> _datetime = new Lazy<IDateTime>(() => new DateTime());

        private IDateTime DateTime
        {
            get { return _datetime.Value; }
        }

        internal const string InstallWithFilePathDeprecationMessage = @"
The ability to specify a direct path to a .nuspec or .nupkg file for installation
is deprecated and will be removed in v2.0.0. Instead of using the full path
to a .nupkg file, use the --source option to specify the containing directory,
and ensure any packages have been packed into a .nupkg file before attempting to
install them.
";

        /// <summary>
        ///   Initializes a new instance of the <see cref="NugetService" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="nugetLogger">The nuget logger</param>
        /// <param name="packageInfoService">Package information service</param>
        /// <param name="filesService">The files service</param>
        /// <param name="ruleService">The rule service</param>
        public NugetService(
            IFileSystem fileSystem,
            ILogger nugetLogger,
            IChocolateyPackageInformationService packageInfoService,
            IFilesService filesService,
            IRuleService ruleService)
        {
            _fileSystem = fileSystem;
            _nugetLogger = nugetLogger;
            _packageInfoService = packageInfoService;
            _filesService = filesService;
            _ruleService = ruleService;
        }

        public string SourceType
        {
            get { return SourceTypes.Normal; }
        }

        public void EnsureSourceAppInstalled(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> ensureAction)
        {
            // nothing to do. Nuget.Core is already part of Chocolatey
        }

        public virtual int Count(ChocolateyConfiguration config)
        {
            if (config.ListCommand.LocalOnly)
            {
                config.Sources = ApplicationParameters.PackagesLocation;
                config.Prerelease = true;

                if (!_fileSystem.DirectoryExists(ApplicationParameters.PackagesLocation))
                {
                    return 0;
                }
            }

            int? pageValue = config.ListCommand.Page;
            try
            {
                var sourceCacheContext = new ChocolateySourceCacheContext(config);
                return NugetList.GetCount(config, _nugetLogger, _fileSystem, sourceCacheContext);
            }
            finally
            {
                config.ListCommand.Page = pageValue;
            }
        }

        public virtual void ListDryRun(ChocolateyConfiguration config)
        {
            this.Log().Info("{0} would have searched for '{1}' against the following source(s) :\"{2}\"".FormatWith(
                ApplicationParameters.Name,
                config.Input,
                config.Sources
                                ));
        }

        public virtual IEnumerable<PackageResult> List(ChocolateyConfiguration config)
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

                if (!_fileSystem.DirectoryExists(ApplicationParameters.PackagesLocation))
                {
                    yield break;
                }
            }

            if ((config.ListCommand.ApprovedOnly || config.ListCommand.DownloadCacheAvailable || config.ListCommand.NotBroken) && config.RegularOutput)
            {
                // This warning has been added here, to provide context to the user, and a follow up issue

                // has been added here: https://github.com/chocolatey/choco/issues/3139 to address the actual
                // issue that causes this warning to be required.
                this.Log().Warn(@"
Starting vith Chocolatey CLI v2.0.0, changes have been made to the
`choco search` command which means that filtering of packages using the
`--approved-only`, `--download-cache`, and `--not-broken` options are
now performed within Chocolatey CLI. Previously, this filtering would
have been performed on the Chocolatey Community Repository. As a result,
it is possible that incomplete package lists are returned from a command
that uses these options.");
            }

            if (config.RegularOutput) this.Log().Debug(() => "Running list with the following filter = '{0}'".FormatWith(config.Input));
            if (config.RegularOutput) this.Log().Debug(ChocolateyLoggers.Verbose, () => "--- Start of List ---");
            foreach (var pkg in NugetList.GetPackages(config, _nugetLogger, _fileSystem))
            {
                var package = pkg; // for lamda access

                ChocolateyPackageMetadata packageLocalMetadata;
                string packageInstallLocation = null;
                if (package.PackagePath != null && !string.IsNullOrWhiteSpace(package.PackagePath))
                {
                    packageLocalMetadata = new ChocolateyPackageMetadata(package.PackagePath, _fileSystem);
                    packageInstallLocation = _fileSystem.GetDirectoryName(package.PackagePath);
                }
                else
                {
                    packageLocalMetadata = null;
                }

                if (config.ListCommand.LocalOnly && packageLocalMetadata != null)
                {
                    var packageInfo = _packageInfoService.Get(packageLocalMetadata);
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
                        this.Log().Info(logger, () => "{0}{1}".FormatWith(package.Identity.Id, config.ListCommand.IdOnly ? string.Empty : " {0}{1}{2}{3}".FormatWith(
                                packageLocalMetadata != null ? packageLocalMetadata.Version.ToFullStringChecked() : package.Identity.Version.ToFullStringChecked(),
                                package.IsApproved ? " [Approved]" : string.Empty,
                                package.IsDownloadCacheAvailable ? " Downloads cached for licensed users" : string.Empty,
                                package.PackageTestResultStatus == "Failing" && package.IsDownloadCacheAvailable ? " - Possibly broken for FOSS users (due to original download location changes by vendor)" : package.PackageTestResultStatus == "Failing" ? " - Possibly broken" : string.Empty
                            ))
                        );

                        if (config.Verbose && !config.ListCommand.IdOnly) this.Log().Info(() =>
                            @" Title: {0} | Published: {1}{2}{3}
 Number of Downloads: {4} | Downloads for this version: {5}
 Package url{6}
 Chocolatey Package Source: {7}{8}
 Tags: {9}
 Software Site: {10}
 Software License: {11}{12}{13}{14}{15}{16}
 Description: {17}{18}
".FormatWith(
                                package.Title.EscapeCurlyBraces(),
                                package.Published.GetValueOrDefault().UtcDateTime.ToShortDateString(),
                                package.IsApproved ? "{0} Package approved {1} on {2}.".FormatWith(
                                        Environment.NewLine,
                                        string.IsNullOrWhiteSpace(package.PackageReviewer) ? "as a trusted package" : "by " + package.PackageReviewer,
                                        package.PackageApprovedDate.GetValueOrDefault().ToString("MMM dd yyyy HH:mm:ss")
                                    ) : string.Empty,
                                string.IsNullOrWhiteSpace(package.PackageTestResultStatus) || package.PackageTestResultStatus.IsEqualTo("unknown") ? string.Empty : "{0} Package testing status: {1} on {2}.".FormatWith(
                                        Environment.NewLine,
                                        package.PackageTestResultStatus,
                                        package.PackageValidationResultDate.GetValueOrDefault().ToString("MMM dd yyyy HH:mm:ss")
                                    ),
                                (package.DownloadCount == null || package.DownloadCount <= 0)  ? "n/a" : package.DownloadCount.ToStringSafe(),
                                (package.VersionDownloadCount == null || package.VersionDownloadCount <= 0) ? "n/a" : package.VersionDownloadCount.ToStringSafe(),
                                package.PackageDetailsUrl == null || string.IsNullOrWhiteSpace(package.PackageDetailsUrl.AbsoluteUri) ? string.Empty : " " + package.PackageDetailsUrl.AbsoluteUri,
                                !string.IsNullOrWhiteSpace(package.PackageSourceUrl.ToStringSafe())
                                    ? package.PackageSourceUrl.ToStringSafe()
                                    : "n/a",
                                string.IsNullOrWhiteSpace(package.PackageHash) ? string.Empty : "{0} Package Checksum: '{1}' ({2})".FormatWith(
                                        Environment.NewLine,
                                        package.PackageHash,
                                        package.PackageHashAlgorithm
                                        ),
                                package.Tags.TrimSafe().EscapeCurlyBraces(),
                                package.ProjectUrl != null ? package.ProjectUrl.ToStringSafe() : "n/a",
                                package.LicenseUrl != null && !string.IsNullOrWhiteSpace(package.LicenseUrl.ToStringSafe()) ? package.LicenseUrl.ToStringSafe() : "n/a",
                                !string.IsNullOrWhiteSpace(package.ProjectSourceUrl.ToStringSafe()) ? "{0} Software Source: {1}".FormatWith(Environment.NewLine, package.ProjectSourceUrl.ToStringSafe()) : string.Empty,
                                !string.IsNullOrWhiteSpace(package.DocsUrl.ToStringSafe()) ? "{0} Documentation: {1}".FormatWith(Environment.NewLine, package.DocsUrl.ToStringSafe()) : string.Empty,
                                !string.IsNullOrWhiteSpace(package.MailingListUrl.ToStringSafe()) ? "{0} Mailing List: {1}".FormatWith(Environment.NewLine, package.MailingListUrl.ToStringSafe()) : string.Empty,
                                !string.IsNullOrWhiteSpace(package.BugTrackerUrl.ToStringSafe()) ? "{0} Issues: {1}".FormatWith(Environment.NewLine, package.BugTrackerUrl.ToStringSafe()) : string.Empty,
                                package.Summary != null && !string.IsNullOrWhiteSpace(package.Summary.ToStringSafe()) ? "\r\n Summary: {0}".FormatWith(package.Summary.EscapeCurlyBraces().ToStringSafe()) : string.Empty,
                                package.Description.EscapeCurlyBraces().Replace("\n    ", "\n").Replace("\n", "\n  "),
                                !string.IsNullOrWhiteSpace(package.ReleaseNotes.ToStringSafe()) ? "{0} Release Notes: {1}".FormatWith(Environment.NewLine, package.ReleaseNotes.EscapeCurlyBraces().Replace("\n    ", "\n").Replace("\n", "\n  ")) : string.Empty
                            ));


                    }
                    else
                    {
                        this.Log().Info(logger, () => "{0}{1}".FormatWith(package.Identity.Id, config.ListCommand.IdOnly ? string.Empty : "|{0}".FormatWith(package.Identity.Version.ToFullStringChecked())));
                    }
                }
                else
                {
                    this.Log().Debug(() => "{0}{1}".FormatWith(package.Identity.Id, config.ListCommand.IdOnly ? string.Empty : " {0}".FormatWith(package.Identity.Version.ToFullStringChecked())));
                }
                count++;

                if (packageLocalMetadata is null)
                {
                    yield return new PackageResult(package, null, config.Sources);
                }
                else
                {
                    yield return new PackageResult(packageLocalMetadata, package, config.ListCommand.LocalOnly ? packageInstallLocation : null, config.Sources);
                }
            }

            if (config.RegularOutput) this.Log().Debug(ChocolateyLoggers.Verbose, () => "--- End of List ---");
            if (config.RegularOutput && !config.QuietOutput)
            {
                this.Log().Warn(() => @"{0} packages {1}.".FormatWith(count, config.ListCommand.LocalOnly ? "installed" : "found"));
            }

            config.Sources = sources;
            config.Prerelease = prerelease;
            config.ListCommand.IncludeVersionOverrides = includeVersionOverrides;

            if (!config.ListCommand.Page.HasValue && !config.ListCommand.LocalOnly)
            {
                var logType = config.RegularOutput ? ChocolateyLoggers.Important : ChocolateyLoggers.LogFileOnly;

                if (NugetList.ThresholdHit)
                {
                    this.Log().Warn(logType, "The threshold of {0:N0} packages per source has been met. Please refine your search, or specify a page to find any more results.".FormatWith(NugetList.LastPackageLimitUsed));
                }
                else if (NugetList.LowerThresholdHit)
                {
                    this.Log().Warn(logType, "Over {0:N0} packages was found per source, there may be more packages available that was filtered out. Please refine your search, or specify a page to check for more packages.".FormatWith(NugetList.LastPackageLimitUsed * 0.9));
                }
            }
        }

        public void PackDryRun(ChocolateyConfiguration config)
        {
            this.Log().Info("{0} would have searched for a nuspec file in \"{1}\" and attempted to compile it.".FormatWith(
                ApplicationParameters.Name,
                config.OutputDirectory ?? _fileSystem.GetCurrentDirectory()
                                ));
        }

        public virtual string GetPackageFileOrThrow(ChocolateyConfiguration config, string extension)
        {
            Func<IFileSystem, string> getLocalFiles = (fileSystem) =>
                {
                    var filesFound = fileSystem.GetFiles(fileSystem.GetCurrentDirectory(), "*" + extension).ToList().OrEmpty();
                    Ensure.That(() => filesFound)
                          .Meets((files) => files.Count() == 1,
                                 (name, value) => { throw new FileNotFoundException("No {0} files (or more than 1) were found to build in '{1}'. Please specify the {0} file or try in a different directory.".FormatWith(extension, _fileSystem.GetCurrentDirectory())); });

                    return filesFound.FirstOrDefault();
                };

            string filePath = !string.IsNullOrWhiteSpace(config.Input) ? config.Input : getLocalFiles.Invoke(_fileSystem);
            Ensure.That(() => filePath).Meets((file) => _fileSystem.GetFileExtension(file).IsEqualTo(extension) && _fileSystem.FileExists(file),
                                              (name, value) => { throw new ArgumentException("File specified is either not found or not a {0} file. '{1}'".FormatWith(extension, value)); });

            return filePath;
        }

        public virtual void Pack(ChocolateyConfiguration config)
        {
            var nuspecFilePath = GetPackageFileOrThrow(config, PackagingConstants.ManifestExtension);
            ValidateNuspec(nuspecFilePath, config);

            var nuspecDirectory = _fileSystem.GetFullPath(_fileSystem.GetDirectoryName(nuspecFilePath));
            if (string.IsNullOrWhiteSpace(nuspecDirectory)) nuspecDirectory = _fileSystem.GetCurrentDirectory();

            // Use case-insensitive properties like "nuget pack".
            var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Add any other properties passed to the pack command overriding any present.
            foreach (var property in config.PackCommand.Properties)
            {
                this.Log().Debug(() => "Setting property '{0}': {1}".FormatWith(
                    property.Key,
                    property.Value));

                properties[property.Key] = property.Value;
            }

            // Set the version property if the flag is set
            if (!string.IsNullOrWhiteSpace(config.Version))
            {
                this.Log().Debug(() => "Setting property 'version': {0}".FormatWith(
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

            string outputFile = builder.Id + "." + builder.Version.ToNormalizedStringChecked() + NuGetConstants.PackageExtension;
            string outputFolder = config.OutputDirectory ?? _fileSystem.GetCurrentDirectory();
            string outputPath = _fileSystem.CombinePaths(outputFolder, outputFile);

            config.Sources = outputFolder;

            this.Log().Info(config.QuietOutput ? ChocolateyLoggers.LogFileOnly : ChocolateyLoggers.Normal, () => "Attempting to build package from '{0}'.".FormatWith(_fileSystem.GetFileName(nuspecFilePath)));
            _fileSystem.EnsureDirectoryExists(outputFolder);

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

            this.Log().Info(config.QuietOutput ? ChocolateyLoggers.LogFileOnly : ChocolateyLoggers.Important, () => "Successfully created package '{0}'".FormatWith(outputPath));
        }

        public void PushDryRun(ChocolateyConfiguration config)
        {
            string nupkgFilePath = GetPackageFileOrThrow(config, NuGetConstants.PackageExtension);
            this.Log().Info(() => "Would have attempted to push '{0}' to source '{1}'.".FormatWith(_fileSystem.GetFileName(nupkgFilePath), config.Sources));
        }

        public virtual void Push(ChocolateyConfiguration config)
        {
            string nupkgFilePath = GetPackageFileOrThrow(config, NuGetConstants.PackageExtension);
            string nupkgFileName = _fileSystem.GetFileName(nupkgFilePath);
            if (config.RegularOutput) this.Log().Info(() => "Attempting to push {0} to {1}".FormatWith(nupkgFileName, config.Sources));

            var sourceCacheContext = new ChocolateySourceCacheContext(config);
            NugetPush.PushPackage(config, _fileSystem.GetFullPath(nupkgFilePath), _nugetLogger, nupkgFileName, _fileSystem, sourceCacheContext);

            if (config.RegularOutput && (config.Sources.IsEqualTo(ApplicationParameters.ChocolateyCommunityFeedPushSource) || config.Sources.IsEqualTo(ApplicationParameters.ChocolateyCommunityFeedPushSourceOld)))
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

        public void InstallDryRun(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
        {
            //todo: #2576 noop should see if packages are already installed and adjust message, amiright?!

            this.Log().Info("{0} would have used NuGet to install packages (if they are not already installed):{1}{2}".FormatWith(
                ApplicationParameters.Name,
                Environment.NewLine,
                config.PackageNames
                                ));

            var tempInstallsLocation = _fileSystem.CombinePaths(_fileSystem.GetTempPath(), ApplicationParameters.Name, "TempInstalls_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_ffff"));
            _fileSystem.EnsureDirectoryExists(tempInstallsLocation);

            var installLocation = ApplicationParameters.PackagesLocation;
            ApplicationParameters.PackagesLocation = tempInstallsLocation;

            Install(config, continueAction);

            _fileSystem.DeleteDirectory(tempInstallsLocation, recursive: true);
            ApplicationParameters.PackagesLocation = installLocation;
        }

        public virtual ConcurrentDictionary<string, PackageResult> Install(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
        {
            return Install(config, continueAction, beforeModifyAction: null);
        }

        public virtual ConcurrentDictionary<string, PackageResult> Install(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeModifyAction)
        {
            _fileSystem.EnsureDirectoryExists(ApplicationParameters.PackagesLocation);
            var packageResultsToReturn = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            //todo: #23 handle all

            NuGetVersion version = !string.IsNullOrWhiteSpace(config.Version) ? NuGetVersion.Parse(config.Version) : null;
            if (config.Force) config.AllowDowngrade = true;

            var sourceCacheContext = new ChocolateySourceCacheContext(config);
            var remoteRepositories = NugetCommon.GetRemoteRepositories(config, _nugetLogger, _fileSystem);
            var remoteEndpoints = NugetCommon.GetRepositoryResources(remoteRepositories, sourceCacheContext);
            var localRepositorySource = NugetCommon.GetLocalRepository();
            var pathResolver = NugetCommon.GetPathResolver(_fileSystem);
            var nugetProject = new FolderNuGetProject(ApplicationParameters.PackagesLocation, pathResolver, NuGetFramework.AnyFramework);
            var projectContext = new ChocolateyNuGetProjectContext(config, _nugetLogger);

            IList<string> packageNames = config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).OrEmpty().ToList();
            if (packageNames.Count == 1)
            {
                var packageName = packageNames.DefaultIfEmpty(string.Empty).FirstOrDefault();
                if (packageName.EndsWith(NuGetConstants.PackageExtension) || packageName.EndsWith(PackagingConstants.ManifestExtension))
                {
                    this.Log().Warn(ChocolateyLoggers.Important, "DEPRECATION WARNING");
                    this.Log().Warn(InstallWithFilePathDeprecationMessage);

                    this.Log().Debug("Updating source and package name to handle *.nupkg or *.nuspec file.");
                    packageNames.Clear();

                    config.Sources = _fileSystem.GetDirectoryName(_fileSystem.GetFullPath(packageName));

                    if (packageName.EndsWith(PackagingConstants.ManifestExtension))
                    {
                        packageNames.Add(_fileSystem.GetFilenameWithoutExtension(packageName));

                        this.Log().Debug("Building nuspec file prior to install.");
                        config.Input = packageName;
                        // build package
                        Pack(config);
                    }
                    else
                    {
                        using (var packageFile = new PackageArchiveReader(_fileSystem.GetFullPath(packageName)))
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
            if (config.Sources.ToStringSafe().EndsWith(NuGetConstants.PackageExtension))
            {
                config.Sources = _fileSystem.GetDirectoryName(_fileSystem.GetFullPath(config.Sources));
            }

            config.CreateBackup();

            foreach (string packageName in packageNames.OrEmpty())
            {
                // We need to ensure we are using a clean configuration file
                // before we start reading it.
                config.RevertChanges();

                var allLocalPackages = GetInstalledPackages(config).ToList();
                var packagesToInstall = new List<IPackageSearchMetadata>();
                var packagesToUninstall = new HashSet<PackageResult>();
                var sourcePackageDependencyInfos = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
                var localPackageToRemoveDependencyInfos = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);

                var installedPackage = allLocalPackages.FirstOrDefault(p => p.Name.IsEqualTo(packageName));

                if (Platform.GetPlatform() != PlatformType.Windows && !packageName.EndsWith(".template"))
                {
                    string logMessage = "{0} is not a supported package on non-Windows systems.{1}Only template packages are currently supported.".FormatWith(packageName, Environment.NewLine);
                    this.Log().Warn(ChocolateyLoggers.Important, logMessage);
                }

                if (installedPackage != null && (version == null || version == installedPackage.PackageMetadata.Version) && !config.Force)
                {
                    string logMessage = "{0} v{1} already installed.{2} Use --force to reinstall, specify a version to install, or try upgrade.".FormatWith(installedPackage.Name, installedPackage.Version, Environment.NewLine);
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
 Please use upgrade if you meant to upgrade to a new version.".FormatWith(installedPackage.Name, installedPackage.Version));

                    //This is set to ensure the same package version is reinstalled
                    latestPackageVersion = installedPackage.PackageMetadata.Version;
                }

                if (installedPackage != null && version != null && version < installedPackage.PackageMetadata.Version && !config.AllowDowngrade)
                {
                    string logMessage = "A newer version of {0} (v{1}) is already installed.{2} Use --allow-downgrade or --force to attempt to install older versions.".FormatWith(installedPackage.Name, installedPackage.Version, Environment.NewLine);
                    var nullResult = packageResultsToReturn.GetOrAdd(packageName, installedPackage);
                    nullResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                    this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    continue;
                }

                if (latestPackageVersion is null && version != null)
                {
                    latestPackageVersion = version;
                }

                var availablePackage = NugetList.FindPackage(packageName, config, _nugetLogger, sourceCacheContext, remoteEndpoints, latestPackageVersion);

                if (availablePackage == null)
                {
                    var logMessage = @"{0} not installed. The package was not found with the source(s) listed.
 Source(s): '{1}'
 NOTE: When you specify explicit sources, it overrides default sources.
If the package version is a prerelease and you didn't specify `--pre`,
 the package may not be found.{2}{3}".FormatWith(packageName, config.Sources, string.IsNullOrWhiteSpace(config.Version)
                            ? String.Empty
                            : @"
Version was specified as '{0}'. It is possible that version
 does not exist for '{1}' at the source specified.".FormatWith(config.Version, packageName),
                        @"
Please see https://docs.chocolatey.org/en-us/troubleshooting for more
 assistance.");
                    this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    var noPkgResult = packageResultsToReturn.GetOrAdd(packageName, new PackageResult(packageName, version.ToFullStringChecked(), null));
                    noPkgResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                    continue;
                }

                NugetCommon.GetPackageDependencies(availablePackage.Identity, NuGetFramework.AnyFramework, sourceCacheContext, _nugetLogger, remoteEndpoints, sourcePackageDependencyInfos, new HashSet<PackageDependency>(), config).GetAwaiter().GetResult();

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
                    // If we're forcing dependencies, we only need to know which dependencies are installed locally
                    .Where(p => config.ForceDependencies
                        ? targetIdsToInstall.Contains(p.Name, StringComparer.OrdinalIgnoreCase)
                        : !targetIdsToInstall.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                    .Select(
                        p => new SourcePackageDependencyInfo(
                            p.SearchMetadata.Identity,
                            p.PackageMetadata.DependencyGroups.SelectMany(x => x.Packages).ToList(),
                            true,
                            localRepositorySource,
                            null,
                            null));

                var removedSources = RemovePinnedSourceDependencies(sourcePackageDependencyInfos, allLocalPackages);
                sourcePackageDependencyInfos.AddRange(localPackagesDependencyInfos);

                if (removedSources.Count > 0 && version == null)
                {
                    RemoveInvalidDependenciesAndParents(availablePackage, removedSources, sourcePackageDependencyInfos, localPackagesDependencyInfos);
                }

                var dependencyResolver = new PackageResolver();

                var allPackagesIdentities = Enumerable.Empty<PackageIdentity>();

                if (availablePackage.DependencySets.Any() || localPackagesDependencyInfos.Any(d => d.Dependencies.Any(dd => dd.Id == availablePackage.Identity.Id)))
                {
                    allPackagesIdentities = allLocalPackages
                        // We exclude any installed package that does have a dependency that is missing,
                        // except if that dependency is one of the targets the user requested.
                        // If we do not exclude such packages, we will get a resolving exception later.
                        .Where(p => IsDependentOnTargetPackages(p, targetIdsToInstall) || !HasMissingDependency(p, allLocalPackages))
                        .Select(p => p.SearchMetadata.Identity)
                        // If we're forcing dependencies, we only need to know which dependencies are installed locally, not the entire list of packages
                        .Where(p => config.ForceDependencies
                            ? sourcePackageDependencyInfos.Any(s => s.Id == p.Id)
                            : !targetIdsToInstall.Contains(p.Id, StringComparer.OrdinalIgnoreCase)).ToList();
                }

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
                                RemovePackageFromCache(config, packageToUninstall.PackageMetadata);
                            }
                            catch (Exception ex)
                            {
                                var forcedResult = packageResultsToReturn.GetOrAdd(packageToUninstall.Identity.Id, packageToUninstall);
                                forcedResult.Messages.Add(new ResultMessage(ResultType.Note, "Removing old version"));
                                string logMessage = "{0}:{1} {2}".FormatWith("Unable to remove existing package", Environment.NewLine, ex.Message);
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

                            // If forcing dependencies, then dependencies already added to packages to remove.
                            // If not forcing dependencies, then package needs to be removed so it can be upgraded to the new version required by the parent
                            packagesToUninstall.AddRange(allLocalPackages.Where(p => resolvedPackages.Select(x => x.Id).Contains(p.Name, StringComparer.OrdinalIgnoreCase)));
                        }
                    }
                    catch (NuGetResolverConstraintException ex)
                    {
                        var logMessage = GetDependencyResolutionErrorMessage(ex);
                        this.Log().Error(ChocolateyLoggers.Important, logMessage);

                        foreach (var pkgMetadata in packagesToInstall)
                        {
                            var errorResult = packageResultsToReturn.GetOrAdd(pkgMetadata.Identity.Id, new PackageResult(pkgMetadata, pathResolver.GetInstallPath(pkgMetadata.Identity)));
                            errorResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Log().Warn("Need to add specific handling for exception type {0}".FormatWith(ex.GetType().Name));
                        this.Log().Warn(ex.Message);
                    }
                }

                foreach (SourcePackageDependencyInfo packageDependencyInfo in resolvedPackages)
                {
                    var packageRemoteMetadata = packagesToInstall.FirstOrDefault(p => p.Identity.Equals(packageDependencyInfo));

                    if (packageRemoteMetadata is null)
                    {
                        var endpoint = NuGetEndpointResources.GetResourcesBySource(packageDependencyInfo.Source, sourceCacheContext);

                        packageRemoteMetadata = endpoint.PackageMetadataResource
                            .GetMetadataAsync(packageDependencyInfo, sourceCacheContext, _nugetLogger, CancellationToken.None)
                            .GetAwaiter().GetResult();
                    }

                    bool shouldAddForcedResultMessage = false;

                    var packageToUninstall = packagesToUninstall.FirstOrDefault(p => p.PackageMetadata.Id.Equals(packageDependencyInfo.Id, StringComparison.OrdinalIgnoreCase));
                    if (packageToUninstall != null)
                    {
                        shouldAddForcedResultMessage = true;
                        BackupAndRunBeforeModify(packageToUninstall, config, beforeModifyAction);
                        packageToUninstall.InstallLocation = pathResolver.GetInstallPath(packageToUninstall.Identity);
                        try
                        {
                            // This deletes satellite files and stuff
                            //But it does not throw or return false if it fails to delete something...
                            var ableToDelete = nugetProject.DeletePackage(packageToUninstall.Identity, projectContext, CancellationToken.None, shouldDeleteDirectory: false).GetAwaiter().GetResult();
                            //So removing directly manually so as to throw if needed.
                            _fileSystem.DeleteDirectoryChecked(packageToUninstall.InstallLocation, true, true, true);
                            RemovePackageFromCache(config, packageToUninstall.PackageMetadata);
                        }
                        catch (Exception ex)
                        {
                            var forcedResult = packageResultsToReturn.GetOrAdd(packageToUninstall.Name, packageToUninstall);
                            forcedResult.Messages.Add(new ResultMessage(ResultType.Note, "Backing up and removing old version"));
                            string logMessage = "{0}:{1} {2}".FormatWith("Unable to remove existing package prior to forced reinstall", Environment.NewLine, ex.Message);
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
                        var endpoint = NuGetEndpointResources.GetResourcesBySource(packageDependencyInfo.Source, sourceCacheContext);
                        var downloadResource = endpoint.DownloadResource;

                        _fileSystem.DeleteFile(pathResolver.GetInstalledPackageFilePath(packageDependencyInfo));

                        ChocolateyProgressInfo.ShouldDisplayDownloadProgress = config.Features.ShowDownloadProgress;

                        using (var downloadResult = downloadResource.GetDownloadResourceResultAsync(
                                   packageDependencyInfo,
                                   new PackageDownloadContext(sourceCacheContext),
                                   NuGetEnvironment.GetFolderPath(NuGetFolderPath.Temp),
                                   _nugetLogger, CancellationToken.None).GetAwaiter().GetResult())
                        {
                            //TODO, do check on downloadResult

                            nugetProject.InstallPackageAsync(
                                packageDependencyInfo,
                                downloadResult,
                                projectContext,
                                CancellationToken.None).GetAwaiter().GetResult();

                        }

                        var installedPath = nugetProject.GetInstalledPath(packageDependencyInfo);
                        NormalizeNuspecCasing(packageRemoteMetadata, installedPath);

                        RemovePackageFromNugetCache(packageRemoteMetadata);

                        var manifestPath = nugetProject.GetInstalledManifestFilePath(packageDependencyInfo);
                        var packageMetadata = new ChocolateyPackageMetadata(manifestPath, _fileSystem);

                        this.Log().Info(ChocolateyLoggers.Important, "{0}{1} v{2}{3}{4}{5}".FormatWith(
                            System.Environment.NewLine,
                            packageMetadata.Id,
                            packageMetadata.Version.ToFullStringChecked(),
                            config.Force ? " (forced)" : string.Empty,
                            packageRemoteMetadata.IsApproved ? " [Approved]" : string.Empty,
                            packageRemoteMetadata.PackageTestResultStatus == "Failing" && packageRemoteMetadata.IsDownloadCacheAvailable ? " - Likely broken for FOSS users (due to download location changes)" : packageRemoteMetadata.PackageTestResultStatus == "Failing" ? " - Possibly broken" : string.Empty
                        ));

                        var packageResult = packageResultsToReturn.GetOrAdd(packageDependencyInfo.Id.ToLowerSafe(), new PackageResult(packageMetadata, packageRemoteMetadata, installedPath));
                        if (shouldAddForcedResultMessage) packageResult.Messages.Add(new ResultMessage(ResultType.Note, "Backing up and removing old version"));
                        packageResult.InstallLocation = installedPath;
                        packageResult.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));

                        var elementsList = _ruleService.ValidateRules(manifestPath)
                            .Where(r => r.Severity == infrastructure.rules.RuleType.Error && !string.IsNullOrEmpty(r.Id))
                            .WhereUnsupportedOrDeprecated()
                            .Select(r => "{0}: {1}".FormatWith(r.Id, r.Message))
                            .ToList();

                        if (elementsList.Count > 0)
                        {
                            var message = "Issues found with nuspec elements\r\n" + elementsList.Join("\r\n");
                            packageResult.Messages.Add(new ResultMessage(ResultType.Warn, message));
                        }

                        if (continueAction != null) continueAction.Invoke(packageResult, config);

                    }
                    catch (Exception ex)
                    {
                        var message = ex.Message;
                        var webException = ex as System.Net.WebException;
                        if (webException != null)
                        {
                            var response = webException.Response as HttpWebResponse;
                            if (response != null && !string.IsNullOrWhiteSpace(response.StatusDescription)) message += " {0}".FormatWith(response.StatusDescription);
                        }

                        var logMessage = "{0} not installed. An error occurred during installation:{1} {2}".FormatWith(packageDependencyInfo.Id, Environment.NewLine, message);
                        this.Log().Error(ChocolateyLoggers.Important, logMessage);
                        var errorResult = packageResultsToReturn.GetOrAdd(packageDependencyInfo.Id, new PackageResult(packageDependencyInfo.Id, version.ToFullStringChecked(), null));
                        errorResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                        if (errorResult.ExitCode == 0) errorResult.ExitCode = 1;
                        if (continueAction != null) continueAction.Invoke(errorResult, config);
                    }
                }
            }

            // Reset the configuration again once we are completely done with the processing of
            // configurations, and make sure that we are removing any backup that was created
            // as part of this run.
            config.RevertChanges(removeBackup: true);

            return packageResultsToReturn;
        }

        protected virtual string GetDependencyResolutionErrorMessage(NuGetResolverConstraintException exception)
        {
            if (exception.Message.StartsWith("Unable to resolve dependency '"))
            {
                return exception.Message;
            }

            var errorMessagePatterns = new string[]
            {
                @"constraint: (?<packageId>\w+)\s\(",
                @"Circular dependency detected '(?<packageId>\w+) "
            };

            string invalidDependencyName = null;
            foreach (var pattern in errorMessagePatterns)
            {
                var invalidDependencyMatch = Regex.Match(exception.Message, pattern, RegexOptions.IgnoreCase);
                if (invalidDependencyMatch.Groups["packageId"].Success)
                {
                    invalidDependencyName = invalidDependencyMatch.Groups["packageId"].Value;
                    break;
                }
            }

            if (invalidDependencyName == null)
            {
                this.Log().Debug("Could not find invalid dependency name in dependency resolution message, add another match pattern to handle this case");
                return $"Unable to resolve dependency: {exception.Message}";
            }

            return $"Unable to resolve dependency '{invalidDependencyName}': {exception.Message}";
        }

        public virtual void EnsureBackupDirectoryRemoved(string packageName)
        {
            var rollbackDirectory = _fileSystem.GetFullPath(_fileSystem.CombinePaths(ApplicationParameters.PackageBackupLocation, packageName));
            if (!_fileSystem.DirectoryExists(rollbackDirectory))
            {
                //search for folder
                var possibleRollbacks = _fileSystem.GetDirectories(ApplicationParameters.PackageBackupLocation, packageName + "*");
                if (possibleRollbacks != null && possibleRollbacks.Count() != 0)
                {
                    rollbackDirectory = possibleRollbacks.OrderByDescending(p => p).DefaultIfEmpty(string.Empty).FirstOrDefault();
                }

                rollbackDirectory = _fileSystem.GetFullPath(rollbackDirectory);
            }

            if (string.IsNullOrWhiteSpace(rollbackDirectory) || !_fileSystem.DirectoryExists(rollbackDirectory)) return;
            if (!rollbackDirectory.StartsWith(ApplicationParameters.PackageBackupLocation) || rollbackDirectory.IsEqualTo(ApplicationParameters.PackageBackupLocation)) return;

            FaultTolerance.TryCatchWithLoggingException(
                () => _fileSystem.DeleteDirectoryChecked(rollbackDirectory, recursive: true),
                "Attempted to remove '{0}' but had an error:".FormatWith(rollbackDirectory),
                logWarningInsteadOfError: true);
        }

        public ConcurrentDictionary<string, PackageResult> UpgradeDryRun(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
        {
            config.Force = false;
            return Upgrade(config, continueAction, performAction: false);
        }

        public ConcurrentDictionary<string, PackageResult> Upgrade(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeUpgradeAction = null)
        {
            return Upgrade(config, continueAction, performAction: true, beforeUpgradeAction: beforeUpgradeAction);
        }

        public virtual ConcurrentDictionary<string, PackageResult> Upgrade(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, bool performAction, Action<PackageResult, ChocolateyConfiguration> beforeUpgradeAction = null)
        {
            _fileSystem.EnsureDirectoryExists(ApplicationParameters.PackagesLocation);
            var packageResultsToReturn = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            NuGetVersion version = !string.IsNullOrWhiteSpace(config.Version) ? NuGetVersion.Parse(config.Version) : null;

            if (config.Force) config.AllowDowngrade = true;

            var sourceCacheContext = new ChocolateySourceCacheContext(config);
            var remoteRepositories = NugetCommon.GetRemoteRepositories(config, _nugetLogger, _fileSystem);
            var remoteEndpoints = NugetCommon.GetRepositoryResources(remoteRepositories, sourceCacheContext);
            var localRepositorySource = NugetCommon.GetLocalRepository();
            var projectContext = new ChocolateyNuGetProjectContext(config, _nugetLogger);

            var configIgnoreDependencies = config.IgnoreDependencies;
            var allLocalPackages = SetPackageNamesIfAllSpecified(config, () => { config.IgnoreDependencies = true; }).ToList();
            config.IgnoreDependencies = configIgnoreDependencies;
            var localPackageListValid = true;

            config.CreateBackup();

            foreach (string packageName in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).OrEmpty())
            {
                // We need to ensure we are using a clean configuration file
                // before we start reading it.
                config.RevertChanges();

                if (!localPackageListValid)
                {
                    allLocalPackages = GetInstalledPackages(config).ToList();
                    localPackageListValid = true;
                }

                var installedPackage = allLocalPackages.FirstOrDefault(p => p.Name.IsEqualTo(packageName));
                var packagesToInstall = new List<IPackageSearchMetadata>();
                var packagesToUninstall = new HashSet<PackageResult>();
                var sourcePackageDependencyInfos = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
                var localPackageToRemoveDependencyInfos = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
                var dependencyResources = remoteEndpoints.DependencyInfoResources();
                var sourceDependencyCache = new HashSet<PackageDependency>();

                if (installedPackage == null)
                {
                    if (config.UpgradeCommand.FailOnNotInstalled)
                    {
                        string failLogMessage = "{0} is not installed. Cannot upgrade a non-existent package.".FormatWith(packageName);
                        var result = packageResultsToReturn.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                        result.Messages.Add(new ResultMessage(ResultType.Error, failLogMessage));
                        if (config.RegularOutput) this.Log().Error(ChocolateyLoggers.Important, failLogMessage);

                        continue;
                    }

                    if (config.Features.SkipPackageUpgradesWhenNotInstalled)
                    {
                        string warnLogMessage = "{0} is not installed and skip non-installed option selected. Skipping...".FormatWith(packageName);
                        var result = packageResultsToReturn.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                        result.Messages.Add(new ResultMessage(ResultType.Warn, warnLogMessage));
                        if (config.RegularOutput) this.Log().Warn(ChocolateyLoggers.Important, warnLogMessage);

                        continue;
                    }

                    string logMessage = @"{0} is not installed. Installing...".FormatWith(packageName);
                    localPackageListValid = false;

                    if (config.RegularOutput) this.Log().Warn(ChocolateyLoggers.Important, logMessage);

                    var packageNames = config.PackageNames;
                    config.PackageNames = packageName;
                    if (config.Noop)
                    {
                        InstallDryRun(config, continueAction);
                    }
                    else
                    {
                        var installResults = Install(config, continueAction, beforeUpgradeAction);
                        foreach (var result in installResults)
                        {
                            packageResultsToReturn.GetOrAdd(result.Key, result.Value);
                        }
                    }

                    config.PackageNames = packageNames;
                    continue;
                }

                var pkgInfo = _packageInfoService.Get(installedPackage.PackageMetadata);
                bool isPinned = pkgInfo != null && pkgInfo.IsPinned;

                if (isPinned && config.OutdatedCommand.IgnorePinned)
                {
                    continue;
                }

                SetConfigFromRememberedArguments(config, pkgInfo);
                var pathResolver = NugetCommon.GetPathResolver(_fileSystem);
                var nugetProject = new FolderNuGetProject(ApplicationParameters.PackagesLocation, pathResolver, NuGetFramework.AnyFramework);

                if (version != null && version < installedPackage.PackageMetadata.Version && !config.AllowDowngrade)
                {
                    string logMessage = "A newer version of {0} (v{1}) is already installed.{2} Use --allow-downgrade or --force to attempt to upgrade to older versions.".FormatWith(installedPackage.PackageMetadata.Id, installedPackage.Version, Environment.NewLine);
                    var nullResult = packageResultsToReturn.GetOrAdd(packageName, new PackageResult(installedPackage.PackageMetadata, pathResolver.GetInstallPath(installedPackage.PackageMetadata.Id)));
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
                var availablePackage = NugetList.FindPackage(packageName, config, _nugetLogger, sourceCacheContext, remoteEndpoints, version);

                config.Prerelease = originalPrerelease;

                if (availablePackage == null)
                {
                    if (config.Features.IgnoreUnfoundPackagesOnUpgradeOutdated) continue;

                    string logMessage = "{0} was not found with the source(s) listed.{1} If you specified a particular version and are receiving this message, it is possible that the package name exists but the version does not.{1} Version: \"{2}\"; Source(s): \"{3}\"".FormatWith(packageName, Environment.NewLine, config.Version, config.Sources);
                    var unfoundResult = packageResultsToReturn.GetOrAdd(packageName, new PackageResult(packageName, version.ToFullStringChecked(), null));

                    if (config.UpgradeCommand.FailOnUnfound)
                    {
                        unfoundResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                        if (config.RegularOutput) this.Log().Error(ChocolateyLoggers.Important, "{0}{1}".FormatWith(Environment.NewLine, logMessage));
                    }
                    else
                    {
                        unfoundResult.Messages.Add(new ResultMessage(ResultType.Warn, "{0} was not found with the source(s) listed.".FormatWith(packageName)));
                        unfoundResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                        if (config.RegularOutput)
                        {
                            this.Log().Warn(ChocolateyLoggers.Important, "{0}{1}".FormatWith(Environment.NewLine, logMessage));
                        }
                        else
                        {
                            //last one is whether this package is pinned or not
                            this.Log().Info("{0}|{1}|{1}|{2}".FormatWith(installedPackage.PackageMetadata.Id, installedPackage.Version, isPinned.ToStringSafe().ToLowerSafe()));
                        }
                    }

                    continue;
                }

                var packageResult = packageResultsToReturn.GetOrAdd(packageName, new PackageResult(availablePackage, pathResolver.GetInstallPath(availablePackage.Identity)));
                if (installedPackage.PackageMetadata.Version > availablePackage.Identity.Version && (!config.AllowDowngrade || (config.AllowDowngrade && version == null)))
                {
                    string logMessage = "{0} v{1} is newer than the most recent.".FormatWith(installedPackage.PackageMetadata.Id, installedPackage.Version);
                    packageResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));

                    if (!config.UpgradeCommand.NotifyOnlyAvailableUpgrades)
                    {
                        if (config.RegularOutput)
                        {
                            this.Log().Info(ChocolateyLoggers.Important, logMessage);
                        }
                        else
                        {
                            this.Log().Info("{0}|{1}|{1}|{2}".FormatWith(installedPackage.PackageMetadata.Id, installedPackage.Version, isPinned.ToStringSafe().ToLowerSafe()));
                        }
                    }

                    continue;
                }

                if (installedPackage.PackageMetadata.Version == availablePackage.Identity.Version)
                {
                    string logMessage = "{0} v{1} is the latest version available based on your source(s).".FormatWith(installedPackage.PackageMetadata.Id, installedPackage.Version);

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
                                this.Log().Info("{0}|{1}|{2}|{3}".FormatWith(installedPackage.PackageMetadata.Id, installedPackage.Version, availablePackage.Identity.Version.ToNormalizedStringChecked(), isPinned.ToStringSafe().ToLowerSafe()));
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
                        string logMessage = "You have {0} v{1} installed. Version {2} is available based on your source(s).".FormatWith(installedPackage.PackageMetadata.Id, installedPackage.Version, availablePackage.Identity.Version);
                        packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));

                        if (config.RegularOutput)
                        {
                            this.Log().Warn("{0}{1}".FormatWith(Environment.NewLine, logMessage));
                        }
                        else
                        {
                            this.Log().Info("{0}|{1}|{2}|{3}".FormatWith(installedPackage.PackageMetadata.Id, installedPackage.Version, availablePackage.Identity.Version, isPinned.ToStringSafe().ToLowerSafe()));
                        }
                    }

                    if (isPinned)
                    {
                        string logMessage = "{0} is pinned. Skipping pinned package.".FormatWith(packageName);
                        packageResult.Messages.Add(new ResultMessage(ResultType.Warn, logMessage));
                        packageResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                        if (config.RegularOutput) this.Log().Warn(ChocolateyLoggers.Important, logMessage);

                        continue;
                    }

                    if (performAction)
                    {
                        localPackageListValid = false;

                        NugetCommon.GetPackageDependencies(availablePackage.Identity, NuGetFramework.AnyFramework, sourceCacheContext, _nugetLogger, remoteEndpoints, sourcePackageDependencyInfos, sourceDependencyCache, config).GetAwaiter().GetResult();


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

                        // For an initial attempt at finding a package resolution solution, check for all parent packages (i.e. locally installed packages
                        // that take a dependency on the package that is currently being upgraded) and find the depdendencies associated with these packages.
                        // NOTE: All the latest availble package version, as well as the specifically requested package version (if applicable) will be
                        // searched for.  If this don't provide enough information to obtain a solution, then a follow up query in the catch block for this
                        // section of the code will be completed.
                        var parentInfos = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
                        NugetCommon.GetPackageParents(availablePackage.Identity.Id, parentInfos, localPackagesDependencyInfos).GetAwaiter().GetResult();
                        foreach (var parentPackage in parentInfos)
                        {
                            if (version != null)
                            {
                                var requestedPackageDependency = NugetList.FindPackage(parentPackage.Id, config, _nugetLogger, sourceCacheContext, remoteEndpoints, version);

                                if (requestedPackageDependency != null)
                                {
                                    NugetCommon.GetPackageDependencies(requestedPackageDependency.Identity, NuGetFramework.AnyFramework, sourceCacheContext, _nugetLogger, remoteEndpoints, sourcePackageDependencyInfos, sourceDependencyCache, config).GetAwaiter().GetResult();
                                }
                            }

                            var configPrerelease = config.Prerelease;
                            config.Prerelease = parentPackage.Version.IsPrerelease;

                            var availablePackageDependency = NugetList.FindPackage(parentPackage.Id, config, _nugetLogger, sourceCacheContext, remoteEndpoints);

                            config.Prerelease = configPrerelease;

                            if (availablePackageDependency != null)
                            {
                                NugetCommon.GetPackageDependencies(availablePackageDependency.Identity, NuGetFramework.AnyFramework, sourceCacheContext, _nugetLogger, remoteEndpoints, sourcePackageDependencyInfos, sourceDependencyCache, config).GetAwaiter().GetResult();
                            }
                            else
                            {
                                this.Log().Warn("Unable to find the parent package '{0}'.", parentPackage.Id);
                            }
                        }

                        var removedSources = RemovePinnedSourceDependencies(sourcePackageDependencyInfos, allLocalPackages);

                        if (version != null || removedSources.Count == 0)
                        {
                            sourcePackageDependencyInfos.RemoveWhere(p => p.Id.Equals(availablePackage.Identity.Id, StringComparison.OrdinalIgnoreCase) && !p.Version.Equals(availablePackage.Identity.Version));
                        }

                        sourcePackageDependencyInfos.AddRange(localPackagesDependencyInfos);

                        if (removedSources.Count > 0 && version == null)
                        {
                            RemoveInvalidDependenciesAndParents(availablePackage, removedSources, sourcePackageDependencyInfos, localPackagesDependencyInfos);
                        }

                        var dependencyResolver = new PackageResolver();

                        var targetIdsToInstall = packagesToInstall.Select(p => p.Identity.Id);

                        var allPackagesIdentities = Enumerable.Empty<PackageIdentity>();

                        if (availablePackage.DependencySets.Any() || localPackagesDependencyInfos.Any(d => d.Dependencies.Any(dd => dd.Id == availablePackage.Identity.Id)))
                        {
                            allPackagesIdentities = allLocalPackages
                                // We exclude any installed package that does have a dependency that is missing,
                                // except if that dependency is one of the targets the user requested.
                                // If we do not exclude such packages, we will get a resolving exception later.
                                .Where(p => IsDependentOnTargetPackages(p, targetIdsToInstall) || !HasMissingDependency(p, allLocalPackages))
                                .Select(p => p.SearchMetadata.Identity)
                                .Where(x => !targetIdsToInstall.Contains(x.Id, StringComparer.OrdinalIgnoreCase)).ToList();
                        }

                        //var allPackagesIdentities = allLocalPackages.Select(p => p.SearchMetadata.Identity).ToList();
                        var allPackagesReferences = allPackagesIdentities.Select(p => new PackageReference(p, NuGetFramework.AnyFramework));

                        IEnumerable<SourcePackageDependencyInfo> resolvedPackages = new List<SourcePackageDependencyInfo>();
                        if (config.IgnoreDependencies)
                        {
                            resolvedPackages = packagesToInstall.Select(p => sourcePackageDependencyInfos.SingleOrDefault(x => p.Identity.Equals(new PackageIdentity(x.Id, x.Version))));

                            if (config.ForceDependencies)
                            {
                                //TODO Log warning here about dependencies being removed and not being reinstalled?
                                foreach (var packageToUninstall in packagesToUninstall.Where(p => !resolvedPackages.Contains(p.Identity)))
                                {
                                    try
                                    {
                                        nugetProject.DeletePackage(packageToUninstall.Identity, projectContext, CancellationToken.None).GetAwaiter().GetResult();
                                        RemovePackageFromCache(config, packageToUninstall.PackageMetadata);
                                    }
                                    catch (Exception ex)
                                    {
                                        var forcedResult = packageResultsToReturn.GetOrAdd(packageToUninstall.Identity.Id, packageToUninstall);
                                        forcedResult.Messages.Add(new ResultMessage(ResultType.Note, "Removing old version"));
                                        string logMessage = "{0}:{1} {2}".FormatWith("Unable to remove existing package", Environment.NewLine, ex.Message);
                                        this.Log().Warn(logMessage);
                                        forcedResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                                    }
                                }
                            }
                        }
                        else
                        {
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

                            try
                            {
                                resolvedPackages = dependencyResolver.Resolve(resolverContext, CancellationToken.None)
                                    .Select(p => sourcePackageDependencyInfos.SingleOrDefault(x => PackageIdentityComparer.Default.Equals(x, p)));

                                if (!config.ForceDependencies)
                                {
                                    var identitiesToUninstall = packagesToUninstall.Select(x => x.Identity);
                                    resolvedPackages = resolvedPackages.Where(p => !(localPackagesDependencyInfos.Contains(p) && !identitiesToUninstall.Contains(p)));

                                    // If forcing dependencies, then dependencies already added to packages to remove.
                                    // If not forcing dependencies, then package needs to be removed so it can be upgraded to the new version required by the parent
                                    packagesToUninstall.AddRange(allLocalPackages.Where(p => resolvedPackages.Select(x => x.Id).Contains(p.Name, StringComparer.OrdinalIgnoreCase)));
                                }
                            }
                            catch (NuGetResolverConstraintException)
                            {
                                this.Log().Warn("Re-attempting package dependency resolution using additional available package information...");

                                try
                                {
                                    // If for some reason, it hasn't been possible to find a solution from the resolverContext, it could be that
                                    // we haven't provided enough information about the available package versions in the sourcePackageDependencyInfos
                                    // object.  If we get here, assume that this is the case and re-attempt the upgrade, by pulling in ALL the
                                    // dependency information, rather than only the latest package version, and specified package version.

                                    // NOTE: There is duplication of work here, compared to what is done above, but further refactoring of this
                                    // entire method would need to be done in order to make it more usable/maintable going forward. In the
                                    // interim, the duplication is "acceptable" as it is hoped that the need to find ALL package dependencies
                                    // will be the edge case, and not the rule.
                                    foreach (var parentPackage in parentInfos)
                                    {
                                        foreach (var packageVersion in NugetList.FindAllPackageVersions(parentPackage.Id, config, _nugetLogger, sourceCacheContext, remoteEndpoints))
                                        {
                                            NugetCommon.GetPackageDependencies(packageVersion.Identity, NuGetFramework.AnyFramework, sourceCacheContext, _nugetLogger, remoteEndpoints, sourcePackageDependencyInfos, sourceDependencyCache, config).GetAwaiter().GetResult();
                                        }
                                    }

                                    resolverContext = new PackageResolverContext(
                                        dependencyBehavior: DependencyBehavior.Highest,
                                        targetIds: targetIdsToInstall,
                                        requiredPackageIds: allPackagesIdentities.Select(p => p.Id),
                                        packagesConfig: allPackagesReferences,
                                        preferredVersions: allPackagesIdentities,
                                        availablePackages: sourcePackageDependencyInfos,
                                        packageSources: remoteRepositories.Select(s => s.PackageSource),
                                        log: _nugetLogger
                                    );

                                    resolvedPackages = dependencyResolver.Resolve(resolverContext, CancellationToken.None)
                                        .Select(p => sourcePackageDependencyInfos.SingleOrDefault(x => PackageIdentityComparer.Default.Equals(x, p)));

                                    if (!config.ForceDependencies)
                                    {
                                        var identitiesToUninstall = packagesToUninstall.Select(x => x.Identity);
                                        resolvedPackages = resolvedPackages.Where(p => !(localPackagesDependencyInfos.Contains(p) && !identitiesToUninstall.Contains(p)));

                                        // If forcing dependencies, then dependencies already added to packages to remove.
                                        // If not forcing dependencies, then package needs to be removed so it can be upgraded to the new version required by the parent
                                        packagesToUninstall.AddRange(allLocalPackages.Where(p => resolvedPackages.Select(x => x.Id).Contains(p.Name, StringComparer.OrdinalIgnoreCase)));
                                    }
                                }
                                catch (NuGetResolverConstraintException nestedEx)
                                {
                                    // If we get here, both the inital attempt to resolve a solution didn't work, as well as a second
                                    // attempt using all available package versions didn't work, so this time around we hard fail, and
                                    // provide information to the user about the conflicts for the package resolution.
                                    var logMessage = GetDependencyResolutionErrorMessage(nestedEx);
                                    this.Log().Error(ChocolateyLoggers.Important, logMessage);

                                    foreach (var pkgMetadata in packagesToInstall)
                                    {
                                        var errorResult = packageResultsToReturn.GetOrAdd(pkgMetadata.Identity.Id, new PackageResult(pkgMetadata, pathResolver.GetInstallPath(pkgMetadata.Identity)));
                                        errorResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                this.Log().Warn("Need to add specific handling for exception type {0}".FormatWith(ex.GetType().Name));
                                this.Log().Warn(ex.Message);
                            }
                        }

                        foreach (SourcePackageDependencyInfo packageDependencyInfo in resolvedPackages)
                        {
                            if (packageDependencyInfo is null)
                            {
                                ResultMessage message = null;

                                if (config.IgnoreDependencies)
                                {
                                    message = new ResultMessage(ResultType.Error, "Unable to resolve dependency chain. This may be caused by a parent package depending on this package, try specifying a specific version to use or don't ignore any dependencies!");
                                }
                                else
                                {
                                    message = new ResultMessage(ResultType.Error, "An unknown failure happened during the resolving of the dependency chain!");
                                }

                                var existingPackage = packageResultsToReturn.GetOrAdd(packageName, (key) =>
                                {
                                    // In general, this value should already be set. But just in case
                                    // it isn't we create a new package result.
                                    return new PackageResult(availablePackage, string.Empty);
                                });
                                existingPackage.Messages.Add(message);

                                break;
                            }

                            var packageRemoteMetadata = packagesToInstall.FirstOrDefault(p => p.Identity.Equals(packageDependencyInfo));

                            if (packageRemoteMetadata is null)
                            {
                                var endpoint = NuGetEndpointResources.GetResourcesBySource(packageDependencyInfo.Source, sourceCacheContext);

                                packageRemoteMetadata = endpoint.PackageMetadataResource
                                    .GetMetadataAsync(packageDependencyInfo, sourceCacheContext, _nugetLogger, CancellationToken.None)
                                    .GetAwaiter().GetResult();
                            }

                            var packageToUninstall = packagesToUninstall.FirstOrDefault(p => p.PackageMetadata.Id.Equals(packageDependencyInfo.Id, StringComparison.OrdinalIgnoreCase));

                            try
                            {
                                if (packageToUninstall != null)
                                {
                                    var oldPkgInfo = _packageInfoService.Get(packageToUninstall.PackageMetadata);

                                    BackupAndRunBeforeModify(packageToUninstall, oldPkgInfo, config, beforeUpgradeAction);

                                    packageToUninstall.InstallLocation = pathResolver.GetInstallPath(packageToUninstall.Identity);
                                    try
                                    {
                                        //It does not throw or return false if it fails to delete something...
                                        //var ableToDelete = nugetProject.DeletePackage(packageToUninstall.Identity, projectContext, CancellationToken.None, shouldDeleteDirectory: false).GetAwaiter().GetResult();
                                        RemoveInstallationFilesUnsafe(packageToUninstall.PackageMetadata, oldPkgInfo);
                                    }
                                    catch (Exception ex)
                                    {
                                        var forcedResult = packageResultsToReturn.GetOrAdd(packageToUninstall.Name, packageToUninstall);
                                        forcedResult.Messages.Add(new ResultMessage(ResultType.Note, "Backing up and removing old version"));
                                        string logMessage = "{0}:{1} {2}".FormatWith("Unable to remove existing package prior to upgrade", Environment.NewLine, ex.Message);
                                        this.Log().Warn(logMessage);
                                        //forcedResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                                        forcedResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                                        if (continueAction != null) continueAction.Invoke(forcedResult, config);

                                        continue;
                                    }

                                    // We need to remove the nupkg file explicitly in case it has
                                    // been modified.
                                    EnsureNupkgRemoved(oldPkgInfo.Package);
                                }

                                var endpoint = NuGetEndpointResources.GetResourcesBySource(packageDependencyInfo.Source, sourceCacheContext);
                                var downloadResource = endpoint.DownloadResource;

                                _fileSystem.DeleteFile(pathResolver.GetInstalledPackageFilePath(packageDependencyInfo));

                                ChocolateyProgressInfo.ShouldDisplayDownloadProgress = config.Features.ShowDownloadProgress;

                                using (var downloadResult = downloadResource.GetDownloadResourceResultAsync(
                                           packageDependencyInfo,
                                           new PackageDownloadContext(sourceCacheContext),
                                           NuGetEnvironment.GetFolderPath(NuGetFolderPath.Temp),
                                           _nugetLogger, CancellationToken.None).GetAwaiter().GetResult())
                                {
                                    //TODO, do check on downloadResult

                                    nugetProject.InstallPackageAsync(
                                        packageDependencyInfo,
                                        downloadResult,
                                        projectContext,
                                        CancellationToken.None).GetAwaiter().GetResult();

                                }

                                var installedPath = nugetProject.GetInstalledPath(packageDependencyInfo);

                                NormalizeNuspecCasing(packageRemoteMetadata, installedPath);

                                var manifestPath = nugetProject.GetInstalledManifestFilePath(packageDependencyInfo);
                                var packageMetadata = new ChocolateyPackageMetadata(manifestPath, _fileSystem);

                                RemovePackageFromNugetCache(packageRemoteMetadata);

                                this.Log().Info(ChocolateyLoggers.Important, "{0}{1} v{2}{3}{4}{5}".FormatWith(
                                    System.Environment.NewLine,
                                    packageMetadata.Id,
                                    packageMetadata.Version.ToFullStringChecked(),
                                    config.Force ? " (forced)" : string.Empty,
                                    packageRemoteMetadata.IsApproved ? " [Approved]" : string.Empty,
                                    packageRemoteMetadata.PackageTestResultStatus == "Failing" && packageRemoteMetadata.IsDownloadCacheAvailable ? " - Likely broken for FOSS users (due to download location changes)" : packageRemoteMetadata.PackageTestResultStatus == "Failing" ? " - Possibly broken" : string.Empty
                                ));

                                var upgradePackageResult = packageResultsToReturn.GetOrAdd(packageDependencyInfo.Id.ToLowerSafe(), new PackageResult(packageMetadata, packageRemoteMetadata, installedPath));
                                upgradePackageResult.ResetMetadata(packageMetadata, packageRemoteMetadata);
                                upgradePackageResult.InstallLocation = installedPath;

                                upgradePackageResult.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));

                                var elementsList = _ruleService.ValidateRules(manifestPath)
                                    .Where(r => r.Severity == infrastructure.rules.RuleType.Error && !string.IsNullOrEmpty(r.Id))
                                    .WhereUnsupportedOrDeprecated()
                                    .Select(r => "{0}: {1}".FormatWith(r.Id, r.Message))
                                    .ToList();

                                if (elementsList.Count > 0)
                                {
                                    var message = "Issues found with nuspec elements\r\n" + elementsList.Join("\r\n");
                                    packageResult.Messages.Add(new ResultMessage(ResultType.Warn, message));
                                }

                                if (continueAction != null) continueAction.Invoke(upgradePackageResult, config);

                                if (packageToUninstall != null)
                                {
                                    // Add any warning messages from when we uninstalled the previous package, so
                                    // these can be propagated to the warning list at the end.
                                    // We add these as the last elements otherwise warnings in the current/new
                                    // package may not show up as expected.
                                    upgradePackageResult.Messages.AddRange(packageToUninstall.Messages
                                        .Where(p => p.MessageType == ResultType.Warn)
                                        .Select(p => new ResultMessage(p.MessageType, "v{0} - {1}".FormatWith(packageToUninstall.Version, p.Message))));
                                }
                            }
                            catch (Exception ex)
                            {
                                var message = ex.Message;
                                var webException = ex as System.Net.WebException;
                                if (webException != null)
                                {
                                    var response = webException.Response as HttpWebResponse;
                                    if (response != null && !string.IsNullOrWhiteSpace(response.StatusDescription)) message += " {0}".FormatWith(response.StatusDescription);
                                }

                                var logMessage = "{0} not upgraded. An error occurred during installation:{1} {2}".FormatWith(packageName, Environment.NewLine, message);
                                this.Log().Error(ChocolateyLoggers.Important, logMessage);
                                var errorResult = packageResultsToReturn.GetOrAdd(packageDependencyInfo.Id, new PackageResult(packageDependencyInfo.Id, version.ToFullStringChecked(), null));
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
            config.RevertChanges(removeBackup: true);

            return packageResultsToReturn;
        }

        public virtual ConcurrentDictionary<string, PackageResult> GetOutdated(ChocolateyConfiguration config)
        {
            var sourceCacheContext = new ChocolateySourceCacheContext(config);
            var remoteRepositories = NugetCommon.GetRemoteRepositories(config, _nugetLogger, _fileSystem);
            var remoteEndpoints = NugetCommon.GetRepositoryResources(remoteRepositories, sourceCacheContext);
            var pathResolver = NugetCommon.GetPathResolver(_fileSystem);

            var outdatedPackages = new ConcurrentDictionary<string, PackageResult>();

            var allPackages = SetPackageNamesIfAllSpecified(config, () => { config.IgnoreDependencies = true; });
            var packageNames = config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).OrEmpty().ToList();

            config.CreateBackup();

            foreach (var packageName in packageNames)
            {
                // We need to ensure we are using a clean configuration file
                // before we start reading it.
                config.RevertChanges();

                var installedPackage = allPackages.FirstOrDefault(p => string.Equals(p.Name, packageName, StringComparison.OrdinalIgnoreCase));

                var pkgInfo = _packageInfoService.Get(installedPackage.PackageMetadata);
                bool isPinned = pkgInfo.IsPinned;

                // if the package is pinned and we are skipping pinned,
                // move on quickly
                if (isPinned && config.OutdatedCommand.IgnorePinned)
                {
                    string pinnedLogMessage = "{0} is pinned. Skipping pinned package.".FormatWith(packageName);
                    var pinnedPackageResult = outdatedPackages.GetOrAdd(packageName, new PackageResult(installedPackage.PackageMetadata, pathResolver.GetInstallPath(installedPackage.PackageMetadata.Id)));
                    pinnedPackageResult.Messages.Add(new ResultMessage(ResultType.Debug, pinnedLogMessage));
                    pinnedPackageResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, pinnedLogMessage));

                    continue;
                }

                if (installedPackage != null && installedPackage.PackageMetadata.Version.IsPrerelease && !config.UpgradeCommand.ExcludePrerelease)
                {
                    // this is a prerelease - opt in for newer prereleases.
                    config.Prerelease = true;
                }

                var latestPackage = NugetList.FindPackage(packageName, config, _nugetLogger, sourceCacheContext, remoteEndpoints);

                if (latestPackage == null)
                {
                    if (config.Features.IgnoreUnfoundPackagesOnUpgradeOutdated) continue;

                    string unfoundLogMessage = "{0} was not found with the source(s) listed.{1} Source(s): \"{2}\"".FormatWith(packageName, Environment.NewLine, config.Sources);
                    var unfoundResult = outdatedPackages.GetOrAdd(packageName, new PackageResult(installedPackage.PackageMetadata, pathResolver.GetInstallPath(installedPackage.PackageMetadata.Id)));
                    unfoundResult.Messages.Add(new ResultMessage(ResultType.Warn, unfoundLogMessage));
                    unfoundResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, unfoundLogMessage));

                    this.Log().Warn("{0}|{1}|{1}|{2}".FormatWith(installedPackage.Name, installedPackage.Version, isPinned.ToStringSafe().ToLowerSafe()));
                    continue;
                }

                if (latestPackage.Identity.Version <= installedPackage.PackageMetadata.Version) continue;

                var packageResult = outdatedPackages.GetOrAdd(packageName, new PackageResult(latestPackage, pathResolver.GetInstallPath(latestPackage.Identity)));

                string logMessage = "You have {0} v{1} installed. Version {2} is available based on your source(s).{3} Source(s): \"{4}\"".FormatWith(installedPackage.Name, installedPackage.Version, latestPackage.Identity.Version, Environment.NewLine, config.Sources);
                packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));

                this.Log().Info("{0}|{1}|{2}|{3}".FormatWith(installedPackage.Name, installedPackage.Version, latestPackage.Identity.Version, isPinned.ToStringSafe().ToLowerSafe()));
            }

            // Reset the configuration again once we are completely done with the processing of
            // configurations, and make sure that we are removing any backup that was created
            // as part of this run.
            config.RevertChanges(removeBackup: true);

            return outdatedPackages;
        }

        /// <summary>
        /// Sets the configuration for the package upgrade
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="packageInfo">The package information.</param>
        /// <returns>The original unmodified configuration, so it can be reset after upgrade</returns>
        protected virtual ChocolateyConfiguration SetConfigFromRememberedArguments(ChocolateyConfiguration config, ChocolateyPackageInformation packageInfo)
        {
            if (!config.Features.UseRememberedArgumentsForUpgrades || string.IsNullOrWhiteSpace(packageInfo.Arguments)) return config;

            var packageArgumentsUnencrypted = packageInfo.Arguments.ContainsSafe(" --") && packageInfo.Arguments.ToStringSafe().Length > 4 ? packageInfo.Arguments : NugetEncryptionUtility.DecryptString(packageInfo.Arguments);

            var sensitiveArgs = true;
            if (!ArgumentsUtility.SensitiveArgumentsProvided(packageArgumentsUnencrypted))
            {
                sensitiveArgs = false;
                this.Log().Debug(ChocolateyLoggers.Verbose, "{0} - Adding remembered arguments for upgrade: {1}".FormatWith(packageInfo.Package.Id, packageArgumentsUnencrypted.EscapeCurlyBraces()));
            }

            var packageArgumentsSplit = packageArgumentsUnencrypted.Split(new[] { " --" }, StringSplitOptions.RemoveEmptyEntries);
            var packageArguments = new List<string>();
            foreach (var packageArgument in packageArgumentsSplit.OrEmpty())
            {
                var packageArgumentSplit = packageArgument.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                var optionName = packageArgumentSplit[0].ToStringSafe();
                var optionValue = string.Empty;
                if (packageArgumentSplit.Length == 2)
                {
                    optionValue = packageArgumentSplit[1].ToStringSafe().UnquoteSafe();
                    if (optionValue.StartsWith("'")) optionValue.UnquoteSafe();
                }

                if (sensitiveArgs)
                {
                    this.Log().Debug(ChocolateyLoggers.Verbose, "{0} - Adding '{1}' to upgrade arguments. Values not shown due to detected sensitive arguments".FormatWith(packageInfo.Package.Id, optionName.EscapeCurlyBraces()));
                }
                packageArguments.Add("--{0}{1}".FormatWith(optionName, string.IsNullOrWhiteSpace(optionValue) ? string.Empty : "=" + optionValue));
            }

            var originalConfig = config.DeepCopy();
            // this changes config globally
            ConfigurationOptions.OptionSet.Parse(packageArguments);

            // there may be overrides from the user running upgrade
            if (!string.IsNullOrWhiteSpace(originalConfig.SourceCommand.Username)) config.SourceCommand.Username = originalConfig.SourceCommand.Username;
            if (!string.IsNullOrWhiteSpace(originalConfig.SourceCommand.Password)) config.SourceCommand.Password = originalConfig.SourceCommand.Password;
            if (!string.IsNullOrWhiteSpace(originalConfig.SourceCommand.Certificate)) config.SourceCommand.Certificate = originalConfig.SourceCommand.Certificate;
            if (!string.IsNullOrWhiteSpace(originalConfig.SourceCommand.CertificatePassword)) config.SourceCommand.CertificatePassword = originalConfig.SourceCommand.CertificatePassword;

            return originalConfig;
        }

        private bool HasMissingDependency(PackageResult package, List<PackageResult> allLocalPackages)
        {
            foreach (var dependency in package.PackageMetadata.DependencyGroups.SelectMany(d => d.Packages))
            {
                if (!allLocalPackages.Any(p => p.Identity.Id.Equals(dependency.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsDependentOnTargetPackages(PackageResult package, IEnumerable<string> packageIds)
        {
            foreach (var dependency in package.PackageMetadata.DependencyGroups.SelectMany(d => d.Packages))
            {
                if (packageIds.Contains(dependency.Id, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RemoveInvalidDependenciesAndParents(
            IPackageSearchMetadata availablePackage,
            HashSet<SourcePackageDependencyInfo> removedSources,
            HashSet<SourcePackageDependencyInfo> sourcePackageDependencyInfos,
            IEnumerable<SourcePackageDependencyInfo> localPackagesDependencyInfos)
        {
            var removedIds = removedSources.Select(r => r.Id).Distinct().ToList();
            SourcePackageDependencyInfo removedAvailablePackage = null;

            foreach (var localPackage in localPackagesDependencyInfos.Where(s => removedIds.Contains(s.Id, StringComparer.OrdinalIgnoreCase)))
            {
                var packagesToRemove = sourcePackageDependencyInfos.Where(s => s.Dependencies.Any(d => d.Id.Equals(localPackage.Id, StringComparison.OrdinalIgnoreCase) && !d.VersionRange.Satisfies(localPackage.Version))).ToList();

                if (removedAvailablePackage == null)
                {
                    removedAvailablePackage = packagesToRemove.FirstOrDefault(p => p.Id.IsEqualTo(availablePackage.Identity.Id) && p.Version == availablePackage.Identity.Version);
                }

                sourcePackageDependencyInfos.RemoveWhere(s => packagesToRemove.Contains(s));
                removedSources.AddRange(packagesToRemove);
            }

            if (removedAvailablePackage != null && !sourcePackageDependencyInfos.Any(s => s.Id.IsEqualTo(removedAvailablePackage.Id)))
            {
                removedSources.Remove(removedAvailablePackage);
                sourcePackageDependencyInfos.Add(removedAvailablePackage);
                removedAvailablePackage = null;
            }

            var removedIdsTargetDependency = removedSources
                .Where(r => r.Dependencies.Any(d => d.Id.Equals(availablePackage.Identity.Id, StringComparison.OrdinalIgnoreCase)))
                .Select(r => r.Id)
                .Distinct();

            var ranges = sourcePackageDependencyInfos
                .Where(s => removedIdsTargetDependency.Contains(s.Id, StringComparer.OrdinalIgnoreCase))
                .SelectMany(r => r.Dependencies)
                .Where(d => d.Id.Equals(availablePackage.Identity.Id, StringComparison.OrdinalIgnoreCase))
                .Select(r => r.VersionRange);
            var maxVersion = sourcePackageDependencyInfos
                .Where(s => s.Id.Equals(availablePackage.Identity.Id, StringComparison.OrdinalIgnoreCase) && ranges.All(r => r.Satisfies(s.Version)))
                .Max(s => s.Version);

            sourcePackageDependencyInfos.RemoveWhere(s => s.Id.Equals(availablePackage.Identity.Id, StringComparison.OrdinalIgnoreCase) && s.Version != maxVersion);
        }

        private HashSet<SourcePackageDependencyInfo> RemovePinnedSourceDependencies(HashSet<SourcePackageDependencyInfo> dependencyInfos, List<PackageResult> localPackages)
        {
            var pinnedPackages = localPackages.Select(l => _packageInfoService.Get(l.PackageMetadata))
                .Where(p => p != null && p.IsPinned)
                .Select(p => p.Package.Id)
                .ToList();

            var dependencyInfosToExclude = new HashSet<SourcePackageDependencyInfo>();

            foreach (var dependencyInfo in dependencyInfos)
            {
                if (pinnedPackages.Contains(dependencyInfo.Id, StringComparer.OrdinalIgnoreCase))
                {
                    dependencyInfosToExclude.Add(dependencyInfo);
                }
            }

            dependencyInfos.RemoveWhere(source => dependencyInfosToExclude.Contains(source));

            return dependencyInfosToExclude;
        }

        private void ValidateNuspec(string nuspecFilePath, ChocolateyConfiguration config)
        {
            var results = _ruleService.ValidateRules(nuspecFilePath);

            if (!config.PackCommand.PackThrowOnUnsupportedElements)
            {
                results = results.WhereUnsupportedOrDeprecated(inverse: true);
            }

            var hasErrors = false;

            foreach (var rule in results)
            {
                var message = string.IsNullOrEmpty(rule.Id)
                    ? rule.Message
                    : "{0}: {1}".FormatWith(rule.Id, rule.Message);

                switch (rule.Severity)
                {
                    case infrastructure.rules.RuleType.Error:
                        this.Log().Error("ERROR: " + message);

                        if (!string.IsNullOrEmpty(rule.HelpUrl))
                        {
                            this.Log().Error("       See {0}", rule.HelpUrl);
                        }

                        hasErrors = true;
                        break;

                    case infrastructure.rules.RuleType.Warning:
                        this.Log().Warn("WARNING: " + message);

                        if (!string.IsNullOrEmpty(rule.HelpUrl))
                        {
                            this.Log().Warn("         See {0}", rule.HelpUrl);
                        }

                        break;

                    case infrastructure.rules.RuleType.Information:
                        this.Log().Info("INFO: " + message);

                        if (!string.IsNullOrEmpty(rule.HelpUrl))
                        {
                            this.Log().Info("      See {0}", rule.HelpUrl);
                        }

                        break;

                    case infrastructure.rules.RuleType.Note:
                        this.Log().Info("NOTE: " + message);

                        if (!string.IsNullOrEmpty(rule.HelpUrl))
                        {
                            this.Log().Info("      See {0}", rule.HelpUrl);
                        }

                        break;
                }
            }

            if (hasErrors)
            {
                this.Log().Info(string.Empty);
                throw new InvalidDataException("One or more issues found with {0}, please fix all validation items above listed as errors.".FormatWith(nuspecFilePath));
            }
        }

        private string GetInstallDirectory(IPackageMetadata installedPackage)
        {
            var pathResolver = NugetCommon.GetPathResolver(_fileSystem);
            var installDirectory = pathResolver.GetInstallPath(new PackageIdentity(installedPackage.Id, installedPackage.Version));

            if (!_fileSystem.DirectoryExists(installDirectory))
            {
                return null;
            }

            return installDirectory;
        }

        protected virtual void EnsurePackageFilesHaveCompatibleAttributes(ChocolateyConfiguration config, IPackageMetadata installedPackage)
        {
            var installDirectory = GetInstallDirectory(installedPackage);
            if (!_fileSystem.DirectoryExists(installDirectory)) return;

            _filesService.EnsureCompatibleFileAttributes(installDirectory, config);
        }

        protected virtual void BackupCurrentVersion(ChocolateyConfiguration config, ChocolateyPackageInformation packageInfo)
        {
            var pkgInstallPath = GetInstallDirectory(packageInfo.Package);
            var backupLocation = _fileSystem.CombinePaths(
                pkgInstallPath.Replace(ApplicationParameters.PackagesLocation, ApplicationParameters.PackageBackupLocation),
                packageInfo.Package.Version.ToNormalizedStringChecked());

            var errored = _filesService.MovePackageUsingBackupStrategy(pkgInstallPath, backupLocation, restoreSource: true);
            RemoveOldPackageScriptsBeforeUpgrade(pkgInstallPath, config.CommandName);

            BackupChangedFiles(pkgInstallPath, config, packageInfo);

            if (errored)
            {
                this.Log().Warn(ChocolateyLoggers.Important,
                                @"There was an error accessing files. This could mean there is a
 process locking the folder or files. Please make sure nothing is
 running that would lock the files or folders in this directory prior
 to upgrade or uninstall. If the package fails to upgrade or uninstall, this is likely the cause.");
            }
        }

        public virtual void RemoveOldPackageScriptsBeforeUpgrade(string directoryPath, string commandName)
        {
            if (commandName.ToLowerSafe() == "upgrade")
            {
                // Due to the way that Package Reducer works, there is a potential that a Chocolatey Packaging
                // script could be incorrectly left in place during an upgrade operation.  To guard against this,
                // remove any Chocolatey Packaging scripts, which will then be restored by the new package, if
                // they are still required
                var filesToDelete = new List<string> { "chocolateyinstall", "chocolateyuninstall", "chocolateybeforemodify" };
                var packagingScripts = _fileSystem.GetFiles(directoryPath, "*.ps1", SearchOption.AllDirectories)
                    .Where(p => filesToDelete.Contains(_fileSystem.GetFilenameWithoutExtension(p).ToLowerSafe()));

                foreach (var packagingScript in packagingScripts)
                {
                    if (_fileSystem.FileExists(packagingScript))
                    {
                        this.Log().Debug("Deleting file {0}".FormatWith(packagingScript));
                        _fileSystem.DeleteFile(packagingScript);
                    }
                }
            }
        }

        public virtual void BackupChangedFiles(string packageInstallPath, ChocolateyConfiguration config, ChocolateyPackageInformation packageInfo)
        {
            if (packageInfo == null || packageInfo.Package == null) return;

            var version = packageInfo.Package.Version.ToNormalizedStringChecked();

            if (packageInfo.FilesSnapshot == null || packageInfo.FilesSnapshot.Files.Count == 0)
            {
                var configFiles = _fileSystem.GetFiles(packageInstallPath, ApplicationParameters.ConfigFileExtensions, SearchOption.AllDirectories);
                foreach (var file in configFiles.OrEmpty())
                {
                    var backupName = "{0}.{1}".FormatWith(_fileSystem.GetFileName(file), version);

                    FaultTolerance.TryCatchWithLoggingException(
                        () => _fileSystem.CopyFile(file, _fileSystem.CombinePaths(_fileSystem.GetDirectoryName(file), backupName), overwriteExisting: true),
                        "Error backing up configuration file");
                }
            }
            else
            {
                var currentFiles = _filesService.CaptureSnapshot(packageInstallPath, config);
                foreach (var currentFile in currentFiles.Files.OrEmpty())
                {
                    var installedFile = packageInfo.FilesSnapshot.Files.FirstOrDefault(x => x.Path.IsEqualTo(currentFile.Path));
                    if (installedFile != null)
                    {
                        if (!currentFile.Checksum.IsEqualTo(installedFile.Checksum))
                        {
                            // skip nupkgs if they are different
                            if (_fileSystem.GetFileExtension(currentFile.Path).IsEqualTo(".nupkg")) continue;

                            var backupName = "{0}.{1}".FormatWith(_fileSystem.GetFileName(currentFile.Path), version);
                            FaultTolerance.TryCatchWithLoggingException(
                                () => _fileSystem.CopyFile(currentFile.Path, _fileSystem.CombinePaths(_fileSystem.GetDirectoryName(currentFile.Path), backupName), overwriteExisting: true),
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
        /// <param name="installedPackage">The installed package.</param>
        private void RemoveShimgenDirectors(IPackageMetadata installedPackage)
        {
            var pkgInstallPath = GetInstallDirectory(installedPackage);

            if (_fileSystem.DirectoryExists(pkgInstallPath))
            {
                var shimDirectorFiles = _fileSystem.GetFiles(pkgInstallPath, ApplicationParameters.ShimDirectorFileExtensions, SearchOption.AllDirectories);
                foreach (var file in shimDirectorFiles.OrEmpty())
                {
                    FaultTolerance.TryCatchWithLoggingException(
                        () => _fileSystem.DeleteFile(file),
                        "Error deleting shim director file");
                }
            }
        }

        private void RemovePackageFromCache(ChocolateyConfiguration config, IPackageMetadata installedPackage)
        {
            this.Log().Debug(ChocolateyLoggers.Verbose, "Ensuring removal of package cache files.");
            var cacheDirectory = _fileSystem.CombinePaths(config.CacheLocation, installedPackage.Id, installedPackage.Version.ToNormalizedStringChecked());

            if (!_fileSystem.DirectoryExists(cacheDirectory)) return;

            FaultTolerance.TryCatchWithLoggingException(
                                       () => _fileSystem.DeleteDirectoryChecked(cacheDirectory, recursive: true),
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
        protected void RemovePackageFromNugetCache(IPackageSearchMetadata installedPackage)
        {
            var tempFolder = NuGetEnvironment.GetFolderPath(NuGetFolderPath.Temp);

            if (string.IsNullOrWhiteSpace(tempFolder))
            {
                return;
            }

            FaultTolerance.TryCatchWithLoggingException(
                () =>
                {
                    var packageFolderPath = _fileSystem.CombinePaths(tempFolder, "{0}/{1}".FormatWith(installedPackage.Identity.Id, installedPackage.Identity.Version.ToNormalizedStringChecked()));
                    var nugetCachedFile = _fileSystem.CombinePaths(packageFolderPath, "{0}.{1}.nupkg".FormatWith(installedPackage.Identity.Id, installedPackage.Identity.Version.ToNormalizedStringChecked()));
                    var nupkgMetaDataFile = _fileSystem.CombinePaths(packageFolderPath, ".nupkg.metadata");
                    var nupkgShaFile = nugetCachedFile + ".sha512";

                    if (_fileSystem.FileExists(nugetCachedFile))
                    {
                        _fileSystem.DeleteFile(nugetCachedFile);
                    }

                    if (_fileSystem.FileExists(nupkgMetaDataFile))
                    {
                        _fileSystem.DeleteFile(nupkgMetaDataFile);
                    }

                    if (_fileSystem.FileExists(nupkgShaFile))
                    {
                        _fileSystem.DeleteFile(nupkgShaFile);
                    }
                },
                "Unable to removed cached NuGet package files.");
        }

        public void UninstallDryRun(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
        {
            var results = Uninstall(config, continueAction, performAction: false);
            foreach (var packageResult in results.OrEmpty())
            {
                var package = packageResult.Value.PackageMetadata;
                if (package != null) this.Log().Warn("Would have uninstalled {0} v{1}.".FormatWith(package.Id, package.Version.ToFullStringChecked()));
            }
        }

        public ConcurrentDictionary<string, PackageResult> Uninstall(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeUninstallAction = null)
        {
            return Uninstall(config, continueAction, performAction: true, beforeUninstallAction: beforeUninstallAction);
        }

        public virtual ConcurrentDictionary<string, PackageResult> Uninstall(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, bool performAction, Action<PackageResult, ChocolateyConfiguration> beforeUninstallAction = null)
        {
            _fileSystem.EnsureDirectoryExists(ApplicationParameters.PackagesLocation);
            var packageResultsToReturn = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            NuGetVersion version = config.Version != null ? NuGetVersion.Parse(config.Version) : null;

            var sourceCacheContext = new ChocolateySourceCacheContext(config);
            var localRepositorySource = NugetCommon.GetLocalRepository();
            var projectContext = new ChocolateyNuGetProjectContext(config, _nugetLogger);
            var allLocalPackages = GetInstalledPackages(config);

            // if we are uninstalling a package and not forcing dependencies,
            // look to see if the user is missing the actual package they meant
            // to uninstall.
            if (!config.ForceDependencies)
            {
                // if you find an install of an .install / .portable / .commandline, allow adding it to the list
                var installedPackages = allLocalPackages.Select(p => p.Name).ToList().Join(ApplicationParameters.PackageNamesSeparator);
                foreach (var packageName in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).OrEmpty())
                {
                    var installerExists = installedPackages.ContainsSafe("{0}.install".FormatWith(packageName));
                    var portableExists = installedPackages.ContainsSafe("{0}.portable".FormatWith(packageName));
                    var cmdLineExists = installedPackages.ContainsSafe("{0}.commandline".FormatWith(packageName));
                    if ((!config.PackageNames.ContainsSafe("{0}.install".FormatWith(packageName))
                            && !config.PackageNames.ContainsSafe("{0}.portable".FormatWith(packageName))
                            && !config.PackageNames.ContainsSafe("{0}.commandline".FormatWith(packageName))
                            )
                        && (installerExists || portableExists || cmdLineExists)
                        )
                    {
                        var actualPackageName = installerExists ?
                            "{0}.install".FormatWith(packageName)
                            : portableExists ?
                                "{0}.portable".FormatWith(packageName)
                                : "{0}.commandline".FormatWith(packageName);

                        var timeoutInSeconds = config.PromptForConfirmation ? 0 : 20;
                        this.Log().Warn(@"You are uninstalling {0}, which is likely a metapackage for an
 *.install/*.portable package that it installed
 ({0} represents discoverability).".FormatWith(packageName));
                        var selection = InteractivePrompt.PromptForConfirmation(
                            "Would you like to uninstall {0} as well?".FormatWith(actualPackageName),
                            new[] { "yes", "no" },
                            defaultChoice: null,
                            requireAnswer: false,
                            allowShortAnswer: true,
                            shortPrompt: true,
                            timeoutInSeconds: timeoutInSeconds
                        );

                        if (selection.IsEqualTo("yes"))
                        {
                            config.PackageNames += ";{0}".FormatWith(actualPackageName);
                        }
                        else
                        {
                            var logMessage = "To finish removing {0}, please also run the command: `choco uninstall {1}`.".FormatWith(packageName, actualPackageName);
                            var actualPackageResult = packageResultsToReturn.GetOrAdd(actualPackageName, new PackageResult(actualPackageName, null, null));
                            actualPackageResult.Messages.Add(new ResultMessage(ResultType.Warn, logMessage));
                            actualPackageResult.Messages.Add(new ResultMessage(ResultType.Inconclusive, logMessage));
                        }
                    }
                }
            }

            SetPackageNamesIfAllSpecified(config, () =>
                {
                    // force remove the item, ignore the dependencies
                    // as those are going to be picked up anyway
                    config.Force = true;
                    config.ForceDependencies = false;
                });

            config.CreateBackup();

            var packageVersionsToRemove = new List<PackageResult>();

            foreach (string packageName in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).OrEmpty())
            {
                // We need to ensure we are using a clean configuration file
                // before we start reading it.
                config.RevertChanges();

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
                    string logMessage = "{0} is not installed. Cannot uninstall a non-existent package.".FormatWith(packageName);
                    var missingResult = packageResultsToReturn.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                    missingResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));

                    if (config.RegularOutput) this.Log().Error(ChocolateyLoggers.Important, logMessage);
                    continue;
                }

                packageVersionsToRemove.AddRange(installedPackageVersions);

                if (!config.AllVersions && installedPackageVersions.Count > 1)
                {
                    if (config.PromptForConfirmation)
                    {
                        packageVersionsToRemove.Clear();

                        IList<string> choices = new List<string>();
                        const string abortChoice = "None";
                        choices.Add(abortChoice);
                        foreach (var installedVersion in installedPackageVersions.OrEmpty())
                        {
                            choices.Add(installedVersion.Version);
                        }

                        const string allVersionsChoice = "All versions";
                        if (installedPackageVersions.Count != 1)
                        {
                            choices.Add(allVersionsChoice);
                        }

                        var selection = InteractivePrompt.PromptForConfirmation("Which version of {0} would you like to uninstall?".FormatWith(packageName),
                            choices,
                            defaultChoice: null,
                            requireAnswer: true,
                            allowShortAnswer: false);

                        if (string.IsNullOrWhiteSpace(selection)) continue;
                        if (selection.IsEqualTo(abortChoice)) continue;
                        if (selection.IsEqualTo(allVersionsChoice))
                        {
                            packageVersionsToRemove.AddRange(installedPackageVersions.ToList());
                            if (config.RegularOutput) this.Log().Info(() => "You selected to remove all versions of {0}".FormatWith(packageName));
                        }
                        else
                        {
                            PackageResult pkg = installedPackageVersions.FirstOrDefault((p) => p.Version.IsEqualTo(selection));
                            packageVersionsToRemove.Add(pkg);
                            if (config.RegularOutput) this.Log().Info(() => "You selected {0} v{1}".FormatWith(pkg.Name, pkg.Version));
                        }
                    }
                }
            }

            foreach (var installedPackage in GetUninstallOrder(packageVersionsToRemove))
            {
                //Need to get this again for dependency resolution
                allLocalPackages = GetInstalledPackages(config);
                var packagesToUninstall = new HashSet<PackageResult>();
                var localPackagesDependencyInfos = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
                var pathResolver = NugetCommon.GetPathResolver(_fileSystem);
                var nugetProject = new FolderNuGetProject(ApplicationParameters.PackagesLocation, pathResolver, NuGetFramework.AnyFramework);

                var pkgInfo = _packageInfoService.Get(installedPackage.PackageMetadata);
                if (pkgInfo != null && pkgInfo.IsPinned)
                {
                    var logMessage = "{0} is pinned. Skipping pinned package.".FormatWith(installedPackage.Name);
                    var pinnedResult = packageResultsToReturn.GetOrAdd(installedPackage.Name, new PackageResult(installedPackage.Name, null, null));
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
                    var uninstallationContext = new UninstallationContext(removeDependencies: config.ForceDependencies, forceRemove: config.Force, warnDependencyResolvingFailure: true);

                    ICollection<PackageIdentity> resolvedPackages = null;
                    try
                    {
                        resolvedPackages = UninstallResolver.GetPackagesToBeUninstalled(installedPackage.Identity, localPackagesDependencyInfos, allPackagesIdentities, uninstallationContext, _nugetLogger);
                    }
                    catch (Exception ex)
                    {
                        this.Log().Warn("[NuGet]: {0}", ex.Message);
                        var result = packageResultsToReturn.GetOrAdd(installedPackage.Name + "." + installedPackage.Version, (key) => new PackageResult(installedPackage.PackageMetadata, pathResolver.GetInstallPath(installedPackage.PackageMetadata.Id)));
                        result.Messages.Add(new ResultMessage(ResultType.Error, ex.Message));
                        continue;
                    }

                    if (resolvedPackages is null)
                    {
                        var result = packageResultsToReturn.GetOrAdd(installedPackage.Name + "." + installedPackage.Version, (key) => new PackageResult(installedPackage.PackageMetadata, pathResolver.GetInstallPath(installedPackage.PackageMetadata.Id)));
                        result.Messages.Add(new ResultMessage(ResultType.Error, "Unable to resolve dependency context. Not able to uninstall package."));
                        continue;
                    }

                    packagesToUninstall.AddRange(allLocalPackages.Where(p => resolvedPackages.Contains(p.Identity)));

                    foreach (var packageToUninstall in packagesToUninstall)
                    {
                        try
                        {
                            this.Log().Info(ChocolateyLoggers.Important, @"
{0} v{1}", packageToUninstall.Name, packageToUninstall.Identity.Version.ToNormalizedStringChecked());

                            var uninstallPkgInfo = _packageInfoService.Get(packageToUninstall.PackageMetadata);
                            BackupAndRunBeforeModify(packageToUninstall, uninstallPkgInfo, config, beforeUninstallAction);

                            var key = packageToUninstall.Name + "." + packageToUninstall.Version.ToStringSafe();

                            if (packageResultsToReturn.TryRemove(key, out PackageResult removedPackage))
                            {
                                // If we are here, we have already tried this package, as such we need
                                // to remove the package but keep any messages. This is required as
                                // Search Metadata may be null which is required later.
                                packageToUninstall.Messages.AddRange(removedPackage.Messages);
                            }

                            var packageResult = packageResultsToReturn.GetOrAdd(key, packageToUninstall);

                            if (!packageResult.Success)
                            {
                                // Remove any previous error messages, otherwise the uninstall may show
                                // up as failing.
                                packageResult.Messages.RemoveAll(m => m.MessageType == ResultType.Error);
                            }

                            packageResult.InstallLocation = packageToUninstall.InstallLocation;
                            var logMessage = "{0}{1} v{2}{3}".FormatWith(Environment.NewLine, packageToUninstall.Name, packageToUninstall.Version.ToStringSafe(), config.Force ? " (forced)" : string.Empty);
                            packageResult.Messages.Add(new ResultMessage(ResultType.Debug, ApplicationParameters.Messages.ContinueChocolateyAction));

                            if (continueAction != null) continueAction.Invoke(packageResult, config);

                            if (packageToUninstall != null)
                            {
                                packageToUninstall.InstallLocation = pathResolver.GetInstallPath(packageToUninstall.Identity);
                                try
                                {
                                    //It does not throw or return false if it fails to delete something...
                                    //var ableToDelete = nugetProject.DeletePackage(packageToUninstall.Identity, projectContext, CancellationToken.None, shouldDeleteDirectory: false).GetAwaiter().GetResult();

                                    // If we have gotten here, it means we may only have files left to remove.
                                    RemoveInstallationFilesUnsafe(packageToUninstall.PackageMetadata, pkgInfo);
                                }
                                catch (Exception ex)
                                {
                                    var errorlogMessage = "{0}:{1} {2}".FormatWith("Unable to delete all existing package files. There will be leftover files requiring manual cleanup", Environment.NewLine, ex.Message);
                                    this.Log().Warn(logMessage);
                                    packageResult.Messages.Add(new ResultMessage(ResultType.Error, errorlogMessage));

                                    // As the only reason we failed the uninstall is due to left over files, let us
                                    // be good citizens and ensure that the nupkg file is removed so the package will
                                    // not be listed as installed. This is needed to do manually as the package may
                                    // have been optimized by licensed edition of Chocolatey CLI.
                                    EnsureNupkgRemoved(packageToUninstall.PackageMetadata, throwError: false);

                                    // Do not call continueAction again here as it has already been called once.
                                    //if (continueAction != null) continueAction.Invoke(packageResult, config);
                                    continue;
                                }
                            }

                            this.Log().Info(ChocolateyLoggers.Important, " {0} has been successfully uninstalled.".FormatWith(packageToUninstall.Name));

                            EnsureNupkgRemoved(packageToUninstall.PackageMetadata);
                            RemoveInstallationFiles(packageToUninstall.PackageMetadata, uninstallPkgInfo);
                        }
                        catch (Exception ex)
                        {
                            var logMessage = "{0} not uninstalled. An error occurred during uninstall:{1} {2}".FormatWith(installedPackage.Name, Environment.NewLine, ex.Message);
                            this.Log().Error(ChocolateyLoggers.Important, logMessage);
                            var result = packageResultsToReturn.GetOrAdd(packageToUninstall.Name + "." + packageToUninstall.Version.ToStringSafe(), new PackageResult(packageToUninstall.PackageMetadata, pathResolver.GetInstallPath(packageToUninstall.PackageMetadata.Id)));
                            result.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                            if (result.ExitCode == 0) result.ExitCode = 1;

                            if (config.Features.StopOnFirstPackageFailure)
                            {
                                throw new ApplicationException("Stopping further execution as {0} has failed uninstallation".FormatWith(packageToUninstall.Name));
                            }
                            // do not call continueAction - will result in multiple passes
                        }
                    }
                }
                else
                {
                    // continue action won't be found b/c we are not actually uninstalling (this is noop)
                    var result = packageResultsToReturn.GetOrAdd(installedPackage.Name + "." + installedPackage.Version.ToStringSafe(), new PackageResult(installedPackage.PackageMetadata, pathResolver.GetInstallPath(installedPackage.PackageMetadata.Id)));
                    if (continueAction != null) continueAction.Invoke(result, config);
                }
            }

            // Reset the configuration again once we are completely done with the processing of
            // configurations, and make sure that we are removing any backup that was created
            // as part of this run.
            config.RevertChanges(removeBackup: true);

            return packageResultsToReturn;
        }

        private IEnumerable<PackageResult> GetUninstallOrder(List<PackageResult> packageVersionsToRemove)
        {
            var newResults = new List<PackageResult>();

            foreach (var package in packageVersionsToRemove)
            {
                var insertIndex = -1;

                foreach (var dependency in package.PackageMetadata.DependencyGroups.SelectMany(d => d.Packages))
                {
                    var existingPackage = newResults.FirstOrDefault(p => p.Name.IsEqualTo(dependency.Id));

                    if (existingPackage != null)
                    {
                        var index = newResults.IndexOf(existingPackage);
                        if (index > -1 && (index < insertIndex || insertIndex == -1))
                        {
                            insertIndex = index;
                        }
                    }
                }

                if (insertIndex >= 0)
                {
                    newResults.Insert(insertIndex, package);
                }
                else
                {
                    newResults.Add(package);
                }
            }

            return newResults;
        }

        /// <summary>
        /// This method should be called before any modifications are made to a package.
        /// Typically this should be called before doing an uninstall of an existing package
        /// or package dependency during an install, upgrade, or uninstall operation.
        /// </summary>
        /// <param name="packageResult">The package currently being modified.</param>
        /// <param name="config">The current configuration.</param>
        /// <param name="beforeModifyAction">Any action to run before performing backup operations. Typically this is an invocation of the chocolateyBeforeModify script.</param>
        protected void BackupAndRunBeforeModify(
            PackageResult packageResult,
            ChocolateyConfiguration config,
            Action<PackageResult, ChocolateyConfiguration> beforeModifyAction)
        {
            var packageInformation = _packageInfoService.Get(packageResult.PackageMetadata);
            BackupAndRunBeforeModify(packageResult, packageInformation, config, beforeModifyAction);
        }

        /// <summary>
        /// This method should be called before any modifications are made to a package.
        /// Typically this should be called before doing an uninstall of an existing package
        /// or package dependency during an install, upgrade, or uninstall operation.
        /// </summary>
        /// <param name="packageResult">The package currently being modified.</param>
        /// <param name="packageInformation">The package information for the package being modified.</param>
        /// <param name="config">The current configuration.</param>
        /// <param name="beforeModifyAction">Any action to run before performing backup operations. Typically this is an invocation of the chocolateyBeforeModify script.</param>
        protected virtual void BackupAndRunBeforeModify(
            PackageResult packageResult,
            ChocolateyPackageInformation packageInformation,
            ChocolateyConfiguration config,
            Action<PackageResult, ChocolateyConfiguration> beforeModifyAction)
        {
            try
            {
                if (packageResult.InstallLocation != null)
                {
                    // If this is an already installed package we're modifying, ensure we run its beforeModify script and back it up properly.
                    if (beforeModifyAction != null)
                    {
                        "chocolatey".Log().Debug("Running beforeModify step for '{0}'", packageResult.PackageMetadata.Id);

                        var packageResultCopy = new PackageResult(packageResult.PackageMetadata, packageResult.SearchMetadata, packageResult.InstallLocation, packageResult.Source);

                        beforeModifyAction(packageResultCopy, config);

                        packageResult.Messages.AddRange(packageResultCopy.Messages.Select(m =>
                        {
                            if (m.MessageType == ResultType.Error)
                            {
                                // We don't want any errors to stop execution when running before modify
                                // scripts. As such we will re-add this message as a warning instead.
                                if (m.Message.ContainsSafe("Error while running"))
                                {
                                    return new ResultMessage(ResultType.Warn, "Error while running the 'chocolateyBeforeModify.ps1'.");
                                }
                                else
                                {
                                    return new ResultMessage(ResultType.Warn, m.Message);
                                }
                            }

                            return m;
                        }));
                    }

                    "chocolatey".Log().Debug("Backing up package files for '{0}'", packageResult.PackageMetadata.Id);

                    BackupCurrentPackageFiles(config, packageResult.PackageMetadata, packageInformation);
                }
            }
            catch (Exception error)
            {
                "chocolatey".Log().Error("Failed to run backup or beforeModify steps for package '{0}': {1}", packageResult.PackageMetadata.Id, error.Message);
                "chocolatey".Log().Trace(error.StackTrace);
            }
        }

        /// <summary>
        /// Takes a backup of the existing package files.
        /// </summary>
        /// <param name="config">The current configuration settings</param>
        /// <param name="package">The metadata for the package to backup</param>
        /// <param name="packageInformation">The package information to backup</param>
        protected void BackupCurrentPackageFiles(ChocolateyConfiguration config, IPackageMetadata package, ChocolateyPackageInformation packageInformation)
        {
            EnsureBackupDirectoryRemoved(package.Id);
            EnsurePackageFilesHaveCompatibleAttributes(config, package);
            BackupCurrentVersion(config, packageInformation);
            RemoveShimgenDirectors(package);
        }

        /// <summary>
        /// NuGet will happily report a package has been uninstalled, even if it doesn't always remove the nupkg.
        /// Ensure that the package is deleted or throw an error.
        /// </summary>
        /// <param name="removedPackage">The installed package.</param>
        /// <param name="throwError">Whether failing to remove the nuget package should throw an exception or not.</param>
        private void EnsureNupkgRemoved(IPackageMetadata removedPackage, bool throwError = true)
        {
            this.Log().Debug(ChocolateyLoggers.Verbose, "Removing nupkg if it still exists.");

            var nupkgFile = "{0}.nupkg".FormatWith(removedPackage.Id);
            var installDir = _fileSystem.CombinePaths(ApplicationParameters.PackagesLocation, removedPackage.Id);
            var nupkg = _fileSystem.CombinePaths(installDir, nupkgFile);

            if (!_fileSystem.FileExists(nupkg)) return;

            FaultTolerance.TryCatchWithLoggingException(
                () => _fileSystem.DeleteFile(nupkg),
                "Error deleting nupkg file",
                throwError: throwError);
        }

        protected void NormalizeNuspecCasing(IPackageSearchMetadata packageMetadata, string packageLocation)
        {
            if (Platform.GetPlatform() == PlatformType.Windows) return;
            this.Log().Debug(ChocolateyLoggers.Verbose, "Fixing nuspec casing if required");

            var expectedNuspec = _fileSystem.CombinePaths(packageLocation, "{0}{1}"
                .FormatWith(packageMetadata.Identity.Id, NuGetConstants.ManifestExtension));
            var lowercaseNuspec = _fileSystem.CombinePaths(packageLocation, "{0}{1}"
                .FormatWith(packageMetadata.Identity.Id.ToLowerSafe(), NuGetConstants.ManifestExtension));

            if (!_fileSystem.FileExists(expectedNuspec) && _fileSystem.FileExists(lowercaseNuspec))
            {
                FaultTolerance.TryCatchWithLoggingException(
                    () => _fileSystem.MoveFile(lowercaseNuspec, expectedNuspec),
                    "Error moving nuspec file {0} to {1}".FormatWith(lowercaseNuspec, expectedNuspec),
                    throwError: true);
            }
        }

        public virtual void RemoveInstallationFilesUnsafe(IPackageMetadata removedPackage, ChocolateyPackageInformation pkgInfo)
        {
            this.Log().Debug(ChocolateyLoggers.Verbose, "Ensuring removal of installation files.");
            var installDir = _fileSystem.CombinePaths(ApplicationParameters.PackagesLocation, removedPackage.Id);

            if (_fileSystem.DirectoryExists(installDir) && pkgInfo != null && pkgInfo.FilesSnapshot != null)
            {
                var exceptions = new List<Exception>();

                foreach (var file in _fileSystem.GetFiles(installDir, "*.*", SearchOption.AllDirectories).OrEmpty())
                {
                    var fileSnapshot = pkgInfo.FilesSnapshot.Files.FirstOrDefault(f => f.Path.IsEqualTo(file));
                    if (fileSnapshot == null) continue;

                    var filesystemFileChecksum = _filesService.GetPackageFile(file).Checksum;

                    if (filesystemFileChecksum == ApplicationParameters.HashProviderFileLocked)
                    {
                        exceptions.Add(new IOException("File {0} is locked".FormatWith(file)));
                        continue;
                    }

                    if (fileSnapshot.Checksum == filesystemFileChecksum)
                    {
                        if (!_fileSystem.FileExists(file)) continue;

                        try
                        {
                            _fileSystem.DeleteFile(file);
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }

                if (exceptions.Count > 1)
                {
                    throw new AggregateException(exceptions);
                }
                else if (exceptions.Count == 1)
                {
                    throw exceptions[0];
                }
            }

            if (_fileSystem.DirectoryExists(installDir) && !_fileSystem.GetFiles(installDir, "*.*", SearchOption.AllDirectories).OrEmpty().Any())
            {
                _fileSystem.DeleteDirectoryChecked(installDir, recursive: true);
            }
        }

        public virtual void RemoveInstallationFiles(IPackageMetadata removedPackage, ChocolateyPackageInformation pkgInfo)
        {
            this.Log().Debug(ChocolateyLoggers.Verbose, "Ensuring removal of installation files.");
            var installDir = _fileSystem.CombinePaths(ApplicationParameters.PackagesLocation, removedPackage.Id);

            if (_fileSystem.DirectoryExists(installDir) && pkgInfo != null && pkgInfo.FilesSnapshot != null)
            {
                foreach (var file in _fileSystem.GetFiles(installDir, "*.*", SearchOption.AllDirectories).OrEmpty())
                {
                    var fileSnapshot = pkgInfo.FilesSnapshot.Files.FirstOrDefault(f => f.Path.IsEqualTo(file));
                    if (fileSnapshot == null) continue;

                    if (fileSnapshot.Checksum == _filesService.GetPackageFile(file).Checksum)
                    {
                        if (!_fileSystem.FileExists(file)) continue;

                        FaultTolerance.TryCatchWithLoggingException(
                            () => _fileSystem.DeleteFile(file),
                            "Error deleting file");
                    }

                    if (fileSnapshot.Checksum == ApplicationParameters.HashProviderFileLocked)
                    {
                        this.Log().Warn(() => "Snapshot for '{0}' was attempted when file was locked.{1} Please inspect and manually remove file{1} at '{2}'".FormatWith(_fileSystem.GetFileName(file), Environment.NewLine, _fileSystem.GetDirectoryName(file)));
                    }
                }
            }

            if (_fileSystem.DirectoryExists(installDir) && !_fileSystem.GetFiles(installDir, "*.*", SearchOption.AllDirectories).OrEmpty().Any())
            {
                _fileSystem.DeleteDirectoryChecked(installDir, recursive: true);
            }
        }

        public IEnumerable<PackageResult> GetInstalledPackages(ChocolateyConfiguration config)
        {
            //todo: #2579 move to deep copy for get all installed
            //var listConfig = config.deep_copy();
            //listConfig.ListCommand.LocalOnly = true;
            //listConfig.Noop = false;
            //listConfig.PackageNames = string.Empty;
            //listConfig.Input = string.Empty;
            //listConfig.QuietOutput = true;

            //return List(listConfig).ToList();

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

            var installedPackages = List(config).ToList();

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

        private IEnumerable<PackageResult> SetPackageNamesIfAllSpecified(ChocolateyConfiguration config, Action customAction)
        {
            var allPackages = GetInstalledPackages(config);
            if (config.PackageNames.IsEqualTo(ApplicationParameters.AllPackages))
            {
                var packagesToUpdate= allPackages.Select(p => p.Name).ToList();

                if (!string.IsNullOrWhiteSpace(config.UpgradeCommand.PackageNamesToSkip))
                {
                    var packagesToSkip = config.UpgradeCommand.PackageNamesToSkip
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Select(p => p.TrimSafe())
                        .ToList();

                    var unknownPackagesToSkip = packagesToSkip
                        .Where(p => !packagesToUpdate.Contains(p, StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    if (unknownPackagesToSkip.Any())
                    {
                        this.Log().Warn(() => "Some packages specified in the 'except' list were not found in the local packages: '{0}'".FormatWith(string.Join(",", unknownPackagesToSkip)));

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

                        this.Log().Info(() => "These packages will not be upgraded because they were specified in the 'except' list: {0}".FormatWith(string.Join(",", packagesToSkip)));
                    }
                }

                config.PackageNames = packagesToUpdate.Join(ApplicationParameters.PackageNamesSeparator);

                if (customAction != null) customAction.Invoke();
            }

            return allPackages;
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void ensure_source_app_installed(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> ensureAction)
            => EnsureSourceAppInstalled(config, ensureAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual int count_run(ChocolateyConfiguration config)
            => Count(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void list_noop(ChocolateyConfiguration config)
            => ListDryRun(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual IEnumerable<PackageResult> list_run(ChocolateyConfiguration config)
            => List(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void pack_noop(ChocolateyConfiguration config)
            => PackDryRun(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual string validate_and_return_package_file(ChocolateyConfiguration config, string extension)
            => GetPackageFileOrThrow(config, extension);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void pack_run(ChocolateyConfiguration config)
            => Pack(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void push_noop(ChocolateyConfiguration config)
            => PushDryRun(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void push_run(ChocolateyConfiguration config)
            => Push(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void install_noop(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
            => InstallDryRun(config, continueAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
            => Install(config, continueAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeModifyAction)
            => Install(config, continueAction, beforeModifyAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected virtual string get_dependency_resolution_error_message(NuGetResolverConstraintException exception)
            => GetDependencyResolutionErrorMessage(exception);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void remove_rollback_directory_if_exists(string packageName)
            => EnsureBackupDirectoryRemoved(packageName);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public ConcurrentDictionary<string, PackageResult> upgrade_noop(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
            => UpgradeDryRun(config, continueAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeUpgradeAction = null)
            => Upgrade(config, continueAction, beforeUpgradeAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, bool performAction, Action<PackageResult, ChocolateyConfiguration> beforeUpgradeAction = null)
            => Upgrade(config, continueAction, performAction, beforeUpgradeAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual ConcurrentDictionary<string, PackageResult> get_outdated(ChocolateyConfiguration config)
            => GetOutdated(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected virtual ChocolateyConfiguration set_package_config_for_upgrade(ChocolateyConfiguration config, ChocolateyPackageInformation packageInfo)
            => SetConfigFromRememberedArguments(config, packageInfo);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected virtual void ensure_package_files_have_compatible_attributes(ChocolateyConfiguration config, IPackageMetadata installedPackage)
            => EnsurePackageFilesHaveCompatibleAttributes(config, installedPackage);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected virtual void backup_existing_version(ChocolateyConfiguration config, ChocolateyPackageInformation packageInfo)
            => BackupCurrentVersion(config, packageInfo);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void remove_packaging_files_prior_to_upgrade(string directoryPath, string commandName)
            => RemoveOldPackageScriptsBeforeUpgrade(directoryPath, commandName);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void backup_changed_files(string packageInstallPath, ChocolateyConfiguration config, ChocolateyPackageInformation packageInfo)
            => BackupChangedFiles(packageInstallPath, config, packageInfo);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void uninstall_noop(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
            => UninstallDryRun(config, continueAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeUninstallAction = null)
            => Uninstall(config, continueAction, beforeUninstallAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, bool performAction, Action<PackageResult, ChocolateyConfiguration> beforeUninstallAction = null)
            => Uninstall(config, continueAction, performAction, beforeUninstallAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected void backup_and_before_modify(
            PackageResult packageResult,
            ChocolateyConfiguration config,
            Action<PackageResult, ChocolateyConfiguration> beforeModifyAction)
            => BackupAndRunBeforeModify(packageResult, config, beforeModifyAction);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected virtual void backup_and_before_modify(
            PackageResult packageResult,
            ChocolateyPackageInformation packageInformation,
            ChocolateyConfiguration config,
            Action<PackageResult, ChocolateyConfiguration> beforeModifyAction)
            => BackupAndRunBeforeModify(packageResult, packageInformation, config, beforeModifyAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected void backup_existing_package_files(ChocolateyConfiguration config, IPackageMetadata package, ChocolateyPackageInformation packageInformation)
            => BackupCurrentPackageFiles(config, package, packageInformation);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void remove_installation_files_unsafe(IPackageMetadata removedPackage, ChocolateyPackageInformation pkgInfo)
            => RemoveInstallationFilesUnsafe(removedPackage, pkgInfo);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void remove_installation_files(IPackageMetadata removedPackage, ChocolateyPackageInformation pkgInfo)
            => RemoveInstallationFiles(removedPackage, pkgInfo);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public IEnumerable<PackageResult> get_all_installed_packages(ChocolateyConfiguration config)
            => GetInstalledPackages(config);
#pragma warning restore IDE1006
    }
}
