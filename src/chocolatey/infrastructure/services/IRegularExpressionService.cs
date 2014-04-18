namespace chocolatey.infrastructure.services
{
    using System.Text.RegularExpressions;

    /// <summary>
    ///   Regular expressions helper
    /// </summary>
    public interface IRegularExpressionService
    {
        /// <summary>
        ///   Replaces the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="matchEvaluator">The match evaluator.</param>
        /// <returns></returns>
        string replace(string input, string pattern, MatchEvaluator matchEvaluator);
    }
}