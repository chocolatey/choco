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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.Reflection;
    using adapters;
    using commandline;
    using configuration;
    using cryptography;
    using domain;
    using infrastructure.commands;
    using infrastructure.registration;
    using logging;
    using NuGet.Packaging;
    using NuGet.Protocol.Core.Types;
    using powershell;
    using results;
    using utility;
    using CryptoHashProvider = cryptography.CryptoHashProvider;
    using Environment = System.Environment;
    using IFileSystem = filesystem.IFileSystem;

    public class PowershellService : IPowershellService
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _customImports;

        public PowershellService(IFileSystem fileSystem)
            : this(fileSystem, new CustomString(string.Empty))
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="PowershellService" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="customImports">The custom imports. This should be everything you need minus the &amp; to start and the ending semi-colon.</param>
        public PowershellService(IFileSystem fileSystem, CustomString customImports)
        {
            _fileSystem = fileSystem;
            _customImports = customImports;
        }

        private string GetPackageScriptForAction(PackageResult packageResult, CommandNameType command)
        {
            var file = "chocolateyInstall.ps1";
            switch (command)
            {
                case CommandNameType.Uninstall:
                    file = "chocolateyUninstall.ps1";
                    break;

                case CommandNameType.Upgrade:
                    file = "chocolateyBeforeModify.ps1";
                    break;
            }

            var packageDirectory = packageResult.InstallLocation;
            var installScript = _fileSystem.GetFiles(packageDirectory, file, SearchOption.AllDirectories).Where(p => !p.ToLowerSafe().ContainsSafe("\\templates\\"));
            if (installScript.Count() != 0)
            {
                return installScript.FirstOrDefault();
            }

            return string.Empty;
        }

        private IEnumerable<string> GetHookScripts(ChocolateyConfiguration configuration, PackageResult packageResult, CommandNameType command, bool isPreHook)
        {
            List<string> hookScriptPaths = new List<string>();

            // If skipping hook scripts, return an empty list
            if (configuration.SkipHookScripts) return hookScriptPaths;

            // If hooks directory doesn't exist, return an empty list to prevent directory not exist warnings
            if (!_fileSystem.DirectoryExists(ApplicationParameters.HooksLocation)) return hookScriptPaths;

            string filenameBase;

            if (isPreHook)
            {
                filenameBase = "pre-";
            }
            else
            {
                filenameBase = "post-";
            }

            switch (command)
            {
                case CommandNameType.Install:
                    filenameBase += "install-";
                    break;

                case CommandNameType.Uninstall:
                    filenameBase += "uninstall-";
                    break;

                case CommandNameType.Upgrade:
                    filenameBase += "beforemodify-";
                    break;

                default:
                    throw new ApplicationException("Could not find CommandNameType '{0}' to get hook scripts".FormatWith(command));
            }

            hookScriptPaths.AddRange(_fileSystem.GetFiles(ApplicationParameters.HooksLocation, "{0}all.ps1".FormatWith(filenameBase), SearchOption.AllDirectories));
            hookScriptPaths.AddRange(_fileSystem.GetFiles(ApplicationParameters.HooksLocation, "{0}{1}.ps1".FormatWith(filenameBase, packageResult.Name), SearchOption.AllDirectories));

            return hookScriptPaths;
        }

        public void DryRunAction(PackageResult packageResult, CommandNameType command)
        {
            var chocoInstall = GetPackageScriptForAction(packageResult, command);
            if (!string.IsNullOrEmpty(chocoInstall))
            {
                this.Log().Info("Would have run '{0}':".FormatWith(_fileSystem.GetFileName(chocoInstall)));
                this.Log().Warn(_fileSystem.ReadFile(chocoInstall).EscapeCurlyBraces());
            }
        }

        public void InstallDryRun(PackageResult packageResult)
        {
            DryRunAction(packageResult, CommandNameType.Install);
        }

        public bool Install(ChocolateyConfiguration configuration, PackageResult packageResult)
        {
            return RunAction(configuration, packageResult, CommandNameType.Install);
        }

        public void UninstallDryRun(PackageResult packageResult)
        {
            DryRunAction(packageResult, CommandNameType.Uninstall);
        }

        public bool Uninstall(ChocolateyConfiguration configuration, PackageResult packageResult)
        {
            return RunAction(configuration, packageResult, CommandNameType.Uninstall);
        }

        public void BeforeModifyDryRun(PackageResult packageResult)
        {
            DryRunAction(packageResult, CommandNameType.Upgrade);
        }

        public bool BeforeModify(ChocolateyConfiguration configuration, PackageResult packageResult)
        {
            return RunAction(configuration, packageResult, CommandNameType.Upgrade);
        }

        private string GetHelpersFolder()
        {
            return _fileSystem.CombinePaths(ApplicationParameters.InstallLocation, "helpers");
        }

        public string WrapScriptWithModule(string script, IEnumerable<string> hookPreScriptPathList, IEnumerable<string> hookPostScriptPathList, ChocolateyConfiguration config)
        {
            var installerModule = _fileSystem.CombinePaths(GetHelpersFolder(), "chocolateyInstaller.psm1");
            var scriptRunner = _fileSystem.CombinePaths(GetHelpersFolder(), "chocolateyScriptRunner.ps1");

            // removed setting all errors to terminating. Will cause too
            // many issues in existing packages, including upgrading
            // Chocolatey from older POSH client due to log errors
            //$ErrorActionPreference = 'Stop';
            return "[System.Threading.Thread]::CurrentThread.CurrentCulture = '';[System.Threading.Thread]::CurrentThread.CurrentUICulture = '';[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::SystemDefault; & import-module -name '{0}';{2} & '{1}' {3}"
                .FormatWith(
                    installerModule,
                    scriptRunner,
                    string.IsNullOrWhiteSpace(_customImports) ? string.Empty : "& {0}".FormatWith(_customImports.EndsWith(";") ? _customImports : _customImports + ";"),
                    GetScriptArguments(script, hookPreScriptPathList, hookPostScriptPathList, config)
                );
        }

        private string GetScriptArguments(string script, IEnumerable<string> hookPreScriptPathList, IEnumerable<string> hookPostScriptPathList, ChocolateyConfiguration config)
        {
            return "-packageScript '{0}' -installArguments '{1}' -packageParameters '{2}'{3}{4} -preRunHookScripts {5} -postRunHookScripts {6}".FormatWith(
                script,
                EscapePowerShellArguments(config.InstallArguments),
                EscapePowerShellArguments(config.PackageParameters),
                config.ForceX86 ? " -forceX86" : string.Empty,
                config.OverrideArguments ? " -overrideArgs" : string.Empty,
                hookPreScriptPathList.Any() ? "{0}".FormatWith(string.Join(",", hookPreScriptPathList)) : "$null",
                hookPostScriptPathList.Any() ? "{0}".FormatWith(string.Join(",", hookPostScriptPathList)) : "$null"
             );
        }

        private string EscapePowerShellArguments(string argument)
        {
            return argument.ToStringSafe().Replace("\"", "\\\"");
        }

        public bool RunAction(ChocolateyConfiguration configuration, PackageResult packageResult, CommandNameType command)
        {
            var installerRun = false;

            Debug.Assert(packageResult.PackageMetadata != null, "Package Metadata is null");
            Debug.Assert(packageResult.SearchMetadata != null, "SearchMetadata is null");

            var packageDirectory = packageResult.InstallLocation;
            if (packageDirectory.IsEqualTo(ApplicationParameters.InstallLocation) || packageDirectory.IsEqualTo(ApplicationParameters.PackagesLocation))
            {
                packageResult.Messages.Add(
                    new ResultMessage(
                        ResultType.Error,
                        "Install location is not specific enough, cannot run PowerShell script:{0} Erroneous install location captured as '{1}'".FormatWith(Environment.NewLine, packageResult.InstallLocation)
                        )
                    );

                return false;
            }

            if (!_fileSystem.DirectoryExists(packageDirectory))
            {
                packageResult.Messages.Add(new ResultMessage(ResultType.Error, "Package install not found:'{0}'".FormatWith(packageDirectory)));
                return installerRun;
            }

            var chocoPowerShellScript = GetPackageScriptForAction(packageResult, command);

            var hookPreScriptPathList = GetHookScripts(configuration, packageResult, command, true);
            var hookPostScriptPathList = GetHookScripts(configuration, packageResult, command, false);

            foreach (var hookScriptPath in hookPreScriptPathList.Concat(hookPostScriptPathList).OrEmpty())
            {
                this.Log().Debug(ChocolateyLoggers.Important, "Contents of '{0}':".FormatWith(chocoPowerShellScript));
                string hookScriptContents = _fileSystem.ReadFile(hookScriptPath);
                this.Log().Debug(() => hookScriptContents.EscapeCurlyBraces());
            }

            if (!string.IsNullOrEmpty(chocoPowerShellScript) || hookPreScriptPathList.Any() || hookPostScriptPathList.Any())
            {
                var failure = false;

                var package = packageResult.SearchMetadata;
                PreparePowerShellEnvironment(package, configuration, packageDirectory);
                bool shouldRun = !configuration.PromptForConfirmation;

                if (!string.IsNullOrEmpty(chocoPowerShellScript))
                {
                    this.Log().Debug(ChocolateyLoggers.Important, "Contents of '{0}':".FormatWith(chocoPowerShellScript));
                    string chocoPowerShellScriptContents = _fileSystem.ReadFile(chocoPowerShellScript);
                    // leave this way, doesn't take it through formatting.
                    this.Log().Debug(() => chocoPowerShellScriptContents.EscapeCurlyBraces());

                    if (!shouldRun)
                    {
                        this.Log().Info(ChocolateyLoggers.Important, () => "The package {0} wants to run '{1}'.".FormatWith(packageResult.Name, _fileSystem.GetFileName(chocoPowerShellScript)));
                        this.Log().Info(ChocolateyLoggers.Important, () => "Note: If you don't run this script, the installation will fail.");
                        this.Log().Info(ChocolateyLoggers.Important, () => @"Note: To confirm automatically next time, use '-y' or consider:");
                        this.Log().Info(ChocolateyLoggers.Important, () => @"choco feature enable -n allowGlobalConfirmation");

                        var selection = InteractivePrompt.PromptForConfirmation(@"Do you want to run the script?",
                            new[] { "yes", "all - yes to all", "no", "print" },
                            defaultChoice: null,
                            requireAnswer: true,
                            allowShortAnswer: true,
                            shortPrompt: true
                        );

                        if (selection.IsEqualTo("print"))
                        {
                            this.Log().Info(ChocolateyLoggers.Important, "------ BEGIN SCRIPT ------");
                            this.Log().Info(() => "{0}{1}{0}".FormatWith(Environment.NewLine, chocoPowerShellScriptContents.EscapeCurlyBraces()));
                            this.Log().Info(ChocolateyLoggers.Important, "------- END SCRIPT -------");
                            selection = InteractivePrompt.PromptForConfirmation(@"Do you want to run this script?",
                                new[] { "yes", "no" },
                                defaultChoice: null,
                                requireAnswer: true,
                                allowShortAnswer: true,
                                shortPrompt: true
                            );
                        }

                        if (selection.IsEqualTo("yes")) shouldRun = true;
                        if (selection.IsEqualTo("all - yes to all"))
                        {
                            configuration.PromptForConfirmation = false;
                            shouldRun = true;
                        }

                        if (selection.IsEqualTo("no"))
                        {
                            //MSI ERROR_INSTALL_USEREXIT - 1602 - https://support.microsoft.com/en-us/kb/304888 / https://msdn.microsoft.com/en-us/library/aa376931.aspx
                            //ERROR_INSTALL_CANCEL - 15608 - https://msdn.microsoft.com/en-us/library/windows/desktop/ms681384.aspx
                            Environment.ExitCode = 15608;
                            packageResult.Messages.Add(new ResultMessage(ResultType.Error, "User canceled powershell portion of installation for '{0}'.{1} Specify -n to skip automated script actions.".FormatWith(chocoPowerShellScript, Environment.NewLine)));
                        }
                    }
                }
                else
                {
                    shouldRun = true;
                    this.Log().Info("No package automation script, running only hooks", ChocolateyLoggers.Important);
                }

                if (shouldRun)
                {
                    installerRun = true;

                    if (configuration.Features.UsePowerShellHost)
                    {
                        AddAssemblyResolver();
                    }

                    var result = new PowerShellExecutionResults
                    {
                        ExitCode = -1
                    };

                    try
                    {
                        result = configuration.Features.UsePowerShellHost
                                    ? Execute.WithTimeout(configuration.CommandExecutionTimeoutSeconds).Command(() => RunHost(configuration, chocoPowerShellScript, null, hookPreScriptPathList, hookPostScriptPathList), result)
                                    : RunExternalPowerShell(configuration, chocoPowerShellScript, hookPreScriptPathList, hookPostScriptPathList);
                    }
                    catch (Exception ex)
                    {
                        this.Log().Error(ex.Message.EscapeCurlyBraces());
                        result.ExitCode = 1;
                    }

                    if (configuration.Features.UsePowerShellHost)
                    {
                        RemoveAssemblyResolver();
                        HttpsSecurity.Reset();
                    }

                    if (result.StandardErrorWritten && configuration.Features.FailOnStandardError)
                    {
                        failure = true;
                    }
                    else if (result.StandardErrorWritten && result.ExitCode == 0)
                    {
                        this.Log().Warn(
                            () =>
                            @"Only an exit code of non-zero will fail the package by default. Set
 `--failonstderr` if you want error messages to also fail a script. See
 `choco -h` for details.");
                    }

                    if (result.ExitCode != 0)
                    {
                        Environment.ExitCode = result.ExitCode;
                        packageResult.ExitCode = result.ExitCode;
                    }

                    // 0 - most widely used success exit code
                    // MSI valid exit codes
                    // 1605 - (uninstall) - the product is not found, could have already been uninstalled
                    // 1614 (uninstall) - the product is uninstalled
                    // 1641 - restart initiated
                    // 3010 - restart required
                    var validExitCodes = new List<int> { 0, 1605, 1614, 1641, 3010 };
                    if (!validExitCodes.Contains(result.ExitCode))
                    {
                        failure = true;
                    }

                    if (!configuration.Features.UsePackageExitCodes)
                    {
                        Environment.ExitCode = failure ? 1 : 0;
                    }

                    if (failure)
                    {
                        packageResult.Messages.Add(new ResultMessage(ResultType.Error, "Error while running '{0}'.{1} See log for details.".FormatWith(chocoPowerShellScript, Environment.NewLine)));
                    }
                    packageResult.Messages.Add(new ResultMessage(ResultType.Note, "Ran '{0}'".FormatWith(chocoPowerShellScript)));
                }
            }

            return installerRun;
        }

        private PowerShellExecutionResults RunExternalPowerShell(ChocolateyConfiguration configuration, string chocoPowerShellScript, IEnumerable<string> hookPreScriptPathList, IEnumerable<string> hookPostScriptPathList)
        {
            var result = new PowerShellExecutionResults();
            result.ExitCode = PowershellExecutor.Execute(
                WrapScriptWithModule(chocoPowerShellScript, hookPreScriptPathList, hookPostScriptPathList, configuration),
                _fileSystem,
                configuration.CommandExecutionTimeoutSeconds,
                (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data)) return;
                    //inspect for different streams
                    if (e.Data.StartsWith("DEBUG:"))
                    {
                        this.Log().Debug(() => " " + e.Data.EscapeCurlyBraces());
                    }
                    else if (e.Data.StartsWith("WARNING:"))
                    {
                        this.Log().Warn(() => " " + e.Data.EscapeCurlyBraces());
                    }
                    else if (e.Data.StartsWith("VERBOSE:"))
                    {
                        this.Log().Info(ChocolateyLoggers.Verbose, () => " " + e.Data.EscapeCurlyBraces());
                    }
                    else
                    {
                        this.Log().Info(() => " " + e.Data.EscapeCurlyBraces());
                    }
                },
                (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data)) return;
                    result.StandardErrorWritten = true;
                    this.Log().Error(() => " " + e.Data.EscapeCurlyBraces());
                });

            return result;
        }

        public void PreparePowerShellEnvironment(IPackageSearchMetadata package, ChocolateyConfiguration configuration, string packageDirectory)
        {
            if (package == null) return;

            EnvironmentSettings.UpdateEnvironmentVariables();
            EnvironmentSettings.SetEnvironmentVariables(configuration);

            Environment.SetEnvironmentVariable("chocolateyPackageName", package.Identity.Id);
            Environment.SetEnvironmentVariable("packageName", package.Identity.Id);
            Environment.SetEnvironmentVariable("chocolateyPackageTitle", package.Title);
            Environment.SetEnvironmentVariable("packageTitle", package.Title);
            Environment.SetEnvironmentVariable("chocolateyPackageVersion", package.Identity.Version.ToNormalizedStringChecked());
            Environment.SetEnvironmentVariable("packageVersion", package.Identity.Version.ToNormalizedStringChecked());
            // We use ToStringSafe on purpose here. There is a need for the version
            // the package specified, not the normalized version we want users to use.
            Environment.SetEnvironmentVariable(StringResources.EnvironmentVariables.ChocolateyPackageNuspecVersion, package.Identity.Version.ToStringSafe());
            Environment.SetEnvironmentVariable(StringResources.EnvironmentVariables.PackageNuspecVersion, package.Identity.Version.ToStringSafe());
            Environment.SetEnvironmentVariable("chocolateyPackageVersionPrerelease", package.Identity.Version.Release.ToStringSafe());

            Environment.SetEnvironmentVariable("chocolateyPackageFolder", packageDirectory);
            Environment.SetEnvironmentVariable("packageFolder", packageDirectory);

            // unset variables that may not be updated so they don't get passed again
            Environment.SetEnvironmentVariable("installArguments", null);
            Environment.SetEnvironmentVariable("installerArguments", null);
            Environment.SetEnvironmentVariable("chocolateyInstallArguments", null);
            Environment.SetEnvironmentVariable("chocolateyInstallOverride", null);
            Environment.SetEnvironmentVariable("packageParameters", null);
            Environment.SetEnvironmentVariable("chocolateyPackageParameters", null);
            Environment.SetEnvironmentVariable("chocolateyChecksum32", null);
            Environment.SetEnvironmentVariable("chocolateyChecksum64", null);
            Environment.SetEnvironmentVariable("chocolateyChecksumType32", null);
            Environment.SetEnvironmentVariable("chocolateyChecksumType64", null);
            Environment.SetEnvironmentVariable("chocolateyForceX86", null);
            Environment.SetEnvironmentVariable("DownloadCacheAvailable", null);

            // we only want to pass the following args to packages that would apply.
            // like choco install git --params '' should pass those params to git.install,
            // but not another package unless the switch apply-install-arguments-to-dependencies is used
            if (!PackageUtility.PackageIdHasDependencySuffix(configuration, package.Identity.Id) || configuration.ApplyInstallArgumentsToDependencies)
            {
                this.Log().Debug(ChocolateyLoggers.Verbose, "Setting installer args for {0}".FormatWith(package.Identity.Id));
                Environment.SetEnvironmentVariable("installArguments", configuration.InstallArguments);
                Environment.SetEnvironmentVariable("installerArguments", configuration.InstallArguments);
                Environment.SetEnvironmentVariable("chocolateyInstallArguments", configuration.InstallArguments);

                if (configuration.OverrideArguments)
                {
                    Environment.SetEnvironmentVariable("chocolateyInstallOverride", "true");
                }
            }

            // we only want to pass package parameters to packages that would apply.
            // but not another package unless the switch apply-package-parameters-to-dependencies is used
            if (!PackageUtility.PackageIdHasDependencySuffix(configuration, package.Identity.Id) || configuration.ApplyPackageParametersToDependencies)
            {
                this.Log().Debug(ChocolateyLoggers.Verbose, "Setting package parameters for {0}".FormatWith(package.Identity.Id));
                Environment.SetEnvironmentVariable("packageParameters", configuration.PackageParameters);
                Environment.SetEnvironmentVariable("chocolateyPackageParameters", configuration.PackageParameters);
            }

            if (!string.IsNullOrWhiteSpace(configuration.DownloadChecksum))
            {
                Environment.SetEnvironmentVariable("chocolateyChecksum32", configuration.DownloadChecksum);
                Environment.SetEnvironmentVariable("chocolateyChecksum64", configuration.DownloadChecksum);
            }

            if (!string.IsNullOrWhiteSpace(configuration.DownloadChecksumType))
            {
                Environment.SetEnvironmentVariable("chocolateyChecksumType32", configuration.DownloadChecksumType);
                Environment.SetEnvironmentVariable("chocolateyChecksumType64", configuration.DownloadChecksumType);
            }

            if (!string.IsNullOrWhiteSpace(configuration.DownloadChecksum64))
            {
                Environment.SetEnvironmentVariable("chocolateyChecksum64", configuration.DownloadChecksum64);
            }

            if (!string.IsNullOrWhiteSpace(configuration.DownloadChecksumType64))
            {
                Environment.SetEnvironmentVariable("chocolateyChecksumType64", configuration.DownloadChecksumType64);
            }

            if (configuration.ForceX86)
            {
                Environment.SetEnvironmentVariable("chocolateyForceX86", "true");
            }

            if (configuration.NotSilent)
            {
                Environment.SetEnvironmentVariable("chocolateyInstallOverride", "true");
            }

            //todo:if (configuration.NoOutput)
            //{
            //    Environment.SetEnvironmentVariable("ChocolateyEnvironmentQuiet","true");
            //}

            if (package.IsDownloadCacheAvailable)
            {
                Environment.SetEnvironmentVariable("DownloadCacheAvailable", "true");

                foreach (var downloadCache in package.DownloadCache.OrEmpty())
                {
                    var urlKey = CryptoHashProvider.ComputeStringHash(downloadCache.OriginalUrl, CryptoHashProviderType.Sha256).Replace("=", string.Empty);
                    Environment.SetEnvironmentVariable("CacheFile_{0}".FormatWith(urlKey), downloadCache.FileName);
                    Environment.SetEnvironmentVariable("CacheChecksum_{0}".FormatWith(urlKey), downloadCache.Checksum);
                    Environment.SetEnvironmentVariable("CacheChecksumType_{0}".FormatWith(urlKey), "sha512");
                }
            }

            HttpsSecurity.Reset();
        }

        private ResolveEventHandler _handler = null;

        private void AddAssemblyResolver()
        {
            _handler = (sender, args) =>
            {
                var requestedAssembly = new AssemblyName(args.Name);

                this.Log().Debug(ChocolateyLoggers.Verbose, "Redirecting {0}, requested by '{1}'".FormatWith(args.Name, args.RequestingAssembly == null ? string.Empty : args.RequestingAssembly.FullName));

                AppDomain.CurrentDomain.AssemblyResolve -= _handler;

                // we build against v1 - everything should update in a kosher manner to the newest, but it may not.
                var assembly = TryLoadVersionedAssembly(requestedAssembly, new Version(5, 0, 0, 0)) ?? TryLoadVersionedAssembly(requestedAssembly, new Version(4, 0, 0, 0));
                if (assembly == null) assembly = TryLoadVersionedAssembly(requestedAssembly, new Version(3, 0, 0, 0));
                if (assembly == null) assembly = TryLoadVersionedAssembly(requestedAssembly, new Version(1, 0, 0, 0));

                return assembly;
            };

            AppDomain.CurrentDomain.AssemblyResolve += _handler;
        }

        private System.Reflection.Assembly TryLoadVersionedAssembly(AssemblyName requestedAssembly, Version version)
        {
            if (requestedAssembly == null) return null;

            requestedAssembly.Version = version;

            if (requestedAssembly.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase) && requestedAssembly.CultureInfo.Name.IsEqualTo("en-US"))
            {
                return null;
            }

            try
            {
                return System.Reflection.Assembly.Load(requestedAssembly);
            }
            catch (Exception ex)
            {
                if (requestedAssembly.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase)) return null;

                this.Log().Debug(ChocolateyLoggers.Verbose, "Attempting to load assembly {0} failed:{1} {2}".FormatWith(requestedAssembly.Name, Environment.NewLine, ex.Message.EscapeCurlyBraces()));

                return null;
            }
        }

        private void RemoveAssemblyResolver()
        {
            if (_handler != null)
            {
                AppDomain.CurrentDomain.AssemblyResolve -= _handler;
            }
        }

        public PowerShellExecutionResults RunHost(ChocolateyConfiguration config, string chocoPowerShellScript, Action<Pipeline> additionalActionsBeforeScript, IEnumerable<string> hookPreScriptPathList, IEnumerable<string> hookPostScriptPathList)
        {
            // since we control output in the host, always set these true
            Environment.SetEnvironmentVariable("ChocolateyEnvironmentDebug", "true");
            Environment.SetEnvironmentVariable("ChocolateyEnvironmentVerbose", "true");

            var result = new PowerShellExecutionResults();

            string commandToRun = WrapScriptWithModule(chocoPowerShellScript, hookPreScriptPathList, hookPostScriptPathList, config);
            var host = new PoshHost(config);
            this.Log().Debug(() => "Calling built-in PowerShell host with ['{0}']".FormatWith(commandToRun.EscapeCurlyBraces()));

            var initialSessionState = InitialSessionState.CreateDefault();
            // override system execution policy without accidentally setting it
            initialSessionState.AuthorizationManager = new AuthorizationManager("choco");
            using (var runspace = RunspaceFactory.CreateRunspace(host, initialSessionState))
            {
                runspace.Open();

                // this will affect actual execution policy
                //RunspaceInvoke invoker = new RunspaceInvoke(runspace);
                //invoker.Invoke("Set-ExecutionPolicy ByPass");

                using (var pipeline = runspace.CreatePipeline())
                {
                    // The powershell host itself handles the following items:
                    // * Write-Debug
                    // * Write-Host
                    // * Write-Verbose
                    // * Write-Warning
                    //
                    // the two methods below will pick up Write-Output and Write-Error

                    // Write-Output
                    pipeline.Output.DataReady += (sender, args) =>
                    {
                        PipelineReader<PSObject> reader = sender as PipelineReader<PSObject>;

                        if (reader != null)
                        {
                            while (reader.Count > 0)
                            {
                                host.UI.WriteLine(reader.Read().ToStringSafe().EscapeCurlyBraces());
                            }
                        }
                    };

                    // Write-Error
                    pipeline.Error.DataReady += (sender, args) =>
                    {
                        PipelineReader<object> reader = sender as PipelineReader<object>;

                        if (reader != null)
                        {
                            while (reader.Count > 0)
                            {
                                host.UI.WriteErrorLine(reader.Read().ToStringSafe().EscapeCurlyBraces());
                            }
                        }
                    };

                    var documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify);
                    var currentUserCurrentHostProfile = _fileSystem.CombinePaths(documentsFolder, "WindowsPowerShell\\Microsoft.PowerShell_profile.ps1");
                    var recreateProfileScript = @"
if ((Test-Path(""{0}"")) -and ($profile -eq $null -or $profile -eq '')) {{
  $global:profile = ""{1}""
}}
".FormatWith(documentsFolder, currentUserCurrentHostProfile);

                    pipeline.Commands.Add(new Command(recreateProfileScript, isScript: true, useLocalScope: false));

                    // The PowerShell Output Redirection bug affects System.Management.Automation
                    // it appears with v3 more than others. It is already known to affect v2
                    // this implements the redirection fix from the post below, fixed up with some comments
                    // http://www.leeholmes.com/blog/2008/07/30/workaround-the-os-handles-position-is-not-what-filestream-expected/
                    const string outputRedirectionFixScript = @"
try {
  $bindingFlags = [Reflection.BindingFlags] ""Instance,NonPublic,GetField""
  $objectRef = $host.GetType().GetField(""externalHostRef"", $bindingFlags).GetValue($host)
  $bindingFlags = [Reflection.BindingFlags] ""Instance,NonPublic,GetProperty""
  $consoleHost = $objectRef.GetType().GetProperty(""Value"", $bindingFlags).GetValue($objectRef, @())
  [void] $consoleHost.GetType().GetProperty(""IsStandardOutputRedirected"", $bindingFlags).GetValue($consoleHost, @())
  $bindingFlags = [Reflection.BindingFlags] ""Instance,NonPublic,GetField""
  $field = $consoleHost.GetType().GetField(""standardOutputWriter"", $bindingFlags)
  $field.SetValue($consoleHost, [Console]::Out)
  [void] $consoleHost.GetType().GetProperty(""IsStandardErrorRedirected"", $bindingFlags).GetValue($consoleHost, @())
  $field2 = $consoleHost.GetType().GetField(""standardErrorWriter"", $bindingFlags)
  $field2.SetValue($consoleHost, [Console]::Error)
} catch {
  Write-Output ""Unable to apply redirection fix""
}
";
                    pipeline.Commands.Add(new Command(outputRedirectionFixScript, isScript: true, useLocalScope: false));

                    if (additionalActionsBeforeScript != null) additionalActionsBeforeScript.Invoke(pipeline);

                    pipeline.Commands.Add(new Command(commandToRun, isScript: true, useLocalScope: false));

                    try
                    {
                        pipeline.Invoke();
                    }
                    catch (RuntimeException ex)
                    {
                        var errorStackTrace = ex.StackTrace;
                        var record = ex.ErrorRecord;
                        if (record != null)
                        {
                            // not available in v1
                            //errorStackTrace = record.ScriptStackTrace;
                            var scriptStackTrace = record.GetType().GetProperty("ScriptStackTrace");
                            if (scriptStackTrace != null)
                            {
                                var scriptError = scriptStackTrace.GetValue(record, null).ToStringSafe();
                                if (!string.IsNullOrWhiteSpace(scriptError)) errorStackTrace = scriptError;
                            }
                        }
                        this.Log().Error("ERROR: {0}{1}".FormatWith(ex.Message.EscapeCurlyBraces(), !config.Debug ? string.Empty : "{0} {1}".FormatWith(Environment.NewLine, errorStackTrace.EscapeCurlyBraces())));
                    }
                    catch (Exception ex)
                    {
                        // Unfortunately this doesn't print line number and character. It might be nice to get back to those items unless it involves tons of work.
                        this.Log().Error("ERROR: {0}{1}".FormatWith(ex.Message.EscapeCurlyBraces(), !config.Debug ? string.Empty : "{0} {1}".FormatWith(Environment.NewLine, ex.StackTrace.EscapeCurlyBraces())));
                    }

                    if (pipeline.PipelineStateInfo != null)
                    {
                        switch (pipeline.PipelineStateInfo.State)
                        {
                            // disconnected is not available unless the assembly version is at least v3
                            //case PipelineState.Disconnected:
                            case PipelineState.Running:
                            case PipelineState.NotStarted:
                            case PipelineState.Failed:
                            case PipelineState.Stopping:
                            case PipelineState.Stopped:
                                if (host.ExitCode == 0) host.SetShouldExit(1);
                                host.HostException = pipeline.PipelineStateInfo.Reason;
                                break;

                            case PipelineState.Completed:
                                if (host.ExitCode == -1) host.SetShouldExit(0);
                                break;
                        }
                    }
                }
            }

            this.Log().Debug("Built-in PowerShell host called with ['{0}'] exited with '{1}'.".FormatWith(commandToRun.EscapeCurlyBraces(), host.ExitCode));

            result.ExitCode = host.ExitCode;
            result.StandardErrorWritten = host.StandardErrorWritten;

            return result;
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void noop_action(PackageResult packageResult, CommandNameType command)
            => DryRunAction(packageResult, command);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void install_noop(PackageResult packageResult)
            => InstallDryRun(packageResult);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public bool install(ChocolateyConfiguration configuration, PackageResult packageResult)
            => Install(configuration, packageResult);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void uninstall_noop(PackageResult packageResult)
            => UninstallDryRun(packageResult);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public bool uninstall(ChocolateyConfiguration configuration, PackageResult packageResult)
            => Uninstall(configuration, packageResult);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void before_modify_noop(PackageResult packageResult)
            => BeforeModifyDryRun(packageResult);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public bool before_modify(ChocolateyConfiguration configuration, PackageResult packageResult)
            => BeforeModify(configuration, packageResult);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public string wrap_script_with_module(string script, IEnumerable<string> hookPreScriptPathList, IEnumerable<string> hookPostScriptPathList, ChocolateyConfiguration config)
            => WrapScriptWithModule(script, hookPreScriptPathList, hookPostScriptPathList, config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public bool run_action(ChocolateyConfiguration configuration, PackageResult packageResult, CommandNameType command)
            => RunAction(configuration, packageResult, command);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void prepare_powershell_environment(IPackageSearchMetadata package, ChocolateyConfiguration configuration, string packageDirectory)
            => PreparePowerShellEnvironment(package, configuration, packageDirectory);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public PowerShellExecutionResults run_host(ChocolateyConfiguration config, string chocoPowerShellScript, Action<Pipeline> additionalActionsBeforeScript, IEnumerable<string> hookPreScriptPathList, IEnumerable<string> hookPostScriptPathList)
            => RunHost(config, chocoPowerShellScript, additionalActionsBeforeScript, hookPreScriptPathList, hookPostScriptPathList);
#pragma warning restore IDE1006
    }
}
