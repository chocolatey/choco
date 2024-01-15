// Copyright © 2017 - 2024 Chocolatey Software, Inc
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

using System;
using System.Collections.Generic;
using System.Linq;
using chocolatey.infrastructure.app.attributes;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.commandline;
using chocolatey.infrastructure.commands;
using chocolatey.infrastructure.logging;
using chocolatey.infrastructure.rules;
using chocolatey.infrastructure.services;

namespace chocolatey.infrastructure.app.commands
{
    [CommandFor("rule", "view or list implemented package rules", Version = "2.3.0")]
    public class ChocolateyRuleCommand : ChocolateyCommandBase, ICommand
    {
        private readonly IRuleService _ruleService;

        public ChocolateyRuleCommand(IRuleService ruleService)
        {
            _ruleService = ruleService ?? throw new ArgumentNullException(nameof(ruleService));
        }

        public virtual void ConfigureArgumentParser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("n=|name=",
                "Name - the name of the rule to show more details about. Required with actions other than list. Defaults to empty.",
                option => configuration.RuleCommand.Name = option);
        }

        public virtual void DryRun(ChocolateyConfiguration configuration)
        {
            Run(configuration);
        }

        public virtual bool MayRequireAdminAccess()
        {
            return false;
        }

        public virtual void ParseAdditionalArguments(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);

            var command = unparsedArguments.DefaultIfEmpty().Select(a => a.ToLowerSafe()).FirstOrDefault();

            if (string.IsNullOrEmpty(command))
            {
                command = "list";
            }

            configuration.RuleCommand.Command = command;

            if (string.IsNullOrEmpty(configuration.RuleCommand.Name) && unparsedArguments.Count >= 2)
            {
                configuration.RuleCommand.Name = unparsedArguments[1];
            }
        }

        public virtual void Run(ChocolateyConfiguration config)
        {
            IEnumerable<ImmutableRule> implementedRules = _ruleService.GetAllAvailableRules().OrderBy(r => r.Id);

            if (!string.IsNullOrEmpty(config.RuleCommand.Name))
            {
                var foundRule = implementedRules.FirstOrDefault(i => i.Id.IsEqualTo(config.RuleCommand.Name));

                // Since the return value is a structure, it will never be null.
                // As such we check that the identifier is not empty.
                if (config.RegularOutput)
                {
                    if (string.IsNullOrEmpty(foundRule.Id))
                    {
                        throw new ApplicationException("No rule with the name {0} could be found.".FormatWith(config.RuleCommand.Name));
                    }

                    // Not using multiline logging here, as it causes issues
                    // with unit tests.
                    this.Log().Info("Name: {0} | Severity: {1}", foundRule.Id, foundRule.Severity);
                    this.Log().Info("Summary: {0}", foundRule.Summary);
                    this.Log().Info("Help URL: {0}", foundRule.HelpUrl);

                    return;
                }
                else if (!string.IsNullOrEmpty(foundRule.Id))
                {
                    implementedRules = new[] { foundRule };
                }
                else
                {
                    return;
                }
            }

            if (config.RegularOutput)
            {
                this.Log().Info(ChocolateyLoggers.Important, "Implemented Package Rules");
            }

            OutputRules("Error/Required", config, implementedRules.Where(r => r.Severity == RuleType.Error).ToList());
            OutputRules("Warning/Guideline", config, implementedRules.Where(r => r.Severity == RuleType.Warning).ToList());
            OutputRules("Information/Suggestion", config, implementedRules.Where(r => r.Severity == RuleType.Information).ToList());
            OutputRules("Note", config, implementedRules.Where(r => r.Severity == RuleType.Note).ToList());
            OutputRules("Disabled", config, implementedRules.Where(r => r.Severity == RuleType.None).ToList());
        }

        public virtual void Validate(ChocolateyConfiguration configuration)
        {
            switch (configuration.RuleCommand.Command)
            {
                case "list":
                    if (!string.IsNullOrEmpty(configuration.RuleCommand.Name))
                    {
                        throw new ApplicationException("A Rule Name (-n|--name) should not be specified when listing all validation rules.");
                    }
                    break;
                case "get":
                    if (string.IsNullOrEmpty(configuration.RuleCommand.Name))
                    {
                        throw new ApplicationException("A Rule Name (-n|--name) is required when getting information for a specific rule.");
                    }
                    break;

                default:
                    this.Log().Warn("Unknown command '{0}'. Setting to list.", configuration.RuleCommand.Command);
                    configuration.RuleCommand.Command = "list";
                    Validate(configuration);
                    break;
            }
        }

        protected override string GetCommandDescription(CommandForAttribute attribute, ChocolateyConfiguration configuration)
        {
            return @"Retrieve information about what rule validations are implemented by Chocolatey CLI.";
        }

        protected override IEnumerable<string> GetCommandExamples(CommandForAttribute[] attributes, ChocolateyConfiguration configuration)
        {
            return new[]
            {
                "choco rule",
                "choco rule list",
                "choco rule get --name CHCR0002"
            };
        }

        protected override IEnumerable<string> GetCommandUsage(CommandForAttribute[] attributes, ChocolateyConfiguration configuration)
        {
            return new[]
            {
                "choco rule [list]|get [options/switches]",
            };
        }

        protected virtual void OutputRules(string type, ChocolateyConfiguration config, IReadOnlyList<ImmutableRule> rules)
        {
            if (config.RegularOutput)
            {
                this.Log().Info("");
                this.Log().Info(ChocolateyLoggers.Important, type + " Rules");
                this.Log().Info("");

                if (rules.Count == 0)
                {
                    this.Log().Info("No implemented " + type + " rules available.");

                    return;
                }
            }

            foreach (var rule in rules)
            {
                if (config.RegularOutput)
                {
                    this.Log().Info("{0}: {1}", rule.Id, rule.Summary);
                }
                else
                {
                    this.Log().Info("{0}|{1}|{2}|{3}", rule.Severity, rule.Id, rule.Summary, rule.HelpUrl);
                }
            }
        }

#pragma warning disable IDE0022, IDE1006

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
            => ConfigureArgumentParser(optionSet, configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
            => ParseAdditionalArguments(unparsedArguments, configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_validation(ChocolateyConfiguration configuration)
            => Validate(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void help_message(ChocolateyConfiguration configuration)
            => HelpMessage(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual bool may_require_admin_access()
            => MayRequireAdminAccess();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void noop(ChocolateyConfiguration configuration)
            => DryRun(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void run(ChocolateyConfiguration configuration)
            => Run(configuration);

#pragma warning restore IDE0022, IDE1006
    }
}
