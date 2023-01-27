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

namespace chocolatey.infrastructure.app.rules
{
    using System.Collections.Generic;
    using chocolatey.infrastructure.rules;
    using NuGet.Packaging;
    using NuGet.Versioning;

    internal sealed class VersionMetadataRule : MetadataRuleBase
    {
        public override IEnumerable<RuleResult> validate(NuspecReader reader)
        {
            var version = get_element_value(reader, "version");

            // We need to check for the $version$ substitution value,
            // as it will not be replaced before the package gets created
            if (!string.IsNullOrEmpty(version) && !version.is_equal_to("$version$") && !NuGetVersion.TryParse(version, out _))
            {
                yield return new RuleResult(RuleType.Error, "'{0}' is not a valid version string in the package nuspec file.".format_with(version));
            }
        }
    }
}
