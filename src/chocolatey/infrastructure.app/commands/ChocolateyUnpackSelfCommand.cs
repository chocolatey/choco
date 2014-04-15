namespace chocolatey.infrastructure.app.commands
{
    using System.Collections.Generic;
    using commandline;
    using configuration;
    using infrastructure.commands;

    public sealed class ChocolateyUnpackSelfCommand : ICommand
    {
        public void configure_argument_parser(OptionSet optionSet, IConfigurationSettings configuration)
        {
        }

        public void handle_unparsed_arguments(IList<string> unparsedArguments, IConfigurationSettings configuration)
        {
        }

        public void help_message(IConfigurationSettings configuration)
        {
            this.Log().Info(@"
UnpackSelf Command

This command should only be used when installing chocolatey, not during normal operation.
");
        }

        public void run(IConfigurationSettings configuration)
        {
            this.Log().Info("{0} is setting itself up for use".format_with(ApplicationParameters.Name));
            //unpack all of the resources over to programdata
        }
    }
}