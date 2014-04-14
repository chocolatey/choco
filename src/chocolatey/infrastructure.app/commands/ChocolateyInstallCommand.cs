namespace chocolatey.infrastructure.app.commands
{
    using System.Collections.Generic;
    using commandline;
    using configuration;
    using infrastructure.commands;

    //install
    public class ChocolateyInstallCommand : ICommand
    {
        public void configure_argument_parser(OptionSet optionSet, IConfigurationSettings configuration)
        {
            optionSet
                .Add("s=|source=",
                     "The source to find the package(s) to install",
                     option => configuration.Source = option)
                .Add("v=|version=",
                     "A specific version to install",
                     option => configuration.Version = option)
                .Add("pre|prerelease",
                     "Include Prereleases?",
                     option => configuration.Prerelease = option != null)
                ;
        }

        public void handle_unparsed_arguments(IList<string> unparsedArguments, IConfigurationSettings configuration)
        {
            configuration.PackageNames = string.Join(" ", unparsedArguments);
        }

        public void help_message(IConfigurationSettings configuration)
        {
            this.Log().Info(@"
Install Command
");
        }

        public void run(ICollection<string> args, IConfigurationSettings configuration)
        {
        }
    }
}