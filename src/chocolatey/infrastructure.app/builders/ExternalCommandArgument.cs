namespace chocolatey.infrastructure.app.builders
{
    /// <summary>
    ///   Used with ExternalCommandArgsBuilder to build arguments to pass to an external command.
    /// </summary>
    public struct ExternalCommandArgument
    {
        /// <summary>
        ///   The argument option, include everything on the left side of equal (including equal or space) e.g. "-debug", "-source=", "-name "
        /// </summary>
        public string ArgumentOption;

        /// <summary>
        ///   The argument value - Leave this empty to be set by argsbuilder
        ///   If the value is already set, it will not be set again, the value will just be used
        /// </summary>
        public string ArgumentValue;

        /// <summary>
        ///   Use the value only, not the argument option
        /// </summary>
        public bool UseValueOnly;

        /// <summary>
        ///   Quote the value, even if not already quoted
        /// </summary>
        public bool QuoteValue;

        /// <summary>
        ///   This argument is required, so include it unconditionally whether value found or not.
        /// </summary>
        public bool Required;
    }
}