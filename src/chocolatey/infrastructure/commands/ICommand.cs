namespace chocolatey.infrastructure.commands
{
    using System.Collections.Generic;
    using app.configuration;
    using commandline;

    /// <summary>
    ///   Commands that can be configured and run
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        ///   Configure the argument parser.
        /// </summary>
        /// <param name="optionSet">The option set.</param>
        /// <param name="configuration">The configuration.</param>
        void configure_argument_parser(OptionSet optionSet, IConfigurationSettings configuration);

        /// <summary>
        ///   Handle the arguments that were not parsed by the argument parser.
        /// </summary>
        /// <param name="unparsedArguments">The unparsed arguments.</param>
        /// <param name="configuration">The configuration.</param>
        void handle_unparsed_arguments(IList<string> unparsedArguments, IConfigurationSettings configuration);

        /// <summary>
        ///   The specific help message for a particular command.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        void help_message(IConfigurationSettings configuration);

        /// <summary>
        ///   Runs the specified arguments.
        /// </summary>
        /// <param name="config">The configuration.</param>
        void run(IConfigurationSettings config);
    }
}