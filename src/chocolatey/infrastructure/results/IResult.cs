namespace chocolatey.infrastructure.results
{
    using System.Collections.Generic;

    /// <summary>
    ///   Outcome from some work performed.
    /// </summary>
    public interface IResult
    {
        /// <summary>
        ///   Gets a value indicating whether this <see cref="IResults" /> is successful.
        /// </summary>
        /// <value>
        ///   <c>true</c> if success; otherwise, <c>false</c>.
        /// </value>
        bool Success { get; }

        /// <summary>
        ///   The messages
        /// </summary>
        ICollection<ResultMessage> Messages { get; }
    }
}