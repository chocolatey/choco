namespace chocolatey.infrastructure.app.runners
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SimpleInjector;
    using attributes;
    using configuration;
    using infrastructure.commands;
    using infrastructure.configuration;

    /// <summary>
    ///   Console application responsible for running chocolatey
    /// </summary>
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
                    var attributes = c.GetType().GetCustomAttributes(typeof (CommandForAttribute), false);
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
                    (unparsedArgs) => command.handle_additional_argument_parsing(unparsedArgs, config),
                    () => command.help_message(config));

                this.Log().Debug(() => "Configuration: {0}".format_with(config.ToString()));

                if (config.HelpRequested)
                {
#if DEBUG
                    Console.WriteLine("Press enter to continue...");
                    Console.ReadKey();
#endif
                    Environment.Exit(1);
                }

                if (config.Noop)
                {
                    if (config.RegularOuptut)
                    {
                        this.Log().Info("_ {0}:{1} - Noop Mode _".format_with(ApplicationParameters.Name, command.GetType().Name));
                    }
                    
                    command.noop(config);
                }
                else
                {
                    this.Log().Debug("_ {0}:{1} - Normal Run Mode _".format_with(ApplicationParameters.Name, command.GetType().Name));
                    command.run(config);
                }
            }
        }
    }
}