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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using chocolatey.infrastructure.guards;
    using chocolatey.infrastructure.rules;
    using chocolatey.infrastructure.services;
    using NuGet.Configuration;
    using NuGet.Packaging;

    public class RuleService : IRuleService
    {
        private readonly IMetadataRule[] _rules;

        public RuleService(IMetadataRule[] rules)
        {
            _rules = rules;
        }

        public virtual IReadOnlyList<ImmutableRule> GetAllAvailableRules()
        {
            return _rules.SelectMany(r => r.GetAvailableRules())
                .Distinct(new RuleIdEqualityComparer())
                .OrderBy(r => r.Severity)
                .ThenBy(r => r.Id)
                .ToList()
                .AsReadOnly();
        }

        public virtual IEnumerable<RuleResult> ValidateRules(string filePath)
        {
            Ensure.That(() => filePath)
                .NotNullOrWhitespace()
                .HasExtension(NuGetConstants.PackageExtension, NuGetConstants.ManifestExtension);

            var rules = filePath.EndsWith(NuGetConstants.PackageExtension)
                ? GetRulesFromPackageAsync(filePath).GetAwaiter().GetResult()
                : GetRulesFromMetadata(filePath);

            return rules
                .OrderBy(r => r.Severity)
                .ThenBy(r => r.Id)
                .ThenBy(r => r.Message);
        }

        private async Task<IEnumerable<RuleResult>> GetRulesFromPackageAsync(string filePath, CancellationToken token = default)
        {
            using (var packageReader = new PackageArchiveReader(filePath))
            {
                var nuspecReader = await packageReader.GetNuspecReaderAsync(token);

                // We add ToList here to ensure that the package
                // reader hasn't been disposed of before we return
                // any results.
                return ValidateNuspec(nuspecReader, _rules).ToList();
            }
        }

        private IEnumerable<RuleResult> GetRulesFromMetadata(string filePath)
        {
            var nuspecReader = new NuspecReader(filePath);

            return ValidateNuspec(nuspecReader, _rules);
        }

        private static IEnumerable<RuleResult> ValidateNuspec(NuspecReader reader, IMetadataRule[] rules)
        {
            foreach (var rule in rules)
            {
                var validationResults = rule.Validate(reader);

                foreach (var result in validationResults.Where(v => v.Severity != RuleType.None))
                {
                    yield return result;
                }
            }
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public IEnumerable<RuleResult> validate_rules(string filePath)
            => ValidateRules(filePath);
#pragma warning restore IDE1006

        private class RuleIdEqualityComparer : IEqualityComparer<ImmutableRule>
        {
            public bool Equals(ImmutableRule x, ImmutableRule y)
            {
                // When the id is empty on both classes, we need to compare
                // using the summary to detect if the rules are unique or not.
                if (string.IsNullOrEmpty(x.Id) && string.IsNullOrEmpty(y.Id))
                {
                    return x.Summary.IsEqualTo(y.Summary);
                }

                return x.Id.IsEqualTo(y.Id);
            }

            public int GetHashCode(ImmutableRule obj)
            {
                // When the id is empty, we need to compare
                // using the summary to detect if the rules are unique or not.

                if (string.IsNullOrEmpty(obj.Id))
                {
                    return obj.Summary?.GetHashCode() ?? 0;
                }

                return obj.Id.GetHashCode();
            }
        }
    }
}
