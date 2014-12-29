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
                .Add("version=",
                     "Version - A specific version to uninstall. Defaults to unspecified.",
                     option => configuration.Version = option)
                .Add("a|allversions|all-versions",
                     "AllVersions - Uninstall all versions? Defaults to false.",
                     option => configuration.AllVersions = option != null)
                .Add("ua=|uninstallargs=|uninstallarguments=|uninstall-arguments=",
                     "UninstallArguments - Uninstall Arguments to pass to the native installer in the package. Defaults to unspecified.",
                     option => configuration.InstallArguments = option)
                .Add("o|override|overrideargs|overridearguments|override-arguments",
                     "OverrideArguments - Should uninstall arguments be used exclusively without appending to current package passed arguments? Defaults to false.",
                     option => configuration.OverrideArguments = option != null)
                .Add("notsilent|not-silent",
                     "NotSilent - Do not uninstall this silently. Defaults to false.",
                     option => configuration.NotSilent = option != null)
                .Add("params=|parameters=|pkgparameters=|packageparameters=|package-parameters=",
                     "PackageParameters - Parameters to pass to the package. Defaults to unspecified.",
                     option => configuration.PackageParameters = option)
                .Add("x|forcedependencies|force-dependencies",
                     "ForceDependencies - Force dependencies to be uninstalled when uninstalling package(s). Defaults to false.",
                     option => configuration.ForceDependencies = option != null)
                .Add("n|skippowershell|skip-powershell",
                     "Skip Powershell - Do not run chocolateyUninstall.ps1. Defaults to false.",
                     option => configuration.SkipPackageInstallProvider = option != null)
                ;
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
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
Uninstalls a package or a list of packages (sometimes passed as a packages.config).

Usage: choco uninstall pkg|packages.config [pkg2 pkgN] [options/switches]

NOTE: `all` is a special package keyword that will allow you to uninstall all
 packages.

");
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            _packageService.uninstall_noop(configuration);
        }

        public void run(ChocolateyConfiguration configuration)
        {
            _packageService.uninstall_run(configuration);
        }
    }
}