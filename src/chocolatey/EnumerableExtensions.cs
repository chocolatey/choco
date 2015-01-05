namespace chocolatey
{
    using System.Collections;
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
        public static IEnumerable<T> or_empty_list_if_null<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }

        /// <summary>
        ///  Safe for each, returns an empty Enumerable if the list to iterate is null.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>Source if not null; otherwise new ArrayList</returns>
        public static IEnumerable or_empty_list_if_null(this IEnumerable source)
        {
            return source ?? new ArrayList();
        }

        /// <summary>
        ///   Joins the specified IEnumerables.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="separator">The value to put in between elements</param>
        /// <returns></returns>
        public static string join(this IEnumerable<string> source, string separator)
        {
            return string.Join(separator, source);
        }

        /// <summary>
        ///   Returns a distinct set of elements using the comparer specified. This implementation will pick the last occurrence
        ///   of each element instead of picking the first. This method assumes that similar items occur in order.
        /// </summary>
        public static IEnumerable<T> distinct_last<T>(this IEnumerable<T> source, IEqualityComparer<T> equalityComparer, IComparer<T> comparer)
        {
            bool first = true;
            bool maxElementHasValue = false;
            var previousElement = default(T);
            var maxElement = default(T);

            foreach (T element in source)
            {
                // If we're starting a new group then return the max element from the last group
                if (!first && !equalityComparer.Equals(element, previousElement))
                {
                    yield return maxElement;

                    // Reset the max element
                    maxElementHasValue = false;
                }

                // If the current max element has a value and is bigger or doesn't have a value then update the max
                if (!maxElementHasValue || (maxElementHasValue && comparer.Compare(maxElement, element) < 0))
                {
                    maxElement = element;
                    maxElementHasValue = true;
                }

                previousElement = element;
                first = false;
            }

            if (!first)
            {
                yield return maxElement;
            }
        }
    }
}