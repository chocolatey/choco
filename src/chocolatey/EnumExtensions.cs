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
        public static string get_description_or_value(this Enum enumeration)
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

        public static TEnum parse_enum_from_description<TEnum>(this string description)
            where TEnum : struct
        {
            if (!typeof (TEnum).IsEnum)
            {
                throw new InvalidEnumArgumentException("TEnum must be of type Enum");
            }

            Type type = typeof (TEnum);
            foreach (var fieldInfo in type.GetFields())
            {
                var attr = fieldInfo.GetCustomAttributes(typeof (DescriptionAttribute), false).Cast<DescriptionAttribute>().SingleOrDefault();
                if (attr != null && attr.Description.Equals(description))
                {
                    return (TEnum) fieldInfo.GetValue(null);
                }
            }

            return default(TEnum);
        }
    }
}