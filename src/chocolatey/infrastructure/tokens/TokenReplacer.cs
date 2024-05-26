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
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace chocolatey.infrastructure.tokens
{
    public sealed class TokenReplacer
    {
        public static string ReplaceTokens<TConfig>(TConfig configuration, string textToReplace, string tokenPrefix = "[[", string tokenSuffix = "]]")
        {
            if (string.IsNullOrEmpty(textToReplace))
            {
                return string.Empty;
            }

            IDictionary<string, string> dictionary = CreateDictionaryFromConfiguration(configuration);
            if (dictionary.Count == 0)
            {
                return textToReplace;
            }

            var regex = new Regex("{0}(?<key>\\w+){1}".FormatWith(Regex.Escape(tokenPrefix), Regex.Escape(tokenSuffix)));

            var output = regex.Replace(textToReplace, m =>
                {
                    var key = "";

                    var originalKey = m.Groups["key"].Value;
                    key = originalKey.ToLowerSafe();
                    if (!dictionary.ContainsKey(key))
                    {
                        return tokenPrefix + originalKey + tokenSuffix;
                    }

                    var value = dictionary[key];
                    return value;
                });

            return output;
        }

        private static IDictionary<string, string> CreateDictionaryFromConfiguration<TConfig>(TConfig configuration)
        {
            if (configuration is IDictionary<string, string>)
            {
                return configuration as IDictionary<string, string>;
            }

            var propertyDictionary = new Dictionary<string, string>();
            foreach (PropertyInfo property in configuration.GetType().GetProperties())
            {
                propertyDictionary.Add(property.Name.ToLowerSafe(), property.GetValue(configuration, null).ToStringSafe());
            }

            return propertyDictionary;
        }

        public static IEnumerable<string> GetTokens(string textWithTokens, string tokenPrefix = "[[", string tokenSuffix = "]]")
        {
            var regexMatches = Regex.Matches(textWithTokens, "{0}(?<key>\\w+){1}"
                .FormatWith(Regex.Escape(tokenPrefix), Regex.Escape(tokenSuffix))
            );
            foreach (Match regexMatch in regexMatches)
            {
                yield return regexMatch.Groups["key"].ToStringSafe();
            }
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static string replace_tokens<TConfig>(TConfig configuration, string textToReplace, string tokenPrefix = "[[", string tokenSuffix = "]]")
            => ReplaceTokens(configuration, textToReplace, tokenPrefix, tokenSuffix);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static IEnumerable<string> get_tokens(string textWithTokens, string tokenPrefix = "[[", string tokenSuffix = "]]")
            => GetTokens(textWithTokens, tokenPrefix, tokenSuffix);
#pragma warning restore IDE0022, IDE1006
    }
}
