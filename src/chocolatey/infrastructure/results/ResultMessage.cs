namespace chocolatey.infrastructure.results
{
    /// <summary>
    ///   A result message
    /// </summary>
    public class ResultMessage
    {
        /// <summary>
        ///   Gets or sets the type of the message.
        /// </summary>
        /// <value>
        ///   The type of the message.
        /// </value>
        public ResultType MessageType { get; set; }

        /// <summary>
        ///   Gets or sets the message.
        /// </summary>
        /// <value>
        ///   The message.
        /// </value>
        public string Message { get; set; }
    }
}