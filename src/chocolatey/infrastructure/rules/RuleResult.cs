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

namespace chocolatey.infrastructure.rules
{
    public sealed class RuleResult
    {
        public RuleResult(RuleType severity, string id, string message)
        {
            Severity = severity;
            Id = id;
            Message = message;
        }

        public string HelpUrl { get; set; }
        public string Id { get; private set; }
        public string Message { get; private set; }
        public RuleType Severity { get; set; }

        internal static RuleResult FromImmutableRule(ImmutableRule result, string summary = null)
        {
            if (string.IsNullOrEmpty(summary))
            {
                return new RuleResult(result.Severity, result.Id, result.Summary) { HelpUrl = result.HelpUrl };
            }
            else
            {
                return new RuleResult(result.Severity, result.Id, summary) { HelpUrl = result.HelpUrl };
            }
        }
    }
}
