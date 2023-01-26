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

namespace chocolatey.infrastructure.app.services
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using chocolatey.infrastructure.guards;
    using chocolatey.infrastructure.rules;
    using chocolatey.infrastructure.services;
    using NuGet.Configuration;
    using NuGet.Packaging;

    public sealed class RuleService : IRuleService
    {
        private readonly IMetadataRule[] _rules;

        public RuleService(IMetadataRule[] rules)
        {
            _rules = rules;
        }

        public IEnumerable<RuleResult> validate_rules(string filePath)
        {
            Ensure.that(() => filePath)
                .is_not_null_or_whitespace()
                .has_any_extension(NuGetConstants.PackageExtension, NuGetConstants.ManifestExtension);

            var rules = filePath.EndsWith(NuGetConstants.PackageExtension)
                ? get_rules_from_package_async(filePath).GetAwaiter().GetResult()
                : get_rules_from_metadata(filePath);

            return rules
                .OrderBy(r => r.Severity)
                .ThenBy(r => r.Message);
        }

        private async Task<IEnumerable<RuleResult>> get_rules_from_package_async(string filePath, CancellationToken token = default)
        {
            using (var packageReader = new PackageArchiveReader(filePath))
            {
                var nuspecReader = await packageReader.GetNuspecReaderAsync(token);

                // We add ToList here to ensure that the package
                // reader hasn't been disposed of before we return
                // any results.
                return validate_nuspec(nuspecReader, _rules).ToList();
            }
        }

        private IEnumerable<RuleResult> get_rules_from_metadata(string filePath)
        {
            var nuspecReader = new NuspecReader(filePath);

            return validate_nuspec(nuspecReader, _rules);
        }

        private static IEnumerable<RuleResult> validate_nuspec(NuspecReader reader, IMetadataRule[] rules)
        {
            foreach (var rule in rules)
            {
                var validationResults = rule.validate(reader);

                foreach (var result in validationResults.Where(v => v.Severity != RuleType.None))
                {
                    yield return result;
                }
            }
        }
    }
}
