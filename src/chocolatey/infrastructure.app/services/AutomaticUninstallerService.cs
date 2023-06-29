// Copyright © 2017 - 2021 Chocolatey Software, Inc
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
        private const int SleepTimeInSeconds = 2;
        private const string SkipFileName = ".skipAutoUninstall";

        public AutomaticUninstallerService(IChocolateyPackageInformationService packageInfoService, IFileSystem fileSystem, IRegistryService registryService, ICommandExecutor commandExecutor)
        {
            _packageInfoService = packageInfoService;
            _fileSystem = fileSystem;
            _registryService = registryService;
            _commandExecutor = commandExecutor;
            WaitForCleanup = true;
        }

        public bool WaitForCleanup { get; set; }

        public void Run(PackageResult packageResult, ChocolateyConfiguration config)
        {
            if (!config.Features.AutoUninstaller)
            {
                this.Log().Info(" Skipping auto uninstaller - AutoUninstaller feature is not enabled.");
                return;
            }

            var packageLocation = packageResult.InstallLocation;
            if (!string.IsNullOrWhiteSpace(packageLocation))
            {
                var skipFiles = _fileSystem.GetFiles(packageLocation, SkipFileName + "*", SearchOption.AllDirectories).Where(p => !p.ToLowerSafe().ContainsSafe("\\templates\\"));
                if (skipFiles.Count() != 0)
                {
                    this.Log().Info(" Skipping auto uninstaller - Package contains a skip file ('{0}').".FormatWith(SkipFileName));
                    return;
                }
            }


            var pkgInfo = _packageInfoService.Get(packageResult.PackageMetadata);
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
                this.Log().Debug("  Sleeping for {0} seconds to allow Windows to finish cleaning up.".FormatWith(SleepTimeInSeconds));
                Thread.Sleep((int)TimeSpan.FromSeconds(SleepTimeInSeconds).TotalMilliseconds);
            }

            foreach (var key in registryKeys.OrEmpty())
            {
                var packageCacheLocation = _fileSystem.CombinePaths(_fileSystem.GetFullPath(config.CacheLocation), package.Id, package.Version.ToNormalizedStringChecked());
                Remove(key, config, packageResult, packageCacheLocation);
            }
        }

        public void Remove(RegistryApplicationKey key, ChocolateyConfiguration config, PackageResult packageResult, string packageCacheLocation)
        {
            var userProvidedUninstallArguments = string.Empty;
            var userOverrideUninstallArguments = false;
            var package = packageResult.PackageMetadata;
            if (package != null)
            {
                if (!PackageUtility.PackageIdHasDependencySuffix(config, package.Id) || config.ApplyInstallArgumentsToDependencies)
                {
                    userProvidedUninstallArguments = config.InstallArguments;
                    userOverrideUninstallArguments = config.OverrideArguments;

                    if (!string.IsNullOrWhiteSpace(userProvidedUninstallArguments)) this.Log().Debug(ChocolateyLoggers.Verbose, " Using user passed {2}uninstaller args for {0}:'{1}'".FormatWith(package.Id, userProvidedUninstallArguments.EscapeCurlyBraces(), userOverrideUninstallArguments ? "overriding " : string.Empty));
                }
            }

            //todo: #2562 if there is a local package, look to use it in the future
            if (string.IsNullOrWhiteSpace(key.UninstallString))
            {
                this.Log().Info(" Skipping auto uninstaller - '{0}' does not have an uninstall string.".FormatWith(!string.IsNullOrEmpty(key.DisplayName.ToStringSafe()) ? key.DisplayName.ToStringSafe().EscapeCurlyBraces() : "The application"));
                return;
            }

            this.Log().Debug(() => " Preparing uninstall key '{0}' for '{1}'".FormatWith(key.UninstallString.ToStringSafe().EscapeCurlyBraces(), key.DisplayName.ToStringSafe().EscapeCurlyBraces()));

            if ((!string.IsNullOrWhiteSpace(key.InstallLocation) && !_fileSystem.DirectoryExists(key.InstallLocation.ToStringSafe().UnquoteSafe())) || !_registryService.InstallerKeyExists(key.KeyPath))
            {
                this.Log().Info(" Skipping auto uninstaller - '{0}' appears to have been uninstalled already by other means.".FormatWith(!string.IsNullOrEmpty(key.DisplayName.ToStringSafe()) ? key.DisplayName.ToStringSafe().EscapeCurlyBraces() : "The application"));
                this.Log().Debug(() => " Searched for install path '{0}' - found? {1}".FormatWith(key.InstallLocation.ToStringSafe().EscapeCurlyBraces(), _fileSystem.DirectoryExists(key.InstallLocation)));
                this.Log().Debug(() => " Searched for registry key '{0}' value '{1}' - found? {2}".FormatWith(key.KeyPath.EscapeCurlyBraces(), ApplicationParameters.RegistryValueInstallLocation, _registryService.InstallerKeyExists(key.KeyPath)));
                return;
            }

            // split on " /" and " -" for quite a bit more accuracy
            IList<string> uninstallArgsSplit = key.UninstallString.ToStringSafe().Replace("&quot;","\"").Replace("&apos;","'").Split(new[] { " /", " -" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var uninstallExe = uninstallArgsSplit.DefaultIfEmpty(string.Empty).FirstOrDefault().TrimSafe();
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
                   this.Log().Debug("Error splitting the uninstall string:{0} {1}".FormatWith(Environment.NewLine,ex.ToStringSafe()));
                }
            }
            var uninstallArgs = key.UninstallString.ToStringSafe().Replace("&quot;", "\"").Replace("&apos;", "'").Replace(uninstallExe.ToStringSafe(), string.Empty).TrimSafe();

            uninstallExe = uninstallExe.UnquoteSafe();
            this.Log().Debug(() => " Uninstaller path is '{0}'".FormatWith(uninstallExe));

            if (uninstallExe.ContainsSafe("\\") || uninstallExe.ContainsSafe("/"))
            {
                if (!_fileSystem.FileExists(uninstallExe))
                {
                    this.Log().Info(" Skipping auto uninstaller - The uninstaller file no longer exists. \"{0}\"".FormatWith(uninstallExe));
                    return;
                }
            }

            IInstaller installer = GetInstallerType(key, uninstallExe, uninstallArgs);
            this.Log().Debug(() => " Installer type is '{0}'".FormatWith(installer.GetType().Name));

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
                //todo: #2563 ultimately we should merge keys
                uninstallArgs += " " + installer.BuildUninstallCommandArguments();
            }

            if (!string.IsNullOrWhiteSpace(userProvidedUninstallArguments))
            {
                if (userOverrideUninstallArguments)
                {
                    this.Log().Debug(() => " Replacing original uninstall arguments of '{0}' with '{1}'".FormatWith(uninstallArgs.EscapeCurlyBraces(),userProvidedUninstallArguments.EscapeCurlyBraces()));
                    uninstallArgs = userProvidedUninstallArguments;
                }
                else
                {
                    this.Log().Debug(() => " Appending original uninstall arguments with '{0}'".FormatWith(userProvidedUninstallArguments.EscapeCurlyBraces()));
                    uninstallArgs += " " + userProvidedUninstallArguments;
                }
            }

            this.Log().Debug(() => " Setting up uninstall logging directory at {0}".FormatWith(packageCacheLocation.EscapeCurlyBraces()));
            _fileSystem.EnsureDirectoryExists(_fileSystem.GetDirectoryName(packageCacheLocation));
            uninstallArgs = uninstallArgs.Replace(InstallTokens.PackageLocation, packageCacheLocation);
            uninstallArgs = uninstallArgs.Replace(InstallTokens.TempLocation, packageCacheLocation);

            this.Log().Debug(() => " Args are '{0}'".FormatWith(uninstallArgs.EscapeCurlyBraces()));

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

                var selection = InteractivePrompt.PromptForConfirmation(
                    "Uninstall may not be silent (could not detect). Proceed?",
                    new[] { "yes", "no" },
                    defaultChoice: "no",
                    requireAnswer: true,
                    allowShortAnswer: true,
                    shortPrompt: true,
                    timeoutInSeconds: timeout
                    );
                if (selection.IsEqualTo("yes")) skipUninstaller = false;

                if (skipUninstaller)
                {
                    this.Log().Info(" Skipping auto uninstaller - Installer type was not detected and no silent uninstall key exists.");
                    this.Log().Warn("If the application was not removed with a chocolateyUninstall.ps1,{0} please remove it from Programs and Features manually.".FormatWith(Environment.NewLine));
                    return;
                }
            }

            var exitCode = _commandExecutor.Execute(
                uninstallExe,
                uninstallArgs.TrimSafe(),
                config.CommandExecutionTimeoutSeconds,
                (s, e) =>
                {
                    if (e == null || string.IsNullOrWhiteSpace(e.Data)) return;
                    this.Log().Info(() => " [AutoUninstaller] {0}".FormatWith(e.Data.EscapeCurlyBraces()));
                },
                (s, e) =>
                {
                    if (e == null || string.IsNullOrWhiteSpace(e.Data)) return;
                    this.Log().Error(() => " [AutoUninstaller] {0}".FormatWith(e.Data.EscapeCurlyBraces()));
                },
                updateProcessPath: false);

            if (!installer.ValidUninstallExitCodes.Contains(exitCode))
            {
                Environment.ExitCode = exitCode;
                string logMessage = " Auto uninstaller failed. Please remove machine installation manually.{0} Exit code was {1}".FormatWith(Environment.NewLine, exitCode);
                this.Log().Error(() => logMessage.EscapeCurlyBraces());
                packageResult.Messages.Add(new ResultMessage(config.Features.FailOnAutoUninstaller ? ResultType.Error : ResultType.Warn, logMessage));
            }
            else
            {
                this.Log().Info(() => " Auto uninstaller has successfully uninstalled {0} or detected previous uninstall.".FormatWith(packageResult.PackageMetadata.Id));
            }
        }

        public virtual IInstaller GetInstallerType(RegistryApplicationKey key, string uninstallExe, string uninstallArgs)
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

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void run(PackageResult packageResult, ChocolateyConfiguration config)
            => Run(packageResult, config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void remove(RegistryApplicationKey key, ChocolateyConfiguration config, PackageResult packageResult, string packageCacheLocation)
            => Remove(key, config, packageResult, packageCacheLocation);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual IInstaller get_installer_type(RegistryApplicationKey key, string uninstallExe, string uninstallArgs)
            => GetInstallerType(key, uninstallExe, uninstallArgs);
#pragma warning restore IDE1006
    }
}
