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
    using System.IO;
    using System.Linq;
    using System.Text;
    using builders;
    using commandline;
    using configuration;
    using domain;
    using events;
    using infrastructure.commands;
    using infrastructure.events;
    using infrastructure.services;
    using logging;
    using NuGet;
    using nuget;
    using platforms;
    using results;
    using tolerance;
    using IFileSystem = filesystem.IFileSystem;

    public class ChocolateyPackageService : IChocolateyPackageService
    {
        private readonly INugetService _nugetService;
        private readonly IPowershellService _powershellService;
        private readonly IEnumerable<ISourceRunner> _sourceRunners;
        private readonly IShimGenerationService _shimgenService;
        private readonly IFileSystem _fileSystem;
        private readonly IRegistryService _registryService;
        private readonly IChocolateyPackageInformationService _packageInfoService;
        private readonly IFilesService _filesService;
        private readonly IAutomaticUninstallerService _autoUninstallerService;
        private readonly IXmlService _xmlService;
        private readonly IConfigTransformService _configTransformService;
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
 Your support ensures an active community, keeps Chocolatey tip top,
 plus it nets you some awesome features!
 https://chocolatey.org/compare",
@"
Did you know some organizations use Chocolatey completely internally
 without using the community repository or downloads from the internet?
 Wait until you see how Package Builder and Package Internalizer can
 help you achieve more, quicker and easier! Get your trial started
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
        private const string PRO_BUSINESS_LIST_MESSAGE = @"
Did you know Pro / Business automatically syncs with Programs and
 Features? Learn more about Package Synchronizer at
 https://chocolatey.org/compare";

        private readonly string _shutdownExe = Environment.ExpandEnvironmentVariables("%systemroot%\\System32\\shutdown.exe");

        // Hold a list of exit codes that are known to be related to reboots
        // 1641 - restart initiated
        // 3010 - restart required
        private readonly List<int> _rebootExitCodes = new List<int> { 1641, 3010 };

        public ChocolateyPackageService(INugetService nugetService, IPowershellService powershellService,
            IEnumerable<ISourceRunner> sourceRunners, IShimGenerationService shimgenService,
            IFileSystem fileSystem, IRegistryService registryService,
            IChocolateyPackageInformationService packageInfoService, IFilesService filesService,
            IAutomaticUninstallerService autoUninstallerService, IXmlService xmlService,
            IConfigTransformService configTransformService)
        {
            _nugetService = nugetService;
            _powershellService = powershellService;
            _sourceRunners = sourceRunners;
            _shimgenService = shimgenService;
            _fileSystem = fileSystem;
            _registryService = registryService;
            _packageInfoService = packageInfoService;
            _filesService = filesService;
            _autoUninstallerService = autoUninstallerService;
            _xmlService = xmlService;
            _configTransformService = configTransformService;
        }

        public virtual void ensure_source_app_installed(ChocolateyConfiguration config)
        {
            perform_source_runner_action(config, r => r.ensure_source_app_installed(config, (packageResult) => handle_package_result(packageResult, config, CommandNameType.install)));
        }

        public virtual int count_run(ChocolateyConfiguration config)
        {
            return perform_source_runner_function(config, r => r.count_run(config));
        }

        private void perform_source_runner_action(ChocolateyConfiguration config, Action<ISourceRunner> action)
        {
            var runner = _sourceRunners.FirstOrDefault(r => r.SourceType == config.SourceType);
            if (runner != null && action != null)
            {
                action.Invoke(runner);
            }
            else
            {
                this.Log().Warn("No runner was found that implements source type '{0}' or action was missing".format_with(config.SourceType.to_string()));
            }
        }

        private T perform_source_runner_function<T>(ChocolateyConfiguration config, Func<ISourceRunner, T> function)
        {
            var runner = _sourceRunners.FirstOrDefault(r => r.SourceType == config.SourceType);
            if (runner != null && function != null)
            {
                return function.Invoke(runner);
            }

            this.Log().Warn("No runner was found that implements source type '{0}' or function was missing.".format_with(config.SourceType.to_string()));
            return default(T);
        }

        public void list_noop(ChocolateyConfiguration config)
        {
            perform_source_runner_action(config, r => r.list_noop(config));
            randomly_notify_about_pro_business(config, PRO_BUSINESS_LIST_MESSAGE);
        }

        public virtual IEnumerable<PackageResult> list_run(ChocolateyConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.Sources) && !config.ListCommand.LocalOnly)
            {
                this.Log().Error(ChocolateyLoggers.Important, @"Unable to search for packages when there are no sources enabled for
 packages and none were passed as arguments.");
                Environment.ExitCode = 1;
                yield break;
            }

            if (config.RegularOutput) this.Log().Debug(() => "Searching for package information");

            var packages = new List<IPackage>();

            foreach (var package in perform_source_runner_function(config, r => r.list_run(config)))
            {
                if (config.SourceType == SourceType.normal)
                {
                    if (!config.ListCommand.IncludeRegistryPrograms)
                    {
                        yield return package;
                    }

                    if (config.ListCommand.LocalOnly && config.ListCommand.IncludeRegistryPrograms && package.Package != null)
                    {
                        packages.Add(package.Package);
                    }
                }
            }

            if (config.RegularOutput)
            {
                if (config.ListCommand.LocalOnly && config.ListCommand.IncludeRegistryPrograms)
                {
                    foreach (var installed in report_registry_programs(config, packages))
                    {
                        yield return installed;
                    }
                }
            }

            randomly_notify_about_pro_business(config, PRO_BUSINESS_LIST_MESSAGE);
        }

        private IEnumerable<PackageResult> report_registry_programs(ChocolateyConfiguration config, IEnumerable<IPackage> list)
        {
            var itemsToRemoveFromMachine = list.Select(package => _packageInfoService.get_package_information(package)).
                                                Where(p => p.RegistrySnapshot != null).
                                                Select(p => p.RegistrySnapshot.RegistryKeys.FirstOrDefault()).
                                                Where(p => p != null).
                                                Select(p => p.DisplayName).ToList();

            var count = 0;
            var machineInstalled = _registryService.get_installer_keys().RegistryKeys.
                                                Where((p) => p.is_in_programs_and_features() && !itemsToRemoveFromMachine.Contains(p.DisplayName) && !p.KeyPath.contains("choco-")).
                                                OrderBy((p) => p.DisplayName).Distinct();
            this.Log().Info(() => "");
            foreach (var key in machineInstalled)
            {
                if (config.RegularOutput)
                {
                    this.Log().Info("{0}|{1}".format_with(key.DisplayName, key.DisplayVersion));
                    if (config.Verbose) this.Log().Info(" InstallLocation: {0}{1} Uninstall:{2}".format_with(key.InstallLocation.to_string().escape_curly_braces(), Environment.NewLine, key.UninstallString.to_string().escape_curly_braces()));
                }
                count++;

                yield return new PackageResult(key.DisplayName, key.DisplayName, key.InstallLocation);
            }

            if (config.RegularOutput)
            {
                this.Log().Warn(() => @"{0} applications not managed with Chocolatey.".format_with(count));
            }
        }

        public void pack_noop(ChocolateyConfiguration config)
        {
            if (config.SourceType != SourceType.normal)
            {
                this.Log().Warn(ChocolateyLoggers.Important, "This source doesn't provide a facility for packaging.");
                return;
            }

            _nugetService.pack_noop(config);
            randomly_notify_about_pro_business(config);
        }

        public virtual void pack_run(ChocolateyConfiguration config)
        {
            if (config.SourceType != SourceType.normal)
            {
                this.Log().Warn(ChocolateyLoggers.Important, "This source doesn't provide a facility for packaging.");
                return;
            }

            _nugetService.pack_run(config);
            randomly_notify_about_pro_business(config);
        }

        public void push_noop(ChocolateyConfiguration config)
        {
            if (config.SourceType != SourceType.normal)
            {
                this.Log().Warn(ChocolateyLoggers.Important, "This source doesn't provide a facility for pushing.");
                return;
            }

            _nugetService.push_noop(config);
            randomly_notify_about_pro_business(config);
        }

        public virtual void push_run(ChocolateyConfiguration config)
        {
            if (config.SourceType != SourceType.normal)
            {
                this.Log().Warn(ChocolateyLoggers.Important, "This source doesn't provide a facility for pushing.");
                return;
            }

            _nugetService.push_run(config);
            randomly_notify_about_pro_business(config);
        }

        public void install_noop(ChocolateyConfiguration config)
        {
            // each package can specify its own configuration values
            foreach (var packageConfig in set_config_from_package_names_and_packages_config(config, new ConcurrentDictionary<string, PackageResult>()).or_empty_list_if_null())
            {
                Action<PackageResult> action = null;
                if (packageConfig.SourceType == SourceType.normal)
                {
                    action = (pkg) => _powershellService.install_noop(pkg);
                }

                perform_source_runner_action(packageConfig, r => r.install_noop(packageConfig, action));
            }

            randomly_notify_about_pro_business(config);
        }

        /// <summary>
        /// Once every 10 runs or so, Chocolatey FOSS should inform the user of the Pro / Business versions.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="message">The message to send.</param>
        /// <remarks>We want it random enough not to be annoying, but informative enough for awareness.</remarks>
        public void randomly_notify_about_pro_business(ChocolateyConfiguration config, string message = null)
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
                        if (chosenMessage >= messageCount) chosenMessage = messageCount -1;
                        message = _proBusinessMessages[chosenMessage];
                    }

                    this.Log().Warn(ChocolateyLoggers.Important, message);
                }
            }
        }

        public virtual void handle_package_result(PackageResult packageResult, ChocolateyConfiguration config, CommandNameType commandName)
        {
            EnvironmentSettings.reset_environment_variables(config);
            set_pending(packageResult, config);

            this.Log().Info("{0} package files {1} completed. Performing other installation steps.".format_with(packageResult.Name, commandName.to_string()));

            var pkgInfo = get_package_information(packageResult, config);

            if (packageResult.Success && config.Information.PlatformType == PlatformType.Windows)
            {
                if (!config.SkipPackageInstallProvider)
                {
                    var installersBefore = _registryService.get_installer_keys();
                    var environmentBefore = get_environment_before(config, allowLogging: false);

                    var powerShellRan = _powershellService.install(config, packageResult);
                    if (powerShellRan)
                    {
                        // we don't care about the exit code
                        if (config.Information.PlatformType == PlatformType.Windows) CommandExecutor.execute_static(_shutdownExe, "/a", config.CommandExecutionTimeoutSeconds, _fileSystem.get_current_directory(), (s, e) => { }, (s, e) => { }, false, false);
                    }

                    var installersDifferences = _registryService.get_installer_key_differences(installersBefore, _registryService.get_installer_keys());
                    if (installersDifferences.RegistryKeys.Count != 0)
                    {
                        //todo v1 - note keys passed in
                        pkgInfo.RegistrySnapshot = installersDifferences;

                        var key = installersDifferences.RegistryKeys.FirstOrDefault();
                        if (key != null && key.HasQuietUninstall)
                        {
                            pkgInfo.HasSilentUninstall = true;
                            this.Log().Info("  {0} can be automatically uninstalled.".format_with(packageResult.Name));
                        }
                        else if (key != null)
                        {
                            this.Log().Info("  {0} may be able to be automatically uninstalled.".format_with(packageResult.Name));
                        }
                    }

                    IEnumerable<GenericRegistryValue> environmentChanges;
                    IEnumerable<GenericRegistryValue> environmentRemovals;
                    get_log_environment_changes(config, environmentBefore, out environmentChanges, out environmentRemovals);
                    //todo: record this with package info
                }

                _filesService.ensure_compatible_file_attributes(packageResult, config);
                _configTransformService.run(packageResult, config);

                //review: is this a Windows only kind of thing?
                pkgInfo.FilesSnapshot = _filesService.capture_package_files(packageResult, config);

                var is32Bit = !config.Information.Is64BitProcess || config.ForceX86;
                create_ignore_files_for_executables(packageResult.InstallLocation, !is32Bit);

                if (packageResult.Success) _shimgenService.install(config, packageResult);
            }
            else
            {
                if (config.Information.PlatformType != PlatformType.Windows) this.Log().Info(ChocolateyLoggers.Important, () => " Skipping Powershell and shimgen portions of the install due to non-Windows.");
            }

            if (packageResult.Success)
            {
                handle_extension_packages(config, packageResult);
                handle_template_packages(config, packageResult);
                pkgInfo.Arguments = capture_arguments(config, packageResult);
            }

            var toolsLocation = Environment.GetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyToolsLocation);
            if (!string.IsNullOrWhiteSpace(toolsLocation) && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyPackageInstallLocation)))
            {
                toolsLocation = _fileSystem.combine_paths(toolsLocation, packageResult.Name);
                if (_fileSystem.directory_exists(toolsLocation))
                {
                    Environment.SetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyPackageInstallLocation, toolsLocation, EnvironmentVariableTarget.Process);
                }
            }

            if (pkgInfo.RegistrySnapshot != null && pkgInfo.RegistrySnapshot.RegistryKeys.Any(k => !string.IsNullOrWhiteSpace(k.InstallLocation)))
            {
                var key = pkgInfo.RegistrySnapshot.RegistryKeys.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k.InstallLocation));
                if (key != null) Environment.SetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyPackageInstallLocation, key.InstallLocation, EnvironmentVariableTarget.Process);
            }

            update_package_information(pkgInfo);
            ensure_bad_package_path_is_clean(config, packageResult);
            EventManager.publish(new HandlePackageResultCompletedMessage(packageResult, config, commandName));

            remove_pending(packageResult, config);

            if(_rebootExitCodes.Contains(packageResult.ExitCode))
            {
                if(config.Features.ExitOnRebootDetected)
                {
                    Environment.ExitCode = ApplicationParameters.ExitCodes.ErrorInstallSuspend;
                    this.Log().Warn(ChocolateyLoggers.Important, @"Chocolatey has detected a pending reboot after installing/upgrading
package '{0}' - stopping further execution".format_with(packageResult.Name));

                    throw new ApplicationException("Reboot required before continuing. Reboot and run same command again.");
                }
            }

            if (!packageResult.Success)
            {
                this.Log().Error(ChocolateyLoggers.Important, "The {0} of {1} was NOT successful.".format_with(commandName.to_string(), packageResult.Name));
                handle_unsuccessful_operation(config, packageResult, movePackageToFailureLocation: true, attemptRollback: true);

                if (config.Features.StopOnFirstPackageFailure)
                {
                    throw new ApplicationException("Stopping further execution as {0} has failed {1}.".format_with(packageResult.Name, commandName.to_string()));
                }

                return;
            }

            remove_rollback_if_exists(packageResult);

            this.Log().Info(ChocolateyLoggers.Important, " The {0} of {1} was successful.".format_with(commandName.to_string(), packageResult.Name));

            var installLocation = Environment.GetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyPackageInstallLocation);
            var installerDetected = Environment.GetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyPackageInstallerType);
            if (!string.IsNullOrWhiteSpace(installLocation))
            {
                 this.Log().Info(ChocolateyLoggers.Important, "  Software installed to '{0}'".format_with(installLocation.escape_curly_braces()));
            }
            else if (!string.IsNullOrWhiteSpace(installerDetected))
            {
                this.Log().Info(ChocolateyLoggers.Important, @"  Software installed as '{0}', install location is likely default.".format_with(installerDetected));
            }
            else
            {
                this.Log().Info(ChocolateyLoggers.Important, @"  Software install location not explicitly set, could be in package or
  default install location if installer.");
            }
        }

        private void create_ignore_files_for_executables(string installLocation, bool is64Bit)
        {
            // If we are using a 64 bit architecure, we want to ignore exe's targetting x86
            // This is done by adding a .ignore file into the package folder for each exe to ignore
            var exeFiles32Bit = (_fileSystem.directory_exists(_fileSystem.combine_paths(installLocation, "tools\\x86")) ? _fileSystem.get_files(_fileSystem.combine_paths(installLocation, "tools\\x86"), pattern: "*.exe", option: SearchOption.AllDirectories) : new List<string>()).ToArray();
            var exeFiles64Bit = (_fileSystem.directory_exists(_fileSystem.combine_paths(installLocation, "tools\\x64")) ? _fileSystem.get_files(_fileSystem.combine_paths(installLocation, "tools\\x64"), pattern: "*.exe", option: SearchOption.AllDirectories) : new List<string>()).ToArray();

            // If 64bit, and there are only 32bit files, we should shim the 32bit versions,
            // therefore, don't ignore anything
            if (is64Bit && !exeFiles64Bit.Any() && exeFiles32Bit.Any())
            {
                return;
            }

            foreach (var exeFile in is64Bit ? exeFiles32Bit.or_empty_list_if_null() : exeFiles64Bit.or_empty_list_if_null())
            {
                _fileSystem.create_file(exeFile + ".ignore");
            }
        }

        protected virtual ChocolateyPackageInformation get_package_information(PackageResult packageResult, ChocolateyConfiguration config)
        {
            var pkgInfo = _packageInfoService.get_package_information(packageResult.Package);
            if (config.AllowMultipleVersions)
            {
                pkgInfo.IsSideBySide = true;
            }

            return pkgInfo;
        }

        protected virtual void update_package_information(ChocolateyPackageInformation pkgInfo)
        {
            _packageInfoService.save_package_information(pkgInfo);
        }

        private string capture_arguments(ChocolateyConfiguration config, PackageResult packageResult)
        {
            var arguments = new StringBuilder();

            // use the config to reconstruct

            //bug:sources are unable to be used - it's late in the process when a package is known
            //arguments.Append(" --source=\"'{0}'\"".format_with(config.Sources));

            if (config.Prerelease) arguments.Append(" --prerelease");
            if (config.IgnoreDependencies) arguments.Append(" --ignore-dependencies");
            if (config.ForceX86) arguments.Append(" --forcex86");

            if (!string.IsNullOrWhiteSpace(config.InstallArguments)) arguments.Append(" --install-arguments=\"'{0}'\"".format_with(config.InstallArguments));
            if (config.OverrideArguments) arguments.Append(" --override-arguments");
            if (config.ApplyInstallArgumentsToDependencies) arguments.Append(" --apply-install-arguments-to-dependencies");

            if (!string.IsNullOrWhiteSpace(config.PackageParameters)) arguments.Append(" --package-parameters=\"'{0}'\"".format_with(config.PackageParameters));
            if (config.ApplyPackageParametersToDependencies) arguments.Append(" --apply-package-parameters-to-dependencies");

            if (config.AllowDowngrade) arguments.Append(" --allow-downgrade");
            if (config.AllowMultipleVersions) arguments.Append(" --allow-multiple-versions");

            // most times folks won't want to skip automation scripts on upgrade
            //if (config.SkipPackageInstallProvider) arguments.Append(" --skip-automation-scripts");
            //if (config.UpgradeCommand.FailOnUnfound) arguments.Append(" --fail-on-unfound");

            if (!string.IsNullOrWhiteSpace(config.SourceCommand.Username)) arguments.Append(" --user=\"'{0}'\"".format_with(config.SourceCommand.Username));
            if (!string.IsNullOrWhiteSpace(config.SourceCommand.Password)) arguments.Append(" --password=\"'{0}'\"".format_with(config.SourceCommand.Password));
            if (!string.IsNullOrWhiteSpace(config.SourceCommand.Certificate)) arguments.Append(" --cert=\"'{0}'\"".format_with(config.SourceCommand.Certificate));
            if (!string.IsNullOrWhiteSpace(config.SourceCommand.CertificatePassword)) arguments.Append(" --certpassword=\"'{0}'\"".format_with(config.SourceCommand.CertificatePassword));

            // this should likely be limited
            //if (!config.Features.ChecksumFiles) arguments.Append(" --ignore-checksums");
            //if (!config.Features.AllowEmptyChecksums) arguments.Append(" --allow-empty-checksums");
            //if (!config.Features.AllowEmptyChecksumsSecure) arguments.Append(" --allow-empty-checksums-secure");
            //arguments.Append(config.Features.UsePackageExitCodes ? " --use-package-exit-codes" : " --ignore-package-exit-codes");

            //global options
            if (config.CommandExecutionTimeoutSeconds != ApplicationParameters.DefaultWaitForExitInSeconds)
            {
                arguments.Append(" --execution-timeout=\"'{0}'\"".format_with(config.CommandExecutionTimeoutSeconds));
            }

            if (!string.IsNullOrWhiteSpace(config.CacheLocation)) arguments.Append(" --cache-location=\"'{0}'\"".format_with(config.CacheLocation));
            if (config.Features.FailOnStandardError) arguments.Append(" --fail-on-standard-error");
            if (!config.Features.UsePowerShellHost) arguments.Append(" --use-system-powershell");

            return NugetEncryptionUtility.EncryptString(arguments.to_string());
        }

        public virtual ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config)
        {
            this.Log().Info(is_packages_config_file(config.PackageNames) ? @"Installing from config file:" : @"Installing the following packages:");
            this.Log().Info(ChocolateyLoggers.Important, @"{0}".format_with(config.PackageNames));

            var packageInstalls = new ConcurrentDictionary<string, PackageResult>();

            if (string.IsNullOrWhiteSpace(config.Sources))
            {
                this.Log().Error(ChocolateyLoggers.Important, @"Installation was NOT successful. There are no sources enabled for
 packages and none were passed as arguments.");
                Environment.ExitCode = 1;
                return packageInstalls;
            }

            this.Log().Info(@"By installing you accept licenses for the packages.");

            get_environment_before(config, allowLogging: true);

            try
            {
                foreach (var packageConfig in set_config_from_package_names_and_packages_config(config, packageInstalls).or_empty_list_if_null())
                {
                    Action<PackageResult> action = null;
                    if (packageConfig.SourceType == SourceType.normal)
                    {
                        action = (packageResult) => handle_package_result(packageResult, packageConfig, CommandNameType.install);
                    }

                    var results = perform_source_runner_function(packageConfig, r => r.install_run(packageConfig, action));

                    foreach (var result in results)
                    {
                        packageInstalls.GetOrAdd(result.Key, result.Value);
                    }
                }
            }
            finally
            {
                var installFailures = report_action_summary(packageInstalls, "installed");
                if (installFailures != 0 && Environment.ExitCode == 0)
                {
                    Environment.ExitCode = 1;
                }

                randomly_notify_about_pro_business(config);
            }

            return packageInstalls;
        }

        public void outdated_noop(ChocolateyConfiguration config)
        {
            this.Log().Info(@"
Would have determined packages that are out of date based on what is
 installed and what versions are available for upgrade.");
        }

        public virtual void outdated_run(ChocolateyConfiguration config)
        {
            if (config.SourceType != SourceType.normal)
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
            var outdatedPackages = _nugetService.get_outdated(config);
            config.RegularOutput = output;

            if (config.RegularOutput)
            {
                var upgradeWarnings = outdatedPackages.Count(p => p.Value.Warning);
                this.Log().Warn(() => @"{0}{1} has determined {2} package(s) are outdated. {3}".format_with(
                    Environment.NewLine,
                    ApplicationParameters.Name,
                    outdatedPackages.Count(p => p.Value.Success && !p.Value.Inconclusive),
                    upgradeWarnings == 0 ? string.Empty : "{0} {1} package(s) had warnings.".format_with(Environment.NewLine, upgradeWarnings)
                    ));

                if (upgradeWarnings != 0)
                {
                    this.Log().Warn(ChocolateyLoggers.Important, "Warnings:");
                    foreach (var warning in outdatedPackages.Where(p => p.Value.Warning).or_empty_list_if_null())
                    {
                        this.Log().Warn(ChocolateyLoggers.Important, " - {0}".format_with(warning.Value.Name));
                    }
                }
            }

            // oudated packages, return 2
            if (config.Features.UseEnhancedExitCodes && outdatedPackages.Count != 0 && Environment.ExitCode == 0)
            {
                Environment.ExitCode = 2;
            }

            randomly_notify_about_pro_business(config);
        }

        private IEnumerable<ChocolateyConfiguration> set_config_from_package_names_and_packages_config(ChocolateyConfiguration config, ConcurrentDictionary<string, PackageResult> packageInstalls)
        {
            // if there are any .config files, split those off of the config. Then return the config without those package names.
            foreach (var packageConfigFile in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null().Where(p => p.EndsWith(".config")).ToList())
            {
                config.PackageNames = config.PackageNames.Replace(packageConfigFile, string.Empty);

                foreach (var packageConfig in get_packages_from_config(packageConfigFile, config, packageInstalls).or_empty_list_if_null())
                {
                   yield return packageConfig;
                }
            }

            yield return config;
        }

        private bool contains_packages_config_file(string packageNames)
        {
            return packageNames.to_string().Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null().Any(p => p.EndsWith(".config", StringComparison.OrdinalIgnoreCase));
        }

        private bool is_packages_config_file(string packageNames)
        {
            return packageNames.to_string().EndsWith(".config", StringComparison.OrdinalIgnoreCase) && !packageNames.to_string().contains(";");
        }

        private IEnumerable<ChocolateyConfiguration> get_packages_from_config(string packageConfigFile, ChocolateyConfiguration config, ConcurrentDictionary<string, PackageResult> packageInstalls)
        {
            IList<ChocolateyConfiguration> packageConfigs = new List<ChocolateyConfiguration>();

            if (!_fileSystem.file_exists(_fileSystem.get_full_path(packageConfigFile)))
            {
                var logMessage = "Could not find '{0}' in the location specified.".format_with(packageConfigFile);
                this.Log().Error(ChocolateyLoggers.Important, logMessage);
                var results = packageInstalls.GetOrAdd(packageConfigFile, new PackageResult(packageConfigFile, null, null));
                results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));

                return packageConfigs;
            }

            var settings = _xmlService.deserialize<PackagesConfigFileSettings>(_fileSystem.get_full_path(packageConfigFile));
            this.Log().Info(@"Installing the following packages:");
            foreach (var pkgSettings in settings.Packages.or_empty_list_if_null())
            {
                if (!pkgSettings.Disabled)
                {
                    var packageConfig = config.deep_copy();
                    packageConfig.PackageNames = pkgSettings.Id;
                    packageConfig.Sources = string.IsNullOrWhiteSpace(pkgSettings.Source) ? packageConfig.Sources : pkgSettings.Source;
                    packageConfig.Version = pkgSettings.Version;
                    packageConfig.InstallArguments = string.IsNullOrWhiteSpace(pkgSettings.InstallArguments) ? packageConfig.InstallArguments : pkgSettings.InstallArguments;
                    packageConfig.PackageParameters = string.IsNullOrWhiteSpace(pkgSettings.PackageParameters) ? packageConfig.PackageParameters : pkgSettings.PackageParameters;
                    if (pkgSettings.ForceX86) packageConfig.ForceX86 = true;
                    if (pkgSettings.AllowMultipleVersions) packageConfig.AllowMultipleVersions = true;
                    if (pkgSettings.IgnoreDependencies) packageConfig.IgnoreDependencies = true;
                    if (pkgSettings.ApplyInstallArgumentsToDependencies) packageConfig.ApplyInstallArgumentsToDependencies = true;
                    if (pkgSettings.ApplyPackageParametersToDependencies) packageConfig.ApplyPackageParametersToDependencies = true;

                    this.Log().Info(ChocolateyLoggers.Important, @"{0}".format_with(packageConfig.PackageNames));
                    packageConfigs.Add(packageConfig);
                }
            }

            return packageConfigs;
        }

        public void upgrade_noop(ChocolateyConfiguration config)
        {
            Action<PackageResult> action = null;
            if (config.SourceType == SourceType.normal)
            {
                action = (pkg) => _powershellService.install_noop(pkg);
            }

            var noopUpgrades = perform_source_runner_function(config, r => r.upgrade_noop(config, action));
            if (config.RegularOutput)
            {
                var noopFailures = report_action_summary(noopUpgrades, "can upgrade");
            }

            randomly_notify_about_pro_business(config);
        }

        public virtual ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config)
        {
            this.Log().Info(@"Upgrading the following packages:");
            this.Log().Info(ChocolateyLoggers.Important, @"{0}".format_with(config.PackageNames));

            if (string.IsNullOrWhiteSpace(config.Sources))
            {
                this.Log().Error(ChocolateyLoggers.Important, @"Upgrading was NOT successful. There are no sources enabled for
 packages and none were passed as arguments.");
                Environment.ExitCode = 1;
                return new ConcurrentDictionary<string, PackageResult>();
            }

            this.Log().Info(@"By upgrading you accept licenses for the packages.");

            foreach (var packageConfigFile in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null().Where(p => p.EndsWith(".config")).ToList())
            {
                throw new ApplicationException("A packages.config file is only used with installs.");
            }

            var packageUpgrades = new ConcurrentDictionary<string, PackageResult>();

            try
            {
                Action<PackageResult> action = null;
                if (config.SourceType == SourceType.normal)
                {
                    action = (packageResult) => handle_package_result(packageResult, config, CommandNameType.upgrade);
                }

                get_environment_before(config, allowLogging: true);

                var beforeUpgradeAction = new Action<PackageResult>(packageResult => before_package_modify(packageResult, config));
                var results = perform_source_runner_function(config, r => r.upgrade_run(config, action, beforeUpgradeAction));

                foreach (var result in results)
                {
                    packageUpgrades.GetOrAdd(result.Key, result.Value);
                }
            }
            finally
            {
                var upgradeFailures = report_action_summary(packageUpgrades, "upgraded");
                if (upgradeFailures != 0 && Environment.ExitCode == 0)
                {
                    Environment.ExitCode = 1;
                }

                randomly_notify_about_pro_business(config);
            }

            return packageUpgrades;
        }

        private void before_package_modify(PackageResult packageResult, ChocolateyConfiguration config)
        {
            if (!config.SkipPackageInstallProvider)
            {
                _powershellService.before_modify(config, packageResult);
            }
        }

        public void uninstall_noop(ChocolateyConfiguration config)
        {
            Action<PackageResult> action = null;
            if (config.SourceType == SourceType.normal)
            {
                action = (pkg) =>
                {
                    _powershellService.before_modify_noop(pkg);
                    _powershellService.uninstall_noop(pkg);
                };
            }

            perform_source_runner_action(config, r => r.uninstall_noop(config, action));
            randomly_notify_about_pro_business(config);
        }

        public virtual ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config)
        {
            this.Log().Info(@"Uninstalling the following packages:");
            this.Log().Info(ChocolateyLoggers.Important, @"{0}".format_with(config.PackageNames));

            if (config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null().Any(p => p.EndsWith(".config")))
            {
                throw new ApplicationException("A packages.config file is only used with installs.");
            }

            var packageUninstalls = new ConcurrentDictionary<string, PackageResult>();

            try
            {
                Action<PackageResult> action = null;
                if (config.SourceType == SourceType.normal)
                {
                    action = (packageResult) => handle_package_uninstall(packageResult, config);
                }

                var environmentBefore = get_environment_before(config);
                var beforeUninstallAction = new Action<PackageResult>(packageResult => before_package_modify(packageResult, config));
                var results = perform_source_runner_function(config, r => r.uninstall_run(config, action, beforeUninstallAction));

                foreach (var result in results)
                {
                    packageUninstalls.GetOrAdd(result.Key, result.Value);
                }

                // not handled in the uninstall handler
                IEnumerable<GenericRegistryValue> environmentChanges;
                IEnumerable<GenericRegistryValue> environmentRemovals;
                get_log_environment_changes(config, environmentBefore, out environmentChanges, out environmentRemovals);
            }
            finally
            {
                var uninstallFailures = report_action_summary(packageUninstalls, "uninstalled");
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
 remove system-installed software. This will only remove the packaging
 files and not things like software installed to Programs and Features.

If a package is failing because it is a dependency of another package
 or packages, then you may first need to consider if it needs removed
 as it is typically installed as a dependency for a reason. If you
 decide that you still want to remove it, head into
 `$env:ChocolateyInstall\lib` and find the package folder you want
 removed. Then delete the folder for the package. This option should
 only be used as a last resort.
 ");
                }

                randomly_notify_about_pro_business(config);
            }

            return packageUninstalls;
        }

        private int report_action_summary(ConcurrentDictionary<string, PackageResult> packageResults, string actionName)
        {
            var successes = packageResults.or_empty_list_if_null().Where(p => p.Value.Success && !p.Value.Inconclusive);
            var failures = packageResults.Count(p => !p.Value.Success);
            var warnings = packageResults.Count(p => p.Value.Warning);
            var rebootPackages = packageResults.Count(p => new[] { 1641, 3010 }.Contains(p.Value.ExitCode));
            this.Log().Warn(
                () => @"{0}{1} {2} {3}/{4} packages. {5}{0} See the log for details ({6}).".format_with(
                    Environment.NewLine,
                    ApplicationParameters.Name,
                    actionName,
                    successes.Count(),
                    packageResults.Count,
                    (failures > 0) ? failures + " packages failed.": string.Empty,
                    _fileSystem.combine_paths(ApplicationParameters.LoggingLocation, ApplicationParameters.LoggingFile)
                    ));

            // summarize results when more than 5
            if (packageResults.Count >= 5 && successes.Count() != 0)
            {
                this.Log().Info("");
                this.Log().Warn("{0}{1}:".format_with(actionName.Substring(0,1).ToUpper(), actionName.Substring(1)));
                foreach (var packageResult in successes.or_empty_list_if_null())
                {
                    this.Log().Info(" - {0} v{1}".format_with(packageResult.Value.Name, packageResult.Value.Version));
                }
            }

            if (warnings != 0)
            {
                this.Log().Info("");
                this.Log().Warn("Warnings:");
                foreach (var warning in packageResults.Where(p => p.Value.Warning).or_empty_list_if_null())
                {
                    var warningMessage = warning.Value.Messages.FirstOrDefault(m => m.MessageType == ResultType.Warn);
                    this.Log().Warn(" - {0}{1}".format_with(warning.Value.Name, warningMessage != null ? " - {0}".format_with(warningMessage.Message) : string.Empty));
                }
            }

            if (rebootPackages != 0)
            {
                this.Log().Info("");
                this.Log().Warn("Packages requiring reboot:");
                foreach (var reboot in packageResults.Where(p => new[] { 1641, 3010 }.Contains(p.Value.ExitCode)).or_empty_list_if_null())
                {
                    this.Log().Warn(" - {0}{1}".format_with(reboot.Value.Name, reboot.Value.ExitCode != 0 ? " (exit code {0})".format_with(reboot.Value.ExitCode) : string.Empty));
                }
                this.Log().Warn(ChocolateyLoggers.Important, @"
The recent package changes indicate a reboot is necessary.
 Please reboot at your earliest convenience.");
            }

            if (failures != 0)
            {
                this.Log().Info("");
                this.Log().Error("Failures");
                foreach (var failure in packageResults.Where(p => !p.Value.Success).or_empty_list_if_null())
                {
                    var errorMessage = failure.Value.Messages.FirstOrDefault(m => m.MessageType == ResultType.Error);
                    this.Log().Error(
                        " - {0}{1}{2}".format_with(
                            failure.Value.Name,
                            failure.Value.ExitCode != 0 ? " (exited {0})".format_with(failure.Value.ExitCode) : string.Empty,
                            errorMessage != null ? " - {0}".format_with(errorMessage.Message) : string.Empty
                            ));
                }
            }

            return failures;
        }

        public virtual void handle_package_uninstall(PackageResult packageResult, ChocolateyConfiguration config)
        {
            if (!_fileSystem.directory_exists(packageResult.InstallLocation))
            {
                packageResult.InstallLocation += ".{0}".format_with(packageResult.Package.Version.to_string());
            }

            _shimgenService.uninstall(config, packageResult);

            if (!config.SkipPackageInstallProvider)
            {
                _powershellService.uninstall(config, packageResult);
            }

            if (packageResult.Success)
            {
                _autoUninstallerService.run(packageResult, config);
            }

            // we don't care about the exit code
            if (config.Information.PlatformType == PlatformType.Windows) CommandExecutor.execute_static(_shutdownExe, "/a", config.CommandExecutionTimeoutSeconds, _fileSystem.get_current_directory(), (s, e) => { }, (s, e) => { }, false, false);

            if (packageResult.Success)
            {
                //todo: v2 clean up package information store for things no longer installed (call it compact?)
                uninstall_cleanup(config, packageResult);
            }
            else
            {
                this.Log().Error(ChocolateyLoggers.Important, "{0} {1} not successful.".format_with(packageResult.Name, "uninstall"));
                handle_unsuccessful_operation(config, packageResult, movePackageToFailureLocation: false, attemptRollback: false);
            }

            if(_rebootExitCodes.Contains(packageResult.ExitCode))
            {
                if(config.Features.ExitOnRebootDetected)
                {
                    Environment.ExitCode = ApplicationParameters.ExitCodes.ErrorInstallSuspend;
                    this.Log().Warn(ChocolateyLoggers.Important, @"Chocolatey has detected a pending reboot after uninstalling
package '{0}' - stopping further execution".format_with(packageResult.Name));

                    throw new ApplicationException("Reboot required before continuing. Reboot and run same command again.");
                }
            }

            if (!packageResult.Success)
            {
                // throw an error so that NuGet Service doesn't attempt to continue with package removal
                throw new ApplicationException("{0} {1} not successful.".format_with(packageResult.Name, "uninstall"));
            }
        }

        private void uninstall_cleanup(ChocolateyConfiguration config, PackageResult packageResult)
        {
            if (config.Features.RemovePackageInformationOnUninstall) _packageInfoService.remove_package_information(packageResult.Package);

            ensure_bad_package_path_is_clean(config, packageResult);
            remove_rollback_if_exists(packageResult);
            handle_extension_packages(config, packageResult);
            handle_template_packages(config, packageResult);

            if (config.Force)
            {
                var packageDirectory = packageResult.InstallLocation;

                if (string.IsNullOrWhiteSpace(packageDirectory) || !_fileSystem.directory_exists(packageDirectory)) return;

                if (packageDirectory.is_equal_to(ApplicationParameters.InstallLocation) || packageDirectory.is_equal_to(ApplicationParameters.PackagesLocation))
                {
                    packageResult.Messages.Add(
                        new ResultMessage(
                            ResultType.Error,
                            "Install location is not specific enough, cannot force remove directory:{0} Erroneous install location captured as '{1}'".format_with(Environment.NewLine, packageResult.InstallLocation)
                            )
                        );
                    return;
                }

                FaultTolerance.try_catch_with_logging_exception(
                    () => _fileSystem.delete_directory_if_exists(packageDirectory, recursive: true),
                    "Attempted to remove '{0}' but had an error".format_with(packageDirectory),
                    logWarningInsteadOfError: true);
            }
        }

        private void handle_extension_packages(ChocolateyConfiguration config, PackageResult packageResult)
        {
            if (packageResult == null) return;
            if (!packageResult.Name.to_lower().EndsWith(".extension") && !packageResult.Name.to_lower().EndsWith(".extensions")) return;

            _fileSystem.create_directory_if_not_exists(ApplicationParameters.ExtensionsLocation);
            var extensionsFolderName = packageResult.Name.to_lower().Replace(".extensions", string.Empty).Replace(".extension", string.Empty);
            var packageExtensionsInstallDirectory = _fileSystem.combine_paths(ApplicationParameters.ExtensionsLocation, extensionsFolderName);

            remove_extension_folder(packageExtensionsInstallDirectory);
            // don't name your package *.extension.extension
            remove_extension_folder(packageExtensionsInstallDirectory + ".extension");
            remove_extension_folder(packageExtensionsInstallDirectory + ".extensions");

            if (!config.CommandName.is_equal_to(CommandNameType.uninstall.to_string()))
            {
                if (packageResult.InstallLocation == null) return;

                _fileSystem.create_directory_if_not_exists(packageExtensionsInstallDirectory);
                var extensionsFolder = _fileSystem.combine_paths(packageResult.InstallLocation, "extensions");
                var extensionFolderToCopy = _fileSystem.directory_exists(extensionsFolder) ? extensionsFolder : packageResult.InstallLocation;

                FaultTolerance.try_catch_with_logging_exception(
                    () => _fileSystem.copy_directory(extensionFolderToCopy, packageExtensionsInstallDirectory, overwriteExisting: true),
                    "Attempted to copy{0} '{1}'{0} to '{2}'{0} but had an error".format_with(Environment.NewLine, extensionFolderToCopy, packageExtensionsInstallDirectory));

                string logMessage = " Installed/updated {0} extensions.".format_with(extensionsFolderName);
                this.Log().Warn(logMessage);
                packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));

                Environment.SetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyPackageInstallLocation, packageExtensionsInstallDirectory, EnvironmentVariableTarget.Process);
            }
            else
            {
                string logMessage = " Uninstalled {0} extensions.".format_with(extensionsFolderName);
                this.Log().Warn(logMessage);
                packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));
            }
        }

        private void remove_extension_folder(string packageExtensionsDirectory)
        {
            if (!_fileSystem.directory_exists(packageExtensionsDirectory)) return;

            // remove old dll files files
            foreach (var oldDllFile in _fileSystem.get_files(packageExtensionsDirectory, "*.dll.old", SearchOption.AllDirectories).or_empty_list_if_null())
            {
                FaultTolerance.try_catch_with_logging_exception(
                    () => _fileSystem.delete_file(oldDllFile),
                    "Attempted to remove '{0}' but had an error".format_with(oldDllFile),
                    throwError: false,
                    logWarningInsteadOfError: true);
            }

            // rename possibly locked dll files
            foreach (var dllFile in _fileSystem.get_files(packageExtensionsDirectory, "*.dll", SearchOption.AllDirectories).or_empty_list_if_null())
            {
                FaultTolerance.try_catch_with_logging_exception(
                    () => _fileSystem.move_file(dllFile, dllFile + ".old"),
                    "Attempted to rename '{0}' but had an error".format_with(dllFile));
            }

            FaultTolerance.try_catch_with_logging_exception(
                () =>
                {
                    foreach (var file in _fileSystem.get_files(packageExtensionsDirectory, "*.*", SearchOption.AllDirectories).or_empty_list_if_null().Where(f => !f.EndsWith(".dll.old")))
                    {
                        FaultTolerance.try_catch_with_logging_exception(
                            () => _fileSystem.delete_file(file),
                            "Attempted to remove '{0}' but had an error".format_with(file),
                            throwError: false,
                            logWarningInsteadOfError: true);
                    }
                },
                "Attempted to remove '{0}' but had an error".format_with(packageExtensionsDirectory),
                throwError: false,
                logWarningInsteadOfError: true);

        }

        private void handle_template_packages(ChocolateyConfiguration config, PackageResult packageResult)
        {
            if (packageResult == null) return;
            if (!packageResult.Name.to_lower().EndsWith(".template")) return;

            _fileSystem.create_directory_if_not_exists(ApplicationParameters.TemplatesLocation);
            var templateFolderName = packageResult.Name.to_lower().Replace(".template", string.Empty);
            var installTemplatePath = _fileSystem.combine_paths(ApplicationParameters.TemplatesLocation, templateFolderName);

            FaultTolerance.try_catch_with_logging_exception(
                () => _fileSystem.delete_directory_if_exists(installTemplatePath, recursive: true),
                "Attempted to remove '{0}' but had an error".format_with(installTemplatePath));

            if (!config.CommandName.is_equal_to(CommandNameType.uninstall.to_string()))
            {
                if (packageResult.InstallLocation == null) return;

                _fileSystem.create_directory_if_not_exists(installTemplatePath);
                var templatesPath = _fileSystem.combine_paths(packageResult.InstallLocation, "templates");
                var templatesFolderToCopy = _fileSystem.directory_exists(templatesPath) ? templatesPath : packageResult.InstallLocation;

                FaultTolerance.try_catch_with_logging_exception(
                    () =>
                    {
                        _fileSystem.copy_directory(templatesFolderToCopy, installTemplatePath, overwriteExisting: true);
                        foreach (var nuspecFile in _fileSystem.get_files(installTemplatePath, "*.nuspec.template").or_empty_list_if_null())
                        {
                            _fileSystem.move_file(nuspecFile, nuspecFile.Replace(".nuspec.template", ".nuspec"));
                        }
                    },
                    "Attempted to copy{0} '{1}'{0} to '{2}'{0} but had an error".format_with(Environment.NewLine, templatesFolderToCopy, installTemplatePath));

                string logMessage = " Installed/updated {0} template.".format_with(templateFolderName);
                this.Log().Warn(logMessage);
                packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));

                Environment.SetEnvironmentVariable(ApplicationParameters.Environment.ChocolateyPackageInstallLocation, installTemplatePath, EnvironmentVariableTarget.Process);
            }
            else
            {
                string logMessage = " Uninstalled {0} template.".format_with(templateFolderName);
                this.Log().Warn(logMessage);
                packageResult.Messages.Add(new ResultMessage(ResultType.Note, logMessage));
            }
        }

        private void ensure_bad_package_path_is_clean(ChocolateyConfiguration config, PackageResult packageResult)
        {
            if (packageResult.InstallLocation == null) return;

            FaultTolerance.try_catch_with_logging_exception(
                () =>
                {
                    string badPackageInstallPath = packageResult.InstallLocation.Replace(ApplicationParameters.PackagesLocation, ApplicationParameters.PackageFailuresLocation);
                    if (_fileSystem.directory_exists(badPackageInstallPath))
                    {
                        _fileSystem.delete_directory(badPackageInstallPath, recursive: true);
                    }
                },
                "Attempted to delete bad package install path if existing. Had an error");
        }

        private void handle_unsuccessful_operation(ChocolateyConfiguration config, PackageResult packageResult, bool movePackageToFailureLocation, bool attemptRollback)
        {
            if (Environment.ExitCode == 0) Environment.ExitCode = 1;

            foreach (var message in packageResult.Messages.Where(m => m.MessageType == ResultType.Error))
            {
                this.Log().Error(message.Message);
            }

            if (attemptRollback || movePackageToFailureLocation)
            {
                var packageDirectory = packageResult.InstallLocation;
                if (packageDirectory.is_equal_to(ApplicationParameters.InstallLocation) || packageDirectory.is_equal_to(ApplicationParameters.PackagesLocation))
                {
                    this.Log().Error(ChocolateyLoggers.Important, @"
Package location is not specific enough, cannot move bad package or
 rollback previous version. Erroneous install location captured as
 '{0}'

ATTENTION: You must take manual action to remove {1} from
 {2}. It will show incorrectly as installed
 until you do. To remove you can simply delete the folder in question.
".format_with(packageResult.InstallLocation, packageResult.Name, ApplicationParameters.PackagesLocation));
                }
                else
                {
                    if (movePackageToFailureLocation) move_bad_package_to_failure_location(packageResult);

                    if (attemptRollback) rollback_previous_version(config, packageResult);
                }
            }
        }

        private void move_bad_package_to_failure_location(PackageResult packageResult)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.PackageFailuresLocation);

            if (!string.IsNullOrWhiteSpace(packageResult.InstallLocation) && _fileSystem.directory_exists(packageResult.InstallLocation))
            {
                FaultTolerance.try_catch_with_logging_exception(
                 () => _fileSystem.move_directory(packageResult.InstallLocation, packageResult.InstallLocation.Replace(ApplicationParameters.PackagesLocation, ApplicationParameters.PackageFailuresLocation)),
                 "Could not move bad package to failure directory It will show as installed.{0} {1}{0} The error".format_with(Environment.NewLine, packageResult.InstallLocation));
            }
        }

        private void rollback_previous_version(ChocolateyConfiguration config, PackageResult packageResult)
        {
            if (packageResult.InstallLocation == null) return;

            var rollbackDirectory = packageResult.InstallLocation.Replace(ApplicationParameters.PackagesLocation, ApplicationParameters.PackageBackupLocation);
            if (!_fileSystem.directory_exists(rollbackDirectory))
            {
                //search for folder
                var possibleRollbacks = _fileSystem.get_directories(ApplicationParameters.PackageBackupLocation, packageResult.Name + "*");
                if (possibleRollbacks != null && possibleRollbacks.Count() != 0)
                {
                    rollbackDirectory = possibleRollbacks.OrderByDescending(p => p).DefaultIfEmpty(string.Empty).FirstOrDefault();
                }
            }

            rollbackDirectory = _fileSystem.get_full_path(rollbackDirectory);

            if (string.IsNullOrWhiteSpace(rollbackDirectory) || !_fileSystem.directory_exists(rollbackDirectory)) return;
            if (!rollbackDirectory.StartsWith(ApplicationParameters.PackageBackupLocation) || rollbackDirectory.is_equal_to(ApplicationParameters.PackageBackupLocation)) return;

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
                var selection = InteractivePrompt.prompt_for_confirmation(
                    " Unsuccessful operation for {0}.{1}  Rollback to previous version (package files only)?".format_with(packageResult.Name, Environment.NewLine),
                    new[] { "yes", "no" },
                    defaultChoice: null,
                    requireAnswer: true,
                    allowShortAnswer: true,
                    shortPrompt: true
                    );
                if (selection.is_equal_to("no")) rollback = false;
            }

            if (rollback)
            {
                _fileSystem.move_directory(rollbackDirectory, rollbackDirectory.Replace(ApplicationParameters.PackageBackupLocation, ApplicationParameters.PackagesLocation));
            }

            remove_rollback_if_exists(packageResult);
        }

        private void remove_rollback_if_exists(PackageResult packageResult)
        {
            _nugetService.remove_rollback_directory_if_exists(packageResult.Name);
        }

        public virtual void set_pending(PackageResult packageResult, ChocolateyConfiguration config)
        {
            var packageDirectory = packageResult.InstallLocation;
            if (string.IsNullOrWhiteSpace(packageDirectory)) return;
            if (packageDirectory.is_equal_to(ApplicationParameters.InstallLocation) || packageDirectory.is_equal_to(ApplicationParameters.PackagesLocation))
            {
                packageResult.Messages.Add(
                    new ResultMessage(
                        ResultType.Error,
                        "Install location is not specific enough, cannot run set package to pending:{0} Erroneous install location captured as '{1}'".format_with(Environment.NewLine, packageResult.InstallLocation)
                        )
                    );

                return;
            }

            var pendingFile = _fileSystem.combine_paths(packageDirectory, ApplicationParameters.PackagePendingFileName);
            _fileSystem.write_file(pendingFile, "{0}".format_with(packageResult.Name));
            if (ApplicationParameters.LockTransactionalInstallFiles)
            {
                _pendingLocks.Add(packageResult.Name.to_lower(), _fileSystem.open_file_exclusive(pendingFile));
            }
        }

        public virtual void remove_pending(PackageResult packageResult, ChocolateyConfiguration config)
        {
            var packageDirectory = packageResult.InstallLocation;
            if (string.IsNullOrWhiteSpace(packageDirectory)) return;
            if (packageDirectory.is_equal_to(ApplicationParameters.InstallLocation) || packageDirectory.is_equal_to(ApplicationParameters.PackagesLocation))
            {
                packageResult.Messages.Add(
                    new ResultMessage(
                        ResultType.Error,
                        "Install location is not specific enough, cannot run set package to pending:{0} Erroneous install location captured as '{1}'".format_with(Environment.NewLine, packageResult.InstallLocation)
                        )
                    );

                return;
            }

            var pendingFile = _fileSystem.combine_paths(packageDirectory, ApplicationParameters.PackagePendingFileName);
            var lockName = packageResult.Name.to_lower();
            if (_pendingLocks.ContainsKey(lockName))
            {
                var fileLock = _pendingLocks[lockName];
                _pendingLocks.Remove(lockName);
                fileLock.Close();
                fileLock.Dispose();
            }

            if (packageResult.Success && _fileSystem.file_exists(pendingFile)) _fileSystem.delete_file(pendingFile);
        }

        private IEnumerable<GenericRegistryValue> get_environment_before(ChocolateyConfiguration config, bool allowLogging = true)
        {
            if (config.Information.PlatformType != PlatformType.Windows) return Enumerable.Empty<GenericRegistryValue>();
            var environmentBefore = _registryService.get_environment_values();

            if (allowLogging && config.Features.LogEnvironmentValues)
            {
                this.Log().Debug("Current environment values (may contain sensitive data):");
                foreach (var environmentValue in environmentBefore.or_empty_list_if_null())
                {
                    this.Log().Debug(@"  * '{0}'='{1}' ('{2}')".format_with(
                        environmentValue.Name.escape_curly_braces(),
                        environmentValue.Value.escape_curly_braces(),
                        environmentValue.ParentKeyName.to_lower().Contains("hkey_current_user") ? "User" : "Machine"));
                }
            }
            return environmentBefore;
        }

        private void get_log_environment_changes(ChocolateyConfiguration config, IEnumerable<GenericRegistryValue> environmentBefore, out IEnumerable<GenericRegistryValue> environmentChanges, out IEnumerable<GenericRegistryValue> environmentRemovals)
        {
            if (config.Information.PlatformType != PlatformType.Windows)
            {
                environmentChanges = Enumerable.Empty<GenericRegistryValue>();
                environmentRemovals = Enumerable.Empty<GenericRegistryValue>();

                return;
            }

            var environmentAfer = _registryService.get_environment_values();
            environmentChanges = _registryService.get_added_changed_environment_differences(environmentBefore, environmentAfer);
            environmentRemovals = _registryService.get_removed_environment_differences(environmentBefore, environmentAfer);
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
                    foreach (var difference in environmentChanges.or_empty_list_if_null())
                    {
                        this.Log().Debug(@"  * {0}='{1}' ({2})".format_with(
                            difference.Name.escape_curly_braces(),
                            config.Features.LogEnvironmentValues ? difference.Value.escape_curly_braces() : "[REDACTED]",
                             difference.ParentKeyName.to_lower().Contains("hkey_current_user") ? "User" : "Machine"
                            ));
                    }
                }

                if (hasEnvironmentRemovals)
                {
                    this.Log().Debug(@"The following values have been removed:");
                    foreach (var difference in environmentRemovals.or_empty_list_if_null())
                    {
                        this.Log().Debug(@"  * {0}='{1}' ({2})".format_with(
                            difference.Name.escape_curly_braces(),
                            config.Features.LogEnvironmentValues ? difference.Value.escape_curly_braces() : "[REDACTED]",
                            difference.ParentKeyName.to_lower().Contains("hkey_current_user") ? "User" : "Machine"
                            ));
                    }
                }
            }
        }
    }
}
