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

namespace chocolatey.infrastructure.app.commands
{
    using System;
    using System.Collections.Generic;
    using attributes;
    using commandline;
    using configuration;
    using infrastructure.commands;
    using infrastructure.configuration;
    using logging;
    using services;

    [CommandFor("install", "installs packages from various sources")]
    public class ChocolateyInstallCommand : ICommand
    {
        private readonly IChocolateyPackageService _packageService;

        public ChocolateyInstallCommand(IChocolateyPackageService packageService)
        {
            _packageService = packageService;
        }

        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("s=|source=",
                     "Source - The source to find the package(s) to install. Special sources include: ruby, webpi, cygwin, windowsfeatures, and python. Defaults to default feeds.",
                     option => configuration.Sources = option.remove_surrounding_quotes())
                .Add("version=",
                     "Version - A specific version to install. Defaults to unspecified.",
                     option => configuration.Version = option.remove_surrounding_quotes())
                .Add("pre|prerelease",
                     "Prerelease - Include Prereleases? Defaults to false.",
                     option => configuration.Prerelease = option != null)
                .Add("x86|forcex86",
                     "ForceX86 - Force x86 (32bit) installation on 64 bit systems. Defaults to false.",
                     option => configuration.ForceX86 = option != null)
                .Add("ia=|installargs=|installarguments=|install-arguments=",
                     "InstallArguments - Install Arguments to pass to the native installer in the package. Defaults to unspecified.",
                     option => configuration.InstallArguments = option.remove_surrounding_quotes())
                .Add("o|override|overrideargs|overridearguments|override-arguments",
                     "OverrideArguments - Should install arguments be used exclusively without appending to current package passed arguments? Defaults to false.",
                     option => configuration.OverrideArguments = option != null)
                .Add("notsilent|not-silent",
                     "NotSilent - Do not install this silently. Defaults to false.",
                     option => configuration.NotSilent = option != null)
                .Add("params=|parameters=|pkgparameters=|packageparameters=|package-parameters=",
                     "PackageParameters - Parameters to pass to the package. Defaults to unspecified.",
                     option => configuration.PackageParameters = option.remove_surrounding_quotes())
                .Add("allowdowngrade|allow-downgrade",
                     "AllowDowngrade - Should an attempt at downgrading be allowed? Defaults to false.",
                     option => configuration.AllowDowngrade = option != null)
                .Add("m|sxs|sidebyside|side-by-side|allowmultiple|allow-multiple|allowmultipleversions|allow-multiple-versions",
                     "AllowMultipleVersions - Should multiple versions of a package be installed? Defaults to false.",
                     option => configuration.AllowMultipleVersions = option != null)
                .Add("i|ignoredependencies|ignore-dependencies",
                     "IgnoreDependencies - Ignore dependencies when installing package(s). Defaults to false.",
                     option => configuration.IgnoreDependencies = option != null)
                .Add("x|forcedependencies|force-dependencies",
                     "ForceDependencies - Force dependencies to be reinstalled when force installing package(s). Must be used in conjunction with --force. Defaults to false.",
                     option => configuration.ForceDependencies = option != null)
                .Add("n|skippowershell|skip-powershell|skipscripts|skip-scripts|skip-automation-scripts",
                     "Skip Powershell - Do not run chocolateyInstall.ps1. Defaults to false.",
                     option => configuration.SkipPackageInstallProvider = option != null)
                .Add("u=|user=",
                     "User - used with authenticated feeds. Defaults to empty.",
                     option => configuration.SourceCommand.Username = option.remove_surrounding_quotes())
                .Add("p=|password=",
                     "Password - the user's password to the source. Defaults to empty.",
                     option => configuration.SourceCommand.Password = option.remove_surrounding_quotes())
                .Add("ignorechecksums|ignore-checksums",
                      "IgnoreChecksums - Ignore checksums provided by the package",
                      option =>
                      {
                          if (option != null) configuration.Features.CheckSumFiles = false;
                      })
                .Add("ignorepackagecodes|ignorepackageexitcodes|ignore-package-codes|ignore-package-exit-codes",
                     "IgnorePackageExitCodes - Exit with a 0 for success and 1 for non-success, no matter what package scripts provide for exit codes. Overrides the default feature '{0}' set to '{1}'. Available in 0.9.10+.".format_with(ApplicationParameters.Features.UsePackageExitCodes, configuration.Features.UsePackageExitCodes.to_string()),
                     option =>
                     {
                         if (option != null)
                         {
                             configuration.Features.UsePackageExitCodes = false;
                         }
                     })
                 .Add("usepackagecodes|usepackageexitcodes|use-package-codes|use-package-exit-codes",
                     "UsePackageExitCodes - Package scripts can provide exit codes. Use those for choco's exit code when non-zero (this value can come from a dependency package). Chocolatey defines valid exit codes as 0, 1605, 1614, 1641, 3010.  Overrides the default feature '{0}' set to '{1}'. Available in 0.9.10+.".format_with(ApplicationParameters.Features.UsePackageExitCodes, configuration.Features.UsePackageExitCodes.to_string()),
                     option => configuration.Features.UsePackageExitCodes = option != null
                     )
                ;

