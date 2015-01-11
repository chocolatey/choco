namespace chocolatey
{
    using System.Globalization;
    using System.Text.RegularExpressions;

    /// <summary>
    ///   Extensions for string
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        ///   Formats string with the formatting passed in. This is a shortcut to string.Format().
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="formatting">The formatting.</param>
        /// <returns>A formatted string.</returns>
        public static string format_with(this string input, params object[] formatting)
        {
            return string.Format(input, formatting);
        }

        /// <summary>
        ///   Performs a trim only if the item is not null
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string trim_safe(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            return input.Trim();
        }

        /// <summary>
        ///   Performs ToLower() only if input is not null
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string to_lower(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            return input.ToLower();
        }

        /// <summary>
        ///   Gets a string representation unless input is null.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string to_string(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            return input;
        }

        private static readonly Regex _spacePattern = new Regex(@"\s", RegexOptions.Compiled);

        /// <summary>
        ///   If the item contains spaces, it wraps it in quotes
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string wrap_spaces_in_quotes(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            if (_spacePattern.IsMatch(input))
            {
                return "\"{0}\"".format_with(input);
            }

            return input;
        }

        /// <summary>
        ///   Are the strings equal(ignoring case and culture)?
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="other">The value to compare to</param>
        /// <returns>True if these are the same</returns>
        public static bool is_equal_to(this string input, string other)
        {
            return string.Compare(input, other, ignoreCase: true, culture: CultureInfo.InvariantCulture) == 0;
        }

        /// <summary>
        ///   Removes quotes or apostrophes surrounding a string
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string remove_surrounding_quotes(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            if (input.StartsWith(" "))
            {
                input = input.trim_safe();
            }

            if ((input.StartsWith("\"") && input.EndsWith("\""))
                || (input.StartsWith("'") && input.EndsWith("'")))
            {
                input = input.Remove(0, 1).Remove(input.Length - 2, 1);
            }

            return input;
        }
    }
}