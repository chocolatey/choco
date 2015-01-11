namespace chocolatey
{
    using System;

    /// <summary>
    ///   Extensions for Type
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        ///   Determines whether a type is a built in system type (ValueType, string, primitive or is in the System Namespace)
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>true if meets criteria for system type</returns>
        public static bool is_built_in_system_type(this Type type)
        {
            if (type == null) return false;

            // if all else fails, check to see if the namespace is at system.
            return type.IsPrimitive
                   || type.IsValueType
                   || (type == typeof (string))
                   || type.Namespace.Equals("System");
        }

        /// <summary>
        ///   Determines whether a type is a collection, like a list, array or dictionary.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>true if enumerable</returns>
        public static bool is_collections_type(this Type type)
        {
            if (type == null) return false;

            return type.IsArray
                   || type.Namespace.Contains("System.Collections");
        }
    }
}