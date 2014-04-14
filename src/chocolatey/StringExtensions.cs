namespace chocolatey
{
    /// <summary>
    ///     Extensions for strings
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        ///     Formats string with the formatting passed in. This is a shortcut to string.Format().
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="formatting">The formatting.</param>
        /// <returns>A formatted string.</returns>
        public static string format_with(this string input, params object[] formatting)
        {
            return string.Format(input, formatting);
        }

        /// <summary>
        ///     Performs a trim only if the item is not null
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string trim_safe(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            return input.Trim();
        }

        /// <summary>
        ///     Toes the lower safe.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string to_lower(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            return input.ToLower();
        }

        /// <summary>
        ///     Gets a string representation of an Ooottt.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string to_string(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            return input;
        }
    }
}