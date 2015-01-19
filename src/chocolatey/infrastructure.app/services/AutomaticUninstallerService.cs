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
    using configuration;
    using domain;
    using filesystem;
    using infrastructure.commands;
    using results;

    public class AutomaticUninstallerService : IAutomaticUninstallerService
    {
        private readonly IChocolateyPackageInformationService _packageInfoService;
        private readonly IFileSystem _fileSystem;
        private readonly IRegistryService _registryService;
        private readonly ICommandExecutor _commandExecutor;

        public AutomaticUninstallerService(IChocolateyPackageInformationService packageInfoService, IFileSystem fileSystem, IRegistryService registryService, ICommandExecutor commandExecutor)
        {
            _packageInfoService = packageInfoService;
            _fileSystem = fileSystem;
            _registryService = registryService;
            _commandExecutor = commandExecutor;
        }

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

            foreach (var key in registryKeys.or_empty_list_if_null())
            {
                this.Log().Debug(() => " Preparing uninstall key '{0}'".format_with(key.UninstallString));

                if (!_fileSystem.directory_exists(key.InstallLocation) || !_registryService.value_exists(key.KeyPath, ApplicationParameters.RegistryValueInstallLocation))
                {
                    this.Log().Info(" Skipping auto uninstaller - The application appears to have been uninstalled already by other means.");
                    this.Log().Debug(() => " Searched for install path '{0}' - found? {1}".format_with(key.InstallLocation.escape_curly_braces(), _fileSystem.directory_exists(key.InstallLocation)));
                    this.Log().Debug(() => " Searched for registry key '{0}' value '{1}' - found? {2}".format_with(key.KeyPath.escape_curly_braces(), ApplicationParameters.RegistryValueInstallLocation, _registryService.value_exists(key.KeyPath, ApplicationParameters.RegistryValueInstallLocation)));
                    continue;
                }

                // split on " /" and " -" for quite a bit more accuracy
                IList<string> uninstallArgs = key.UninstallString.to_string().Split(new[] {" /", " -"}, StringSplitOptions.RemoveEmptyEntries).ToList();
                var uninstallExe = uninstallArgs.DefaultIfEmpty(string.Empty).FirstOrDefault();
                uninstallArgs.Remove(uninstallExe);
                uninstallExe = uninstallExe.remove_surrounding_quotes();
                this.Log().Debug(() => " Uninstaller path is '{0}'".format_with(uninstallExe));

                //todo: ultimately we should merge keys with logging
                if (!key.HasQuietUninstall)
                {
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
                            installer = new CustomInstaller();
                            break;
                        default:
                            // skip
                            break;
                    }

                    this.Log().Debug(() => " Installer type is '{0}'".format_with(installer.GetType().Name));

                    uninstallArgs.Add(installer.build_uninstall_command_arguments());
                }

                this.Log().Debug(() => " Args are '{0}'".format_with(uninstallArgs.@join(" ")));

                var exitCode = _commandExecutor.execute(
                    uninstallExe, 
                    uninstallArgs.@join(" "), 
                    config.CommandExecutionTimeoutSeconds,
                    (s, e) =>
                        {
                            if (e == null || string.IsNullOrWhiteSpace(e.Data)) return;
                            this.Log().Debug(() => " [AutoUninstaller] {0}".format_with(e.Data));
                        },
                    (s, e) =>
                        {
                            if (e == null || string.IsNullOrWhiteSpace(e.Data)) return;
                            this.Log().Error(() => " [AutoUninstaller] {0}".format_with(e.Data));
                        });

                if (exitCode != 0)
                {
                    Environment.ExitCode = exitCode;
                    string logMessage = " Auto uninstaller failed. Please remove machine installation manually.";
                    this.Log().Error(() => logMessage);
                    packageResult.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                }
                else
                {
                    this.Log().Info(() => " Auto uninstaller has successfully uninstalled {0} from your machine.".format_with(packageResult.Package.Id));
                }
            }
        }
    }
}