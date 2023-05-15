// Copyright © 2017 - 2021 Chocolatey Software, Inc
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
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    ///   Extensions for Enum
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        ///   Gets the description [Description("")] or ToString() value of an enumeration.
        /// </summary>
        /// <param name="enumeration">The enumeration item.</param>
        public static string DescriptionOrValue(this Enum enumeration)
        {
            string description = enumeration.ToString();

            Type type = enumeration.GetType();
            MemberInfo[] memberInfo = type.GetMember(description);

            if (memberInfo != null && memberInfo.Length > 0)
            {
                var attrib = memberInfo[0].GetCustomAttributes(typeof (DescriptionAttribute), false).Cast<DescriptionAttribute>().SingleOrDefault();

                if (attrib != null)
                {
                    description = attrib.Description;
                }
            }

            return description;
        }

        public static TEnum ParseEnumDescription<TEnum>(this string description)
            where TEnum : struct, Enum
        {
            Type type = typeof (TEnum);
            foreach (var fieldInfo in type.GetFields())
            {
                var attr = fieldInfo.GetCustomAttributes(typeof (DescriptionAttribute), false).Cast<DescriptionAttribute>().SingleOrDefault();
                if (attr != null && attr.Description.Equals(description, StringComparison.Ordinal))
                {
                    return (TEnum) fieldInfo.GetValue(null);
                }
            }

            return default(TEnum);
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static string get_description_or_value(this Enum enumeration)
            => DescriptionOrValue(enumeration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static TEnum parse_enum_from_description<TEnum>(this string description)
            where TEnum : struct, Enum
            => ParseEnumDescription<TEnum>(description);
#pragma warning restore IDE1006
    }
}
