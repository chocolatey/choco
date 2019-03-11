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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using commandline;
    using configuration;
    using domain;
    using domain.installers;
    using filesystem;
    using infrastructure.commands;
    using logging;
    using results;
    using utility;

    public class AutomaticUninstallerService : IAutomaticUninstallerService
    {
        private readonly IChocolateyPackageInformationService _packageInfoService;
        private readonly IFileSystem _fileSystem;
        private readonly IRegistryService _registryService;
        private readonly ICommandExecutor _commandExecutor;
        private const int SLEEP_TIME = 2;
        private const string SKIP_FILE_NAME = ".skipAutoUninstall";

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

            var packageLocation = packageResult.InstallLocation;
            if (!string.IsNullOrWhiteSpace(packageLocation))
            {
                var skipFiles = _fileSystem.get_files(packageLocation, SKIP_FILE_NAME + "*", SearchOption.AllDirectories).Where(p => !p.to_lower().contains("\\templates\\"));
                if (skipFiles.Count() != 0)
                {
                    this.Log().Info(" Skipping auto uninstaller - Package contains a skip file ('{0}').".format_with(SKIP_FILE_NAME));
                    return;
                }
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

            var package = pkgInfo.Package;
            if (package == null)
            {
                this.Log().Info(" Skipping auto uninstaller - No package in package information.");
                return;
            }

            this.Log().Info(" Running auto uninstaller...");
            if (WaitForCleanup)
            {
                this.Log().Debug("  Sleeping for {0} seconds to allow Windows to finish cleaning up.".format_with(SLEEP_TIME));
                Thread.Sleep((int)TimeSpan.FromSeconds(SLEEP_TIME).TotalMilliseconds);
            }

            foreach (var key in registryKeys.or_empty_list_if_null())
            {
                var packageCacheLocation = _fileSystem.combine_paths(_fileSystem.get_full_path(config.CacheLocation), package.Id, package.Version.to_string());
                remove(key, config, packageResult, packageCacheLocation);
            }
        }

        public void remove(RegistryApplicationKey key, ChocolateyConfiguration config, PackageResult packageResult, string packageCacheLocation)
        {
            var userProvidedUninstallArguments = string.Empty;
            var userOverrideUninstallArguments = false;
            var package = packageResult.Package;
            if (package != null)
            {
                if (!PackageUtility.package_is_a_dependency(config, package.Id) || config.ApplyInstallArgumentsToDependencies)
                {
                    userProvidedUninstallArguments = config.InstallArguments;
                    userOverrideUninstallArguments = config.OverrideArguments;

                    if (!string.IsNullOrWhiteSpace(userProvidedUninstallArguments)) this.Log().Debug(ChocolateyLoggers.Verbose, " Using user passed {2}uninstaller args for {0}:'{1}'".format_with(package.Id, userProvidedUninstallArguments.escape_curly_braces(), userOverrideUninstallArguments ? "overriding " : string.Empty));
                }
            }

            //todo: if there is a local package, look to use it in the future
            if (string.IsNullOrWhiteSpace(key.UninstallString))
            {
                this.Log().Info(" Skipping auto uninstaller - '{0}' does not have an uninstall string.".format_with(!string.IsNullOrEmpty(key.DisplayName.to_string()) ? key.DisplayName.to_string().escape_curly_braces() : "The application"));
                return;
            }

            this.Log().Debug(() => " Preparing uninstall key '{0}' for '{1}'".format_with(key.UninstallString.to_string().escape_curly_braces(), key.DisplayName.to_string().escape_curly_braces()));

            if ((!string.IsNullOrWhiteSpace(key.InstallLocation) && !_fileSystem.directory_exists(key.InstallLocation)) || !_registryService.installer_value_exists(key.KeyPath, ApplicationParameters.RegistryValueInstallLocation))
            {
                this.Log().Info(" Skipping auto uninstaller - '{0}' appears to have been uninstalled already by other means.".format_with(!string.IsNullOrEmpty(key.DisplayName.to_string()) ? key.DisplayName.to_string().escape_curly_braces() : "The application"));
                this.Log().Debug(() => " Searched for install path '{0}' - found? {1}".format_with(key.InstallLocation.to_string().escape_curly_braces(), _fileSystem.directory_exists(key.InstallLocation)));
                this.Log().Debug(() => " Searched for registry key '{0}' value '{1}' - found? {2}".format_with(key.KeyPath.escape_curly_braces(), ApplicationParameters.RegistryValueInstallLocation, _registryService.installer_value_exists(key.KeyPath, ApplicationParameters.RegistryValueInstallLocation)));
                return;
            }

            // split on " /" and " -" for quite a bit more accuracy
            IList<string> uninstallArgsSplit = key.UninstallString.to_string().Replace("&quot;","\"").Replace("&apos;","'").Split(new[] { " /", " -" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var uninstallExe = uninstallArgsSplit.DefaultIfEmpty(string.Empty).FirstOrDefault().trim_safe();
            if (uninstallExe.Count(u => u == '"') > 2)
            {
                uninstallExe = uninstallExe.Split(new []{" \""}, StringSplitOptions.RemoveEmptyEntries).First();
            }

            if (uninstallExe.Count(u => u == ':') > 1)
            {
                try
                {
                    var firstMatch = Regex.Match(uninstallExe, @"\s+\w\:",RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                    uninstallExe = uninstallExe.Substring(0, firstMatch.Index);
                }
                catch (Exception ex)
                {
                   this.Log().Debug("Error splitting the uninstall string:{0} {1}".format_with(Environment.NewLine,ex.to_string()));
                }
            }
            var uninstallArgs = key.UninstallString.to_string().Replace("&quot;", "\"").Replace("&apos;", "'").Replace(uninstallExe.to_string(), string.Empty).trim_safe();

            uninstallExe = uninstallExe.remove_surrounding_quotes();
            this.Log().Debug(() => " Uninstaller path is '{0}'".format_with(uninstallExe));

            if (uninstallExe.contains("\\") || uninstallExe.contains("/"))
            {
                if (!_fileSystem.file_exists(uninstallExe))
                {
                    this.Log().Info(" Skipping auto uninstaller - The uninstaller file no longer exists. \"{0}\"".format_with(uninstallExe));
                    return;
                }
            }

            IInstaller installer = get_installer_type(key, uninstallExe, uninstallArgs);
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

            if (!string.IsNullOrWhiteSpace(userProvidedUninstallArguments))
            {
                if (userOverrideUninstallArguments)
                {
                    this.Log().Debug(() => " Replacing original uninstall arguments of '{0}' with '{1}'".format_with(uninstallArgs.escape_curly_braces(),userProvidedUninstallArguments.escape_curly_braces()));
                    uninstallArgs = userProvidedUninstallArguments;
                }
                else
                {
                    this.Log().Debug(() => " Appending original uninstall arguments with '{0}'".format_with(userProvidedUninstallArguments.escape_curly_braces()));
                    uninstallArgs += " " + userProvidedUninstallArguments;
                }
            }

            this.Log().Debug(() => " Setting up uninstall logging directory at {0}".format_with(packageCacheLocation.escape_curly_braces()));
            _fileSystem.create_directory_if_not_exists(_fileSystem.get_directory_name(packageCacheLocation));
            uninstallArgs = uninstallArgs.Replace(InstallTokens.PACKAGE_LOCATION, packageCacheLocation);
            uninstallArgs = uninstallArgs.Replace(InstallTokens.TEMP_LOCATION, packageCacheLocation);

            this.Log().Debug(() => " Args are '{0}'".format_with(uninstallArgs.escape_curly_braces()));

            if (!key.HasQuietUninstall && installer.GetType() == typeof(CustomInstaller))
            {
                if (!config.Information.IsLicensedVersion)
                {
                    this.Log().Warn(@"
  Did you know licensed versions of Chocolatey are 95% effective with 
   Automatic Uninstaller due to licensed enhancements and Package 
   Synchronizer?
");
                }

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

        public virtual IInstaller get_installer_type(RegistryApplicationKey key, string uninstallExe, string uninstallArgs)
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
                    installer = new InstallShieldInstaller();
                    break;
            }

            return installer;
        }
    }
}