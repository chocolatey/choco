// Copyright © 2023 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.app.commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.logging;

    /// <summary>
    /// A base class for any Chocolatey commands which need to utilise shared logic.
    /// </summary>
    public abstract class ChocolateyCommandBase
    {
        public virtual void HelpMessage(ChocolateyConfiguration configuration)
        {
            var allCommandForAttributes = GetType().GetCustomAttributes<CommandForAttribute>().ToArray();
            var commandForAttribute = allCommandForAttributes.OrEmpty().FirstOrDefault();

            if (commandForAttribute == null)
            {
                throw new ApplicationException("No command for attribute was found on the command class: {0}".FormatWith(GetType().Name));
            }

            this.Log().Info(ChocolateyLoggers.Important, "{0} Command", GetCommandName(commandForAttribute));

            if (!string.IsNullOrEmpty(commandForAttribute.Version))
            {
                this.Log().Info(@"
WARNING: {0}", GetCommandVersionWarning(commandForAttribute, configuration));
            }

            this.Log().Info(@"
{0}
", GetCommandDescription(commandForAttribute, configuration));

            var commandUsages = GetCommandUsage(allCommandForAttributes, configuration).ToArray();

            if (commandUsages.Length > 0)
            {
                this.Log().Info(ChocolateyLoggers.Important, @"Usage");
                this.Log().Info(string.Empty);

                foreach (var commandUsage in commandUsages)
                {
                    this.Log().Info("    {0}", commandUsage);
                }

                this.Log().Info(string.Empty);
            }

            foreach (var note in GetCommandUsageNotes(configuration))
            {
                this.Log().Info(@"NOTE: {0}
", note);
            }

            var commandExamples = GetCommandExamples(allCommandForAttributes, configuration).ToArray();

            if (commandExamples.Length > 0)
            {
                this.Log().Info(ChocolateyLoggers.Important, "Examples");
                this.Log().Info(string.Empty);

                foreach (var commandExample in commandExamples)
                {
                    this.Log().Info("    {0}", commandExample);
                }

                var exampleDescription = GetCommandExampleDescription(configuration);

                if (!string.IsNullOrEmpty(exampleDescription))
                {
                    this.Log().Info(string.Empty);
                    this.Log().Info(exampleDescription);
                }

                this.Log().Info(@"
NOTE: See scripting in the command reference (`choco -?`) for how to
 write proper scripts and integrations.
");
            }

            var normalExitCodes = GetNormalExitCodes(configuration).ToArray();
            var enhancedExitCodes = GetEnhancedExitCodes(configuration).ToArray();
            var additionalExitCodeDescription = GetAdditionalExitCodeDescription(configuration);

            if (normalExitCodes.Length > 0 || enhancedExitCodes.Length > 0 || !string.IsNullOrEmpty(additionalExitCodeDescription))
            {
                this.Log().Info(ChocolateyLoggers.Important, "Exit Codes");
                this.Log().Info(@"
Exit codes that normally result from running this command.
");
            }

            if (normalExitCodes.Length > 0)
            {
                this.Log().Info("Normal:");
                OutputExitCodes(normalExitCodes);
            }

            if (enhancedExitCodes.Length > 0)
            {
                this.Log().Info("Enhanced:");
                OutputExitCodes(enhancedExitCodes);
            }

            if (normalExitCodes.Length > 0 || enhancedExitCodes.Length > 0)
            {
                this.Log().Info(@"
If you find other exit codes that we have not yet documented, please
 file a ticket so we can document it at
 {0}.

", GetRepositoryIssueLink());
            }

            if (!string.IsNullOrEmpty(additionalExitCodeDescription))
            {
                this.Log().Info(additionalExitCodeDescription);
                this.Log().Info(string.Empty);
            }

            var additionalHelpContent = GetAdditionalHelpContent(configuration);

            if (!string.IsNullOrEmpty(additionalHelpContent))
            {
                this.Log().Info(additionalHelpContent);
                this.Log().Info(string.Empty);
            }

            this.Log().Info(ChocolateyLoggers.Important, "Options and Switches");

            var optionsAndSwitchesContent = GetOptionsAndSwitchesDescription(configuration);

            if (!string.IsNullOrEmpty(optionsAndSwitchesContent))
            {
                this.Log().Info(string.Empty);
                this.Log().Info(optionsAndSwitchesContent);
            }
        }

        protected virtual string GetCommandDescription(CommandForAttribute attribute, ChocolateyConfiguration configuration)
        {
            return attribute.Description;
        }

        protected virtual string GetCommandExampleDescription(ChocolateyConfiguration configuration)
        {
            return string.Empty;
        }

        protected virtual IEnumerable<string> GetCommandExamples(CommandForAttribute[] attributes, ChocolateyConfiguration configuration)
        {
            foreach (var attribute in attributes)
            {
                yield return "choco {0}".FormatWith(attribute.CommandName);
            }
        }

        protected virtual string GetCommandName(CommandForAttribute attribute)
        {
            return char.ToUpperInvariant(attribute.CommandName[0]) + attribute.CommandName.Substring(1);
        }

        protected virtual IEnumerable<string> GetCommandUsage(CommandForAttribute[] attributes, ChocolateyConfiguration configuration)
        {
            foreach (var attribute in attributes)
            {
                yield return "choco {0} [options/switches]".FormatWith(attribute.CommandName);
            }
        }

        protected virtual IEnumerable<string> GetCommandUsageNotes(ChocolateyConfiguration configuration)
        {
            return Enumerable.Empty<string>();
        }

        protected virtual string GetCommandVersionWarning(CommandForAttribute attribute, ChocolateyConfiguration configuration)
        {
            return "This command was introduced in Chocolatey CLI v{0}".FormatWith(attribute.Version);
        }

        protected virtual IEnumerable<ExitCodeDescription> GetEnhancedExitCodes(ChocolateyConfiguration configuration)
        {
            return Enumerable.Empty<ExitCodeDescription>();
        }

        protected virtual IEnumerable<ExitCodeDescription> GetNormalExitCodes(ChocolateyConfiguration configuration)
        {
            return new[]
            {
                new ExitCodeDescription("operation was successful, no issues detected", 0),
                new ExitCodeDescription("an error has occurred", -1, 1)
            };
        }

        protected virtual Uri GetRepositoryIssueLink()
        {
            return new Uri("https://github.com/chocolatey/choco/issues/new/choose");
        }

        /// <summary>
        /// Emit a warning to the use if any of the options which are known to be deprecated are found in the <paramref name="unparsedOptions"/>.
        /// </summary>
        /// <param name="unparsedOptions">The list of unrecognised and unparsed options.</param>
        /// <param name="removedOptions">The list of options which are known to be removed and should be warned for.</param>
        protected virtual void WarnForRemovedOptions(IEnumerable<string> unparsedOptions, IEnumerable<string> removedOptions)
        {
            if (!unparsedOptions.OrEmpty().Any() || !removedOptions.OrEmpty().Any())
            {
                return;
            }

            foreach (var removed in removedOptions)
            {
                if (unparsedOptions.Contains(removed))
                {
                    this.Log().Warn("The {0} option is no longer supported.", removed);
                }
            }
        }

        private string GetAdditionalExitCodeDescription(ChocolateyConfiguration configuration)
        {
            return string.Empty;
        }

        private string GetAdditionalHelpContent(ChocolateyConfiguration configuration)
        {
            return string.Empty;
        }

        private string GetOptionsAndSwitchesDescription(ChocolateyConfiguration configuration)
        {
            return string.Empty;
        }

        private void OutputExitCodes(ExitCodeDescription[] normalExitCodes)
        {
            foreach (var exitCodeDescription in normalExitCodes)
            {
                var sb = new StringBuilder(" - ");

                for (var j = 0; j < exitCodeDescription.ExitCodes.Length; j++)
                {
                    if (j > 0 && j + 1 == exitCodeDescription.ExitCodes.Length)
                    {
                        sb.Append(" or ");
                    }
                    else if (j > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(exitCodeDescription.ExitCodes[j]);
                }

                if (exitCodeDescription.ExitCodes.Length > 0)
                {
                    sb.AppendFormat(": {0}", exitCodeDescription.Description);
                }
                else
                {
                    sb.Append(exitCodeDescription.Description);
                }

                this.Log().Info(sb.ToString());
            }
        }
    }
}