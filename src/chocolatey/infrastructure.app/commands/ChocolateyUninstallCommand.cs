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

    [CommandFor(CommandNameType.uninstall)]
    public sealed class ChocolateyUninstallCommand : ICommand
    {
        private readonly IChocolateyPackageService _packageService;

        public ChocolateyUninstallCommand(IChocolateyPackageService packageService)
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
                     "Version - A specific version to uninstall. Defaults to unspecified.",
                     option => configuration.Version = option.remove_surrounding_quotes())
                .Add("a|allversions|all-versions",
                     "AllVersions - Uninstall all versions? Defaults to false.",
                     option => configuration.AllVersions = option != null)
                .Add("ua=|uninstallargs=|uninstallarguments=|uninstall-arguments=",
                     "UninstallArguments - Uninstall Arguments to pass to the native installer in the package. Defaults to unspecified.",
                     option => configuration.InstallArguments = option.remove_surrounding_quotes())
                .Add("o|override|overrideargs|overridearguments|override-arguments",
                     "OverrideArguments - Should uninstall arguments be used exclusively without appending to current package passed arguments? Defaults to false.",
                     option => configuration.OverrideArguments = option != null)
                .Add("notsilent|not-silent",
                     "NotSilent - Do not uninstall this silently. Defaults to false.",
                     option => configuration.NotSilent = option != null)
                .Add("params=|parameters=|pkgparameters=|packageparameters=|package-parameters=",
                     "PackageParameters - Parameters to pass to the package. Defaults to unspecified.",
                     option => configuration.PackageParameters = option.remove_surrounding_quotes())
                .Add("x|forcedependencies|force-dependencies|removedependencies|remove-dependencies",
                     "RemoveDependencies - Uninstall dependencies when uninstalling package(s). Defaults to false.",
                     option => configuration.ForceDependencies = option != null)
                .Add("n|skippowershell|skip-powershell",
                     "Skip Powershell - Do not run chocolateyUninstall.ps1. Defaults to false.",
                     option => configuration.SkipPackageInstallProvider = option != null)
                ;
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
                throw new ApplicationException("Package name is required. Please pass at least one package name to uninstall.");
            }
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Uninstall Command");
            this.Log().Info(@"
Uninstalls a package or a list of packages. Some may prefer to use 
 `cuninst` as a shortcut for `choco uninstall`.

NOTE: Automatic Uninstaller (AutoUninstall) is turned off by default 
 while in preview mode. To turn it on, run the following command:

    feature enable -n autoUninstaller
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco uninstall <pkg|all> [pkg2 pkgN] [options/switches]
    cuninst <pkg|all> [pkg2 pkgN] [options/switches]

NOTE: `all` is a special package keyword that will allow you to 
 uninstall all packages.
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco uninstall git
    choco uninstall notepadplusplus googlechrome atom 7zip 
    choco uninstall notepadplusplus googlechrome atom 7zip -dv
    choco uninstall ruby --version 1.8.7.37402
    choco uninstall nodejs.install --all-versions
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

        public void noop(ChocolateyConfiguration configuration)
        {
            _packageService.uninstall_noop(configuration);
        }

        public void run(ChocolateyConfiguration configuration)
        {
            _packageService.ensure_source_app_installed(configuration);
            _packageService.uninstall_run(configuration);
        }

        public bool may_require_admin_access()
        {
            return true;
        }
    }
}