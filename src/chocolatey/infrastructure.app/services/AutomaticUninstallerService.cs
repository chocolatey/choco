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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using commandline;
    using configuration;
    using domain;
    using domain.installers;
    using filesystem;
    using infrastructure.commands;
    using results;

    public class AutomaticUninstallerService : IAutomaticUninstallerService
    {
        private readonly IChocolateyPackageInformationService _packageInfoService;
        private readonly IFileSystem _fileSystem;
        private readonly IRegistryService _registryService;
        private readonly ICommandExecutor _commandExecutor;
        private const int SLEEP_TIME = 2;

        public AutomaticUninstallerService(IChocolateyPackageInformationService packageInfoService, IFileSystem fileSystem, IRegistryService registryService, ICommandExecutor commandExecutor)
        {
            _packageInfoService = packageInfoService;
            _fileSystem = fileSystem;
            _registryService = registryService;
            _commandExecutor = commandExecutor;
            WaitForCleanup = true;
        }

        public bool WaitForCleanup { get; set; }

        public void run(PackageResult packageResult, ChocolateyConfiguration config)
        {
            if (!config.Features.AutoUninstaller)
            {
                this.Log().Info(" Skipping auto uninstaller - AutoUninstaller feature is not enabled.");
                return;
            }

            var pkgInfo = _packageInfoService.get_package_information(packageResult.Package);

            if (pkgInfo.RegistrySnapshot == null)
            {
                this.Log().Info(" Skipping auto uninstaller - No registry snapshot.");
                return;
            }

            var registryKeys = pkgInfo.RegistrySnapshot.RegistryKeys;
            if (registryKeys == null || registryKeys.Count == 0)
            {
                this.Log().Info(" Skipping auto uninstaller - No registry keys in snapshot.");
                return;
            }

            this.Log().Info(" Running auto uninstaller...");
            if (WaitForCleanup)
            {
                this.Log().Debug("Sleeping for {0} seconds to allow Windows to finish cleaning up.".format_with(SLEEP_TIME));
                Thread.Sleep((int)TimeSpan.FromSeconds(SLEEP_TIME).TotalMilliseconds);
            }
            
            foreach (var key in registryKeys.or_empty_list_if_null())
            {
                this.Log().Debug(() => " Preparing uninstall key '{0}'".format_with(key.UninstallString.escape_curly_braces()));

                if ((!string.IsNullOrWhiteSpace(key.InstallLocation) && !_fileSystem.directory_exists(key.InstallLocation)) || !_registryService.installer_value_exists(key.KeyPath, ApplicationParameters.RegistryValueInstallLocation))
                {
                    this.Log().Info(" Skipping auto uninstaller - The application appears to have been uninstalled already by other means.");
                    this.Log().Debug(() => " Searched for install path '{0}' - found? {1}".format_with(key.InstallLocation.escape_curly_braces(), _fileSystem.directory_exists(key.InstallLocation)));
                    this.Log().Debug(() => " Searched for registry key '{0}' value '{1}' - found? {2}".format_with(key.KeyPath.escape_curly_braces(), ApplicationParameters.RegistryValueInstallLocation, _registryService.installer_value_exists(key.KeyPath, ApplicationParameters.RegistryValueInstallLocation)));
                    continue;
                }

                // split on " /" and " -" for quite a bit more accuracy
                IList<string> uninstallArgsSplit = key.UninstallString.to_string().Split(new[] {" /", " -"}, StringSplitOptions.RemoveEmptyEntries).ToList();
                var uninstallExe = uninstallArgsSplit.DefaultIfEmpty(string.Empty).FirstOrDefault();
                var uninstallArgs = key.UninstallString.to_string().Replace(uninstallExe.to_string(), string.Empty);
                uninstallExe = uninstallExe.remove_surrounding_quotes();
                this.Log().Debug(() => " Uninstaller path is '{0}'".format_with(uninstallExe));


                IInstaller installer = new CustomInstaller();

                switch (key.InstallerType)
                {
                    case InstallerType.Msi:
                        installer = new MsiInstaller();
                        break;
                    case InstallerType.InnoSetup:
                        installer = new InnoSetupInstaller();
                        break;
                    case InstallerType.Nsis:
                        installer = new NsisInstaller();
                        break;
                    case InstallerType.InstallShield:
                        installer = new InstallShieldInstaller();
                        break;
                }

                this.Log().Debug(() => " Installer type is '{0}'".format_with(installer.GetType().Name));

                if (key.InstallerType == InstallerType.Msi)
                {
                    // because sometimes the key is set with /i to allow for modify :/
                    uninstallArgs = uninstallArgs.Replace("/I{", "/X{");
                    uninstallArgs = uninstallArgs.Replace("/i{", "/X{");
                    uninstallArgs = uninstallArgs.Replace("/I ", "/X ");
                    uninstallArgs = uninstallArgs.Replace("/i ", "/X ");
                }

                if (!key.HasQuietUninstall)
                {
                    //todo: ultimately we should merge keys
                    uninstallArgs += " " + installer.build_uninstall_command_arguments();
                }

                var logLocation = _fileSystem.combine_paths(_fileSystem.get_full_path(config.CacheLocation), "chocolatey", pkgInfo.Package.Id, pkgInfo.Package.Version.to_string());
                this.Log().Debug(() => " Setting up uninstall logging directory at {0}".format_with(logLocation.escape_curly_braces()));
                _fileSystem.create_directory_if_not_exists(_fileSystem.get_directory_name(logLocation));
                uninstallArgs = uninstallArgs.Replace(InstallTokens.PACKAGE_LOCATION, logLocation);

                this.Log().Debug(() => " Args are '{0}'".format_with(uninstallArgs.escape_curly_braces()));

                if (!key.HasQuietUninstall &&  installer.GetType() == typeof(CustomInstaller))
                {
                    var skipUninstaller = true;

                    var timeout = config.PromptForConfirmation ? 0 : 30;
                    
                    var selection = InteractivePrompt.prompt_for_confirmation(
                        "Uninstall may not be silent (could not detect). Proceed?",
                        new[] { "yes", "no" },
                        defaultChoice: "no",
                        requireAnswer: true,
                        allowShortAnswer: true,
                        shortPrompt: true,
                        timeoutInSeconds: timeout
                        );
                    if (selection.is_equal_to("yes")) skipUninstaller = false;
                    

                    if (skipUninstaller)
                    {
                        this.Log().Info(" Skipping auto uninstaller - Installer type was not detected and no silent uninstall key exists.");
                        this.Log().Warn("If the application was not removed with a chocolateyUninstall.ps1,{0} please remove it from Programs and Features manually.".format_with(Environment.NewLine));
                        return;
                    }
                }

                var exitCode = _commandExecutor.execute(
                    uninstallExe,
                    uninstallArgs.trim_safe(),
                    config.CommandExecutionTimeoutSeconds,
                    (s, e) =>
                        {
                            if (e == null || string.IsNullOrWhiteSpace(e.Data)) return;
                            this.Log().Info(() => " [AutoUninstaller] {0}".format_with(e.Data.escape_curly_braces()));
                        },
                    (s, e) =>
                        {
                            if (e == null || string.IsNullOrWhiteSpace(e.Data)) return;
                            this.Log().Error(() => " [AutoUninstaller] {0}".format_with(e.Data.escape_curly_braces()));
                        },
                    updateProcessPath: false);

                if (!installer.ValidUninstallExitCodes.Contains(exitCode))
                {
                    Environment.ExitCode = exitCode;
                    string logMessage = " Auto uninstaller failed. Please remove machine installation manually.{0} Exit code was {1}".format_with(Environment.NewLine, exitCode);
                    this.Log().Error(() => logMessage.escape_curly_braces());
                    packageResult.Messages.Add(new ResultMessage(config.Features.FailOnAutoUninstaller ? ResultType.Error : ResultType.Warn, logMessage));
                }
                else
                {
                    this.Log().Info(() => " Auto uninstaller has successfully uninstalled {0} or detected previous uninstall.".format_with(packageResult.Package.Id));
                }
            }
        }
    }
}