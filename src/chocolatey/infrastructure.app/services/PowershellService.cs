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
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;
    using adapters;
    using builders;
    using commandline;
    using configuration;
    using cryptography;
    using domain;
    using infrastructure.commands;
    using infrastructure.registration;
    using logging;
    using NuGet;
    using powershell;
    using results;
    using utility;
    using Assembly = adapters.Assembly;
    using Console = System.Console;
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

        private string get_script_for_action(PackageResult packageResult, CommandNameType command)
        {
            var file = "chocolateyInstall.ps1";
            switch (command)
            {
                case CommandNameType.uninstall:
                    file = "chocolateyUninstall.ps1";
                    break;

                case CommandNameType.upgrade:
                    file = "chocolateyBeforeModify.ps1";
                    break;
            }

            var packageDirectory = packageResult.InstallLocation;
            var installScript = _fileSystem.get_files(packageDirectory, file, SearchOption.AllDirectories).Where(p => !p.to_lower().contains("\\templates\\"));
            if (installScript.Count() != 0)
            {
                return installScript.FirstOrDefault();
            }

            return string.Empty;
        }

        public void noop_action(PackageResult packageResult, CommandNameType command)
        {
            var chocoInstall = get_script_for_action(packageResult, command);
            if (!string.IsNullOrEmpty(chocoInstall))
            {
                this.Log().Info("Would have run '{0}':".format_with(_fileSystem.get_file_name(chocoInstall)));
                this.Log().Warn(_fileSystem.read_file(chocoInstall).escape_curly_braces());
            }
        }

        public void install_noop(PackageResult packageResult)
        {
            noop_action(packageResult, CommandNameType.install);
        }

        public bool install(ChocolateyConfiguration configuration, PackageResult packageResult)
        {
            return run_action(configuration, packageResult, CommandNameType.install);
        }

        public void uninstall_noop(PackageResult packageResult)
        {
            noop_action(packageResult, CommandNameType.uninstall);
        }

        public bool uninstall(ChocolateyConfiguration configuration, PackageResult packageResult)
        {
            return run_action(configuration, packageResult, CommandNameType.uninstall);
        }

        public void before_modify_noop(PackageResult packageResult)
        {
            noop_action(packageResult, CommandNameType.upgrade);
        }

        public bool before_modify(ChocolateyConfiguration configuration, PackageResult packageResult)
        {
            return run_action(configuration, packageResult, CommandNameType.upgrade);
        }

        private string get_helpers_folder()
        {
            return _fileSystem.combine_paths(ApplicationParameters.InstallLocation, "helpers");
        }

        public string wrap_script_with_module(string script, ChocolateyConfiguration config)
        {
            var installerModule = _fileSystem.combine_paths(get_helpers_folder(), "chocolateyInstaller.psm1");
            var scriptRunner = _fileSystem.combine_paths(get_helpers_folder(), "chocolateyScriptRunner.ps1");

            // removed setting all errors to terminating. Will cause too
            // many issues in existing packages, including upgrading
            // Chocolatey from older POSH client due to log errors
            //$ErrorActionPreference = 'Stop';
            return "[System.Threading.Thread]::CurrentThread.CurrentCulture = '';[System.Threading.Thread]::CurrentThread.CurrentUICulture = ''; & import-module -name '{0}';{2} & '{1}' {3}"
                .format_with(
                    installerModule,
                    scriptRunner,
                    string.IsNullOrWhiteSpace(_customImports) ? string.Empty : "& {0}".format_with(_customImports.EndsWith(";") ? _customImports : _customImports + ";"),
                    get_script_arguments(script, config)
                );
        }

        private string get_script_arguments(string script, ChocolateyConfiguration config)
        {
            return "-packageScript '{0}' -installArguments '{1}' -packageParameters '{2}'{3}{4}".format_with(
                script,
                prepare_powershell_arguments(config.InstallArguments),
                prepare_powershell_arguments(config.PackageParameters),
                config.ForceX86 ? " -forceX86" : string.Empty,
                config.OverrideArguments ? " -overrideArgs" : string.Empty
             );
        }

        private string prepare_powershell_arguments(string argument)
        {
            return argument.to_string().Replace("\"", "\\\"");
        }

        public bool run_action(ChocolateyConfiguration configuration, PackageResult packageResult, CommandNameType command)
        {
            var installerRun = false;

            var packageDirectory = packageResult.InstallLocation;
            if (packageDirectory.is_equal_to(ApplicationParameters.InstallLocation) || packageDirectory.is_equal_to(ApplicationParameters.PackagesLocation))
            {
                packageResult.Messages.Add(
                    new ResultMessage(
                        ResultType.Error,
                        "Install location is not specific enough, cannot run PowerShell script:{0} Erroneous install location captured as '{1}'".format_with(Environment.NewLine, packageResult.InstallLocation)
                        )
                    );

                return false;
            }

            if (!_fileSystem.directory_exists(packageDirectory))
            {
                packageResult.Messages.Add(new ResultMessage(ResultType.Error, "Package install not found:'{0}'".format_with(packageDirectory)));
                return installerRun;
            }

            var chocoPowerShellScript = get_script_for_action(packageResult, command);
            if (!string.IsNullOrEmpty(chocoPowerShellScript))
            {
                var failure = false;
                var package = packageResult.Package;
                prepare_powershell_environment(package, configuration, packageDirectory);

                this.Log().Debug(ChocolateyLoggers.Important, "Contents of '{0}':".format_with(chocoPowerShellScript));
                string chocoPowerShellScriptContents = _fileSystem.read_file(chocoPowerShellScript);
                // leave this way, doesn't take it through formatting.
                this.Log().Debug(() => chocoPowerShellScriptContents.escape_curly_braces());

                bool shouldRun = !configuration.PromptForConfirmation;

                if (!shouldRun)
                {
                    this.Log().Info(ChocolateyLoggers.Important, () => "The package {0} wants to run '{1}'.".format_with(package.Id, _fileSystem.get_file_name(chocoPowerShellScript)));
                    this.Log().Info(ChocolateyLoggers.Important, () => "Note: If you don't run this script, the installation will fail.");
                    this.Log().Info(ChocolateyLoggers.Important, () => @"Note: To confirm automatically next time, use '-y' or consider:");
                    this.Log().Info(ChocolateyLoggers.Important, () => @"choco feature enable -n allowGlobalConfirmation");

                    var selection = InteractivePrompt.prompt_for_confirmation(@"Do you want to run the script?",
                        new[] { "yes", "no", "print" },
                        defaultChoice: null,
                        requireAnswer: true,
                        allowShortAnswer: true,
                        shortPrompt: true
                        );

                    if (selection.is_equal_to("print"))
                    {
                        this.Log().Info(ChocolateyLoggers.Important, "------ BEGIN SCRIPT ------");
                        this.Log().Info(() => "{0}{1}{0}".format_with(Environment.NewLine, chocoPowerShellScriptContents.escape_curly_braces()));
                        this.Log().Info(ChocolateyLoggers.Important, "------- END SCRIPT -------");
                        selection = InteractivePrompt.prompt_for_confirmation(@"Do you want to run this script?",
                            new[] { "yes", "no" },
                            defaultChoice: null,
                            requireAnswer: true,
                            allowShortAnswer: true,
                            shortPrompt: true
                            );
                    }

                    if (selection.is_equal_to("yes")) shouldRun = true;
                    if (selection.is_equal_to("no"))
                    {
                        //MSI ERROR_INSTALL_USEREXIT - 1602 - https://support.microsoft.com/en-us/kb/304888 / https://msdn.microsoft.com/en-us/library/aa376931.aspx
                        //ERROR_INSTALL_CANCEL - 15608 - https://msdn.microsoft.com/en-us/library/windows/desktop/ms681384.aspx
                        Environment.ExitCode = 15608;
                        packageResult.Messages.Add(new ResultMessage(ResultType.Error, "User canceled powershell portion of installation for '{0}'.{1} Specify -n to skip automated script actions.".format_with(chocoPowerShellScript, Environment.NewLine)));
                    }
                }

                if (shouldRun)
                {
                    installerRun = true;

                    if (configuration.Features.UsePowerShellHost)
                    {
                        add_assembly_resolver();
                    }

                    var result = new PowerShellExecutionResults
                    {
                        ExitCode = -1
                    };

                    try
                    {
                        result = configuration.Features.UsePowerShellHost
                                    ? Execute.with_timeout(configuration.CommandExecutionTimeoutSeconds).command(() => run_host(configuration, chocoPowerShellScript, null), result)
                                    : run_external_powershell(configuration, chocoPowerShellScript);
                    }
                    catch (Exception ex)
                    {
                        this.Log().Error(ex.Message.escape_curly_braces());
                        result.ExitCode = 1;
                    }

                    if (configuration.Features.UsePowerShellHost)
                    {
                        remove_assembly_resolver();
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
                        packageResult.Messages.Add(new ResultMessage(ResultType.Error, "Error while running '{0}'.{1} See log for details.".format_with(chocoPowerShellScript, Environment.NewLine)));
                    }
                    packageResult.Messages.Add(new ResultMessage(ResultType.Note, "Ran '{0}'".format_with(chocoPowerShellScript)));
                }
            }

            return installerRun;
        }

        private PowerShellExecutionResults run_external_powershell(ChocolateyConfiguration configuration, string chocoPowerShellScript)
        {
            var result = new PowerShellExecutionResults();
            result.ExitCode = PowershellExecutor.execute(
                wrap_script_with_module(chocoPowerShellScript, configuration),
                _fileSystem,
                configuration.CommandExecutionTimeoutSeconds,
                (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data)) return;
                    //inspect for different streams
                    if (e.Data.StartsWith("DEBUG:"))
                    {
                        this.Log().Debug(() => " " + e.Data.escape_curly_braces());
                    }
                    else if (e.Data.StartsWith("WARNING:"))
                    {
                        this.Log().Warn(() => " " + e.Data.escape_curly_braces());
                    }
                    else if (e.Data.StartsWith("VERBOSE:"))
                    {
                        this.Log().Info(ChocolateyLoggers.Verbose, () => " " + e.Data.escape_curly_braces());
                    }
                    else
                    {
                        this.Log().Info(() => " " + e.Data.escape_curly_braces());
                    }
                },
                (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(e.Data)) return;
                    result.StandardErrorWritten = true;
                    this.Log().Error(() => " " + e.Data.escape_curly_braces());
                });

            return result;
        }

        public void prepare_powershell_environment(IPackage package, ChocolateyConfiguration configuration, string packageDirectory)
        {
            if (package == null) return;

            EnvironmentSettings.update_environment_variables();
            EnvironmentSettings.set_environment_variables(configuration);

            Environment.SetEnvironmentVariable("chocolateyPackageName", package.Id);
            Environment.SetEnvironmentVariable("packageName", package.Id);
            Environment.SetEnvironmentVariable("chocolateyPackageTitle", package.Title);
            Environment.SetEnvironmentVariable("packageTitle", package.Title);
            Environment.SetEnvironmentVariable("chocolateyPackageVersion", package.Version.to_string());
            Environment.SetEnvironmentVariable("packageVersion", package.Version.to_string());
            Environment.SetEnvironmentVariable("chocolateyPackageVersionPrerelease", package.Version.SpecialVersion.to_string());
            Environment.SetEnvironmentVariable("chocolateyPackageVersionPackageRelease", package.Version.PackageReleaseVersion.to_string());

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
            if (!PackageUtility.package_is_a_dependency(configuration, package.Id) || configuration.ApplyInstallArgumentsToDependencies)
            {
                this.Log().Debug(ChocolateyLoggers.Verbose, "Setting installer args for {0}".format_with(package.Id));
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
            if (!PackageUtility.package_is_a_dependency(configuration, package.Id) || configuration.ApplyPackageParametersToDependencies)
            {
                this.Log().Debug(ChocolateyLoggers.Verbose, "Setting package parameters for {0}".format_with(package.Id));
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

                foreach (var downloadCache in package.DownloadCache.or_empty_list_if_null())
                {
                    var urlKey = CryptoHashProvider.hash_value(downloadCache.OriginalUrl, CryptoHashProviderType.Sha256).Replace("=", string.Empty);
                    Environment.SetEnvironmentVariable("CacheFile_{0}".format_with(urlKey), downloadCache.FileName);
                    Environment.SetEnvironmentVariable("CacheChecksum_{0}".format_with(urlKey), downloadCache.Checksum);
                    Environment.SetEnvironmentVariable("CacheChecksumType_{0}".format_with(urlKey), "sha512");
                }
            }

            SecurityProtocol.set_protocol(configuration, provideWarning:false);
        }

        private ResolveEventHandler _handler = null;

        private void add_assembly_resolver()
        {
            _handler = (sender, args) =>
            {
                var requestedAssembly = new AssemblyName(args.Name);

                this.Log().Debug(ChocolateyLoggers.Verbose, "Redirecting {0}, requested by '{1}'".format_with(args.Name, args.RequestingAssembly == null ? string.Empty : args.RequestingAssembly.FullName));

                AppDomain.CurrentDomain.AssemblyResolve -= _handler;

                // we build against v1 - everything should update in a kosher manner to the newest, but it may not.
                var assembly = attempt_version_load(requestedAssembly, new Version(5, 0, 0, 0)) ?? attempt_version_load(requestedAssembly, new Version(4, 0, 0, 0));
                if (assembly == null) assembly = attempt_version_load(requestedAssembly, new Version(3, 0, 0, 0));
                if (assembly == null) assembly = attempt_version_load(requestedAssembly, new Version(1, 0, 0, 0));

                return assembly;
            };

            AppDomain.CurrentDomain.AssemblyResolve += _handler;
        }

        private System.Reflection.Assembly attempt_version_load(AssemblyName requestedAssembly, Version version)
        {
            if (requestedAssembly == null) return null;

            requestedAssembly.Version = version;

            if (requestedAssembly.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase) && requestedAssembly.CultureInfo.Name.is_equal_to("en-US"))
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

                this.Log().Debug(ChocolateyLoggers.Verbose, "Attempting to load assembly {0} failed:{1} {2}".format_with(requestedAssembly.Name, Environment.NewLine, ex.Message.escape_curly_braces()));
                
                return null;
            }
        }

        private void remove_assembly_resolver()
        {
            if (_handler != null)
            {
                AppDomain.CurrentDomain.AssemblyResolve -= _handler;
            }
        }

        public PowerShellExecutionResults run_host(ChocolateyConfiguration config, string chocoPowerShellScript, Action<Pipeline> additionalActionsBeforeScript)
        {
            // since we control output in the host, always set these true
            Environment.SetEnvironmentVariable("ChocolateyEnvironmentDebug", "true");
            Environment.SetEnvironmentVariable("ChocolateyEnvironmentVerbose", "true");

            var result = new PowerShellExecutionResults();
            string commandToRun = wrap_script_with_module(chocoPowerShellScript, config);
            var host = new PoshHost(config);
            this.Log().Debug(() => "Calling built-in PowerShell host with ['{0}']".format_with(commandToRun.escape_curly_braces()));

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
                                host.UI.WriteLine(reader.Read().to_string().escape_curly_braces());
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
                                host.UI.WriteErrorLine(reader.Read().to_string().escape_curly_braces());
                            }
                        }
                    };

                    var documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify);
                    var currentUserCurrentHostProfile = _fileSystem.combine_paths(documentsFolder, "WindowsPowerShell\\Microsoft.PowerShell_profile.ps1");
                    var recreateProfileScript = @"
