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
    using System.Reflection;
    using chocolatey.infrastructure.app.rules;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.rules;
    using chocolatey.infrastructure.services;
    using FluentAssertions;

    public class RulesServiceSpecs : TinySpec
    {
        private RuleService _service;
        private IReadOnlyList<ImmutableRule> _detectedRules;
        // We can't reference RuleIdentifiers directly as it's Internal. We should either get these from there, or do something different...
        private const string EmptyRequiredElement = "CHCR0001";
        private const string InvalidTypeElement = "CHCU0001";
        private const string MissingElementOnRequiringLicenseAcceptance = "CHCR0002";
        private const string UnsupportedElementUsed = "CHCU0002";

        public override void Context()
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

            _service = new RuleService(rules.ToArray());
        }

        public override void Because()
        {
            _detectedRules = _service.GetAllAvailableRules();
        }

        [Fact]
        public void GetsRulesFromService()
        {
            _detectedRules.Count().Should().Be(4);
            IEnumerable<string> ruleIds = _detectedRules.Select(t => t.Id);
            ruleIds.Should().Contain(UnsupportedElementUsed);
            ruleIds.Should().Contain(EmptyRequiredElement);
            ruleIds.Should().Contain(InvalidTypeElement);
            ruleIds.Should().Contain(MissingElementOnRequiringLicenseAcceptance);
        }
    }
}
