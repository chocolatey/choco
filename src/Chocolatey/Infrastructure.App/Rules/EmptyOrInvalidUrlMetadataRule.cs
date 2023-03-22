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

namespace Chocolatey.Infrastructure.App.Rules
{
    using System;
    using System.Collections.Generic;
    using Chocolatey.Infrastructure.Rules;
    using global::NuGet.Packaging;

    internal sealed class EmptyOrInvalidUrlMetadataRule : MetadataRuleBase
    {
        public override IEnumerable<RuleResult> Validate(NuspecReader reader)
        {
            var items = new[]
            {
                "projectUrl",
                "projectSourceUrl",
                "docsUrl",
                "bugTrackerUrl",
                "mailingListUrl",
                "iconUrl",
                "licenseUrl"
            };

            foreach (var item in items)
            {
                if (HasElement(reader, item))
                {
                    var value = GetElementValue(reader, item);

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        yield return new RuleResult(RuleType.Error, RuleIdentifiers.EmptyRequiredElement, "The {0} element in the package nuspec file cannot be empty.".FormatWith(item));
                    }
                    else if (!Uri.TryCreate(value, UriKind.Absolute, out _))
                    {
                        yield return new RuleResult(RuleType.Error, RuleIdentifiers.InvalidTypeElement, "'{0}' is not a valid URL for the {1} element in the package nuspec file.".FormatWith(value, item));
                    }
                }
            }
        }
    }
}
