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

using System;
using System.Collections.Generic;
using System.Linq;
using chocolatey.infrastructure.app.attributes;
using chocolatey.infrastructure.commandline;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.commands;
using chocolatey.infrastructure.logging;
using chocolatey.infrastructure.app.services;

namespace chocolatey.infrastructure.app.commands
{
    [CommandFor("uninstall", "uninstalls a package")]
    public class ChocolateyUninstallCommand : ChocolateyCommandBase, ICommand
    {
        private readonly IChocolateyPackageService _packageService;

        private readonly string[] _removedOptions = new[]
        {
            "-m",
            "-sxs",
            "--sidebyside",
            "--side-by-side",
            "--allowmultiple",
            "--allow-multiple",
            "--allowmultipleversions",
            "--allow-multiple-versions",
        };

        public ChocolateyUninstallCommand(IChocolateyPackageService packageService)
        {
            _packageService = packageService;
        }

        public virtual void ConfigureArgumentParser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("s=|source=",
                     "Source - The source to find the package(s) to install. Special sources include: ruby, cygwin, windowsfeatures, and python. Defaults to default feeds.",
                     option => configuration.Sources = option.UnquoteSafe())
                .Add("version=",
                     "Version - A specific version to uninstall. Defaults to unspecified.",
                     option => configuration.Version = option.UnquoteSafe())
                .Add("a|allversions|all-versions",
                     "AllVersions - Uninstall all versions? Defaults to false.",
                     option => configuration.AllVersions = option != null)
                .Add("ua=|uninstallargs=|uninstallarguments=|uninstall-arguments=",
                     "UninstallArguments - Uninstall Arguments to pass to the native installer in the package. Defaults to unspecified.",
                     option => configuration.InstallArguments = option.UnquoteSafe())
                .Add("o|override|overrideargs|overridearguments|override-arguments",
                     "OverrideArguments - Should uninstall arguments be used exclusively without appending to current package passed arguments? Defaults to false.",
                     option => configuration.OverrideArguments = option != null)
                .Add("notsilent|not-silent",
                     "NotSilent - Do not uninstall this silently. Defaults to false.",
                     option => configuration.NotSilent = option != null)
                .Add("params=|parameters=|pkgparameters=|packageparameters=|package-parameters=",
                     "PackageParameters - Parameters to pass to the package. Defaults to unspecified.",
                     option => configuration.PackageParameters = option.UnquoteSafe())
                .Add("argsglobal|args-global|installargsglobal|install-args-global|applyargstodependencies|apply-args-to-dependencies|apply-install-arguments-to-dependencies",
                     "Apply Install Arguments To Dependencies  - Should install arguments be applied to dependent packages? Defaults to false.",
                     option => configuration.ApplyInstallArgumentsToDependencies = option != null)
                .Add("paramsglobal|params-global|packageparametersglobal|package-parameters-global|applyparamstodependencies|apply-params-to-dependencies|apply-package-parameters-to-dependencies",
                     "Apply Package Parameters To Dependencies  - Should package parameters be applied to dependent packages? Defaults to false.",
                     option => configuration.ApplyPackageParametersToDependencies = option != null)
                .Add("x|forcedependencies|force-dependencies|removedependencies|remove-dependencies",
                     "RemoveDependencies - Uninstall dependencies when uninstalling package(s). Defaults to false.",
                     option => configuration.ForceDependencies = option != null)
                .Add("n|skippowershell|skip-powershell|skipscripts|skip-scripts|skip-automation-scripts",
                     "Skip PowerShell - Do not run chocolateyUninstall.ps1. Defaults to false.",
                     option => configuration.SkipPackageInstallProvider = option != null)
                .Add("ignorepackagecodes|ignorepackageexitcodes|ignore-package-codes|ignore-package-exit-codes",
                     "IgnorePackageExitCodes - Exit with a 0 for success and 1 for non-success, no matter what package scripts provide for exit codes. Overrides the default feature '{0}' set to '{1}'.".FormatWith(ApplicationParameters.Features.UsePackageExitCodes, configuration.Features.UsePackageExitCodes.ToStringSafe()),
                     option =>
                     {
                         if (option != null)
                         {
                             configuration.Features.UsePackageExitCodes = false;
                         }
                     })
                 .Add("usepackagecodes|usepackageexitcodes|use-package-codes|use-package-exit-codes",
                     "UsePackageExitCodes - Package scripts can provide exit codes. Use those for choco's exit code when non-zero (this value can come from a dependency package). Chocolatey defines valid exit codes as 0, 1605, 1614, 1641, 3010. Overrides the default feature '{0}' set to '{1}'.".FormatWith(ApplicationParameters.Features.UsePackageExitCodes, configuration.Features.UsePackageExitCodes.ToStringSafe()),
                     option => configuration.Features.UsePackageExitCodes = option != null
                     )
                 .Add("autouninstaller|use-autouninstaller",
                     "UseAutoUninstaller - Use auto uninstaller service when uninstalling. Overrides the default feature '{0}' set to '{1}'.".FormatWith(ApplicationParameters.Features.AutoUninstaller, configuration.Features.AutoUninstaller.ToStringSafe()),
                     option => configuration.Features.AutoUninstaller = option != null
                     )
                 .Add("skipautouninstaller|skip-autouninstaller",
                     "SkipAutoUninstaller - Skip auto uninstaller service when uninstalling. Overrides the default feature '{0}' set to '{1}'.".FormatWith(ApplicationParameters.Features.AutoUninstaller, configuration.Features.AutoUninstaller.ToStringSafe()),
                     option =>
                     {
                         if (option != null)
                         {
                             configuration.Features.AutoUninstaller = false;
                         }
                     })
                 .Add("failonautouninstaller|fail-on-autouninstaller",
                     "FailOnAutoUninstaller - Fail the package uninstall if the auto uninstaller reports and error. Overrides the default feature '{0}' set to '{1}'.".FormatWith(ApplicationParameters.Features.FailOnAutoUninstaller, configuration.Features.FailOnAutoUninstaller.ToStringSafe()),
                     option => configuration.Features.FailOnAutoUninstaller = option != null
                     )
                 .Add("ignoreautouninstallerfailure|ignore-autouninstaller-failure",
                     "Ignore Auto Uninstaller Failure - Do not fail the package if auto uninstaller reports an error. Overrides the default feature '{0}' set to '{1}'.".FormatWith(ApplicationParameters.Features.FailOnAutoUninstaller, configuration.Features.FailOnAutoUninstaller.ToStringSafe()),
                     option =>
                     {
                         if (option != null)
                         {
                             configuration.Features.FailOnAutoUninstaller = false;
                         }
                     })
                 .Add("stoponfirstfailure|stop-on-first-failure|stop-on-first-package-failure",
                     "Stop On First Package Failure - stop running install, upgrade or uninstall on first package failure instead of continuing with others. Overrides the default feature '{0}' set to '{1}'.".FormatWith(ApplicationParameters.Features.StopOnFirstPackageFailure, configuration.Features.StopOnFirstPackageFailure.ToStringSafe()),
                     option => configuration.Features.StopOnFirstPackageFailure = option != null
                     )
                 .Add("exitwhenrebootdetected|exit-when-reboot-detected",
                     "Exit When Reboot Detected - Stop running install, upgrade, or uninstall when a reboot request is detected. Requires '{0}' feature to be turned on. Will exit with either {1} or {2}.  Overrides the default feature '{3}' set to '{4}'.".FormatWith
                         (ApplicationParameters.Features.UsePackageExitCodes, ApplicationParameters.ExitCodes.ErrorFailNoActionReboot, ApplicationParameters.ExitCodes.ErrorInstallSuspend, ApplicationParameters.Features.ExitOnRebootDetected, configuration.Features.ExitOnRebootDetected.ToStringSafe()),
                     option => configuration.Features.ExitOnRebootDetected = option != null
                     )
                 .Add("ignoredetectedreboot|ignore-detected-reboot",
                     "Ignore Detected Reboot - Ignore any detected reboots if found. Overrides the default feature '{0}' set to '{1}'.".FormatWith
                         (ApplicationParameters.Features.ExitOnRebootDetected, configuration.Features.ExitOnRebootDetected.ToStringSafe()),
                     option =>
                     {
                         if (option != null)
                         {
                             configuration.Features.ExitOnRebootDetected = false;
                         }
                     })
                .Add("skiphooks|skip-hooks",
                    "Skip hooks - Do not run hook scripts. Available in 1.2.0+",
                    option => configuration.SkipHookScripts = option != null
                    )
                ;
        }

