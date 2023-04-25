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
    public readonly struct ImmutableRule
    {
        public ImmutableRule(RuleType severity, string id, string summary, string helpUrl = null)
        {
            Severity = severity;
            Id = id;
            Summary = summary;
            HelpUrl = helpUrl;
        }

        public readonly RuleType Severity;
        public readonly string Id;
        public readonly string Summary;
        public readonly string HelpUrl;
    }
}
