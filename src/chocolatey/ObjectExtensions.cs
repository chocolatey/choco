// Copyright © 2017 - 2025 Chocolatey Software, Inc
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
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace chocolatey
{
    /// <summary>
    ///   Extensions for Object
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        ///   A null safe variant of <see cref="object.ToString"/>.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns><see cref="string.Empty"/> if <paramref name="input"/> is null, otherwise <paramref name="input"/>.ToString()</returns>
        public static string ToStringSafe(this object input)
        {
            if (input == null)
            {
                return string.Empty;
            }

            return input.ToString();
        }

        /// <summary>
        ///   Creates a deep copy of <paramref name="other"/> and the object graph it references.
        /// </summary>
        /// <remarks>
        ///   This used to round-trip through <c>BinaryFormatter</c>, which has been removed from
        ///   .NET. It now copies the serializable instance fields recursively (mirroring the
        ///   formatter's behaviour: private/inherited fields are copied, <c>[NonSerialized]</c>
        ///   fields are skipped, and object cycles are preserved). Intended for the in-process
        ///   configuration graph; it is not a general-purpose serializer.
        /// </remarks>
        public static T DeepCopy<T>(this T other)
        {
            return (T)DeepCopyInternal(other, new Dictionary<object, object>(ReferenceEqualityComparer.Instance));
        }

        private static object DeepCopyInternal(object original, Dictionary<object, object> visited)
        {
            if (original is null)
            {
                return null;
            }

            var type = original.GetType();

            // Copy-by-value: primitives, enums, strings and common immutable types are
            // safe to share. Type instances are runtime singletons and must be shared.
            if (type.IsPrimitive || type.IsEnum
                || original is string || original is decimal
                || original is DateTime || original is DateTimeOffset || original is TimeSpan
                || original is Guid || original is Type)
            {
                return original;
            }

            if (visited.TryGetValue(original, out var alreadyCopied))
            {
                return alreadyCopied;
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var source = (Array)original;
                var copiedArray = Array.CreateInstance(elementType, source.Length);
                visited[original] = copiedArray;

                for (var i = 0; i < source.Length; i++)
                {
                    copiedArray.SetValue(DeepCopyInternal(source.GetValue(i), visited), i);
                }

                return copiedArray;
            }

            // Build the clone without running constructors, then copy each field. This
            // reproduces collection internals (e.g. List<T>) as well as plain objects.
            var clone = RuntimeHelpers.GetUninitializedObject(type);

            // Boxed value types must be re-boxed after their fields are set.
            if (!type.IsValueType)
            {
                visited[original] = clone;
            }

            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsNotSerialized)
                    {
                        continue;
                    }

                    var value = field.GetValue(original);
                    field.SetValue(clone, DeepCopyInternal(value, visited));
                }
            }

            return clone;
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static string to_string(this object input)
            => ToStringSafe(input);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static T deep_copy<T>(this T other)
            => DeepCopy(other);
#pragma warning restore IDE0022, IDE1006
    }
}
