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
        public ResultType MessageType { get; private set; }

        /// <summary>
        ///   Gets or sets the message.
        /// </summary>
        /// <value>
        ///   The message.
        /// </value>
        public string Message { get; private set; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ResultMessage" /> class.
        /// </summary>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="message">The message.</param>
        public ResultMessage(ResultType messageType, string message)
        {
            MessageType = messageType;
            Message = message;
        }
    }
}