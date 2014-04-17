namespace chocolatey.infrastructure.app.commands
{
    using System.Collections.Generic;
    using attributes;
    using commandline;
    using configuration;
    using infrastructure.commands;

    [CommandFor(CommandNameType.install)]
    public sealed class ChocolateyInstallCommand : ICommand
    {
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

        }

        public void run(ChocolateyConfiguration configuration)
        {
            //start log
            //is this a packages.config? If so run that command (that will call back into here).
            //are we installing from an alternate source? If so run that command instead

            this.Log().Info("Command not yet functional, stay tuned...");
        }
    }
}