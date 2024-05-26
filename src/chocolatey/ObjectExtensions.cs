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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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

        public static T DeepCopy<T>(this T other)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, other);
                ms.Position = 0;
                return (T)formatter.Deserialize(ms);
            }
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
