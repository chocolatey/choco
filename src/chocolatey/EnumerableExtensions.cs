namespace chocolatey
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///   Extensions for IEnumerable
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        ///   Safe for each, returns an empty Enumerable if the list to iterate is null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <returns>
        ///   Source if not null; otherwise Enumerable.Empty&lt;<see cref="T" />&gt;
        /// </returns>
        public static IEnumerable<T> OrEmptyListIfNull<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }
    }
}