            //todo: Checksum / ChecksumType defaults to md5 / package name can be a url / installertype
        }

        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);
            configuration.PackageNames = string.Join(ApplicationParameters.PackageNamesSeparator.to_string(), unparsedArguments);
        }

        public virtual void handle_validation(ChocolateyConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.PackageNames))
            {
                throw new ApplicationException("Package name is required. Please pass at least one package name to install.");
            }
            // Need a better check on this before releasing. Issue will be covered by other fixes
            //// investigate https://msdn.microsoft.com/en-us/library/system.io.path.getinvalidpathchars(v=vs.100).aspx
            //if (configuration.PackageNames.Contains(":"))
            //{
            //    throw new ApplicationException("Package name cannot contain invalid characters.");
            //}

            if (configuration.ForceDependencies && !configuration.Force)
            {
                throw new ApplicationException("Force dependencies can only be used with force also turned on.");
            }
        }

        public virtual void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Install Command");
            this.Log().Info(@"
Installs a package or a list of packages (sometimes specified as a
 packages.config). Some may prefer to use `cinst` as a shortcut for 
 `choco install`.
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco install <pkg|packages.config> [<pkg2> <pkgN>] [<options/switches>]
    cinst <pkg|packages.config> [<pkg2> <pkgN>] [<options/switches>]

NOTE: `all` is a special package keyword that will allow you to install 
 all packages from a custom feed. Will not work with Chocolatey default 
 feed. THIS IS NOT YET REIMPLEMENTED.

NOTE: For packages.config, please see https://bit.ly/packages_config
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco install sysinternals
    choco install notepadplusplus googlechrome atom 7zip 
    choco install notepadplusplus --force --force-dependencies
    choco install notepadplusplus googlechrome atom 7zip -dvfy
    choco install git --params=""/GitAndUnixToolsOnPath /NoAutoCrlf"" -y
    choco install nodejs.install --version 0.10.35
    choco install git -s ""https://somewhere/out/there""
    choco install git -s ""https://somewhere/protected"" -u user -p pass

Choco can also install directly from a nuspec/nupkg file. This aids in 
 testing packages.

    choco install <path/to/nuspec>
    choco install <path/to/nupkg>

Install multiple versions of a package using -m (AllowMultiple versions)
 
    choco install ruby --version 1.9.3.55100 -my
    choco install ruby --version 2.0.0.59800 -my
    choco install ruby --version 2.1.5 -my

NOTE: All of these will add to PATH variable. We'll be adding a special 
 option to not allow PATH changes. Until then you will need to manually 
 go modify Path to just one Ruby and then use something like uru or pik
 to switch between versions.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
            "chocolatey".Log().Info(@"
NOTE: Options and switches apply to all items passed, so if you are 
 installing multiple packages, and you use `--version=1.0.0`, it is 
 going to look for and try to install version 1.0.0 of every package 
 passed. So please split out multiple package calls when wanting to 
 pass specific options.
");
        }

        public virtual void noop(ChocolateyConfiguration configuration)
        {
            _packageService.install_noop(configuration);
        }

        public virtual void run(ChocolateyConfiguration configuration)
        {
            _packageService.ensure_source_app_installed(configuration);
            _packageService.install_run(configuration);
        }

        public virtual bool may_require_admin_access()
        {
            return true;
        }
    }
}