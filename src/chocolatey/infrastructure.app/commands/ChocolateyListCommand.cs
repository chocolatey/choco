namespace chocolatey.infrastructure.app.commands
{
    using System.Collections.Generic;
    using commandline;
    using configuration;
    using infrastructure.commands;

    public sealed class ChocolateyListCommand : ICommand
    {
        public void configure_argument_parser(OptionSet optionSet, IConfigurationSettings configuration)
        {
            optionSet
                .Add("s=|source=",
                     "Source",
                     option => configuration.Source = option)
                .Add("lo|localonly",
                     "Local Only",
                     option => configuration.LocalOnly = option != null)
                ;
        }

        public void handle_unparsed_arguments(IList<string> unparsedArguments, IConfigurationSettings configuration)
        {
            configuration.Filter = string.Join(" ", unparsedArguments);
        }

        public void help_message(IConfigurationSettings configuration)
        {
            this.Log().Info(@"
List Command

");
        }

        public void run(ICollection<string> args, IConfigurationSettings configuration)
        {
        }
    }
}