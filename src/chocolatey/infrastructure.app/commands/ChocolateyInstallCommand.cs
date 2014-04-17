namespace chocolatey.infrastructure.app.commands
{
    using System;
    using System.Collections.Generic;
    using attributes;
    using builders;
    using commandline;
    using configuration;
    using infrastructure.commands;

    [CommandFor(CommandNameType.install)]
    public sealed class ChocolateyInstallCommand : ICommand
    {
        private const string PACKAGE_NAME_TOKEN = "{{packagename}}";
        private readonly string _nugetExePath = ApplicationParameters.Tools.NugetExe;
        private readonly IDictionary<string, ExternalCommandArgument> _nugetArguments = new Dictionary<string, ExternalCommandArgument>();

        public ChocolateyInstallCommand()
        {
            set_nuget_args_dictionary();
        }

        private void set_nuget_args_dictionary()
        {
            _nugetArguments.Add("_install_", new ExternalCommandArgument {ArgumentOption = "install", Required = true});
            _nugetArguments.Add("_package_name_", new ExternalCommandArgument {ArgumentOption = PACKAGE_NAME_TOKEN, Required = true});
            _nugetArguments.Add("Version", new ExternalCommandArgument {ArgumentOption = "-version ",});
            _nugetArguments.Add("_output_directory_", new ExternalCommandArgument
                {
                    ArgumentOption = "-outputdirectory ",
                    ArgumentValue = "{0}".format_with(ApplicationParameters.PackagesLocation),
                    QuoteValue = true,
                    Required = true
                });
            _nugetArguments.Add("Source", new ExternalCommandArgument {ArgumentOption = "-source ", QuoteValue = true});
            _nugetArguments.Add("Prerelease", new ExternalCommandArgument {ArgumentOption = "-prerelease"});
            _nugetArguments.Add("_non_interactive_", new ExternalCommandArgument {ArgumentOption = "-noninteractive", Required = true});
            _nugetArguments.Add("_no_cache_", new ExternalCommandArgument {ArgumentOption = "-nocache", Required = true});
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("s=|source=",
                     "Source - The source to find the package(s) to install. Special sources include: ruby, webpi, cygwin, windowsfeatures, and python. Defaults to default feeds.",
                     option => configuration.Source = option)
                .Add("version=",
                     "Version - A specific version to install. Defaults to unspecified.",
                     option => configuration.Version = option)
                .Add("pre|prerelease",
                     "Prerelease - Include Prereleases? Defaults to false.",
                     option => configuration.Prerelease = option != null)
                .Add("x86|forcex86",
                     "ForceX86 - Force x86 (32bit) installation on 64 bit systems. Defaults to false.",
                     option => configuration.ForceX86 = option != null)
                .Add("ia=|installargs=|installarguments=",
                     "InstallArguments - Install Arguments to pass to the native installer in the package. Defaults to unspecified.",
                     option => configuration.InstallArguments = option)
                .Add("o|override|overrideargs|overridearguments",
                     "OverrideArguments - Should install arguments be used exclusively without appending to current package passed arguments? Defaults to false.",
                     option => configuration.OverrideArguments = option != null)
                .Add("notsilent",
                     "NotSilent - Do not install this silently. Defaults to false.",
                     option => configuration.NotSilent = option != null)
                .Add("params=|parameters=|pkgparameters=|packageparameters=",
                     "PackageParameters - Parameters to pass to the package. Defaults to unspecified.",
                     option => configuration.PackageParameters = option)
                .Add("ignoredependencies",
                     "IgnoreDependencies - Ignore dependencies when installing package(s). Defaults to false.",
                     option => configuration.IgnoreDependencies = option != null)
                ;
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.PackageNames = string.Join(" ", unparsedArguments);
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(@"_ Install Command _

Installs a package or a list of packages (sometimes passed as a packages.config).

Usage: choco install pkg|packages.config [pkg2 pkgN] [options/switches]

NOTE: `all` is a special package keyword that will allow you to install all
 packages from a custom feed. Will not work with Chocolatey default feed.

");
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            this.Log().Info("{0} would have run the following to install packages:{1}'\"{2}\" {3}'".format_with(
                ApplicationParameters.Name,
                Environment.NewLine,
                _nugetExePath,
                ExternalCommandArgsBuilder.BuildArguments(configuration, _nugetArguments)
                                )
                );
        }

        public void run(ChocolateyConfiguration configuration)
        {
            //start log
            //is this a packages.config? If so run that command (that will call back into here).
            //are we installing from an alternate source? If so run that command instead

            var packagesInstalled = new Dictionary<string, int>();

            var args = ExternalCommandArgsBuilder.BuildArguments(configuration, _nugetArguments);

            var packageSuccesses = 0;
            var totalPackageCount = 0;
            int exitCode = -1;
            foreach (var packageToInstall in configuration.PackageNames.Split(' '))
            {
                var argsForPackage = args.Replace(PACKAGE_NAME_TOKEN, packageToInstall);
                totalPackageCount += 1;
                exitCode = CommandExecutor.execute(_nugetExePath, argsForPackage, true);
                
                //todo: will need to get into the command log and see what we have as installed dependencies
                //overall, if one fails, the process should report as a failure.
                if (Environment.ExitCode != 0)
                {
                    Environment.ExitCode = exitCode;
                }
                else
                {
                    packageSuccesses += 1;
                }

                packagesInstalled.Add(packageToInstall, exitCode);
            }

            this.Log().Info(() => "Install Summary: {0}/{1} packages installed. Please check logs for errors.".format_with(packageSuccesses, totalPackageCount));
            this.Log().Warn("Command not yet fully functional, stay tuned...");
        }
    }
}