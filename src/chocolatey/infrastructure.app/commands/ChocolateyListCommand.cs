namespace chocolatey.infrastructure.app.commands
{
    using System;
    using System.Collections.Generic;
    using attributes;
    using builders;
    using commandline;
    using configuration;
    using infrastructure.commands;

    [CommandFor(CommandNameType.list)]
    [CommandFor(CommandNameType.search)]
    public sealed class ChocolateyListCommand : ICommand
    {
        private readonly string _nugetExePath = ApplicationParameters.Tools.NugetExe;
        private readonly IDictionary<string, ExternalCommandArgument> _nugetListArguments = new Dictionary<string, ExternalCommandArgument>();

        public ChocolateyListCommand()
        {
            set_nuget_list_dictionary();
        }

        private void set_nuget_list_dictionary()
        {
            _nugetListArguments.Add("_list_", new ExternalCommandArgument {ArgumentOption = "list", Required = true});
            _nugetListArguments.Add("Filter", new ExternalCommandArgument {ArgumentOption = "filter", UseValueOnly = true});
            _nugetListArguments.Add("AllVersions", new ExternalCommandArgument {ArgumentOption = "-all"});
            _nugetListArguments.Add("Prerelease", new ExternalCommandArgument {ArgumentOption = "-prerelease"});
            _nugetListArguments.Add("Verbose", new ExternalCommandArgument {ArgumentOption = "-verbosity", ArgumentValue = " detailed"});
            _nugetListArguments.Add("_non_interactive_", new ExternalCommandArgument {ArgumentOption = "-noninteractive", Required = true});
            _nugetListArguments.Add("Source", new ExternalCommandArgument {ArgumentOption = "-source ", QuoteValue = true});
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("s=|source=",
                     "Source - Source location for install. Can include special 'webpi'. Defaults to sources.",
                     option => configuration.Source = option)
                .Add("lo|localonly",
                     "LocalOnly - Only search in installed items",
                     option => configuration.LocalOnly = option != null)
                .Add("a|all|allversions",
                     "AllVersions - include results from all versions",
                     option => configuration.AllVersions = option != null)
                ;
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Filter = string.Join(" ", unparsedArguments);

            if (configuration.LocalOnly)
            {
                configuration.Source = ApplicationParameters.PackagesLocation;
                configuration.Source = @"c:\chocolatey\lib"; //todo:temporary
            }
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(@"_ List/Search Command _

Chocolatey will perform a search for a package local or remote.

Usage: choco search filter [options/switches]
Usage: choco list filter [options/switches]

");
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            if (configuration.Source.is_equal_to(SpecialSourceTypes.webpi.to_string()))
            {
                //todo: webpi
            }
            else
            {
                this.Log().Info("{0} would have run the following to return a list of results:{1}'\"{2}\" {3}'".format_with(
                    ApplicationParameters.Name,
                    Environment.NewLine,
                    _nugetExePath,
                    ExternalCommandArgsBuilder.BuildArguments(configuration, _nugetListArguments)
                                    )
                    );
            }
        }

        public void run(ChocolateyConfiguration configuration)
        {
            this.Log().Debug(() => "Searching for package information");

            if (configuration.Source.is_equal_to(SpecialSourceTypes.webpi.to_string()))
            {
                //todo: webpi
                //install webpi if not installed
                //run the webpi command 
                this.Log().Info("Command not yet functional, stay tuned...");
            }
            else
            {
                var args = ExternalCommandArgsBuilder.BuildArguments(configuration, _nugetListArguments);
                int exitCode = CommandExecutor.execute(_nugetExePath, args, true);
                Environment.ExitCode = exitCode;
                this.Log().Debug(() => "Command '\"{0}\" {1}' exited with '{2}'".format_with(_nugetExePath, args, exitCode));
            }
        }
    }
}