if ((Test-Path(""{0}"")) -and ($profile -eq $null -or $profile -eq '')) {{
  $global:profile = ""{1}""
}}
".format_with(documentsFolder, currentUserCurrentHostProfile);

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
                                var scriptError = scriptStackTrace.GetValue(record, null).to_string();
                                if (!string.IsNullOrWhiteSpace(scriptError)) errorStackTrace = scriptError;
                            }
                        }
                        this.Log().Error("ERROR: {0}{1}".format_with(ex.Message.escape_curly_braces(), !config.Debug ? string.Empty : "{0} {1}".format_with(Environment.NewLine, errorStackTrace.escape_curly_braces())));
                    }
                    catch (Exception ex)
                    {
                        // Unfortunately this doesn't print line number and character. It might be nice to get back to those items unless it involves tons of work.
                        this.Log().Error("ERROR: {0}{1}".format_with(ex.Message.escape_curly_braces(), !config.Debug ? string.Empty : "{0} {1}".format_with(Environment.NewLine, ex.StackTrace.escape_curly_braces())));
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

            this.Log().Debug("Built-in PowerShell host called with ['{0}'] exited with '{1}'.".format_with(commandToRun.escape_curly_braces(), host.ExitCode));

            result.ExitCode = host.ExitCode;
            result.StandardErrorWritten = host.StandardErrorWritten;

            return result;
        }
    }
}
