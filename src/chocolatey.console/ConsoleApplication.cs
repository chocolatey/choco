namespace chocolatey.console
{
    using System.Collections.Generic;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.configuration;
    using infrastructure.registration;

    public class ConsoleApplication
    {
        public void run(string[] args, IConfigurationSettings config)
        {
            this.Log().Info(() => "{0} v{1}".format_with(ApplicationParameters.Name, config.ChocolateyVersion));
            this.Log().Debug(() => "Passed in arguments: {0}".format_with(string.Join(" ", args)));

            IList<string> command_args = new List<string>();
            //shift the first arg off 
            int count = 0;
            foreach (var arg in args)
            {
                if (count == 0)
                {
                    count += 1;
                    continue;
                }

                command_args.Add(arg);
            }

            SimpleInjectorContainer.Initialize();

            // get the runner you need and go to town
            var runner = new ChocolateyInstallCommand();
            //

            ConfigurationOptions.parse_arguments_and_update_configuration(
                command_args,
                config,
                (optionSet) => runner.configure_argument_parser(optionSet, config),
                (unparsedArgs) => runner.handle_unparsed_arguments(unparsedArgs, config),
                () => runner.help_message(config));
            this.Log().Debug(() => "Configuration: {0}".format_with(config.ToString()));

            runner.run(command_args, config);
        }
    }
}