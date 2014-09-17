namespace chocolatey.infrastructure.results
{
    /// <summary>
    ///   When working with results, this identifies the type of result
    /// </summary>
    public enum ResultType
    {
        /// <summary>
        ///   The default result type.
        /// </summary>
        None,

        /// <summary>
        ///   Debugging messages that may help the recipient determine items leading up to errors
        /// </summary>
        Debug,

        /// <summary>
        ///   Verbose messages that may help the recipient determine items leading up to errors
        /// </summary>
        Verbose,

        /// <summary>
        ///   These are notes to pass along with the result
        /// </summary>
        Note,

        /// <summary>
        ///   There was no result.
        /// </summary>
        Inconclusive,

        /// <summary>
        ///   These are warnings
        /// </summary>
        Warn,

        /// <summary>
        ///   These are errors
        /// </summary>
        Error,
    }
}