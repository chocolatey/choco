// Copyright © 2017 - 2021 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
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

namespace chocolatey.tests.infrastructure.app.services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using chocolatey.infrastructure.app.rules;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.rules;
    using chocolatey.infrastructure.services;
    using FluentAssertions;
    using NuGet.Packaging;

    public class RulesServiceSpecs
    {
        [Categories.RuleEngine]
        public abstract class RulesServiceSpecsBase : TinySpec
        {
            protected RuleService Service;
            protected IReadOnlyList<ImmutableRule> DetectedRules;

            public override void Context()
            {
                var rules = GetRules();

                Service = new RuleService(rules.ToArray());
            }

            public override void Because()
            {
                DetectedRules = Service.GetAllAvailableRules();
            }

            protected abstract IEnumerable<IMetadataRule> GetRules();
        }

        public class WhenGettingAllAvailableRulesShouldGetTheExpectedRules : RulesServiceSpecsBase
        {
            // We can't reference RuleIdentifiers directly as it's Internal. We should either get these from there, or do something different...
            private const string EmptyRequiredElement = "CHCR0001";

            private const string InvalidTypeElement = "CHCU0001";
            private const string MissingElementOnRequiringLicenseAcceptance = "CHCR0002";
            private const string UnsupportedElementUsed = "CHCU0002";

            protected override IEnumerable<IMetadataRule> GetRules()
            {
                Type[] availableRules = typeof(IRuleService).Assembly
                .GetTypes()
                .Where(t => !t.IsInterface && !t.IsAbstract && typeof(IMetadataRule).IsAssignableFrom(t))
                .ToArray();
                var rules = new List<IMetadataRule>();

                foreach (Type availableRule in availableRules)
                {
                    // We do first here as we want it to fail if the constructor can't be found.
                    var rule = availableRule.GetConstructors().First().Invoke(new object[] { });
                    rules.Add((MetadataRuleBase)rule);
                }

                return rules;
            }

            [Fact]
            public void GetsRulesFromService()
            {
                DetectedRules.Should().HaveCount(4);
                var ruleIds = DetectedRules.Select(t => t.Id);

                ruleIds.Should().ContainInOrder(new[]
                {
                    EmptyRequiredElement,
                    MissingElementOnRequiringLicenseAcceptance,
                    InvalidTypeElement,
                    UnsupportedElementUsed,
                });
            }
        }

        public class WhenTwoAvailableRulesContainsNoIdentifierAndWithTheSameSummary : RulesServiceSpecsBase
        {
            private static readonly ImmutableRule _expectedRule = new ImmutableRule(RuleType.Warning, string.Empty, "Some summary");

            protected override IEnumerable<IMetadataRule> GetRules()
            {
                yield return new EmptyRulesValidator();
            }

            [Fact]
            public void ShouldHaveOnlyASingleResult()
            {
                DetectedRules.Should().ContainSingle();
            }

            [Fact]
            public void ShouldOnlyContainsTheFirstFoundItem()
            {
                DetectedRules.Should().ContainEquivalentOf(_expectedRule);
            }

            private class EmptyRulesValidator : IMetadataRule
            {
                public IReadOnlyList<ImmutableRule> GetAvailableRules()
                {
                    return new[]
                    {
                        _expectedRule,
                        new ImmutableRule(RuleType.Error, string.Empty, "Some summary")
                    };
                }

                public IEnumerable<RuleResult> Validate(NuspecReader reader)
                {
                    throw new NotImplementedException();
                }

                public IEnumerable<RuleResult> validate(NuspecReader reader)
                {
                    throw new NotImplementedException();
                }
            }
        }

        public class WhenTwoAvailableRulesContainNoIdentifierWithDifferentSummaries : RulesServiceSpecsBase
        {
            protected override IEnumerable<IMetadataRule> GetRules()
            {
                yield return new EmptyRulesValidator();
            }

            [Fact]
            public void ShouldHaveGottenTwoAvailableRules()
            {
                DetectedRules.Should().HaveCount(2);
            }

            [Fact]
            public void ShouldHaveGottenExpectedRules()
            {
                var ruleSummaries = DetectedRules.Select(t => t.Summary);

                ruleSummaries.Should().ContainInOrder("Some summary of rule 2", "Some summary of rule 1");
            }

            private class EmptyRulesValidator : IMetadataRule
            {
                public IReadOnlyList<ImmutableRule> GetAvailableRules()
                {
                    return new[]
                    {
                        new ImmutableRule(RuleType.Warning, string.Empty, "Some summary of rule 1"),
                        new ImmutableRule(RuleType.Error, string.Empty, "Some summary of rule 2")
                    };
                }

                public IEnumerable<RuleResult> Validate(NuspecReader reader)
                {
                    throw new NotImplementedException();
                }

                public IEnumerable<RuleResult> validate(NuspecReader reader)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}