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
    using logging;
    using services;

    [CommandFor("upgrade", "upgrades packages from various sources")]
    public class ChocolateyUpgradeCommand : ICommand
    {
        //todo v1 Deprecation reseal this class and remove virtuals

        private readonly IChocolateyPackageService _packageService;

        public ChocolateyUpgradeCommand(IChocolateyPackageService packageService)
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
                .Add("apply-install-arguments-to-dependencies",
                     "Apply Install Arguments To Dependencies  - Should install arguments be applied to dependent packages? Defaults to false.",
                     option => configuration.ApplyInstallArgumentsToDependencies = option != null)
                .Add("apply-package-parameters-to-dependencies",
                     "Apply Package Parameters To Dependencies  - Should package parameters be applied to dependent packages? Defaults to false.",
                     option => configuration.ApplyPackageParametersToDependencies = option != null)
                .Add("allowdowngrade|allow-downgrade",
                     "AllowDowngrade - Should an attempt at downgrading be allowed? Defaults to false.",
                     option => configuration.AllowDowngrade = option != null)
                .Add("m|sxs|sidebyside|side-by-side|allowmultiple|allow-multiple|allowmultipleversions|allow-multiple-versions",
                     "AllowMultipleVersions - Should multiple versions of a package be installed? Defaults to false.",
                     option => configuration.AllowMultipleVersions = option != null)
                .Add("i|ignoredependencies|ignore-dependencies",
                     "IgnoreDependencies - Ignore dependencies when upgrading package(s). Defaults to false.",
                     option => configuration.IgnoreDependencies = option != null)
                .Add("n|skippowershell|skip-powershell|skipscripts|skip-scripts|skip-automation-scripts",
                     "Skip Powershell - Do not run chocolateyInstall.ps1. Defaults to false.",
                     option => configuration.SkipPackageInstallProvider = option != null)
                .Add("failonunfound|fail-on-unfound",
                     "Fail On Unfound Packages - If a package is not found in feeds specified, fail instead of warn.",
                     option => configuration.UpgradeCommand.FailOnUnfound = option != null)
                .Add("failonnotinstalled|fail-on-not-installed",
                     "Fail On Non-installed Packages - If a package is not already intalled, fail instead of installing.",
                     option => configuration.UpgradeCommand.FailOnNotInstalled = option != null)
                .Add("u=|user=",
                     "User - used with authenticated feeds. Defaults to empty.",
                     option => configuration.SourceCommand.Username = option.remove_surrounding_quotes())
                .Add("p=|password=",
                     "Password - the user's password to the source. Defaults to empty.",
                     option => configuration.SourceCommand.Password = option.remove_surrounding_quotes())
                .Add("cert=",
                     "Client certificate - PFX pathname for an x509 authenticated feeds. Defaults to empty. Available in 0.9.10+.",
                     option => configuration.SourceCommand.Certificate = option.remove_surrounding_quotes())
                .Add("cp=|certpassword=",
                     "Certificate Password - the client certificate's password to the source. Defaults to empty. Available in 0.9.10+.",
                     option => configuration.SourceCommand.CertificatePassword = option.remove_surrounding_quotes())
                .Add("ignorechecksum|ignore-checksum|ignorechecksums|ignore-checksums",
                      "IgnoreChecksums - Ignore checksums provided by the package. Overrides the default feature '{0}' set to '{1}'. Available in 0.9.9.9+.".format_with(ApplicationParameters.Features.ChecksumFiles, configuration.Features.ChecksumFiles.to_string()),
                      option =>
                      {
                          if (option != null) configuration.Features.ChecksumFiles = false;
                      })
                .Add("allowemptychecksum|allowemptychecksums|allow-empty-checksums",
                      "Allow Empty Checksums - Allow packages to have empty/missing checksums for downloaded resources from non-secure locations (HTTP, FTP). Use this switch is not recommended if using sources that download resources from the internet. Overrides the default feature '{0}' set to '{1}'. Available in 0.10.0+.".format_with(ApplicationParameters.Features.AllowEmptyChecksums, configuration.Features.AllowEmptyChecksums.to_string()),
                      option =>
                      {
                          if (option != null) configuration.Features.AllowEmptyChecksums = true;
                      })
                 .Add("allowemptychecksumsecure|allowemptychecksumssecure|allow-empty-checksums-secure",
                      "Allow Empty Checksums Secure - Allow packages to have empty checksums for downloaded resources from secure locations (HTTPS). Overrides the default feature '{0}' set to '{1}'. Available in 0.10.0+.".format_with(ApplicationParameters.Features.AllowEmptyChecksumsSecure, configuration.Features.AllowEmptyChecksumsSecure.to_string()),
                      option =>
                      {
                          if (option != null) configuration.Features.AllowEmptyChecksumsSecure = true;
                      })
                .Add("requirechecksum|requirechecksums|require-checksums",
                      "Require Checksums - Requires packages to have checksums for downloaded resources (both non-secure and secure). Overrides the default feature '{0}' set to '{1}' and '{2}' set to '{3}'. Available in 0.10.0+.".format_with(ApplicationParameters.Features.AllowEmptyChecksums, configuration.Features.AllowEmptyChecksums.to_string(), ApplicationParameters.Features.AllowEmptyChecksumsSecure, configuration.Features.AllowEmptyChecksumsSecure.to_string()),
                      option =>
                      {
                          if (option != null)
                          {
                              configuration.Features.AllowEmptyChecksums = false;
                              configuration.Features.AllowEmptyChecksumsSecure = false;

                          }
                      })
                .Add("checksum=|downloadchecksum=|download-checksum=",
                     "Download Checksum - a user provided checksum for downloaded resources for the package. Overrides the package checksum (if it has one).  Defaults to empty. Available in 0.10.0+.",
                     option => configuration.DownloadChecksum = option.remove_surrounding_quotes())
                .Add("checksum64=|checksumx64=|downloadchecksumx64=|download-checksum-x64=",
                     "Download Checksum 64bit - a user provided checksum for 64bit downloaded resources for the package. Overrides the package 64-bit checksum (if it has one). Defaults to same as Download Checksum. Available in 0.10.0+.",
                     option => configuration.DownloadChecksum64 = option.remove_surrounding_quotes())
                .Add("checksumtype=|checksum-type=|downloadchecksumtype=|download-checksum-type=",
                     "Download Checksum Type - a user provided checksum type. Overrides the package checksum type (if it has one). Used in conjunction with Download Checksum. Available values are 'md5', 'sha1', 'sha256' or 'sha512'. Defaults to 'md5'. Available in 0.10.0+.",
                     option => configuration.DownloadChecksumType = option.remove_surrounding_quotes())
                .Add("checksumtype64=|checksumtypex64=|checksum-type-x64=|downloadchecksumtypex64=|download-checksum-type-x64=",
                     "Download Checksum Type 64bit - a user provided checksum for 64bit downloaded resources for the package. Overrides the package 64-bit checksum (if it has one). Used in conjunction with Download Checksum 64bit. Available values are 'md5', 'sha1', 'sha256' or 'sha512'. Defaults to same as Download Checksum Type. Available in 0.10.0+.",
                     option => configuration.DownloadChecksumType64 = option.remove_surrounding_quotes())
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
                     "UsePackageExitCodes - Package scripts can provide exit codes. Use those for choco's exit code when non-zero (this value can come from a dependency package). Chocolatey defines valid exit codes as 0, 1605, 1614, 1641, 3010. Overrides the default feature '{0}' set to '{1}'. Available in 0.9.10+.".format_with(ApplicationParameters.Features.UsePackageExitCodes, configuration.Features.UsePackageExitCodes.to_string()),
                     option => configuration.Features.UsePackageExitCodes = option != null
                     )
                 .Add("except=",
                     "Except - a comma-separated list of package names that should not be upgraded when upgrading 'all'. Defaults to empty. Available in 0.9.10+.",
                     option => configuration.UpgradeCommand.PackageNamesToSkip = option.remove_surrounding_quotes())
                ;
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
                throw new ApplicationException("Package name is required. Please pass at least one package name to upgrade.");
            }
        }

        public virtual void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Upgrade Command");
            this.Log().Info(@"
Upgrades a package or a list of packages. Some may prefer to use `cup`
 as a shortcut for `choco upgrade`. If you do not have a package
 installed, upgrade will install it.

NOTE: 100% compatible with older Chocolatey client (0.9.8.x and below)
 with options and switches. Add `-y` for previous behavior with no
 prompt. In most cases you can still pass options and switches with one
 dash (`-`). For more details, see the command reference (`choco -?`).
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco upgrade <pkg|all> [<pkg2> <pkgN>] [<options/switches>]
    cup <pkg|all> [<pkg2> <pkgN>] [<options/switches>]

NOTE: `all` is a special package keyword that will allow you to upgrade
 all currently installed packages.

Skip upgrading certain packages with `choco pin` or with the option
 `--except`.

NOTE: Chocolatey Pro / Business automatically synchronizes with 
 Programs and Features, ensuring automatically updating apps' versions
 (like Chrome) are up to date in Chocolatey's repository. 
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco upgrade chocolatey
    choco upgrade notepadplusplus googlechrome atom 7zip
    choco upgrade notepadplusplus googlechrome atom 7zip -dvfy
    choco upgrade git --params=""'/GitAndUnixToolsOnPath /NoAutoCrlf'"" -y
    choco upgrade nodejs.install --version 0.10.35
    choco upgrade git -s ""'https://somewhere/out/there'""
    choco upgrade git -s ""'https://somewhere/protected'"" -u user -p pass
    choco upgrade all
    choco upgrade all --except=""'skype,conemu'""
");
            "chocolatey".Log().Info(ChocolateyLoggers.Important, "See It In Action");
            "chocolatey".Log().Info(@"
choco upgrade: https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/choco_upgrade.gif

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
            _packageService.upgrade_noop(configuration);
        }

        public virtual void run(ChocolateyConfiguration configuration)
        {
            _packageService.ensure_source_app_installed(configuration);
            _packageService.upgrade_run(configuration);
        }

        public virtual bool may_require_admin_access()
        {
            return true;
        }
    }
}
