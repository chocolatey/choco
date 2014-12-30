namespace chocolatey.infrastructure.app.runners
{
    using System.Collections.Generic;
    using SimpleInjector;
    using configuration;

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

            var runner = new GenericRunner();
            runner.run(config, container, isConsole: true, parseArgs: command =>
                {
                    ConfigurationOptions.parse_arguments_and_update_configuration(
                        commandArgs,
                        config,
                        (optionSet) => command.configure_argument_parser(optionSet, config),
                        (unparsedArgs) => command.handle_additional_argument_parsing(unparsedArgs, config),
                        () => command.handle_validation(config),
                        () => command.help_message(config));
                });
        }
    }
}