        public virtual void ParseAdditionalArguments(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);
            configuration.PackageNames = string.Join(ApplicationParameters.PackageNamesSeparator.ToStringSafe(), unparsedArguments.Where(arg => !arg.StartsWith("-")));

            if (configuration.RegularOutput)
            {
                WarnForRemovedOptions(unparsedArguments.Where(arg => arg.StartsWith("-")), _removedOptions);
            }
        }

        public virtual void Validate(ChocolateyConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.PackageNames))
            {
                throw new ApplicationException("Package name is required. Please pass at least one package name to uninstall.");
            }
        }

        public override void HelpMessage(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Uninstall Command");
            this.Log().Info(@"
Uninstalls a package or a list of packages.

Chocolatey automatically tracks registry changes for ""Programs and
 Features"" of the underlying software's native installers when
 installing packages. The ""Automatic Uninstaller"" (auto uninstaller)
 service is a feature that can use that information to automatically
 determine how to uninstall these natively installed applications. This
 means that a package may not need an explicit chocolateyUninstall.ps1
 to reverse the installation done in the install script.

Chocolatey tracks packages, which are the files in
 `$env:ChocolateyInstall\lib\packagename`. These packages may or may not
 contain the software (applications/tools) that each package represents.
 The software may actually be installed in Program Files (most native
 installers will install the software there) or elsewhere on the
 machine.

With auto uninstaller turned off, a chocolateyUninstall.ps1 is required
 to perform uninstall from the system. In the absence of
 chocolateyUninstall.ps1, choco uninstall only removes the package from
 Chocolatey but does not remove the software from your system (unless
 in the package directory).

NOTE: A package with a failing uninstall can be removed with the
`-n --skipautouninstaller` flags. This will remove the package from
chocolatey without attempting to uninstall the program.

NOTE: Chocolatey Pro / Business automatically synchronizes with
 Programs and Features, ensuring manually removed apps are
 automatically removed from Chocolatey's repository.

NOTE: Synchronizer and AutoUninstaller enhancements in licensed
 versions of Chocolatey ensure that Autouninstaller is up to 95%
 effective at removing software without an uninstall script. This is
 because synchronizer ensures the registry snapshot stays up to date
 and licensed enhancements have the ability to inspect more locations
 to determine how to automatically uninstall software.
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco uninstall <pkg|all> [pkg2 pkgN] [options/switches]

NOTE: `all` is a special package keyword that will allow you to
 uninstall all packages.

");
            "chocolatey".Log().Info(ChocolateyLoggers.Important, "See It In Action");
            "chocolatey".Log().Info(@"
choco uninstall: https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/choco_uninstall.gif

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco uninstall git
    choco uninstall notepadplusplus googlechrome atom 7zip
    choco uninstall notepadplusplus googlechrome atom 7zip -dv
    choco uninstall ruby --version 1.8.7.37402
    choco uninstall nodejs.install --all-versions

NOTE: See scripting in the command reference (`choco -?`) for how to
 write proper scripts and integrations.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Exit Codes");
            "chocolatey".Log().Info(@"
Exit codes that normally result from running this command.

Normal:
 - 0: operation was successful, no issues detected
 - -1 or 1: an error has occurred

Package Exit Codes:
 - 1605: software is not installed
 - 1614: product is uninstalled
 - 1641: success, reboot initiated
 - 3010: success, reboot required
 - other (not listed): likely an error has occurred

In addition to normal exit codes, packages are allowed to exit
 with their own codes when the feature '{0}' is
 turned on.

Reboot Exit Codes:
 - 350: pending reboot detected, no action has occurred
 - 1604: install suspended, incomplete

In addition to the above exit codes, you may also see reboot exit codes
 when the feature '{1}' is turned on. It typically requires
 the feature '{0}' to also be turned on to work properly.
".FormatWith(ApplicationParameters.Features.UsePackageExitCodes, ApplicationParameters.Features.ExitOnRebootDetected));

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
            "chocolatey".Log().Info(@"
NOTE: Options and switches apply to all items passed, so if you are
 installing multiple packages, and you use `--version=1.0.0`, it is
 going to look for and try to install version 1.0.0 of every package
 passed. So please split out multiple package calls when wanting to
 pass specific options.
");
        }

        public virtual void DryRun(ChocolateyConfiguration configuration)
        {
            _packageService.UninstallDryRun(configuration);
        }

        public virtual void Run(ChocolateyConfiguration configuration)
        {
            _packageService.Uninstall(configuration);
        }

        public virtual bool MayRequireAdminAccess()
        {
            return true;
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
            => ConfigureArgumentParser(optionSet, configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
            => ParseAdditionalArguments(unparsedArguments, configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_validation(ChocolateyConfiguration configuration)
            => Validate(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void help_message(ChocolateyConfiguration configuration)
            => HelpMessage(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void noop(ChocolateyConfiguration configuration)
            => DryRun(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void run(ChocolateyConfiguration configuration)
            => Run(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual bool may_require_admin_access()
            => MayRequireAdminAccess();
#pragma warning restore IDE0022, IDE1006
    }
}
