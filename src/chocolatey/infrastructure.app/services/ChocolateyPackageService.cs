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
    using System.Threading;
    using configuration;
    using domain;
    using filesystem;
    using logging;
    using platforms;
    using results;

    public class ChocolateyPackageService : IChocolateyPackageService
    {
        private readonly INugetService _nugetService;
        private readonly IPowershellService _powershellService;
        private readonly IShimGenerationService _shimgenService;
        private readonly IFileSystem _fileSystem;
        private readonly IRegistryService _registryService;
        private readonly IChocolateyPackageInformationService _packageInfoService;
        private readonly IAutomaticUninstallerService _autoUninstallerService;

        public ChocolateyPackageService(INugetService nugetService, IPowershellService powershellService, IShimGenerationService shimgenService, IFileSystem fileSystem, IRegistryService registryService, IChocolateyPackageInformationService packageInfoService, IAutomaticUninstallerService autoUninstallerService)
        {
            _nugetService = nugetService;
            _powershellService = powershellService;
            _shimgenService = shimgenService;
            _fileSystem = fileSystem;
            _registryService = registryService;
            _packageInfoService = packageInfoService;
            _autoUninstallerService = autoUninstallerService;
        }

        public void list_noop(ChocolateyConfiguration config)
        {
            if (config.Sources.is_equal_to(SpecialSourceType.webpi.to_string()))
            {
                //todo: webpi
            }
            else
            {
                _nugetService.list_noop(config);
            }
        }

        public void list_run(ChocolateyConfiguration config, bool logResults)
        {
            this.Log().Debug(() => "Searching for package information");

            if (config.Sources.is_equal_to(SpecialSourceType.webpi.to_string()))
            {
                //todo: webpi
                //install webpi if not installed
                //run the webpi command 
                this.Log().Warn("Command not yet functional, stay tuned...");
            }
            else
            {
                var list = _nugetService.list_run(config, logResults: true);
                if (config.RegularOuptut)
                {
                    this.Log().Warn(() => @"{0} packages {1}.".format_with(list.Count, config.ListCommand.LocalOnly ? "installed" : "found"));

                    if (config.ListCommand.LocalOnly && config.ListCommand.IncludeRegistryPrograms)
                    {
                        report_registry_programs(config, list);
                    }
                }
            }
        }

        private void report_registry_programs(ChocolateyConfiguration config, ConcurrentDictionary<string, PackageResult> list)
        {
            var itemsToRemoveFromMachine = new List<string>();
            foreach (var packageResult in list)
            {
                if (packageResult.Value != null && packageResult.Value.Package != null)
                {
                    var pkginfo = _packageInfoService.get_package_information(packageResult.Value.Package);
                    if (pkginfo.RegistrySnapshot == null)
                    {
                        continue;
                    }
                    var key = pkginfo.RegistrySnapshot.RegistryKeys.FirstOrDefault();
                    if (key != null)
                    {
                        itemsToRemoveFromMachine.Add(key.DisplayName);
                    }
                }
            }
            var machineInstalled = _registryService.get_installer_keys().RegistryKeys.Where((p) => p.is_in_programs_and_features() && !itemsToRemoveFromMachine.Contains(p.DisplayName)).OrderBy((p) => p.DisplayName).Distinct().ToList();
            if (machineInstalled.Count != 0)
            {
                this.Log().Info(() => "");
                foreach (var key in machineInstalled.or_empty_list_if_null())
                {
                    this.Log().Info("{0}|{1}".format_with(key.DisplayName, key.DisplayVersion));
                    if (config.Verbose) this.Log().Info(" InstallLocation: {0}{1} Uninstall:{2}".format_with(key.InstallLocation.escape_curly_braces(), Environment.NewLine, key.UninstallString.escape_curly_braces()));
                }
                this.Log().Warn(() => @"{0} applications not managed with Chocolatey.".format_with(machineInstalled.Count));
            }
        }

        public void pack_noop(ChocolateyConfiguration config)
        {
            _nugetService.pack_noop(config);
        }

        public void pack_run(ChocolateyConfiguration config)
        {
            _nugetService.pack_run(config);
        }

        public void push_noop(ChocolateyConfiguration config)
        {
            _nugetService.push_noop(config);
        }

        public void push_run(ChocolateyConfiguration config)
        {
            _nugetService.push_run(config);
        }

        public void install_noop(ChocolateyConfiguration config)
        {
            _nugetService.install_noop(config, (pkg) => _powershellService.install_noop(pkg));
        }

        public void handle_package_result(PackageResult packageResult, ChocolateyConfiguration config, CommandNameType commandName)
        {
            var pkgInfo = _packageInfoService.get_package_information(packageResult.Package);
            if (config.AllowMultipleVersions)
            {
                pkgInfo.IsSideBySide = true;
            }

            if (config.Information.PlatformType == PlatformType.Windows)
            {
                if (!config.SkipPackageInstallProvider)
                {
                    var before = _registryService.get_installer_keys();

                    var powerShellRan = _powershellService.install(config, packageResult);
                    if (powerShellRan)
                    {
                        //todo: prevent reboots
                    }

                    var difference = _registryService.get_differences(before, _registryService.get_installer_keys());
                    if (difference.RegistryKeys.Count != 0)
                    {
                        //todo v1 - determine the installer type and write it to the snapshot
                        //todo v1 - note keys passed in 
                        pkgInfo.RegistrySnapshot = difference;

                        var key = difference.RegistryKeys.FirstOrDefault();
                        if (key != null && key.HasQuietUninstall)
                        {
                            pkgInfo.HasSilentUninstall = true;
                        }
                    }
                }

                if (packageResult.Success)
                {
                    _shimgenService.install(config, packageResult);
                }
            }
            else
            {
                this.Log().Info(ChocolateyLoggers.Important, () => " Skipping Powershell and shimgen portions of the install due to non-Windows.");
            }

            _packageInfoService.save_package_information(pkgInfo);
            ensure_bad_package_path_is_clean(config, packageResult);

            if (!packageResult.Success)
            {
                this.Log().Error(ChocolateyLoggers.Important, "{0} {1} not successful.".format_with(packageResult.Name, commandName.to_string()));
                handle_unsuccessful_install(packageResult);

                return;
            }

            this.Log().Info(ChocolateyLoggers.Important, " {0} has been {1}ed successfully.".format_with(packageResult.Name, commandName.to_string()));
        }

        public ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config)
        {
            //todo:are we installing from an alternate source? If so run that command instead

            this.Log().Info(@"Installing the following packages:");
            this.Log().Info(ChocolateyLoggers.Important, @"{0}".format_with(config.PackageNames));
            this.Log().Info(@"By installing you accept licenses for the packages.");

            var packageInstalls = _nugetService.install_run(
                config,
                (packageResult) => handle_package_result(packageResult, config, CommandNameType.install)
                );

            var installFailures = packageInstalls.Count(p => !p.Value.Success);
            this.Log().Warn(() => @"{0}{1} installed {2}/{3} packages. {4} packages failed.{0}See the log for details.".format_with(
                Environment.NewLine,
                ApplicationParameters.Name,
                packageInstalls.Count(p => p.Value.Success && !p.Value.Inconclusive),
                packageInstalls.Count,
                installFailures));

            if (installFailures != 0)
            {
                this.Log().Error("Failures");
                foreach (var failure in packageInstalls.Where(p => !p.Value.Success).or_empty_list_if_null())
                {
                    this.Log().Error(" - {0}".format_with(failure.Value.Name));
                }
            }

            if (installFailures != 0 && Environment.ExitCode == 0)
            {
                Environment.ExitCode = 1;
            }

            return packageInstalls;
        }

        public void upgrade_noop(ChocolateyConfiguration config)
        {
            _nugetService.upgrade_noop(config, (pkg) => _powershellService.install_noop(pkg));
        }

        public ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config)
        {
            //todo:are we upgrading an alternate source? If so run that command instead

            this.Log().Info(@"Upgrading the following packages:");
            this.Log().Info(ChocolateyLoggers.Important, @"{0}".format_with(config.PackageNames));
            this.Log().Info(@"By installing you accept licenses for the packages.");

            var packageUpgrades = _nugetService.upgrade_run(
                config,
                (packageResult) => handle_package_result(packageResult, config, CommandNameType.upgrade)
                );

            var upgradeFailures = packageUpgrades.Count(p => !p.Value.Success);
            this.Log().Warn(() => @"{0}{1} upgraded {2}/{3} packages. {4} packages failed.{0}See the log for details.".format_with(
                Environment.NewLine,
                ApplicationParameters.Name,
                packageUpgrades.Count(p => p.Value.Success && !p.Value.Inconclusive),
                packageUpgrades.Count,
                upgradeFailures));

            if (upgradeFailures != 0)
            {
                this.Log().Error("Failures");
                foreach (var failure in packageUpgrades.Where(p => !p.Value.Success).or_empty_list_if_null())
                {
                    this.Log().Error(" - {0}".format_with(failure.Value.Name));
                }
            }

            if (upgradeFailures != 0 && Environment.ExitCode == 0)
            {
                Environment.ExitCode = 1;
            }

            return packageUpgrades;
        }

        public void uninstall_noop(ChocolateyConfiguration config)
        {
            _nugetService.uninstall_noop(config, (pkg) => { _powershellService.uninstall_noop(pkg); });
        }

        public ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config)
        {
            this.Log().Info(@"Uninstalling the following packages:");
            this.Log().Info(ChocolateyLoggers.Important, @"{0}".format_with(config.PackageNames));

            var packageUninstalls = _nugetService.uninstall_run(
                config,
                (packageResult) =>
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

                        _autoUninstallerService.run(packageResult, config);

                        if (packageResult.Success)
                        {
                            //todo: v2 clean up package information store for things no longer installed (call it compact?)
                            _packageInfoService.remove_package_information(packageResult.Package);
                        }

                        //todo:prevent reboots
                    });

            var uninstallFailures = packageUninstalls.Count(p => !p.Value.Success);
            this.Log().Warn(() => @"{0}{1} uninstalled {2}/{3} packages. {4} packages failed.{0}See the log for details.".format_with(
                Environment.NewLine,
                ApplicationParameters.Name,
                packageUninstalls.Count(p => p.Value.Success && !p.Value.Inconclusive),
                packageUninstalls.Count,
                uninstallFailures));

            if (uninstallFailures != 0)
            {
                this.Log().Error("Failures");
                foreach (var failure in packageUninstalls.Where(p => !p.Value.Success).or_empty_list_if_null())
                {
                    this.Log().Error(" - {0}".format_with(failure.Value.Name));
                }
            }

            if (uninstallFailures != 0 && Environment.ExitCode == 0)
            {
                Environment.ExitCode = 1;
            }

            return packageUninstalls;
        }

        private void handle_unsuccessful_install(PackageResult packageResult)
        {
            foreach (var message in packageResult.Messages.Where(m => m.MessageType == ResultType.Error))
            {
                this.Log().Error(message.Message);
            }

            _fileSystem.create_directory_if_not_exists(ApplicationParameters.PackageFailuresLocation);
            foreach (var file in _fileSystem.get_files(packageResult.InstallLocation, "*.*", SearchOption.AllDirectories))
            {
                var badFile = file.Replace(ApplicationParameters.PackagesLocation, ApplicationParameters.PackageFailuresLocation);
                _fileSystem.create_directory_if_not_exists(_fileSystem.get_directory_name(badFile));
                _fileSystem.move_file(file, badFile);
                //_fileSystem.copy_file_unsafe(file, badFile,overwriteTheExistingFile:true);
            }
            Thread.Sleep(2000); // sleep for enough time that the for half a second to allow the folder to be cleared
            _fileSystem.delete_directory(packageResult.InstallLocation, recursive: true);
        }

        private void ensure_bad_package_path_is_clean(ChocolateyConfiguration config, PackageResult packageResult)
        {
            try
            {
                string badPackageInstallPath = packageResult.InstallLocation.Replace(ApplicationParameters.PackagesLocation, ApplicationParameters.PackageFailuresLocation);
                if (_fileSystem.directory_exists(badPackageInstallPath))
                {
                    _fileSystem.delete_directory(badPackageInstallPath, recursive: true);
                }
            }
            catch (Exception ex)
            {
                if (config.Debug)
                {
                    this.Log().Error(() => "Attempted to delete bad package install path if existing. Had an error:{0}{1}".format_with(Environment.NewLine, ex));
                }
                else
                {
                    this.Log().Error(() => "Attempted to delete bad package install path if existing. Had an error:{0}{1}".format_with(Environment.NewLine, ex.Message));
                }
            }
        }
    }
}