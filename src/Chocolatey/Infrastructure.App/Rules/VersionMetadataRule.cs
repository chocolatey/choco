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
    using System.Collections.Generic;
    using Chocolatey.Infrastructure.Rules;
    using global::NuGet.Packaging;
    using global::NuGet.Versioning;

    internal sealed class VersionMetadataRule : MetadataRuleBase
    {
        public override IEnumerable<RuleResult> Validate(NuspecReader reader)
        {
            var version = GetElementValue(reader, "version");

            // We need to check for the $version$ substitution value, as it will not be replaced
            // before the package gets created
            if (!string.IsNullOrEmpty(version) && !version.IsEqualTo("$version$") && !NuGetVersion.TryParse(version, out _))
            {
                yield return new RuleResult(RuleType.Error, RuleIdentifiers.InvalidTypeElement, "'{0}' is not a valid version string in the package nuspec file.".FormatWith(version));
            }
        }
    }
}
