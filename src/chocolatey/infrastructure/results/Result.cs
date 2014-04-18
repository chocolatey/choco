namespace chocolatey.infrastructure.results
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///   Outcome from some work performed
    /// </summary>
    public class Result : IResult
    {
        private readonly Lazy<List<ResultMessage>> _messages = new Lazy<List<ResultMessage>>();

        public bool Success
        {
            get { return !_messages.Value.Any(x => x.MessageType == ResultType.Error); }
        }

        public ICollection<ResultMessage> Messages
        {
            get { return _messages.Value; }
        }
    }
}