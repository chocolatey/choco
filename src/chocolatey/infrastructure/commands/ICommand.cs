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
        void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration);

        /// <summary>
        ///   Handle the arguments that were not parsed by the argument parser and/or do additional parsing work
        /// </summary>
        /// <param name="unparsedArguments">The unparsed arguments.</param>
        /// <param name="configuration">The configuration.</param>
        void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration);

        /// <summary>
        ///   The specific help message for a particular command.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        void help_message(ChocolateyConfiguration configuration);

        /// <summary>
        ///   Runs in no op mode, which means it doesn't actually make any changes.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        void noop(ChocolateyConfiguration configuration);

        /// <summary>
        ///   Runs the command.
        /// </summary>
        /// <param name="config">The configuration.</param>
        void run(ChocolateyConfiguration config);
    }
}