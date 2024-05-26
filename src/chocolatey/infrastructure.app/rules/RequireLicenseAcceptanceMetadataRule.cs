﻿// Copyright © 2023-Present Chocolatey Software, Inc
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

using System.Collections.Generic;
using chocolatey.infrastructure.rules;
using NuGet.Packaging;

namespace chocolatey.infrastructure.app.rules
{
    internal sealed class RequireLicenseAcceptanceMetadataRule : MetadataRuleBase
    {
        public override IEnumerable<RuleResult> Validate(NuspecReader reader)
        {
            if (string.IsNullOrWhiteSpace(reader.GetLicenseUrl()) && reader.GetRequireLicenseAcceptance())
            {
                yield return GetRule(RuleIdentifiers.MissingElementOnRequiringLicenseAcceptance);
            }
        }

        protected override IEnumerable<ImmutableRule> GetRules()
        {
            yield return new ImmutableRule(RuleType.Error, RuleIdentifiers.MissingElementOnRequiringLicenseAcceptance, "Enabling license acceptance requires a license url.");
        }
    }
}