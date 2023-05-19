// Copyright © 2023-Present Chocolatey Software, Inc
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
    using System.Collections.Generic;
    using System.Linq;
    using chocolatey.infrastructure.rules;

    public static class RuleResultExtensions
    {
        /// <summary>
        /// Extension method used to filter out any rules that hasn't been marked as either unsupported or deprecated.
        /// </summary>
        /// <param name="ruleResults">The rule results to apply the filter to.</param>
        /// <param name="inverse"><c>True</c> if the applied filters should exclude unsupported and deprecated rules; otherwise <c>False</c></param>
        /// <returns>The passed in rule results with the applied filters.</returns>
        public static IEnumerable<RuleResult> WhereUnsupportedOrDeprecated(this IEnumerable<RuleResult> ruleResults, bool inverse = false)
        {
            if (!inverse)
            {
                return ruleResults
                    .Where(r => r != null && !string.IsNullOrEmpty(r.Id))
                    .Where(r => r.Id.StartsWith("CHCU") || r.Id.StartsWith("CHCD"));
            }

            return ruleResults
                .Where(r => r != null)
                .Where(r => string.IsNullOrEmpty(r.Id) || (!r.Id.StartsWith("CHCU") && !r.Id.StartsWith("CHCD")));
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static IEnumerable<RuleResult> where_unsupported_or_deprecated(this IEnumerable<RuleResult> ruleResults, bool inverse = false)
            => WhereUnsupportedOrDeprecated(ruleResults, inverse);
#pragma warning restore IDE1006
    }
}
