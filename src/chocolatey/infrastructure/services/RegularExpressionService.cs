namespace chocolatey.infrastructure.services
{
    using System.Text.RegularExpressions;

    /// <summary>
    ///   Regular Expressions helper
    /// </summary>
    public class RegularExpressionService : IRegularExpressionService
    {
        public string replace(string input, string pattern, MatchEvaluator matchEvaluator)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return regex.Replace(input, matchEvaluator);
        }
    }
}