﻿// Copyright © 2017 - 2021 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace chocolatey
{
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
        ///   Source if not null; otherwise Enumerable.Empty&lt;<see cref="OrEmpty{T}" />&gt;
        /// </returns>
        public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }

        /// <summary>
        ///   Safe for each, returns an empty Enumerable if the list to iterate is null.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>Source if not null; otherwise new ArrayList</returns>
        public static IEnumerable OrEmpty(this IEnumerable source)
        {
            return source ?? new ArrayList();
        }

        /// <summary>
        ///   Joins the specified IEnumerable to required separator.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="separator">The value to put in between elements</param>
        /// <returns></returns>
        public static string Join(this IEnumerable<string> source, string separator)
        {
            return string.Join(separator, source);
        }

        /// <summary>
        ///   Returns a distinct set of elements using the comparer specified. This implementation will pick the last occurrence
        ///   of each element instead of picking the first. This method assumes that similar items occur in order.
        /// </summary>
        public static IEnumerable<T> LastDistinct<T>(this IEnumerable<T> source, IEqualityComparer<T> equalityComparer, IComparer<T> comparer)
        {
            var first = true;
            var maxElementHasValue = false;
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

        /// <summary>
        ///   Generates a unique hash code for a source of objects
        /// </summary>
        /// <typeparam name="T">Generic type.</typeparam>
        /// <param name="source">The source.</param>
        /// <returns>
        ///   Integer value representing hash code for input collection.
        /// </returns>
        /// <remarks>
        ///   Taken from here: https://stackoverflow.com/a/30758270/671491
        /// </remarks>
        public static int SequenceHashCode<T>(this IEnumerable<T> source)
        {
            const int seed = 487;
            const int modifier = 31;

            unchecked
            {
                return source.Aggregate(seed, (current, item) =>
                    (current * modifier) + item.GetHashCode());
            }
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static IEnumerable<T> or_empty_list_if_null<T>(this IEnumerable<T> source)
            => OrEmpty(source);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static IEnumerable or_empty_list_if_null(this IEnumerable source)
            => OrEmpty(source);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static string join(this IEnumerable<string> source, string separator)
            => Join(source, separator);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static IEnumerable<T> distinct_last<T>(this IEnumerable<T> source, IEqualityComparer<T> equalityComparer, IComparer<T> comparer)
            => LastDistinct(source, equalityComparer, comparer);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static int get_sequence_hash_code<T>(this IEnumerable<T> source)
            => SequenceHashCode<T>(source);
#pragma warning restore IDE0022, IDE1006
    }
}
