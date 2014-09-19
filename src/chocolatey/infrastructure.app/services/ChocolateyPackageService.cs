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
    using infrastructure.commands;
    using logging;
    using results;

    public class ChocolateyPackageService : IChocolateyPackageService
    {
        private readonly INugetService _nugetService;
        private readonly IPowershellService _powershellService;
        private readonly IShimGenerationService _shimgenService;
        private readonly IFileSystem _fileSystem;
        private readonly IRegistryService _registryService;
        private readonly IChocolateyPackageInformationService _packageInfoService;

        public ChocolateyPackageService(INugetService nugetService, IPowershellService powershellService, IShimGenerationService shimgenService, IFileSystem fileSystem, IRegistryService registryService, IChocolateyPackageInformationService packageInfoService)
        {
            _nugetService = nugetService;
            _powershellService = powershellService;
            _shimgenService = shimgenService;
            _fileSystem = fileSystem;
            _registryService = registryService;
            _packageInfoService = packageInfoService;
        }

        public void list_noop(ChocolateyConfiguration configuration)
        {
            if (configuration.Source.is_equal_to(SpecialSourceType.webpi.to_string()))
            {
                //todo: webpi
            }
            else
            {
                _nugetService.list_noop(configuration);
            }
        }

        public void list_run(ChocolateyConfiguration configuration, bool logResults)
        {
            this.Log().Debug(() => "Searching for package information");

            if (configuration.Source.is_equal_to(SpecialSourceType.webpi.to_string()))
            {
                //todo: webpi
                //install webpi if not installed
                //run the webpi command 
                this.Log().Warn("Command not yet functional, stay tuned...");
            }
            else
            {
                var list = _nugetService.list_run(configuration, logResults: true);
                if (configuration.RegularOuptut)
                {
                    this.Log().Warn(() => @"{0} packages {1}.".format_with(list.Count, configuration.LocalOnly ? "installed" : "found"));

                    if (configuration.LocalOnly && configuration.IncludeRegistryPrograms)
                    {
                        report_registry_programs(configuration, list);
                    }
                }
            }
        }

        private void report_registry_programs(ChocolateyConfiguration configuration, ConcurrentDictionary<string, PackageResult> list)
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
                    if (configuration.Verbose) this.Log().Info(" InstallLocation: {0}{1} Uninstall:{2}".format_with(key.InstallLocation, Environment.NewLine, key.UninstallString));
                }
                this.Log().Warn(() => @"{0} applications not managed with Chocolatey.".format_with(machineInstalled.Count));
            }
        }

        public void pack_noop(ChocolateyConfiguration configuration)
        {
            _nugetService.pack_noop(configuration);
        }

        public void pack_run(ChocolateyConfiguration configuration)
        {
            _nugetService.pack_run(configuration);
        }

        public void install_noop(ChocolateyConfiguration configuration)
        {
            _nugetService.install_noop(configuration, (pkg) => _powershellService.install_noop(pkg));
        }

        public void handle_package_result(PackageResult packageResult, ChocolateyConfiguration configuration, CommandNameType commandName)
        {
            var pkgInfo = _packageInfoService.get_package_information(packageResult.Package);
            if (configuration.AllowMultipleVersions)
            {
                pkgInfo.IsSideBySide = true;
            }

            if (!configuration.SkipPackageInstallProvider)
            {
                var before = _registryService.get_installer_keys();

                var powerShellRan = _powershellService.install(configuration, packageResult);
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
                _shimgenService.install(configuration, packageResult);
            }

            _packageInfoService.save_package_information(pkgInfo);
            ensure_bad_package_path_is_clean(configuration, packageResult);

            if (!packageResult.Success)
            {
                this.Log().Error(ChocolateyLoggers.Important, "{0} {1} not successful.".format_with(packageResult.Name, commandName.to_string()));
                handle_unsuccessful_install(packageResult);

                return;
            }

            this.Log().Info(ChocolateyLoggers.Important, " {0} has been {1}ed successfully.".format_with(packageResult.Name, commandName.to_string()));
        }

        public ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration configuration)
        {
            //todo:are we installing from an alternate source? If so run that command instead

            this.Log().Info(@"Installing the following packages:");
            this.Log().Info(ChocolateyLoggers.Important, @"{0}".format_with(configuration.PackageNames));
            this.Log().Info(@"By installing you accept licenses for the packages.");

            var packageInstalls = _nugetService.install_run(
                configuration,
                (packageResult) => handle_package_result(packageResult, configuration, CommandNameType.install)
                );

            var installFailures = packageInstalls.Count(p => !p.Value.Success);
            this.Log().Warn(() => @"{0}{1} installed {2}/{3} packages. {4} packages failed.{0}See the log for details.".format_with(
                Environment.NewLine,
                ApplicationParameters.Name,
                packageInstalls.Count(p => p.Value.Success && !p.Value.Inconclusive),
                packageInstalls.Count,
                installFailures));

            if (installFailures != 0 && Environment.ExitCode == 0)
            {
                Environment.ExitCode = 1;
            }

            return packageInstalls;
        }

        public void upgrade_noop(ChocolateyConfiguration configuration)
        {
            _nugetService.upgrade_noop(configuration, (pkg) => _powershellService.install_noop(pkg));
        }

        public ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration configuration)
        {
            //todo:are we upgrading an alternate source? If so run that command instead

            this.Log().Info(@"Upgrading the following packages:");
            this.Log().Info(ChocolateyLoggers.Important, @"{0}".format_with(configuration.PackageNames));
            this.Log().Info(@"By installing you accept licenses for the packages.");

            var packageUpgrades = _nugetService.upgrade_run(
                configuration,
                (packageResult) => handle_package_result(packageResult, configuration, CommandNameType.upgrade)
                );

            var upgradeFailures = packageUpgrades.Count(p => !p.Value.Success);
            this.Log().Warn(() => @"{0}{1} upgraded {2}/{3} packages. {4} packages failed.{0}See the log for details.".format_with(
                Environment.NewLine,
                ApplicationParameters.Name,
                packageUpgrades.Count(p => p.Value.Success && !p.Value.Inconclusive),
                packageUpgrades.Count,
                upgradeFailures));

            if (upgradeFailures != 0 && Environment.ExitCode == 0)
            {
                Environment.ExitCode = 1;
            }

            return packageUpgrades;
        }

        public void uninstall_noop(ChocolateyConfiguration configuration)
        {
            _nugetService.uninstall_noop(configuration, (pkg) => { _powershellService.uninstall_noop(pkg); });
        }

        public ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration configuration)
        {
            this.Log().Info(@"Uninstalling the following packages:");
            this.Log().Info(ChocolateyLoggers.Important, @"{0}".format_with(configuration.PackageNames));

            var packageUninstalls = _nugetService.uninstall_run(
                configuration,
                (packageResult) =>
                    {
                        if (!_fileSystem.directory_exists(packageResult.InstallLocation))
                        {
                            packageResult.InstallLocation += ".{0}".format_with(packageResult.Package.Version.to_string());
                        }

                        _shimgenService.uninstall(configuration, packageResult);

                        var powershellRan = false;
                        if (!configuration.SkipPackageInstallProvider)
                        {
                            powershellRan = _powershellService.uninstall(configuration, packageResult);
                        }

                        if (!powershellRan)
                        {
                            var pkgInfo = _packageInfoService.get_package_information(packageResult.Package);

                            if (pkgInfo.RegistrySnapshot != null)
                            {
                                //todo:refactor this crap
                                this.Log().Info(" Running AutoUninstaller...");

                                foreach (var key in pkgInfo.RegistrySnapshot.RegistryKeys.or_empty_list_if_null())
                                {
                                    this.Log().Debug(() => " Preparing uninstall key '{0}'".format_with(key.UninstallString));
                                    // split on " /" and " -" for quite a bit more accuracy
                                    IList<string> uninstallArgs = key.UninstallString.to_string().Split(new[] {" /", " -"}, StringSplitOptions.RemoveEmptyEntries).ToList();
                                    var uninstallExe = uninstallArgs.DefaultIfEmpty(string.Empty).FirstOrDefault().Replace("\"","");
                                    this.Log().Debug(() => " Uninstaller path is '{0}'".format_with(uninstallExe));
                                    uninstallArgs.Remove(uninstallExe);

                                    if (!key.HasQuietUninstall)
                                    {
                                        IInstaller installer = new CustomInstaller();

                                        //refactor this to elsewhere
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

                                    this.Log().Debug(() => " Args are '{0}'".format_with(uninstallArgs.join(" ")));

                                    var exitCode = CommandExecutor.execute(
                                        uninstallExe, uninstallArgs.join(" "), true,
                                        (s, e) =>
                                            {
                                                if (string.IsNullOrWhiteSpace(e.Data)) return;
                                                this.Log().Debug(() => " [AutoUninstaller] {0}".format_with(e.Data));
                                            },
                                        (s, e) =>
                                            {
                                                if (string.IsNullOrWhiteSpace(e.Data)) return;
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
                                        this.Log().Info(() => " AutoUninstaller has successfully uninstalled {0} from your machine install".format_with(packageResult.Package.Id));
                                    }
                                }
                            }
                        }

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

        private void ensure_bad_package_path_is_clean(ChocolateyConfiguration configuration, PackageResult packageResult)
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
                if (configuration.Debug)
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