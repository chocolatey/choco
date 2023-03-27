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

namespace Chocolatey
{
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text.RegularExpressions;
    using Infrastructure.App;
    using Infrastructure.Logging;

    /// <summary>
    ///   Extensions for string
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        ///   Formats string with the formatting passed in. This is a null-safe wrapper for <see cref="string.Format(string, object[])"/>.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="formatting">The formatting.</param>
        /// <returns>A formatted string, or <see cref="string.Empty"/> if <paramref name="input"/> is null.</returns>
        public static string FormatWith(this string input, params object[] formatting)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            try
            {
                return string.Format(input, formatting);
            }
            catch (Exception ex)
            {
                "chocolatey".Log().Error(ChocolateyLoggers.Important, "{0} had an error formatting string:{1} {2}", ApplicationParameters.Name, Environment.NewLine, ex.Message);
                return input;
            }
        }

        /// <summary>
        ///   Performs a trim only if the item is not null
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string TrimSafe(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            return input.Trim();
        }

        /// <summary>
        ///   Performs ToLower() only if input is not null
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string ToLowerChecked(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            return input.ToLower();
        }

        /// <summary>
        ///   Gets a string representation unless input is null.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string ToStringSafe(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            return input;
        }

        /// <summary>
        /// Takes a string and returns a secure string
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static SecureString ToSecureString(this string input)
        {
            var secureString = new SecureString();

            if (string.IsNullOrWhiteSpace(input)) return secureString;

            foreach (char character in input)
            {
                secureString.AppendChar(character);
            }

            return secureString;
        }

        public static string FromSecureString(this SecureString input)
        {
            if (input == null) return string.Empty;

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(input);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        private static readonly Regex _spacePattern = new Regex(@"\s", RegexOptions.Compiled);

        /// <summary>
        ///   If the item contains spaces, it wraps it in quotes
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string QuoteIfContainsSpaces(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            if (_spacePattern.IsMatch(input))
            {
                return "\"{0}\"".FormatWith(input);
            }

            return input;
        }

        /// <summary>
        ///   Are the strings equal(ignoring case and culture)?
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="other">The value to compare to</param>
        /// <returns>True if these are the same</returns>
        public static bool IsEqualTo(this string input, string other)
        {
            return string.Compare(input, other, ignoreCase: true, culture: CultureInfo.InvariantCulture) == 0;
        }

        /// <summary>
        /// Determines whether a string value contains a search value.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="search">The value to search for.</param>
        /// <param name="comparison">The comparison.</param>
        /// <returns>True if the value to search for is in the input string</returns>
        public static bool ContainsSafe(this string input, string search, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return input.ToStringSafe().IndexOf(search, 0, comparison) >= 0;
        }

        /// <summary>
        ///   Removes quotes or apostrophes surrounding a string
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string UnquoteSafe(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            if (input.StartsWith(" "))
            {
                input = input.TrimSafe();
            }

            if ((input.StartsWith("\"") && input.EndsWith("\""))
                || (input.StartsWith("'") && input.EndsWith("'")))
            {
                input = input.Remove(0, 1).Remove(input.Length - 2, 1);
            }

            return input;
        }

        private static readonly Regex _openBraceRegex = new Regex("(?<!{){(?!{)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex _closeBraceRegex = new Regex("(?<!})}(?!})", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        public static string EscapeCurlyBraces(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            return _openBraceRegex.Replace(_closeBraceRegex.Replace(input,"}}"),"{{");
        }

        /// <summary>
        /// Surrounds with quotes if a pipe is found in the input string.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The input, but with double quotes if there is a pipe character found in the string.</returns>
        public static string QuoteIfContainsPipe(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input.ToStringSafe();

            if (input.ContainsSafe("|")) return "\"{0}\"".FormatWith(input);

            return input;
        }

    }
}
