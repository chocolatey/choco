// Copyright © 2017 - 2018 Chocolatey Software, Inc
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

namespace chocolatey
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    /// <summary>
    ///   Extensions for Object
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        ///   A null safe variant of ToString().
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>String.Empty if input is null, otherwise input.ToString()</returns>
        public static string to_string(this object input)
        {
            if (input == null) return string.Empty;

            return input.ToString();
        }

        public static T deep_copy<T>(this T other)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, other);
                ms.Position = 0;
                return (T)formatter.Deserialize(ms);
            }
        }
    }
}
