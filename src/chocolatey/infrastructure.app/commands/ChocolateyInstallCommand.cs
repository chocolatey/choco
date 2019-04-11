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

namespace chocolatey.infrastructure.app.commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
                     "Source - The source to find the package(s) to install. Special sources include: ruby, webpi, cygwin, windowsfeatures, and python. To specify more than one source, pass it with a semi-colon separating the values (e.g. \"'source1;source2'\"). Defaults to default feeds.",
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
                .Add("ia=|installargs=|install-args=|installarguments=|install-arguments=",
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
                .Add("argsglobal|args-global|installargsglobal|install-args-global|applyargstodependencies|apply-args-to-dependencies|apply-install-arguments-to-dependencies",
                     "Apply Install Arguments To Dependencies  - Should install arguments be applied to dependent packages? Defaults to false.",
                     option => configuration.ApplyInstallArgumentsToDependencies = option != null)
                .Add("paramsglobal|params-global|packageparametersglobal|package-parameters-global|applyparamstodependencies|apply-params-to-dependencies|apply-package-parameters-to-dependencies",
                     "Apply Package Parameters To Dependencies  - Should package parameters be applied to dependent packages? Defaults to false.",
                     option => configuration.ApplyPackageParametersToDependencies = option != null)
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
                     "UsePackageExitCodes - Package scripts can provide exit codes. Use those for choco's exit code when non-zero (this value can come from a dependency package). Chocolatey defines valid exit codes as 0, 1605, 1614, 1641, 3010.  Overrides the default feature '{0}' set to '{1}'. Available in 0.9.10+.".format_with(ApplicationParameters.Features.UsePackageExitCodes, configuration.Features.UsePackageExitCodes.to_string()),
                     option => configuration.Features.UsePackageExitCodes = option != null
                     )
                 .Add("stoponfirstfailure|stop-on-first-failure|stop-on-first-package-failure",
                     "Stop On First Package Failure - stop running install, upgrade or uninstall on first package failure instead of continuing with others. Overrides the default feature '{0}' set to '{1}'. Available in 0.10.4+.".format_with(ApplicationParameters.Features.StopOnFirstPackageFailure, configuration.Features.StopOnFirstPackageFailure.to_string()),
                     option => configuration.Features.StopOnFirstPackageFailure = option != null
                     )
                 .Add("exitwhenrebootdetected|exit-when-reboot-detected",
                     "Exit When Reboot Detected - Stop running install, upgrade, or uninstall when a reboot request is detected. Requires '{0}' feature to be turned on. Will exit with either {1} or {2}. Overrides the default feature '{3}' set to '{4}'. Available in 0.10.12+.".format_with
                     (ApplicationParameters.Features.UsePackageExitCodes, ApplicationParameters.ExitCodes.ErrorFailNoActionReboot, ApplicationParameters.ExitCodes.ErrorInstallSuspend, ApplicationParameters.Features.ExitOnRebootDetected, configuration.Features.ExitOnRebootDetected.to_string()),
                     option => configuration.Features.ExitOnRebootDetected = option != null
                     )
                 .Add("ignoredetectedreboot|ignore-detected-reboot",
                     "Ignore Detected Reboot - Ignore any detected reboots if found. Overrides the default feature '{0}' set to '{1}'. Available in 0.10.12+.".format_with
                     (ApplicationParameters.Features.ExitOnRebootDetected, configuration.Features.ExitOnRebootDetected.to_string()),
                     option =>
                     {
                         if (option != null)
                         {
                             configuration.Features.ExitOnRebootDetected = false;
                         }
                     })
                .Add("disable-repository-optimizations|disable-package-repository-optimizations",
                     "Disable Package Repository Optimizations - Do not use optimizations for reducing bandwidth with repository queries during package install/upgrade/outdated operations. Should not generally be used, unless a repository needs to support older methods of query. When used, this makes queries similar to the way they were done in Chocolatey v0.10.11 and before. Overrides the default feature '{0}' set to '{1}'. Available in 0.10.14+.".format_with
                        (ApplicationParameters.Features.UsePackageRepositoryOptimizations, configuration.Features.UsePackageRepositoryOptimizations.to_string()),
                     option =>
                     {
                         if (option != null)
                         {
                             configuration.Features.UsePackageRepositoryOptimizations = false;
                         }
                     })
                ;

            //todo: package name can be a url / installertype
        }

        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);
            configuration.PackageNames = string.Join(ApplicationParameters.PackageNamesSeparator.to_string(), unparsedArguments.Where(arg => !arg.StartsWith("-")));
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

            if (!string.IsNullOrWhiteSpace(configuration.Input))
            {
                var unparsedOptionsAndPackages = configuration.Input.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (!configuration.Information.IsLicensedVersion)
                {
                    foreach (var argument in unparsedOptionsAndPackages.or_empty_list_if_null())
                    {
                        var arg = argument.to_lower();
                        if (arg.StartsWith("-dir") || arg.StartsWith("--dir") || arg.StartsWith("-install") || arg.StartsWith("--install"))
                        {
                            throw new ApplicationException("It appears you are attempting to use options that may be only available in licensed versions of Chocolatey ('{0}'). There may be ways in the open source edition to achieve what you are looking to do. Please remove the argument and consult the documentation.".format_with(arg));
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(configuration.SourceCommand.Username) && string.IsNullOrWhiteSpace(configuration.SourceCommand.Password))
            {
                this.Log().Debug(ChocolateyLoggers.LogFileOnly, "Username '{0}' provided. Asking for password.".format_with(configuration.SourceCommand.Username));
                System.Console.Write("User name '{0}' provided. Password: ".format_with(configuration.SourceCommand.Username));
                configuration.SourceCommand.Password = InteractivePrompt.get_password(configuration.PromptForConfirmation);
            }
        }

        public virtual void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Install Command");
            this.Log().Info(@"
Installs a package or a list of packages (sometimes specified as a
 packages.config). Some may prefer to use `cinst` as a shortcut for
 `choco install`.

NOTE: 100% compatible with older chocolatey client (0.9.8.32 and below)
 with options and switches. Add `-y` for previous behavior with no
 prompt. In most cases you can still pass options and switches with one
 dash (`-`). For more details, see the command reference (`choco -?`).
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco install <pkg|packages.config> [<pkg2> <pkgN>] [<options/switches>]
    cinst <pkg|packages.config> [<pkg2> <pkgN>] [<options/switches>]

NOTE: `all` is a special package keyword that will allow you to install
 all packages from a custom feed. Will not work with Chocolatey default
 feed. THIS IS NOT YET REIMPLEMENTED.

NOTE: Any package name ending with .config is considered a
 'packages.config' file. Please see https://bit.ly/packages_config

NOTE: Chocolatey Pro / Business builds on top of a great open source
 experience with quite a few features that enhance the your use of the
 community package repository (when using Pro), and really enhance the
 Chocolatey experience all around. If you are an organization looking
 for a better ROI, look no further than Business - automatic package
 creation from installer files, automatic recompile support, runtime
 malware protection, private CDN download cache, synchronize with
 Programs and Features, etc - https://chocolatey.org/compare.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco install sysinternals
    choco install notepadplusplus googlechrome atom 7zip
    choco install notepadplusplus --force --force-dependencies
    choco install notepadplusplus googlechrome atom 7zip -dvfy
    choco install git -y --params=""'/GitAndUnixToolsOnPath /NoAutoCrlf'""
    choco install git -y --params=""'/GitAndUnixToolsOnPath /NoAutoCrlf'"" --install-arguments=""'/DIR=C:\git'""
    # Params are package parameters, passed to the package
    # Install args are installer arguments, appended to the silentArgs 
    #  in the package for the installer itself
    choco install nodejs.install --version 0.10.35
    choco install git -s ""'https://somewhere/out/there'""
    choco install git -s ""'https://somewhere/protected'"" -u user -p pass

Choco can also install directly from a nuspec/nupkg file. This aids in
 testing packages:

    choco install <path/to/nuspec>
    choco install <path/to/nupkg>

Install multiple versions of a package using -m (AllowMultiple versions)

    choco install ruby --version 1.9.3.55100 -my
    choco install ruby --version 2.0.0.59800 -my
    choco install ruby --version 2.1.5 -my

What is `-my`? See option bundling in the command reference
 (`choco -?`).

NOTE: All of these will add to PATH variable. We'll be adding a special
 option to not allow PATH changes. Until then you will need to manually
 go modify Path to just one Ruby and then use something like uru
 (https://bitbucket.org/jonforums/uru) or pik
 (https://chocolatey.org/packages/pik) to switch between versions.

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
 - 1641: success, reboot initiated
 - 3010: success, reboot required
 - other (not listed): likely an error has occurred

In addition to normal exit codes, packages are allowed to exit
 with their own codes when the feature '{0}' is
 turned on. Uninstall command has additional valid exit codes.
 Available in v0.9.10+.

Reboot Exit Codes:
 - 350: pending reboot detected, no action has occurred
 - 1604: install suspended, incomplete

In addition to the above exit codes, you may also see reboot exit codes
 when the feature '{1}' is turned on. It typically requires
 the feature '{0}' to also be turned on to work properly.
 Available in v0.10.12+.
".format_with(ApplicationParameters.Features.UsePackageExitCodes, ApplicationParameters.Features.ExitOnRebootDetected));

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "See It In Action");
            "chocolatey".Log().Info(@"
Chocolatey FOSS install showing tab completion and `refreshenv` (a way
 to update environment variables without restarting the shell).

FOSS install in action: https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/choco_install.gif

Chocolatey Professional showing private download cache and virus scan
 protection.

Pro install in action: https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/chocopro_install_stopped.gif
");
            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Packages.config");
            "chocolatey".Log().Info(@"
Alternative to PackageName. This is a list of packages in an xml manifest for Chocolatey to install. This is like the packages.config that NuGet uses except it also adds other options and switches. This can also be the path to the packages.config file if it is not in the current working directory.

NOTE: The filename is only required to end in .config, the name is not required to be packages.config.

    <?xml version=""1.0"" encoding=""utf-8""?>
    <packages>
      <package id=""apackage"" />
      <package id=""anotherPackage"" version=""1.1"" />
      <package id=""chocolateytestpackage"" version=""0.1"" source=""somelocation"" />
      <package id=""alloptions"" version=""0.1.1""
               source=""https://somewhere/api/v2/"" installArguments=""""
               packageParameters="""" forceX86=""false"" allowMultipleVersions=""false""
               ignoreDependencies=""false""
               />
    </packages>

");
            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Alternative Sources");
            "chocolatey".Log().Info(@"
Available in 0.9.10+.

Ruby
This specifies the source is Ruby Gems and that we are installing a
 gem. If you do not have ruby installed prior to running this command,
 the command will install that first.
 e.g. `choco install compass -source ruby`

WebPI
This specifies the source is Web PI (Web Platform Installer) and that
 we are installing a WebPI product, such as IISExpress. If you do not
 have the Web PI command line installed, it will install that first and
 then the product requested.
 e.g. `choco install IISExpress --source webpi`

Cygwin
This specifies the source is Cygwin and that we are installing a cygwin
 package, such as bash. If you do not have Cygwin installed, it will
 install that first and then the product requested.
 e.g. `choco install bash --source cygwin`

Python
This specifies the source is Python and that we are installing a python
 package, such as Sphinx. If you do not have easy_install and Python
 installed, it will install those first and then the product requested.
 e.g. `choco install sphinx --source python`

Windows Features
This specifies that the source is a Windows Feature and we should
 install via the Deployment Image Servicing and Management tool (DISM)
 on the local machine.
 e.g. `choco install IIS-WebServerRole --source windowsfeatures`

");
            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Resources");
            "chocolatey".Log().Info(@"
 * How-To: A complete example of how you can use the PackageParameters argument
   when creating a Chocolatey Package can be seen at
   https://chocolatey.org/docs/how-to-parse-package-parameters-argument
 * One may want to override the default installation directory of a
   piece of software. See
   https://chocolatey.org/docs/getting-started#overriding-default-install-directory-or-other-advanced-install-concepts.

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
