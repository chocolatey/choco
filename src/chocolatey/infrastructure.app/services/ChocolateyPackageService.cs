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
    using System.ComponentModel.Design;
    using System.IO;
    using System.Linq;
    using System.Text;
    using chocolatey.infrastructure.app.registration;
    using commandline;
    using configuration;
    using domain;
    using events;
    using infrastructure.commands;
    using infrastructure.events;
    using infrastructure.services;
    using logging;
    using nuget;
    using NuGet.Configuration;
    using NuGet.Packaging;
    using NuGet.Protocol.Core.Types;
    using NuGet.Versioning;
    using platforms;
    using results;
    using SimpleInjector;
    using tolerance;
    using IFileSystem = filesystem.IFileSystem;

    public class ChocolateyPackageService : IChocolateyPackageService
    {
        private readonly INugetService _nugetService;
        private readonly IPowershellService _powershellService;
        private readonly IShimGenerationService _shimgenService;
        private readonly IFileSystem _fileSystem;
        private readonly IRegistryService _registryService;
        private readonly IChocolateyPackageInformationService _packageInfoService;
        private readonly IFilesService _filesService;
        private readonly IAutomaticUninstallerService _autoUninstallerService;
        private readonly IXmlService _xmlService;
        private readonly IConfigTransformService _configTransformService;
        private readonly IContainerResolver _containerResolver;
        private readonly IDictionary<string, FileStream> _pendingLocks = new Dictionary<string, FileStream>();

        private readonly IList<string> _proBusinessMessages = new List<string> {
@"
Are you ready for the ultimate experience? Check out Pro / Business!
 https://chocolatey.org/compare"
,
@"
Enjoy using Chocolatey? Explore more amazing features to take your
experience to the next level at
 https://chocolatey.org/compare"
,
@"
Did you know the proceeds of Pro (and some proceeds from other
 licensed editions) go into bettering the community infrastructure?
 Your support ensures an active community, keeps Chocolatey tip-top,
 plus it nets you some awesome features!
 https://chocolatey.org/compare",
@"
Did you know some organizations use Chocolatey completely internally
 without using the community repository or downloads from the internet?
 Wait until you see how Package Builder and Package Internalizer can
 help you achieve more, quicker, and easier! Get your trial started
 today at https://chocolatey.org/compare",
@"
An organization needed total software management life cycle automation.
 They evaluated Chocolatey for Business. You won't believe what happens
 next!
 https://chocolatey.org/compare",
@"
Did you know that Package Synchronizer and AutoUninstaller enhancements
 in licensed versions are up to 95% effective in removing system
 installed software without an uninstall script? Find out more at
 https://chocolatey.org/compare",
@"
Did you know Chocolatey goes to eleven? And it turns great developers /
 system admins into something amazing! Singlehandedly solve your
 organization's struggles with software management and save the day!
 https://chocolatey.org/compare"
};

        private const string ProBusinessListMessage = @"
Did you know Pro / Business automatically syncs with Programs and
 Features? Learn more about Package Synchronizer at
 https://chocolatey.org/compare";

        private readonly string _shutdownExe = Environment.ExpandEnvironmentVariables("%systemroot%\\System32\\shutdown.exe");

        // Hold a list of exit codes that are known to be related to reboots
        // 1641 - restart initiated
        // 3010 - restart required
        private readonly List<int> _rebootExitCodes = new List<int> { 1641, 3010 };

        public ChocolateyPackageService(
            INugetService nugetService,
            IPowershellService powershellService,
            IShimGenerationService shimgenService,
            IFileSystem fileSystem,
            IRegistryService registryService,
            IChocolateyPackageInformationService packageInfoService,
            IFilesService filesService,
            IAutomaticUninstallerService autoUninstallerService,
            IXmlService xmlService,
            IConfigTransformService configTransformService,
            IContainerResolver containerResolver)
        {
            _nugetService = nugetService;
            _powershellService = powershellService;
            _shimgenService = shimgenService;
            _fileSystem = fileSystem;
            _registryService = registryService;
            _packageInfoService = packageInfoService;
            _filesService = filesService;
            _autoUninstallerService = autoUninstallerService;
            _xmlService = xmlService;
            _configTransformService = configTransformService;
            _containerResolver = containerResolver;
        }

        public virtual int Count(ChocolateyConfiguration config)
        {
            if (config.SourceType.IsEqualTo(SourceTypes.Normal))
            {
                return _nugetService.Count(config);
            }

            return PerformSourceRunnerFunction<ICountSourceRunner, int>(
                config,
                runner => runner.Count(config),
                throwOnException: true);
        }

        private void PerformSourceRunnerAction<TSourceRunner>(ChocolateyConfiguration config, Action<TSourceRunner> action, bool throwOnException = false)
            where TSourceRunner : class, IAlternativeSourceRunner
        {
            var alternativeSourceRunners = _containerResolver.ResolveAll<TSourceRunner>();
            PerformSourceRunnerAction(config, action, alternativeSourceRunners, throwOnException);
        }

        private void PerformSourceRunnerAction<TSourceRunner>(ChocolateyConfiguration config, Action<TSourceRunner> action, IEnumerable<TSourceRunner> alternativeSourceRunners, bool throwOnException = false)
            where TSourceRunner : class, IAlternativeSourceRunner
        {
            var runner = GetSourceRunner(config.SourceType, alternativeSourceRunners);
            if (runner != null && action != null)
            {
                if (runner is IBootstrappableSourceRunner bootstrapper)
                {
                    bootstrapper.EnsureSourceAppInstalled(config, (packageResult, configuration) => HandlePackageResult(packageResult, configuration, CommandNameType.Install));
                }

                action.Invoke(runner);
            }
            else
            {
                if (throwOnException)
                {
                    throw new NotSupportedException("No runner was found that implements source type '{0}' or action was missing".FormatWith(config.SourceType));
                }
                else
                {
                    this.Log().Warn("No runner was found that implements source type '{0}' or action was missing".FormatWith(config.SourceType));
                }
            }
        }

        private TResult PerformSourceRunnerFunction<TSourceRunner, TResult>(ChocolateyConfiguration config, Func<TSourceRunner, TResult> function, bool throwOnException = false)
            where TSourceRunner : class, IAlternativeSourceRunner
        {
            var alternativeSourceRunners = _containerResolver.ResolveAll<TSourceRunner>();
            return PerformSourceRunnerFunction(config, function, alternativeSourceRunners, throwOnException);
        }

        private TResult PerformSourceRunnerFunction<TSourceRunner, TResult>(ChocolateyConfiguration config, Func<TSourceRunner, TResult> function, IEnumerable<TSourceRunner> alternativeSourceRunners, bool throwOnException = false)
            where TSourceRunner : class, IAlternativeSourceRunner
        {
            var runner = GetSourceRunner(config.SourceType, alternativeSourceRunners);
            if (runner != null && function != null)
            {
                if (runner is IBootstrappableSourceRunner bootstrapper)
                {
                    // NOTE: Here we are passing the original config into the HandlePackageResult method, rather than what comes out of the Action, since
                    // that configuration could be wildly different from what was passed in, and we want to continue with what was in play before
                    // installing the required source application.
                    bootstrapper.EnsureSourceAppInstalled(config, (packageResult, configuration) => HandlePackageResult(packageResult, config, CommandNameType.Install));
                }

                return function.Invoke(runner);
            }

            if (throwOnException)
            {
                throw new NotSupportedException("No runner was found that implements source type '{0}' or, it does not support requested functionality.".FormatWith(config.SourceType));
            }
            else
            {
                this.Log().Warn("No runner was found that implements source type '{0}' or, it does not support requested functionality.".FormatWith(config.SourceType));
            }

            return default;
        }

        public void ListDryRun(ChocolateyConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.Sources) && !config.ListCommand.LocalOnly)
            {
                this.Log().Error(ChocolateyLoggers.Important, @"Unable to search for packages when there are no sources enabled for
 packages and none were passed as arguments.");
                Environment.ExitCode = 1;
                return;
            }

            config.Noop = true;

            if (config.ListCommand.LocalOnly && !config.SourceType.IsEqualTo(SourceTypes.Normal))
            {
                PerformSourceRunnerAction<IListSourceRunner>(config, runner => runner.ListDryRun(config));
            }
            else if (config.SourceType.IsEqualTo(SourceTypes.Normal))
            {
                _nugetService.ListDryRun(config);
            }
            else
            {
                PerformSourceRunnerAction<ISearchableSourceRunner>(config, runner => runner.SearchDryRun(config));
            }

            RandomlyNotifyAboutLicensedFeatures(config, ProBusinessListMessage);
        }

        public virtual IEnumerable<PackageResult> List(ChocolateyConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.Sources) && !config.ListCommand.LocalOnly)
            {
                this.Log().Error(ChocolateyLoggers.Important, @"Unable to search for packages when there are no sources enabled for
 packages and none were passed as arguments.");
                Environment.ExitCode = 1;
                yield break;
            }

            if (config.RegularOutput) this.Log().Debug(() => "Searching for package information");

            var packages = new List<PackageResult>();

            IEnumerable<PackageResult> results;

            if (config.ListCommand.LocalOnly && !config.SourceType.IsEqualTo(SourceTypes.Normal))
            {
                results = PerformSourceRunnerFunction<IListSourceRunner, IEnumerable<PackageResult>>(config, runner => runner.List(config));
            }
            else if (config.SourceType.IsEqualTo(SourceTypes.Normal))
            {
                results = _nugetService.List(config).OrEmpty();
            }
            else
            {
                results = PerformSourceRunnerFunction<ISearchableSourceRunner, IEnumerable<PackageResult>>(config, runner => runner.Search(config));
            }

            if (results is null)
            {
                var message = config.ListCommand.LocalOnly ? "The '{0}' source does not support listing of packages".FormatWith(config.SourceType) : "The '{0}' source does not support searching for packages".FormatWith(config.SourceType);
                throw new NotSupportedException(message);
            }

            foreach (PackageResult package in results)
            {
                if (config.SourceType.IsEqualTo(SourceTypes.Normal))
                {
                    if (!config.ListCommand.IncludeRegistryPrograms)
                    {
                        yield return package;
                    }

                    if (config.ListCommand.LocalOnly && config.ListCommand.IncludeRegistryPrograms && package.PackageMetadata != null)
                    {
                        packages.Add(package);
                    }
                }
            }

            if (config.RegularOutput)
            {
                if (config.ListCommand.LocalOnly && config.ListCommand.IncludeRegistryPrograms)
                {
                    foreach (var installed in ReportRegistryPrograms(config, packages))
                    {
                        yield return installed;
                    }
                }
            }

            RandomlyNotifyAboutLicensedFeatures(config, ProBusinessListMessage);
        }

        private IEnumerable<PackageResult> ReportRegistryPrograms(ChocolateyConfiguration config, IEnumerable<PackageResult> list)
        {
            var itemsToRemoveFromMachine = list.Select(package => _packageInfoService.Get(package.PackageMetadata)).Where(p => p.RegistrySnapshot != null).ToList();

            var count = 0;
            var machineInstalled = _registryService.GetInstallerKeys().RegistryKeys.Where(
                p => p.IsInProgramsAndFeatures() &&
                     !itemsToRemoveFromMachine.Any(pkg => pkg.RegistrySnapshot.RegistryKeys.Any(k => k.DisplayName.IsEqualTo(p.DisplayName))) &&
                     !p.KeyPath.ContainsSafe("choco-")).OrderBy(p => p.DisplayName).Distinct();

            this.Log().Info(() => "");
            foreach (var key in machineInstalled)
            {
                if (config.RegularOutput)
                {
                    this.Log().Info("{0}|{1}".FormatWith(key.DisplayName, key.DisplayVersion));
                    if (config.Verbose) this.Log().Info(" InstallLocation: {0}{1} Uninstall:{2}".FormatWith(key.InstallLocation.ToStringSafe().EscapeCurlyBraces(), Environment.NewLine, key.UninstallString.ToStringSafe().EscapeCurlyBraces()));
                }
                count++;

                yield return new PackageResult(key.DisplayName, key.DisplayName, key.InstallLocation);
            }

            if (config.RegularOutput)
            {
                this.Log().Warn(() => @"{0} applications not managed with Chocolatey.".FormatWith(count));
            }
        }

        public void PackDryRun(ChocolateyConfiguration config)
        {
            if (!config.SourceType.IsEqualTo(SourceTypes.Normal))
            {
                this.Log().Warn(ChocolateyLoggers.Important, "This source doesn't provide a facility for packaging.");
                return;
            }

            _nugetService.PackDryRun(config);
            RandomlyNotifyAboutLicensedFeatures(config);
        }

        public virtual void Pack(ChocolateyConfiguration config)
        {
            if (!config.SourceType.IsEqualTo(SourceTypes.Normal))
            {
                this.Log().Warn(ChocolateyLoggers.Important, "This source doesn't provide a facility for packaging.");
                return;
            }

            _nugetService.Pack(config);
            RandomlyNotifyAboutLicensedFeatures(config);
        }

        public void PushDryRun(ChocolateyConfiguration config)
        {
            if (!config.SourceType.IsEqualTo(SourceTypes.Normal))
            {
                this.Log().Warn(ChocolateyLoggers.Important, "This source doesn't provide a facility for pushing.");
                return;
            }

            _nugetService.PushDryRun(config);
            RandomlyNotifyAboutLicensedFeatures(config);
        }

        public virtual void Push(ChocolateyConfiguration config)
        {
            if (!config.SourceType.IsEqualTo(SourceTypes.Normal))
            {
                this.Log().Warn(ChocolateyLoggers.Important, "This source doesn't provide a facility for pushing.");
                return;
            }

            _nugetService.Push(config);
            RandomlyNotifyAboutLicensedFeatures(config);
        }

        public void InstallDryRun(ChocolateyConfiguration config)
        {
            ValidatePackageNames(config);

            var installSourceRunners = _containerResolver.ResolveAll<IInstallSourceRunner>();

            // each package can specify its own configuration values
            foreach (var packageConfig in GetConfigFromInputAndPackageConfigInput(config, new ConcurrentDictionary<string, PackageResult>(), installSourceRunners).OrEmpty())
            {
                config.Noop = true;

                if (packageConfig.SourceType.IsEqualTo(SourceTypes.Normal))
                {
                    Action<PackageResult, ChocolateyConfiguration> action = (pkg, configuration) => _powershellService.InstallDryRun(pkg);
                    _nugetService.InstallDryRun(packageConfig, action);
                }
                else
                {
                    PerformSourceRunnerAction(config, runner => runner.InstallDryRun(packageConfig, null), installSourceRunners);
                }
            }

            RandomlyNotifyAboutLicensedFeatures(config);
        }

        /// <summary>
        /// Once every 10 runs or so, Chocolatey FOSS should inform the user of the Pro / Business versions.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="message">The message to send.</param>
        /// <remarks>We want it random enough not to be annoying, but informative enough for awareness.</remarks>
        public void RandomlyNotifyAboutLicensedFeatures(ChocolateyConfiguration config, string message = null)
        {
            if (!config.Information.IsLicensedVersion && config.RegularOutput)
            {
                // magic numbers! Basically about 10% of the time show a message.
                if (new Random().Next(1, 10) == 3)
                {
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        // Choose a message at random to display. It is
                        // specifically done like this as sometimes Random
                        // doesn't like to grab the max value.
                        var messageCount = _proBusinessMessages.Count;
                        var chosenMessage = new Random().Next(0, messageCount);
                        if (chosenMessage >= messageCount) chosenMessage = messageCount - 1;
                        message = _proBusinessMessages[chosenMessage];
                    }

                    this.Log().Warn(ChocolateyLoggers.Important, message);
                }
            }
        }

        public virtual void HandlePackageResult(PackageResult packageResult, ChocolateyConfiguration config, CommandNameType commandName)
        {
            EnvironmentSettings.ResetEnvironmentVariables(config);
            MarkPackagePending(packageResult, config);

            this.Log().Info(packageResult.ExitCode == 0
                ? "{0} package files {1} completed. Performing other installation steps.".FormatWith(packageResult.Name, commandName.ToStringSafe())
                : "{0} package files {1} failed with exit code {2}. Performing other installation steps.".FormatWith(packageResult.Name, commandName.ToStringSafe(), packageResult.ExitCode));

            var pkgInfo = GetPackageInformation(packageResult, config);

            // initialize this here so it can be used for the install location later
            bool powerShellRan = false;

            if (packageResult.Success && config.Information.PlatformType == PlatformType.Windows)
            {
                if (!config.SkipPackageInstallProvider)
                {
                    var installersBefore = _registryService.GetInstallerKeys();
                    var environmentBefore = GetInitialEnvironment(config, allowLogging: false);

                    powerShellRan = _powershellService.Install(config, packageResult);
                    if (powerShellRan)
                    {
                        // we don't care about the exit code
                        if (config.Information.PlatformType == PlatformType.Windows) CommandExecutor.ExecuteStatic(_shutdownExe, "/a", config.CommandExecutionTimeoutSeconds, _fileSystem.GetCurrentDirectory(), (s, e) => { }, (s, e) => { }, false, false);
                    }

                    var installersDifferences = _registryService.GetInstallerKeysChanged(installersBefore, _registryService.GetInstallerKeys());
                    if (installersDifferences.RegistryKeys.Count != 0)
                    {
                        //todo: #2567 - note keys passed in
                        pkgInfo.RegistrySnapshot = installersDifferences;

                        var key = installersDifferences.RegistryKeys.FirstOrDefault();
                        if (key != null && key.HasQuietUninstall)
                        {
                            pkgInfo.HasSilentUninstall = true;
                            this.Log().Info("  {0} can be automatically uninstalled.".FormatWith(packageResult.Name));
                        }
                        else if (key != null)
                        {
                            this.Log().Info("  {0} may be able to be automatically uninstalled.".FormatWith(packageResult.Name));
                        }
                    }

                    IEnumerable<GenericRegistryValue> environmentChanges;
                    IEnumerable<GenericRegistryValue> environmentRemovals;
                    LogEnvironmentChanges(config, environmentBefore, out environmentChanges, out environmentRemovals);
                    //todo: #564 record this with package info
                }

                _filesService.EnsureCompatibleFileAttributes(packageResult, config);
                _configTransformService.Run(packageResult, config);

                pkgInfo.FilesSnapshot = _filesService.CaptureSnapshot(packageResult, config);

                var is32Bit = !config.Information.Is64BitProcess || config.ForceX86;
                CreateExecutableIgnoreFiles(packageResult.InstallLocation, !is32Bit);

                if (packageResult.Success) _shimgenService.Install(config, packageResult);
            }
            else
            {
                if (config.Information.PlatformType != PlatformType.Windows) this.Log().Info(ChocolateyLoggers.Important, () => " Skipping PowerShell and shimgen portions of the install due to non-Windows.");
                if (packageResult.Success)
                {
                    _configTransformService.Run(packageResult, config);
                    pkgInfo.FilesSnapshot = _filesService.CaptureSnapshot(packageResult, config);
                }
            }

            if (packageResult.Success)
            {
                HandleExtensionPackages(config, packageResult);
                HandleTemplatePackages(config, packageResult);
                HandleHookPackages(config, packageResult);
                pkgInfo.Arguments = CaptureArguments(config, packageResult);
                pkgInfo.IsPinned = config.PinPackage;
            }

            var toolsLocation = Environment.GetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyToolsLocation);
            if (!string.IsNullOrWhiteSpace(toolsLocation) && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyPackageInstallLocation)))
            {
                toolsLocation = _fileSystem.CombinePaths(toolsLocation, packageResult.Name);
                if (_fileSystem.DirectoryExists(toolsLocation))
                {
                    Environment.SetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyPackageInstallLocation, toolsLocation, EnvironmentVariableTarget.Process);
                }
            }

            if (!powerShellRan && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyPackageInstallLocation)))
            {
                Environment.SetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyPackageInstallLocation, packageResult.InstallLocation, EnvironmentVariableTarget.Process);
            }

            if (pkgInfo.RegistrySnapshot != null && pkgInfo.RegistrySnapshot.RegistryKeys.Any(k => !string.IsNullOrWhiteSpace(k.InstallLocation)))
            {
                var key = pkgInfo.RegistrySnapshot.RegistryKeys.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k.InstallLocation));
                if (key != null) Environment.SetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyPackageInstallLocation, key.InstallLocation, EnvironmentVariableTarget.Process);
            }

            UpdatePackageInformation(pkgInfo);
            EnsureBadPackagesPathIsClean(config, packageResult);
            EventManager.Publish(new HandlePackageResultCompletedMessage(packageResult, config, commandName));

            UnmarkPackagePending(packageResult, config);

            if (_rebootExitCodes.Contains(packageResult.ExitCode))
            {
                if (config.Features.ExitOnRebootDetected)
                {
                    Environment.ExitCode = ApplicationParameters.ExitCodes.ErrorInstallSuspend;
                    this.Log().Warn(ChocolateyLoggers.Important, @"Chocolatey has detected a pending reboot after installing/upgrading
package '{0}' - stopping further execution".FormatWith(packageResult.Name));

                    throw new ApplicationException("Reboot required before continuing. Reboot and run the same command again.");
                }
            }

            if (!packageResult.Success)
            {
                this.Log().Error(ChocolateyLoggers.Important, "The {0} of {1} was NOT successful.".FormatWith(commandName.ToStringSafe(), packageResult.Name));
                HandleFailedOperation(config, packageResult, movePackageToFailureLocation: true, attemptRollback: true);

                if (config.Features.StopOnFirstPackageFailure)
                {
                    throw new ApplicationException("Stopping further execution as {0} has failed {1}.".FormatWith(packageResult.Name, commandName.ToStringSafe()));
                }

                return;
            }

            RemoveBackupIfExists(packageResult);

            this.Log().Info(ChocolateyLoggers.Important, " The {0} of {1} was successful.".FormatWith(commandName.ToStringSafe(), packageResult.Name));

            var installLocation = Environment.GetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyPackageInstallLocation);
            var installerDetected = Environment.GetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyPackageInstallerType);
            if (!string.IsNullOrWhiteSpace(installLocation))
            {
                this.Log().Info(ChocolateyLoggers.Important, "  Software installed to '{0}'".FormatWith(installLocation.EscapeCurlyBraces()));
            }
            else if (!string.IsNullOrWhiteSpace(installerDetected))
            {
                this.Log().Info(ChocolateyLoggers.Important, @"  Software installed as '{0}', install location is likely default.".FormatWith(installerDetected));
            }
            else
            {
                this.Log().Info(ChocolateyLoggers.Important, @"  Software install location not explicitly set, it could be in package or
  default install location of installer.");
            }
        }

        private void CreateExecutableIgnoreFiles(string installLocation, bool is64Bit)
        {
            // If we are using a 64 bit architecture, we want to ignore exe's targeting x86
            // This is done by adding a .ignore file into the package folder for each exe to ignore
            var exeFiles32Bit = (_fileSystem.DirectoryExists(_fileSystem.CombinePaths(installLocation, "tools\\x86")) ? _fileSystem.GetFiles(_fileSystem.CombinePaths(installLocation, "tools\\x86"), pattern: "*.exe", option: SearchOption.AllDirectories) : new List<string>()).ToArray();
            var exeFiles64Bit = (_fileSystem.DirectoryExists(_fileSystem.CombinePaths(installLocation, "tools\\x64")) ? _fileSystem.GetFiles(_fileSystem.CombinePaths(installLocation, "tools\\x64"), pattern: "*.exe", option: SearchOption.AllDirectories) : new List<string>()).ToArray();

            // If 64bit, and there are only 32bit files, we should shim the 32bit versions,
            // therefore, don't ignore anything
            if (is64Bit && !exeFiles64Bit.Any() && exeFiles32Bit.Any())
            {
                return;
            }

            foreach (var exeFile in is64Bit ? exeFiles32Bit.OrEmpty() : exeFiles64Bit.OrEmpty())
            {
                _fileSystem.CreateFile(exeFile + ".ignore");
            }
        }

        protected virtual ChocolateyPackageInformation GetPackageInformation(PackageResult packageResult, ChocolateyConfiguration config)
        {
            var pkgInfo = _packageInfoService.Get(packageResult.PackageMetadata);

            return pkgInfo;
        }

        protected virtual void UpdatePackageInformation(ChocolateyPackageInformation pkgInfo)
        {
            _packageInfoService.Save(pkgInfo);
        }

        private string CaptureArguments(ChocolateyConfiguration config, PackageResult packageResult)
        {
            var arguments = new StringBuilder();

            // use the config to reconstruct

            //bug:sources are unable to be used - it's late in the process when a package is known
            //arguments.Append(" --source=\"'{0}'\"".format_with(config.Sources));

            if (config.Prerelease) arguments.Append(" --prerelease");
            if (config.IgnoreDependencies) arguments.Append(" --ignore-dependencies");
            if (config.ForceX86) arguments.Append(" --forcex86");

            if (!string.IsNullOrWhiteSpace(config.InstallArguments)) arguments.Append(" --install-arguments=\"'{0}'\"".FormatWith(config.InstallArguments));
            if (config.OverrideArguments) arguments.Append(" --override-arguments");
            if (config.ApplyInstallArgumentsToDependencies) arguments.Append(" --apply-install-arguments-to-dependencies");

            if (!string.IsNullOrWhiteSpace(config.PackageParameters)) arguments.Append(" --package-parameters=\"'{0}'\"".FormatWith(config.PackageParameters));
            if (config.ApplyPackageParametersToDependencies) arguments.Append(" --apply-package-parameters-to-dependencies");

            if (config.AllowDowngrade) arguments.Append(" --allow-downgrade");

            // most times folks won't want to skip automation scripts on upgrade
            //if (config.SkipPackageInstallProvider) arguments.Append(" --skip-automation-scripts");
            //if (config.UpgradeCommand.FailOnUnfound) arguments.Append(" --fail-on-unfound");

            if (!string.IsNullOrWhiteSpace(config.SourceCommand.Username)) arguments.Append(" --user=\"'{0}'\"".FormatWith(config.SourceCommand.Username));
            if (!string.IsNullOrWhiteSpace(config.SourceCommand.Password)) arguments.Append(" --password=\"'{0}'\"".FormatWith(config.SourceCommand.Password));
            if (!string.IsNullOrWhiteSpace(config.SourceCommand.Certificate)) arguments.Append(" --cert=\"'{0}'\"".FormatWith(config.SourceCommand.Certificate));
            if (!string.IsNullOrWhiteSpace(config.SourceCommand.CertificatePassword)) arguments.Append(" --certpassword=\"'{0}'\"".FormatWith(config.SourceCommand.CertificatePassword));

            // this should likely be limited
            //if (!config.Features.ChecksumFiles) arguments.Append(" --ignore-checksums");
            //if (!config.Features.AllowEmptyChecksums) arguments.Append(" --allow-empty-checksums");
            //if (!config.Features.AllowEmptyChecksumsSecure) arguments.Append(" --allow-empty-checksums-secure");
            //arguments.Append(config.Features.UsePackageExitCodes ? " --use-package-exit-codes" : " --ignore-package-exit-codes");

            //global options
            if (config.CommandExecutionTimeoutSeconds != ApplicationParameters.DefaultWaitForExitInSeconds)
            {
                arguments.Append(" --execution-timeout=\"'{0}'\"".FormatWith(config.CommandExecutionTimeoutSeconds));
            }

            if (!string.IsNullOrWhiteSpace(config.CacheLocation)) arguments.Append(" --cache-location=\"'{0}'\"".FormatWith(config.CacheLocation));
            if (config.Features.FailOnStandardError) arguments.Append(" --fail-on-standard-error");
            if (!config.Features.UsePowerShellHost) arguments.Append(" --use-system-powershell");

            return NugetEncryptionUtility.EncryptString(arguments.ToStringSafe());
        }

        public virtual ConcurrentDictionary<string, PackageResult> Install(ChocolateyConfiguration config)
        {
            ValidatePackageNames(config);

            this.Log().Info(IsPackagesConfigFile(config.PackageNames) ? @"Installing from config file:" : @"Installing the following packages:");
            this.Log().Info(ChocolateyLoggers.Important, @"{0}".FormatWith(config.PackageNames));

            var packageInstalls = new ConcurrentDictionary<string, PackageResult>();

            if (string.IsNullOrWhiteSpace(config.Sources))
            {
                this.Log().Error(ChocolateyLoggers.Important, @"Installation was NOT successful. There are no sources enabled for
 packages, and none were passed as arguments.");
                Environment.ExitCode = 1;
                return packageInstalls;
            }

            this.Log().Info(@"By installing, you accept licenses for the packages.");

            GetInitialEnvironment(config, allowLogging: true);

            var installSourceRunners = _containerResolver.ResolveAll<IInstallSourceRunner>();

            try
            {
                foreach (var packageConfig in GetConfigFromInputAndPackageConfigInput(config, packageInstalls, installSourceRunners).OrEmpty())
                {
                    ConcurrentDictionary<string, PackageResult> results;

                    if (packageConfig.SourceType.IsEqualTo(SourceTypes.Normal))
                    {
                        var action = new Action<PackageResult, ChocolateyConfiguration>((packageResult, configuration) => HandlePackageResult(packageResult, configuration, CommandNameType.Install));
                        var beforeModifyAction = new Action<PackageResult, ChocolateyConfiguration>((packageResult, configuration) => BeforeModifyAction(packageResult, configuration));

                        results = _nugetService.Install(packageConfig, action, beforeModifyAction);
                    }
                    else
                    {
                        results = PerformSourceRunnerFunction<IInstallSourceRunner, ConcurrentDictionary<string, PackageResult>>(packageConfig, runner => runner.Install(packageConfig, null));
                    }

                    if (results is null)
                    {
                        throw new NotSupportedException("The '{0}' source does not support installing packages".FormatWith(config.SourceType));
                    }

                    foreach (var result in results)
                    {
                        packageInstalls.GetOrAdd(result.Key, result.Value);
                    }
                }
            }
            finally
            {
                var installFailures = ReportActionSummary(packageInstalls, "installed");
                if (installFailures != 0 && Environment.ExitCode == 0)
                {
                    Environment.ExitCode = 1;
                }

                RandomlyNotifyAboutLicensedFeatures(config);
            }

            return packageInstalls;
        }

        public void OutdatedDryRun(ChocolateyConfiguration config)
        {
            this.Log().Info(@"
Would have determined packages that are out of date based on what is
 installed and what versions are available for upgrade.");
        }

        public virtual void Outdated(ChocolateyConfiguration config)
        {
            if (!config.SourceType.IsEqualTo(SourceTypes.Normal))
            {
                this.Log().Warn(ChocolateyLoggers.Important, "This source doesn't provide a facility for outdated.");
                return;
            }

            if (config.RegularOutput) this.Log().Info(ChocolateyLoggers.Important, @"Outdated Packages
 Output is package name | current version | available version | pinned?
");

            config.PackageNames = ApplicationParameters.AllPackages;
            config.UpgradeCommand.NotifyOnlyAvailableUpgrades = true;

            var output = config.RegularOutput;
            config.RegularOutput = false;
            var outdatedPackages = _nugetService.GetOutdated(config);
            config.RegularOutput = output;
            var outdatedPackageCount = outdatedPackages.Count(p => p.Value.Success && !p.Value.Inconclusive);

            if (config.RegularOutput)
            {
                var upgradeWarnings = outdatedPackages.Count(p => p.Value.Warning);
                this.Log().Warn(() => @"{0}{1} has determined {2} package(s) are outdated. {3}".FormatWith(
                    Environment.NewLine,
                    ApplicationParameters.Name,
                    outdatedPackageCount,
                    upgradeWarnings == 0 ? string.Empty : "{0} {1} package(s) had warnings.".FormatWith(Environment.NewLine, upgradeWarnings)
                    ));

                if (upgradeWarnings != 0)
                {
                    this.Log().Warn(ChocolateyLoggers.Important, "Warnings:");
                    foreach (var warning in outdatedPackages.Where(p => p.Value.Warning).OrEmpty())
                    {
                        this.Log().Warn(ChocolateyLoggers.Important, " - {0}".FormatWith(warning.Value.Name));
                    }
                }
            }

            // outdated packages, return 2
            if (config.Features.UseEnhancedExitCodes && outdatedPackageCount != 0 && Environment.ExitCode == 0)
            {
                Environment.ExitCode = 2;
            }

            RandomlyNotifyAboutLicensedFeatures(config);
        }

        private IEnumerable<ChocolateyConfiguration> GetConfigFromInputAndPackageConfigInput(ChocolateyConfiguration config, ConcurrentDictionary<string, PackageResult> packageInstalls, IEnumerable<IAlternativeSourceRunner> alternativeSourceRunners)
        {
            // if there are any .config files, split those off of the config. Then return the config without those package names.
            foreach (var packageConfigFile in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).OrEmpty().Where(p => p.EndsWith(".config")).ToList())
            {
                config.PackageNames = config.PackageNames.Replace(packageConfigFile, string.Empty);

                foreach (var packageConfig in GetPackagesFromConfigFile(packageConfigFile, config, packageInstalls, alternativeSourceRunners).OrEmpty())
                {
                    yield return packageConfig;
                }
            }

            yield return config;
        }

        private bool IsPackagesConfigFile(string packageNames)
        {
            return packageNames.ToStringSafe().EndsWith(".config", StringComparison.OrdinalIgnoreCase) && !packageNames.ToStringSafe().ContainsSafe(";");
        }

        private IEnumerable<ChocolateyConfiguration> GetPackagesFromConfigFile(string packageConfigFile, ChocolateyConfiguration config, ConcurrentDictionary<string, PackageResult> packageInstalls, IEnumerable<IAlternativeSourceRunner> alternativeSourceRunners)
        {
            IList<ChocolateyConfiguration> packageConfigs = new List<ChocolateyConfiguration>();

            if (!_fileSystem.FileExists(_fileSystem.GetFullPath(packageConfigFile)))
            {
                var logMessage = "'{0}' could not be found in the location specified.".FormatWith(packageConfigFile);
                this.Log().Error(ChocolateyLoggers.Important, logMessage);
                var results = packageInstalls.GetOrAdd(packageConfigFile, new PackageResult(packageConfigFile, null, null));
                results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));

                return packageConfigs;
            }

            var settings = _xmlService.Deserialize<PackagesConfigFileSettings>(_fileSystem.GetFullPath(packageConfigFile));
            this.Log().Info(@"Installing the following packages:");
            foreach (var pkgSettings in settings.Packages.OrEmpty())
            {
                if (!pkgSettings.Disabled)
                {
                    var packageConfig = config.DeepCopy();
                    packageConfig.PackageNames = pkgSettings.Id;
                    packageConfig.Sources = string.IsNullOrWhiteSpace(pkgSettings.Source) ? packageConfig.Sources : pkgSettings.Source;
                    packageConfig.Version = pkgSettings.Version;
                    packageConfig.InstallArguments = string.IsNullOrWhiteSpace(pkgSettings.InstallArguments) ? packageConfig.InstallArguments : pkgSettings.InstallArguments;
                    packageConfig.PackageParameters = string.IsNullOrWhiteSpace(pkgSettings.PackageParameters) ? packageConfig.PackageParameters : pkgSettings.PackageParameters;
                    if (pkgSettings.ForceX86) packageConfig.ForceX86 = true;
                    if (pkgSettings.IgnoreDependencies) packageConfig.IgnoreDependencies = true;
                    if (pkgSettings.ApplyInstallArgumentsToDependencies) packageConfig.ApplyInstallArgumentsToDependencies = true;
                    if (pkgSettings.ApplyPackageParametersToDependencies) packageConfig.ApplyPackageParametersToDependencies = true;

                    if (!string.IsNullOrWhiteSpace(pkgSettings.Source) && HasSourceType(pkgSettings.Source, alternativeSourceRunners))
                    {
                        packageConfig.SourceType = pkgSettings.Source;
                    }

                    if (pkgSettings.PinPackage) packageConfig.PinPackage = true;
                    if (pkgSettings.Force) packageConfig.Force = true;
                    packageConfig.CommandExecutionTimeoutSeconds = pkgSettings.ExecutionTimeout == -1 ? packageConfig.CommandExecutionTimeoutSeconds : pkgSettings.ExecutionTimeout;
                    if (pkgSettings.Prerelease) packageConfig.Prerelease = true;
                    if (pkgSettings.OverrideArguments) packageConfig.OverrideArguments = true;
                    if (pkgSettings.NotSilent) packageConfig.NotSilent = true;
                    if (pkgSettings.AllowDowngrade) packageConfig.AllowDowngrade = true;
                    if (pkgSettings.ForceDependencies) packageConfig.ForceDependencies = true;
                    if (pkgSettings.SkipAutomationScripts) packageConfig.SkipPackageInstallProvider = true;
                    packageConfig.SourceCommand.Username = string.IsNullOrWhiteSpace(pkgSettings.User) ? packageConfig.SourceCommand.Username : pkgSettings.User;
                    packageConfig.SourceCommand.Password = string.IsNullOrWhiteSpace(pkgSettings.Password) ? packageConfig.SourceCommand.Password : pkgSettings.Password;
                    packageConfig.SourceCommand.Certificate = string.IsNullOrWhiteSpace(pkgSettings.Cert) ? packageConfig.SourceCommand.Certificate : pkgSettings.Cert;
                    packageConfig.SourceCommand.CertificatePassword = string.IsNullOrWhiteSpace(pkgSettings.CertPassword) ? packageConfig.SourceCommand.CertificatePassword : pkgSettings.CertPassword;
                    if (pkgSettings.IgnoreChecksums) packageConfig.Features.ChecksumFiles = false;
                    if (pkgSettings.AllowEmptyChecksums) packageConfig.Features.AllowEmptyChecksums = true;
                    if (pkgSettings.AllowEmptyChecksumsSecure) packageConfig.Features.AllowEmptyChecksumsSecure = true;
                    if (pkgSettings.RequireChecksums)
                    {
                        packageConfig.Features.AllowEmptyChecksums = false;
                        packageConfig.Features.AllowEmptyChecksumsSecure = false;
                    }
                    packageConfig.DownloadChecksum = string.IsNullOrWhiteSpace(pkgSettings.DownloadChecksum) ? packageConfig.DownloadChecksum : pkgSettings.DownloadChecksum;
                    packageConfig.DownloadChecksum64 = string.IsNullOrWhiteSpace(pkgSettings.DownloadChecksum64) ? packageConfig.DownloadChecksum64 : pkgSettings.DownloadChecksum64;
                    packageConfig.DownloadChecksumType = string.IsNullOrWhiteSpace(pkgSettings.DownloadChecksumType) ? packageConfig.DownloadChecksumType : pkgSettings.DownloadChecksumType;
                    packageConfig.DownloadChecksumType64 = string.IsNullOrWhiteSpace(pkgSettings.DownloadChecksumType64) ? packageConfig.DownloadChecksumType : pkgSettings.DownloadChecksumType64;
                    if (pkgSettings.IgnorePackageExitCodes) packageConfig.Features.UsePackageExitCodes = false;
                    if (pkgSettings.UsePackageExitCodes) packageConfig.Features.UsePackageExitCodes = true;
                    if (pkgSettings.StopOnFirstFailure) packageConfig.Features.StopOnFirstPackageFailure = true;
                    if (pkgSettings.ExitWhenRebootDetected) packageConfig.Features.ExitOnRebootDetected = true;
                    if (pkgSettings.IgnoreDetectedReboot) packageConfig.Features.ExitOnRebootDetected = false;
                    if (pkgSettings.DisableRepositoryOptimizations) packageConfig.Features.UsePackageRepositoryOptimizations = false;
                    if (pkgSettings.AcceptLicense) packageConfig.AcceptLicense = true;
                    if (pkgSettings.Confirm)
                    {
                        packageConfig.PromptForConfirmation = false;
                        packageConfig.AcceptLicense = true;
                    }
                    if (pkgSettings.LimitOutput) packageConfig.RegularOutput = false;
                    packageConfig.CacheLocation = string.IsNullOrWhiteSpace(pkgSettings.CacheLocation) ? packageConfig.CacheLocation : pkgSettings.CacheLocation;
                    if (pkgSettings.FailOnStderr) packageConfig.Features.FailOnStandardError = true;
                    if (pkgSettings.UseSystemPowershell) packageConfig.Features.UsePowerShellHost = false;
                    if (pkgSettings.NoProgress) packageConfig.Features.ShowDownloadProgress = false;

                    this.Log().Info(ChocolateyLoggers.Important, @"{0}".FormatWith(packageConfig.PackageNames));
                    packageConfigs.Add(packageConfig);
                    this.Log().Debug(() => "Package Configuration Start:{0}{1}{0}Package Configuration End".FormatWith(System.Environment.NewLine, packageConfig.ToString()));
                }
            }

            return packageConfigs;
        }

        public void UpgradeDryRun(ChocolateyConfiguration config)
        {
            ValidatePackageNames(config);

            var upgradeSourceRunners = _containerResolver.ResolveAll<IUpgradeSourceRunner>();

            config.Noop = true;

            ConcurrentDictionary<string, PackageResult> noopUpgrades;

            if (config.SourceType.IsEqualTo(SourceTypes.Normal))
            {
                Action<PackageResult, ChocolateyConfiguration> action = (pkg, configuration) => _powershellService.InstallDryRun(pkg);
                noopUpgrades = _nugetService.UpgradeDryRun(config, action);
            }
            else
            {
                noopUpgrades = PerformSourceRunnerFunction(config, runner => runner.UpgradeDryRun(config, null), upgradeSourceRunners);
            }

            if (config.RegularOutput)
            {
                var noopFailures = ReportActionSummary(noopUpgrades, "can upgrade");
            }

            RandomlyNotifyAboutLicensedFeatures(config);
        }

        public virtual ConcurrentDictionary<string, PackageResult> Upgrade(ChocolateyConfiguration config)
        {
            ValidatePackageNames(config);

            this.Log().Info(@"Upgrading the following packages:");
            this.Log().Info(ChocolateyLoggers.Important, @"{0}".FormatWith(config.PackageNames));

            if (string.IsNullOrWhiteSpace(config.Sources))
            {
                this.Log().Error(ChocolateyLoggers.Important, @"Upgrading was NOT successful. There are no sources enabled for
 packages and none were passed as arguments.");
                Environment.ExitCode = 1;
                return new ConcurrentDictionary<string, PackageResult>();
            }

            this.Log().Info(@"By upgrading, you accept licenses for the packages.");

            foreach (var packageConfigFile in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).OrEmpty().Where(p => p.EndsWith(".config")).ToList())
            {
                throw new ApplicationException("A packages.config file is only used with installs.");
            }

            var packageUpgrades = new ConcurrentDictionary<string, PackageResult>();

            try
            {
                GetInitialEnvironment(config, allowLogging: true);

                ConcurrentDictionary<string, PackageResult> results;

                if (config.SourceType.IsEqualTo(SourceTypes.Normal))
                {
                    var action = new Action<PackageResult, ChocolateyConfiguration>((packageResult, configuration) => HandlePackageResult(packageResult, configuration, CommandNameType.Upgrade));
                    var beforeUpgradeAction = new Action<PackageResult, ChocolateyConfiguration>((packageResult, configuration) => BeforeModifyAction(packageResult, configuration));

                    results = _nugetService.Upgrade(config, action, beforeUpgradeAction);
                }
                else
                {
                    results = PerformSourceRunnerFunction<IUpgradeSourceRunner, ConcurrentDictionary<string, PackageResult>>(config, runner => runner.Upgrade(config, null));
                }

                if (results is null)
                {
                    throw new NotSupportedException("The '{0}' source does not support upgrading packages".FormatWith(config.SourceType));
                }

                foreach (var result in results)
                {
                    packageUpgrades.GetOrAdd(result.Key, result.Value);
                }
            }
            finally
            {
                var upgradeFailures = ReportActionSummary(packageUpgrades, "upgraded");
                if (upgradeFailures != 0 && Environment.ExitCode == 0)
                {
                    Environment.ExitCode = 1;
                }

                RandomlyNotifyAboutLicensedFeatures(config);
            }

            return packageUpgrades;
        }

        private void BeforeModifyAction(PackageResult packageResult, ChocolateyConfiguration config)
        {
            if (!config.SkipPackageInstallProvider && config.Information.PlatformType == PlatformType.Windows)
            {
                _powershellService.BeforeModify(config, packageResult);
            }
            else
            {
                if (config.Information.PlatformType != PlatformType.Windows) this.Log().Info(ChocolateyLoggers.Important, () => " Skipping beforemodify PowerShell script due to non-Windows.");
            }
        }

        public void UninstallDryRun(ChocolateyConfiguration config)
        {
            var uninstallSourceRunners = _containerResolver.ResolveAll<IUninstallSourceRunner>();

            config.Noop = true;

            if (config.SourceType.IsEqualTo(SourceTypes.Normal))
            {
                Action<PackageResult, ChocolateyConfiguration> action = (pkg, configuration) =>
                {
                    _powershellService.BeforeModifyDryRun(pkg);
                    _powershellService.UninstallDryRun(pkg);
                };
                _nugetService.UninstallDryRun(config, action);
            }
            else
            {
                PerformSourceRunnerAction(config, runner => runner.UninstallDryRun(config, null), uninstallSourceRunners);
            }

            RandomlyNotifyAboutLicensedFeatures(config);
        }

        public virtual ConcurrentDictionary<string, PackageResult> Uninstall(ChocolateyConfiguration config)
        {
            this.Log().Info(@"Uninstalling the following packages:");
            this.Log().Info(ChocolateyLoggers.Important, @"{0}".FormatWith(config.PackageNames));

            if (config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).OrEmpty().Any(p => p.EndsWith(".config")))
            {
                throw new ApplicationException("A packages.config file is only used with installs.");
            }

            var packageUninstalls = new ConcurrentDictionary<string, PackageResult>();

            try
            {
                var environmentBefore = GetInitialEnvironment(config);

                ConcurrentDictionary<string, PackageResult> results;

                if (config.SourceType.IsEqualTo(SourceTypes.Normal))
                {
                    var action = new Action<PackageResult, ChocolateyConfiguration>(HandlePackageUninstall);
                    var beforeUninstallAction = new Action<PackageResult, ChocolateyConfiguration>(BeforeModifyAction);

                    results = _nugetService.Uninstall(config, action, beforeUninstallAction);
                }
                else
                {
                    results = PerformSourceRunnerFunction<IUninstallSourceRunner, ConcurrentDictionary<string, PackageResult>>(config, runner => runner.Uninstall(config, null));
                }

                if (results is null)
                {
                    throw new NotSupportedException("The '{0}' source does not support uninstalling packages".FormatWith(config.SourceType));
                }

                foreach (var result in results)
                {
                    packageUninstalls.GetOrAdd(result.Key, result.Value);
                }

                // not handled in the uninstall handler
                IEnumerable<GenericRegistryValue> environmentChanges;
                IEnumerable<GenericRegistryValue> environmentRemovals;
                LogEnvironmentChanges(config, environmentBefore, out environmentChanges, out environmentRemovals);
            }
            finally
            {
                var uninstallFailures = ReportActionSummary(packageUninstalls, "uninstalled");
                if (uninstallFailures != 0 && Environment.ExitCode == 0)
                {
                    Environment.ExitCode = 1;
                }

                if (uninstallFailures != 0)
                {
                    this.Log().Warn(@"
If a package uninstall is failing and/or you've already uninstalled the
 software outside of Chocolatey, you can attempt to run the command
 with `-n` to skip running a chocolateyUninstall script, additionally
 adding `--skip-autouninstaller` to skip an attempt to automatically
 remove system-installed software. Only the packaging files are removed
 and not things like software installed to Programs and Features.

If a package is failing because it is a dependency of another package
 or packages, then you may first need to consider if it needs to be
 removed as packages have dependencies for a reason. If
 you decide that you still want to remove it, head into
 `$env:ChocolateyInstall\lib` and find the package folder you want to
 be removed. Then delete the folder for the package. You should use
 this option only as a last resort.
 ");
                }

                RandomlyNotifyAboutLicensedFeatures(config);
            }

            return packageUninstalls;
        }

        private void ValidatePackageNames(ChocolateyConfiguration config)
        {
            foreach (var packageName in config.PackageNames.Split(';'))
            {
                if (packageName.EndsWith(NuGetConstants.PackageExtension))
                {
                    if (Uri.TryCreate(packageName, UriKind.Absolute, out var uri) && (uri.IsFile || uri.IsUnc))
                    {
                        ThrowInvalidPathError(uri.LocalPath, uri.IsUnc, config.CommandName);
                    }
                    else if (_fileSystem.FileExists(packageName))
                    {
                        var fullPath = _fileSystem.GetFullPath(packageName);

                        if (!string.IsNullOrWhiteSpace(fullPath) && Uri.TryCreate(fullPath, UriKind.Absolute, out uri))
                        {
                            ThrowInvalidPathError(uri.LocalPath, uri.IsUnc, config.CommandName);
                        }

                        ThrowInvalidPathError(fullPath, isUncPath: false, commandName: config.CommandName);
                    }
                    else
                    {
                        throw new ApplicationException("Package name cannot point directly to a local, or remote file. Please use the --source argument and point it to a local file directory, UNC directory path or a NuGet feed instead.");

                    }
                }
                else if (packageName.EndsWith(NuGetConstants.ManifestExtension))
                {
                    throw new ApplicationException("Package name cannot point directly to a package manifest file. Please create a package by running 'choco pack' on the .nuspec file first.");

                }
            }
        }

        private void ThrowInvalidPathError(string packageName, bool isUncPath, string commandName)
        {
            var sb = new StringBuilder("Package name cannot be a path to a file ");

            if (isUncPath)
            {
                sb.AppendLine("on a UNC location.")
                  .AppendLine()
                  .Append("To ")
                  .Append(commandName.ToLowerSafe())
                  .AppendLine(" a file in a UNC location, you may use:");
            }
            else
            {
                sb.AppendLine("on a remote, or local file system.")
                  .AppendLine()
                  .Append("To ")
                  .Append(commandName.ToLowerSafe())
                  .AppendLine(" a local, or remote file, you may use:");

            }

            BuildInstallExample(packageName, sb, commandName.ToLowerSafe());

            throw new ApplicationException(sb.AppendLine().ToString());
        }

        private void BuildInstallExample(string packageName, StringBuilder sb, string commandName)
        {
            var fileName = _fileSystem.GetFilenameWithoutExtension(packageName);
            var version = string.Empty;
            // We need to get the directory name in this way in case it is a UNC path.
            // Using normal way to get the directory name may trim out parts of the necessary path.
            var length = packageName.Length - _fileSystem.GetFileName(packageName).Length - 1;
            var directory = length > 0 ? packageName.Substring(0, length) : string.Empty;

            if (fileName.Contains('.'))
            {
                var originalFileName = fileName;
                fileName = string.Empty;

                while (fileName.Length < originalFileName.Length)
                {
                    if (NuGetVersion.TryParse(originalFileName.Substring(fileName.Length + 1), out var nugetVersion))
                    {
                        version = nugetVersion.ToNormalizedStringChecked();
                        break;
                    }

                    var index = originalFileName.IndexOf('.', fileName.Length + 1);

                    if (index <= 0)
                    {
                        fileName = originalFileName;
                        break;
                    }

                    fileName = originalFileName.Substring(0, index);
                }
            }

            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException(sb.ToString());
            }

            sb.Append("  choco ")
              .Append(commandName)
              .Append(' ')
              .Append(fileName.QuoteIfContainsSpaces());

            if (!string.IsNullOrWhiteSpace(version))
            {
                sb.AppendFormat(" --version=\"{0}\"", version);

                if (version.Contains('-'))
                {
                    sb.Append(" --prerelease");
                }
            }

            if (!string.IsNullOrWhiteSpace(directory))
            {
                sb.AppendFormat(" --source=\"{0}\"", directory);
            }
        }

        private int ReportActionSummary(ConcurrentDictionary<string, PackageResult> packageResults, string actionName)
        {
            var successes = packageResults.OrEmpty().Where(p => p.Value.Success && !p.Value.Inconclusive).OrderBy(p => p.Value.Name);
            var failures = packageResults.Count(p => !p.Value.Success);
            var warnings = packageResults.Count(p => p.Value.Warning);
            var rebootPackages = packageResults.Count(p => new[] { 1641, 3010 }.Contains(p.Value.ExitCode));
            this.Log().Warn(
                () => @"{0}{1} {2} {3}/{4} packages. {5}{0} See the log for details ({6}).".FormatWith(
                    Environment.NewLine,
                    ApplicationParameters.Name,
                    actionName,
                    successes.Count(),
                    packageResults.Count,
                    (failures > 0) ? failures + " packages failed." : string.Empty,
                    _fileSystem.CombinePaths(ApplicationParameters.LoggingLocation, ApplicationParameters.LoggingFile)
                    ));

            // summarize results when more than 5
            if (packageResults.Count >= 5 && successes.Count() != 0)
            {
                this.Log().Info("");
                this.Log().Warn("{0}{1}:".FormatWith(actionName.Substring(0, 1).ToUpper(), actionName.Substring(1)));
                foreach (var packageResult in successes.OrEmpty())
                {
                    this.Log().Info(" - {0} v{1}".FormatWith(packageResult.Value.Name, packageResult.Value.Version));
                }
            }

            if (warnings != 0)
            {
                this.Log().Info("");
                this.Log().Warn("Warnings:");
                foreach (var warning in packageResults.Where(p => p.Value.Warning).OrderBy(p => p.Value.Name).OrEmpty())
                {
                    var warningMessage = warning.Value.Messages.FirstOrDefault(m => m.MessageType == ResultType.Warn);
                    this.Log().Warn(" - {0}{1}".FormatWith(warning.Value.Name, warningMessage != null ? " - {0}".FormatWith(warningMessage.Message) : string.Empty));
                }
            }

            if (rebootPackages != 0)
            {
                this.Log().Info("");
                this.Log().Warn("Packages requiring reboot:");
                foreach (var reboot in packageResults.Where(p => new[] { 1641, 3010 }.Contains(p.Value.ExitCode)).OrderBy(p => p.Value.Name).OrEmpty())
                {
                    this.Log().Warn(" - {0}{1}".FormatWith(reboot.Value.Name, reboot.Value.ExitCode != 0 ? " (exit code {0})".FormatWith(reboot.Value.ExitCode) : string.Empty));
                }
                this.Log().Warn(ChocolateyLoggers.Important, @"
The recent package changes indicate a reboot is necessary.
 Please reboot at your earliest convenience.");
            }

            if (failures != 0)
            {
                this.Log().Info("");
                this.Log().Error("Failures");
                foreach (var failure in packageResults.Where(p => !p.Value.Success).OrderBy(p => p.Value.Name).OrEmpty())
                {
                    var errorMessage = failure.Value.Messages.FirstOrDefault(m => m.MessageType == ResultType.Error);
                    this.Log().Error(
                        " - {0}{1}{2}".FormatWith(
                            failure.Value.Name,
                            failure.Value.ExitCode != 0 ? " (exited {0})".FormatWith(failure.Value.ExitCode) : string.Empty,
                            errorMessage != null ? " - {0}".FormatWith(errorMessage.Message) : string.Empty
                            ));
                }
            }

            return failures;
        }

        public virtual void HandlePackageUninstall(PackageResult packageResult, ChocolateyConfiguration config)
        {
            //These items only apply to windows systems.
            if (config.Information.PlatformType == PlatformType.Windows)
            {
                _shimgenService.Uninstall(config, packageResult);

                if (!config.SkipPackageInstallProvider)
                {
                    _powershellService.Uninstall(config, packageResult);
                }

                if (packageResult.Success)
                {
                    _autoUninstallerService.Run(packageResult, config);
                }

                // we don't care about the exit code
                CommandExecutor.ExecuteStatic(_shutdownExe, "/a", config.CommandExecutionTimeoutSeconds, _fileSystem.GetCurrentDirectory(), (s, e) => { }, (s, e) => { }, false, false);
            }
            else
            {
                this.Log().Info(ChocolateyLoggers.Important, () => " Skipping PowerShell, shimgen, and autoUninstaller portions of the uninstall due to non-Windows.");
            }

            if (packageResult.Success)
            {
                //todo: #2568 v2 clean up package information store for things no longer installed (call it compact?)
                UninstallCleanup(config, packageResult);
            }
            else
            {
                var oldPrompt = config.PromptForConfirmation;

                this.Log().Error(ChocolateyLoggers.Important, "{0} {1} not successful.".FormatWith(packageResult.Name, "uninstall"));
                HandleFailedOperation(config, packageResult, movePackageToFailureLocation: true, attemptRollback: true);
            }

            if (_rebootExitCodes.Contains(packageResult.ExitCode))
            {
                if (config.Features.ExitOnRebootDetected)
                {
                    Environment.ExitCode = ApplicationParameters.ExitCodes.ErrorInstallSuspend;
                    this.Log().Warn(ChocolateyLoggers.Important, @"Chocolatey has detected a pending reboot after uninstalling
package '{0}' - stopping further execution".FormatWith(packageResult.Name));

                    throw new ApplicationException("Reboot required before continuing. Reboot and run the same command again.");
                }
            }

            if (!packageResult.Success)
            {
                // throw an error so that NuGet Service doesn't attempt to continue with package removal
                throw new ApplicationException("{0} {1} not successful.".FormatWith(packageResult.Name, "uninstall"));
            }
        }

        private void UninstallCleanup(ChocolateyConfiguration config, PackageResult packageResult)
        {
            if (config.Features.RemovePackageInformationOnUninstall) _packageInfoService.Remove(packageResult.PackageMetadata);

            EnsureBadPackagesPathIsClean(config, packageResult);
            RemoveBackupIfExists(packageResult);
            HandleExtensionPackages(config, packageResult);
            HandleTemplatePackages(config, packageResult);
            HandleHookPackages(config, packageResult);

            if (config.Force)
            {
                var packageDirectory = packageResult.InstallLocation;

                if (string.IsNullOrWhiteSpace(packageDirectory) || !_fileSystem.DirectoryExists(packageDirectory)) return;

                if (packageDirectory.IsEqualTo(ApplicationParameters.InstallLocation) || packageDirectory.IsEqualTo(ApplicationParameters.PackagesLocation))
                {
                    packageResult.Messages.Add(
                        new ResultMessage(
                            ResultType.Error,
                            "Install location is not specific enough, cannot force remove directory:{0} Erroneous install location captured as '{1}'".FormatWith(Environment.NewLine, packageResult.InstallLocation)
                            )
                        );
                    return;
                }

                FaultTolerance.TryCatchWithLoggingException(
                    () => _fileSystem.DeleteDirectoryChecked(packageDirectory, recursive: true),
                    "Attempted to remove '{0}' but had an error".FormatWith(packageDirectory),
                    logWarningInsteadOfError: true);
            }
        }

        // This should probably be split into install(/upgrade)/uninstall methods with shared logic placed in helper method(s).
        private void HandleExtensionPackages(ChocolateyConfiguration config, PackageResult packageResult)
        {
            if (packageResult == null) return;
            if (!packageResult.Name.ToLowerSafe().EndsWith(".extension") && !packageResult.Name.ToLowerSafe().EndsWith(".extensions")) return;

            _fileSystem.EnsureDirectoryExists(ApplicationParameters.ExtensionsLocation);
            var extensionsFolderName = packageResult.Name.ToLowerSafe().Replace(".extensions", string.Empty).Replace(".extension", string.Empty);
            var packageExtensionsInstallDirectory = _fileSystem.CombinePaths(ApplicationParameters.ExtensionsLocation, extensionsFolderName);

            RemoveExtensionFolder(packageExtensionsInstallDirectory);
            // don't name your package *.extension.extension
            RemoveExtensionFolder(packageExtensionsInstallDirectory + ".extension");
            RemoveExtensionFolder(packageExtensionsInstallDirectory + ".extensions");

            if (!config.CommandName.IsEqualTo(CommandNameType.Uninstall.ToStringSafe()))
            {
                if (packageResult.InstallLocation == null) return;

                _fileSystem.EnsureDirectoryExists(packageExtensionsInstallDirectory);
                var extensionsFolder = _fileSystem.CombinePaths(packageResult.InstallLocation, "extensions");
                var extensionFolderToCopy = _fileSystem.DirectoryExists(extensionsFolder) ? extensionsFolder : packageResult.InstallLocation;

                FaultTolerance.TryCatchWithLoggingException(
                    () => _fileSystem.CopyDirectory(extensionFolderToCopy, packageExtensionsInstallDirectory, overwriteExisting: true),
                    "Attempted to copy{0} '{1}'{0} to '{2}'{0} but had an error".FormatWith(Environment.NewLine, extensionFolderToCopy, packageExtensionsInstallDirectory));

                string logMessage = " Installed/updated {0} extensions.".FormatWith(extensionsFolderName);
                this.Log().Warn(logMessage);
                packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));

                Environment.SetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyPackageInstallLocation, packageExtensionsInstallDirectory, EnvironmentVariableTarget.Process);
            }
            else
            {
                string logMessage = " Uninstalled {0} extensions.".FormatWith(extensionsFolderName);
                this.Log().Warn(logMessage);
                packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));
            }
        }

        private void RemoveExtensionFolder(string packageExtensionsDirectory)
        {
            if (!_fileSystem.DirectoryExists(packageExtensionsDirectory)) return;

            // remove old dll files files
            foreach (var oldDllFile in _fileSystem.GetFiles(packageExtensionsDirectory, "*.dll.old", SearchOption.AllDirectories).OrEmpty())
            {
                FaultTolerance.TryCatchWithLoggingException(
                    () => _fileSystem.DeleteFile(oldDllFile),
                    "Attempted to remove '{0}' but had an error".FormatWith(oldDllFile),
                    throwError: false,
                    logWarningInsteadOfError: true);
            }

            // rename possibly locked dll files
            foreach (var dllFile in _fileSystem.GetFiles(packageExtensionsDirectory, "*.dll", SearchOption.AllDirectories).OrEmpty())
            {
                FaultTolerance.TryCatchWithLoggingException(
                    () => _fileSystem.MoveFile(dllFile, dllFile + ".old"),
                    "Attempted to rename '{0}' but had an error".FormatWith(dllFile));
            }

            FaultTolerance.TryCatchWithLoggingException(
                () =>
                {
                    foreach (var file in _fileSystem.GetFiles(packageExtensionsDirectory, "*.*", SearchOption.AllDirectories).OrEmpty().Where(f => !f.EndsWith(".dll.old")))
                    {
                        FaultTolerance.TryCatchWithLoggingException(
                            () => _fileSystem.DeleteFile(file),
                            "Attempted to remove '{0}' but had an error".FormatWith(file),
                            throwError: false,
                            logWarningInsteadOfError: true);
                    }
                },
                "Attempted to remove '{0}' but had an error".FormatWith(packageExtensionsDirectory),
                throwError: false,
                logWarningInsteadOfError: true);
        }

        // This should probably be split into install(/upgrade)/uninstall methods with shared logic placed in helper method(s).
        private void HandleTemplatePackages(ChocolateyConfiguration config, PackageResult packageResult)
        {
            if (packageResult == null) return;
            if (!packageResult.Name.ToLowerSafe().EndsWith(".template")) return;

            _fileSystem.EnsureDirectoryExists(ApplicationParameters.TemplatesLocation);
            var templateFolderName = packageResult.Name.ToLowerSafe().Replace(".template", string.Empty);
            var installTemplatePath = _fileSystem.CombinePaths(ApplicationParameters.TemplatesLocation, templateFolderName);

            FaultTolerance.TryCatchWithLoggingException(
                () => _fileSystem.DeleteDirectoryChecked(installTemplatePath, recursive: true),
                "Attempted to remove '{0}' but had an error".FormatWith(installTemplatePath));

            if (!config.CommandName.IsEqualTo(CommandNameType.Uninstall.ToStringSafe()))
            {
                if (packageResult.InstallLocation == null) return;

                _fileSystem.EnsureDirectoryExists(installTemplatePath);
                var templatesPath = _fileSystem.CombinePaths(packageResult.InstallLocation, "templates");
                var templatesFolderToCopy = _fileSystem.DirectoryExists(templatesPath) ? templatesPath : packageResult.InstallLocation;

                FaultTolerance.TryCatchWithLoggingException(
                    () =>
                    {
                        _fileSystem.CopyDirectory(templatesFolderToCopy, installTemplatePath, overwriteExisting: true);
                        foreach (var nuspecFile in _fileSystem.GetFiles(installTemplatePath, "*.nuspec.template").OrEmpty())
                        {
                            _fileSystem.MoveFile(nuspecFile, nuspecFile.Replace(".nuspec.template", ".nuspec"));
                        }
                    },
                    "Attempted to copy{0} '{1}'{0} to '{2}'{0} but had an error".FormatWith(Environment.NewLine, templatesFolderToCopy, installTemplatePath));

                string logMessage = " Installed/updated {0} template.".FormatWith(templateFolderName);
                this.Log().Warn(logMessage);
                packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));

                Environment.SetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyPackageInstallLocation, installTemplatePath, EnvironmentVariableTarget.Process);
            }
            else
            {
                string logMessage = " Uninstalled {0} template.".FormatWith(templateFolderName);
                this.Log().Warn(logMessage);
                packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));
            }
        }

        private void EnsureBadPackagesPathIsClean(ChocolateyConfiguration config, PackageResult packageResult)
        {
            if (packageResult.InstallLocation == null) return;

            FaultTolerance.TryCatchWithLoggingException(
                () =>
                {
                    string badPackageInstallPath = packageResult.InstallLocation.Replace(ApplicationParameters.PackagesLocation, ApplicationParameters.PackageFailuresLocation);
                    if (_fileSystem.DirectoryExists(badPackageInstallPath))
                    {
                        _fileSystem.DeleteDirectory(badPackageInstallPath, recursive: true);
                    }
                },
                "Attempted to delete bad package install path if existing. Had an error");
        }

        private void HandleFailedOperation(ChocolateyConfiguration config, PackageResult packageResult, bool movePackageToFailureLocation, bool attemptRollback)
        {
            if (Environment.ExitCode == 0) Environment.ExitCode = 1;

            foreach (var message in packageResult.Messages.Where(m => m.MessageType == ResultType.Error))
            {
                this.Log().Error(message.Message);
            }

            if (attemptRollback || movePackageToFailureLocation)
            {
                var packageDirectory = packageResult.InstallLocation;
                if (packageDirectory.IsEqualTo(ApplicationParameters.InstallLocation) || packageDirectory.IsEqualTo(ApplicationParameters.PackagesLocation))
                {
                    this.Log().Error(ChocolateyLoggers.Important, @"
Package location is not specific enough, cannot move bad package or
 rollback the previous version. Erroneous install location captured as
 '{0}'

ATTENTION: You must take manual action to remove {1} from
 {2}. It will show incorrectly as installed
 until you do. To remove, you can simply delete the folder in question.
".FormatWith(packageResult.InstallLocation, packageResult.Name, ApplicationParameters.PackagesLocation));
                }
                else
                {
                    if (movePackageToFailureLocation) MovePackageToFailedPackagesLocation(packageResult);

                    if (attemptRollback) RestorePreviousPackageVersion(config, packageResult);
                }
            }
        }

        private bool HasSourceType(string sourceType, IEnumerable<IAlternativeSourceRunner> alternativeSourceRunners)
        {
            return alternativeSourceRunners.Any(s => s.SourceType == sourceType || s.SourceType == sourceType + "s");
        }

        private void MovePackageToFailedPackagesLocation(PackageResult packageResult)
        {
            _fileSystem.EnsureDirectoryExists(ApplicationParameters.PackageFailuresLocation);

            if (!string.IsNullOrWhiteSpace(packageResult.InstallLocation) && _fileSystem.DirectoryExists(packageResult.InstallLocation))
            {
                var normalizedVersion = new NuGetVersion(packageResult.Version).ToNormalizedStringChecked();
                var failuresFolder = _fileSystem.CombinePaths(ApplicationParameters.PackageFailuresLocation, packageResult.Name, normalizedVersion);

                if (_filesService.MovePackageUsingBackupStrategy(packageResult.InstallLocation, failuresFolder, restoreSource: false))
                {
                    this.Log().Error("Could not move the bad package to the failure directory. It may show as installed.{0} {1}{0}", Environment.NewLine, packageResult.InstallLocation);
                }
            }
        }

        private void RestorePreviousPackageVersion(ChocolateyConfiguration config, PackageResult packageResult)
        {
            if (packageResult.InstallLocation == null) return;

            var normalizedVersion = new NuGetVersion(packageResult.Version).ToNormalizedStringChecked();

            var rollbackDirectory = _fileSystem.CombinePaths(ApplicationParameters.PackageBackupLocation, packageResult.Name, normalizedVersion);

            if (!_fileSystem.DirectoryExists(rollbackDirectory))
            {
                //search for folder
                var path = _fileSystem.CombinePaths(ApplicationParameters.PackageBackupLocation, packageResult.Name);
                var possibleRollbacks = _fileSystem.GetDirectories(path, normalizedVersion);

                if (possibleRollbacks == null || possibleRollbacks.Count() == 0)
                {
                    possibleRollbacks = _fileSystem.GetDirectories(path, "*");
                }

                if (possibleRollbacks != null && possibleRollbacks.Count() != 0)
                {
                    rollbackDirectory = possibleRollbacks.OrderByDescending(p => p).DefaultIfEmpty(string.Empty).FirstOrDefault();
                }
            }

            rollbackDirectory = _fileSystem.GetFullPath(rollbackDirectory);

            if (string.IsNullOrWhiteSpace(rollbackDirectory) || !_fileSystem.DirectoryExists(rollbackDirectory)) return;
            if (!rollbackDirectory.StartsWith(ApplicationParameters.PackageBackupLocation) || rollbackDirectory.IsEqualTo(ApplicationParameters.PackageBackupLocation)) return;

            this.Log().Debug("Attempting rollback");

            var rollback = true;
            var shouldPrompt = config.PromptForConfirmation;

            // if user canceled, then automatically rollback without prompting.
            //MSI ERROR_INSTALL_USEREXIT - 1602 - https://support.microsoft.com/en-us/kb/304888 / https://msdn.microsoft.com/en-us/library/aa376931.aspx
            //ERROR_INSTALL_CANCEL - 15608 - https://msdn.microsoft.com/en-us/library/windows/desktop/ms681384.aspx
            if (Environment.ExitCode == 15608 || Environment.ExitCode == 1602)
            {
                shouldPrompt = false;
            }

            if (shouldPrompt)
            {
                var selection = InteractivePrompt.PromptForConfirmation(
                    " Unsuccessful operation for {0}.{1}  Rollback to previous version (package files only)?".FormatWith(packageResult.Name, Environment.NewLine),
                    new[] { "yes", "no" },
                    defaultChoice: null,
                    requireAnswer: true,
                    allowShortAnswer: true,
                    shortPrompt: true
                    );
                if (selection.IsEqualTo("no")) rollback = false;
            }

            if (rollback)
            {
             var destination = _fileSystem.CombinePaths(ApplicationParameters.PackagesLocation, packageResult.Identity.Id);
                _filesService.MovePackageUsingBackupStrategy(rollbackDirectory, destination, restoreSource: false);
            }

            RemoveBackupIfExists(packageResult);
        }

        private void RemoveBackupIfExists(PackageResult packageResult)
        {
            _nugetService.EnsureBackupDirectoryRemoved(packageResult.Name);
        }

        public virtual void MarkPackagePending(PackageResult packageResult, ChocolateyConfiguration config)
        {
            var packageDirectory = packageResult.InstallLocation;
            if (string.IsNullOrWhiteSpace(packageDirectory)) return;
            if (packageDirectory.IsEqualTo(ApplicationParameters.InstallLocation) || packageDirectory.IsEqualTo(ApplicationParameters.PackagesLocation))
            {
                packageResult.Messages.Add(
                    new ResultMessage(
                        ResultType.Error,
                        "Install location is not specific enough, cannot run set package to pending:{0} Erroneous install location captured as '{1}'".FormatWith(Environment.NewLine, packageResult.InstallLocation)
                        )
                    );

                return;
            }

            var pendingFile = _fileSystem.CombinePaths(packageDirectory, ApplicationParameters.PackagePendingFileName);
            _fileSystem.WriteFile(pendingFile, "{0}".FormatWith(packageResult.Name));
            if (ApplicationParameters.LockTransactionalInstallFiles)
            {
                _pendingLocks.Add(packageResult.Name.ToLowerSafe(), _fileSystem.OpenFileExclusive(pendingFile));
            }
        }

        public virtual void UnmarkPackagePending(PackageResult packageResult, ChocolateyConfiguration config)
        {
            var packageDirectory = packageResult.InstallLocation;
            if (string.IsNullOrWhiteSpace(packageDirectory)) return;
            if (packageDirectory.IsEqualTo(ApplicationParameters.InstallLocation) || packageDirectory.IsEqualTo(ApplicationParameters.PackagesLocation))
            {
                packageResult.Messages.Add(
                    new ResultMessage(
                        ResultType.Error,
                        "Install location is not specific enough, cannot run set package to pending:{0} Erroneous install location captured as '{1}'".FormatWith(Environment.NewLine, packageResult.InstallLocation)
                        )
                    );

                return;
            }

            var pendingFile = _fileSystem.CombinePaths(packageDirectory, ApplicationParameters.PackagePendingFileName);
            var lockName = packageResult.Name.ToLowerSafe();
            if (_pendingLocks.ContainsKey(lockName))
            {
                var fileLock = _pendingLocks[lockName];
                _pendingLocks.Remove(lockName);
                fileLock.Close();
                fileLock.Dispose();
            }

            if (packageResult.Success && _fileSystem.FileExists(pendingFile)) _fileSystem.DeleteFile(pendingFile);
        }

        private IEnumerable<GenericRegistryValue> GetInitialEnvironment(ChocolateyConfiguration config, bool allowLogging = true)
        {
            if (config.Information.PlatformType != PlatformType.Windows) return Enumerable.Empty<GenericRegistryValue>();
            var environmentBefore = _registryService.GetEnvironmentValues();

            if (allowLogging && config.Features.LogEnvironmentValues)
            {
                this.Log().Debug("Current environment values (may contain sensitive data):");
                foreach (var environmentValue in environmentBefore.OrEmpty())
                {
                    this.Log().Debug(@"  * '{0}'='{1}' ('{2}')".FormatWith(
                        environmentValue.Name.EscapeCurlyBraces(),
                        environmentValue.Value.EscapeCurlyBraces(),
                        environmentValue.ParentKeyName.ToLowerSafe().Contains("hkey_current_user") ? "User" : "Machine"));
                }
            }
            return environmentBefore;
        }

        private void LogEnvironmentChanges(ChocolateyConfiguration config, IEnumerable<GenericRegistryValue> environmentBefore, out IEnumerable<GenericRegistryValue> environmentChanges, out IEnumerable<GenericRegistryValue> environmentRemovals)
        {
            if (config.Information.PlatformType != PlatformType.Windows)
            {
                environmentChanges = Enumerable.Empty<GenericRegistryValue>();
                environmentRemovals = Enumerable.Empty<GenericRegistryValue>();

                return;
            }

            var environmentAfer = _registryService.GetEnvironmentValues();
            environmentChanges = _registryService.GetNewAndModifiedEnvironmentValues(environmentBefore, environmentAfer);
            environmentRemovals = _registryService.GetRemovedEnvironmentValues(environmentBefore, environmentAfer);
            var hasEnvironmentChanges = environmentChanges.Count() != 0;
            var hasEnvironmentRemovals = environmentRemovals.Count() != 0;
            if (hasEnvironmentChanges || hasEnvironmentRemovals)
            {
                this.Log().Warn(ChocolateyLoggers.Important, @"Environment Vars (like PATH) have changed. Close/reopen your shell to
 see the changes (or in powershell/cmd.exe just type `refreshenv`).");

                if (!config.Features.LogEnvironmentValues)
                {
                    this.Log().Debug(@"Logging of values is not turned on by default because it
 could potentially expose sensitive data. If you understand the risk,
 please see `choco feature -h` for information to turn it on.");
                }

                if (hasEnvironmentChanges)
                {
                    this.Log().Debug(@"The following values have been added/changed (may contain sensitive data):");
                    foreach (var difference in environmentChanges.OrEmpty())
                    {
                        this.Log().Debug(@"  * {0}='{1}' ({2})".FormatWith(
                            difference.Name.EscapeCurlyBraces(),
                            config.Features.LogEnvironmentValues ? difference.Value.EscapeCurlyBraces() : "[REDACTED]",
                             difference.ParentKeyName.ToLowerSafe().Contains("hkey_current_user") ? "User" : "Machine"
                            ));
                    }
                }

                if (hasEnvironmentRemovals)
                {
                    this.Log().Debug(@"The following values have been removed:");
                    foreach (var difference in environmentRemovals.OrEmpty())
                    {
                        this.Log().Debug(@"  * {0}='{1}' ({2})".FormatWith(
                            difference.Name.EscapeCurlyBraces(),
                            config.Features.LogEnvironmentValues ? difference.Value.EscapeCurlyBraces() : "[REDACTED]",
                            difference.ParentKeyName.ToLowerSafe().Contains("hkey_current_user") ? "User" : "Machine"
                            ));
                    }
                }
            }
        }

        private TSourceRunner GetSourceRunner<TSourceRunner>(string sourceType, IEnumerable<TSourceRunner> alternativeSourceRunners)
            where TSourceRunner : class, IAlternativeSourceRunner
        {
            return alternativeSourceRunners.FirstOrDefault(s => s.SourceType == sourceType || s.SourceType == sourceType + "s");
        }

        // This should probably be split into install(/upgrade)/uninstall methods with shared logic placed in helper method(s).
        private void HandleHookPackages(ChocolateyConfiguration config, PackageResult packageResult)
        {
            if (packageResult == null) return;
            if (!packageResult.Name.ToLowerSafe().EndsWith(ApplicationParameters.HookPackageIdExtension)) return;

            _fileSystem.EnsureDirectoryExists(ApplicationParameters.HooksLocation);
            var hookFolderName = packageResult.Name.ToLowerSafe().Replace(ApplicationParameters.HookPackageIdExtension, string.Empty);
            var installHookPath = _fileSystem.CombinePaths(ApplicationParameters.HooksLocation, hookFolderName);

            FaultTolerance.TryCatchWithLoggingException(
                () =>
                {
                    _fileSystem.DeleteDirectoryChecked(installHookPath, recursive: true);
                },
                "Attempted to remove '{0}' but had an error".FormatWith(installHookPath));

            if (!config.CommandName.IsEqualTo(CommandNameType.Uninstall.ToStringSafe()))
            {
                if (packageResult.InstallLocation == null) return;

                _fileSystem.EnsureDirectoryExists(installHookPath);
                var hookPath = _fileSystem.CombinePaths(packageResult.InstallLocation, "hook");
                var hookFolderToCopy = _fileSystem.DirectoryExists(hookPath) ? hookPath : packageResult.InstallLocation;

                FaultTolerance.TryCatchWithLoggingException(
                    () =>
                    {
                        _fileSystem.CopyDirectory(hookFolderToCopy, installHookPath, overwriteExisting: true);
                    },
                    "Attempted to copy{0} '{1}'{0} to '{2}'{0} but had an error".FormatWith(Environment.NewLine, hookFolderToCopy, installHookPath));

                string logMessage = " Installed/updated {0} hook.".FormatWith(hookFolderName);
                this.Log().Warn(logMessage);
                packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));

                Environment.SetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyPackageInstallLocation, installHookPath, EnvironmentVariableTarget.Process);
            }
            else
            {
                string logMessage = " Uninstalled {0} hook.".FormatWith(hookFolderName);
                this.Log().Warn(logMessage);
                packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));
            }
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual int count_run(ChocolateyConfiguration config)
            => Count(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void list_noop(ChocolateyConfiguration config)
            => ListDryRun(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual IEnumerable<PackageResult> list_run(ChocolateyConfiguration config)
            => List(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void pack_noop(ChocolateyConfiguration config)
            => PackDryRun(config);

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
        public void install_noop(ChocolateyConfiguration config)
            => InstallDryRun(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void randomly_notify_about_pro_business(ChocolateyConfiguration config, string message = null)
            => RandomlyNotifyAboutLicensedFeatures(config, message);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_package_result(PackageResult packageResult, ChocolateyConfiguration config, CommandNameType commandName)
            => HandlePackageResult(packageResult, config, commandName);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected virtual ChocolateyPackageInformation get_package_information(PackageResult packageResult, ChocolateyConfiguration config)
            => GetPackageInformation(packageResult, config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected virtual void update_package_information(ChocolateyPackageInformation pkgInfo)
            => UpdatePackageInformation(pkgInfo);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config)
            => Install(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void outdated_noop(ChocolateyConfiguration config)
            => OutdatedDryRun(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void outdated_run(ChocolateyConfiguration config)
            => Outdated(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void upgrade_noop(ChocolateyConfiguration config)
            => UpgradeDryRun(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config)
            => Upgrade(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void uninstall_noop(ChocolateyConfiguration config)
            => UninstallDryRun(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config)
            => Uninstall(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_package_uninstall(PackageResult packageResult, ChocolateyConfiguration config)
            => HandlePackageUninstall(packageResult, config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void set_pending(PackageResult packageResult, ChocolateyConfiguration config)
            => MarkPackagePending(packageResult, config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void remove_pending(PackageResult packageResult, ChocolateyConfiguration config)
            => UnmarkPackagePending(packageResult, config);
#pragma warning restore IDE1006
    }
}
