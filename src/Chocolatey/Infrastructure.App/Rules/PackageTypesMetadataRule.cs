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

namespace Chocolatey.Infrastructure.App.Rules
{
    using System.Collections.Generic;
    using Chocolatey.Infrastructure.Rules;
    using global::NuGet.Packaging;

    internal sealed class PackageTypesMetadataRule : MetadataRuleBase
    {
        public override IEnumerable<RuleResult> Validate(NuspecReader reader)
        {
            if (HasElement(reader, "packageTypes"))
            {
                yield return new RuleResult(RuleType.Error, RuleIdentifiers.UnsupportedElementUsed, "<packageTypes> elements are not supported in Chocolatey CLI.");
            }
        }
    }
}
