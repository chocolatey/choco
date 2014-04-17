namespace chocolatey.console
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SimpleInjector;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.configuration;

    public sealed class ConsoleApplication
    {
        public void run(string[] args, ChocolateyConfiguration config, Container container)
        {
            this.Log().Debug(() => "Passed in arguments: {0}".format_with(string.Join(" ", args)));

            IList<string> commandArgs = new List<string>();
            //shift the first arg off 
            int count = 0;
            foreach (var arg in args)
            {
                if (count == 0)
                {
                    count += 1;
                    continue;
                }

                commandArgs.Add(arg);
            }

            var commands = container.GetAllInstances<ICommand>();

            var command = commands.Where((c) =>
                {
                    var attributes = c.GetType().GetCustomAttributes(typeof(CommandForAttribute), false);
                    foreach (CommandForAttribute attribute in attributes)
                    {
                        if (attribute.CommandName.to_string().to_lower() == config.CommandName.to_lower()) return true;
                    }

                    return false;
                }).FirstOrDefault();

            if (command == null)
            {
                if (!string.IsNullOrWhiteSpace(config.CommandName))
                {
                    throw new Exception("Could not find a command registered that meets '{0}'".format_with(config.CommandName));
                }

                Environment.ExitCode = 1;
            }
            else
            {
                ConfigurationOptions.parse_arguments_and_update_configuration(
                    commandArgs,
                    config,
                    (optionSet) => command.configure_argument_parser(optionSet, config),
                    (unparsedArgs) => command.handle_unparsed_arguments(unparsedArgs, config),
                    () => command.help_message(config));

                this.Log().Debug(() => "Configuration: {0}".format_with(config.ToString()));

                if (config.HelpRequested)
                {
#if DEBUG
                    Console.WriteLine("Press enter to continue...");
                    Console.ReadKey();
#endif
                    Environment.Exit(-1);
                }

                command.run(config);
            }


        }
    }
}