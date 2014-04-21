namespace chocolatey
{
    /// <summary>
    /// Extensions for Object
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// A null safe variant of ToString().
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>String.Empty if input is null, otherwise input.ToString()</returns>
        public static string to_string(this object input)
        {
            if (input == null) return string.Empty;

            return input.ToString();
        }
    }
}