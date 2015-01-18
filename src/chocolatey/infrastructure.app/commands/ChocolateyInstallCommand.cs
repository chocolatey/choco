﻿// Copyright © 2011 - Present RealDimensions Software, LLC
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

    [CommandFor(CommandNameType.install)]
    public sealed class ChocolateyInstallCommand : ICommand
    {
        private readonly IChocolateyPackageService _packageService;

        public ChocolateyInstallCommand(IChocolateyPackageService packageService)
        {
            _packageService = packageService;
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
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
                     "IgnoreDependencies - Ignore dependencies when installing package(s). Defaults to false.",
                     option => configuration.IgnoreDependencies = option != null)
                .Add("x|forcedependencies|force-dependencies",
                     "ForceDependencies - Force dependencies to be reinstalled when force installing package(s). Must be used in conjunction with --force. Defaults to false.",
                     option => configuration.ForceDependencies = option != null)
                .Add("n|skippowershell|skip-powershell",
                     "Skip Powershell - Do not run chocolateyInstall.ps1. Defaults to false.",
                     option => configuration.SkipPackageInstallProvider = option != null)
                ;

            //todo: Checksum / ChecksumType defaults to md5 / package name can be a url / installertype
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);
            configuration.PackageNames = string.Join(ApplicationParameters.PackageNamesSeparator.to_string(), unparsedArguments);
        }

        public void handle_validation(ChocolateyConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.PackageNames))
            {
                throw new ApplicationException("Package name is required. Please pass at least one package name to install.");
            }
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Install Command");
            this.Log().Info(@"
Installs a package or a list of packages (sometimes specified as a 
 packages.config).

Usage: choco install pkg|packages.config [pkg2 pkgN] [options/switches]

NOTE: `all` is a special package keyword that will allow you to install 
 all packages from a custom feed. Will not work with Chocolatey default 
 feed. THIS IS NOT YET REIMPLEMENTED.

Examples:

 choco install sysinternals
 choco install notepadplusplus googlechrome atom 7zip 
 choco install notepadplusplus --force --force-dependencies
 choco install notepadplusplus googlechrome atom 7zip -dvfy
 choco install git --params=""/GitAndUnixToolsOnPath /NoAutoCrlf"" -y
 choco install nodejs.install --version 0.10.35

Choco can also install directly from a nuspec/nupkg file. This aids in 
 testing packages.

 choco install path/to/nuspec
 choco install path/to/nupkg

Install multiple versions of a package using -m (AllowMultiple versions)

Example:
 
 choco install ruby --version 1.8.7.37402 -my
 choco install ruby --version 1.9.3.55100 -my
 choco install ruby --version 2.0.0.59800 -my
 choco install ruby --version 2.1.5 -my

NOTE: All of these will add to PATH variable. We'll be adding a special 
 option to not allow PATH changes. Until then you will need to manually 
 go modify Path to just one Ruby and then use something like uru or pik
 to switch between versions.

NOTE: Ruby 1.8.7 reached EOL (end of life) on 7/31/2014 and 1.9.3 will 
 reach EOL on 2/23/2015. They are provided for example only and not 
 recommended to be installed as they could pose security threats once 
 they are past EOL.

");
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            _packageService.install_noop(configuration);
        }

        public void run(ChocolateyConfiguration configuration)
        {
            _packageService.install_run(configuration);
        }
    }
}