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
    using domain;
    using infrastructure.commands;
    using logging;
    using services;

    [CommandFor(CommandNameType.upgrade)]
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
                     option => configuration.Sources = option)
                .Add("version=",
                     "Version - A specific version to install. Defaults to unspecified.",
                     option => configuration.Version = option)
                .Add("pre|prerelease",
                     "Prerelease - Include Prereleases? Defaults to false.",
                     option => configuration.Prerelease = option != null)
                .Add("x86|forcex86",
                     "ForceX86 - Force x86 (32bit) installation on 64 bit systems. Defaults to false.",
                     option => configuration.ForceX86 = option != null)
                .Add("ia=|installargs=|installarguments=|install-arguments=",
                     "InstallArguments - Install Arguments to pass to the native installer in the package. Defaults to unspecified.",
                     option => configuration.InstallArguments = option)
                .Add("o|override|overrideargs|overridearguments|override-arguments",
                     "OverrideArguments - Should install arguments be used exclusively without appending to current package passed arguments? Defaults to false.",
                     option => configuration.OverrideArguments = option != null)
                .Add("notsilent|not-silent",
                     "NotSilent - Do not install this silently. Defaults to false.",
                     option => configuration.NotSilent = option != null)
                .Add("params=|parameters=|pkgparameters=|packageparameters=|package-parameters=",
                     "PackageParameters - Parameters to pass to the package. Defaults to unspecified.",
                     option => configuration.PackageParameters = option)
                .Add("m|sxs|sidebyside|side-by-side|allowmultiple|allow-multiple|allowmultipleversions|allow-multiple-versions",
                     "AllowMultipleVersions - Should multiple versions of a package be installed? Defaults to false.",
                     option => configuration.AllowMultipleVersions = option != null)
                .Add("i|ignoredependencies|ignore-dependencies",
                     "IgnoreDependencies - Ignore dependencies when upgrading package(s). Defaults to false.",
                     option => configuration.IgnoreDependencies = option != null)
                .Add("n|skippowershell|skip-powershell",
                     "Skip Powershell - Do not run chocolateyInstall.ps1. Defaults to false.",
                     option => configuration.SkipPackageInstallProvider = option != null)
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

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Upgrade Command");
            this.Log().Info(@"
Upgrades a package or a list of packages. Some may prefer to use `cup` 
 as a shortcut for `choco upgrade`.
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco upgrade pkg [pkg2 pkgN] [options/switches]

NOTE: `all` is a special package keyword that will allow you to upgrade 
 all currently installed packages.

NOTE: If you do not have a package installed, upgrade will error.
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco upgrade chocolatey
    choco upgrade notepadplusplus googlechrome atom 7zip 
    choco upgrade notepadplusplus googlechrome atom 7zip -dvfy
    choco upgrade git --params=""/GitAndUnixToolsOnPath /NoAutoCrlf"" -y
    choco upgrade nodejs.install --version 0.10.35
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
            _packageService.upgrade_run(configuration);
        }
    }
}