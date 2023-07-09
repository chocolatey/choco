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

namespace chocolatey.infrastructure.powershell
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Management.Automation;
    using System.Management.Automation.Host;
    using System.Security;
    using System.Text;
    using app.configuration;
    using commandline;
    using logging;
    using Console = adapters.Console;

    public class PoshHostUserInterface : PSHostUserInterface
    {
        private readonly ChocolateyConfiguration _configuration;
        protected readonly Console Console = new Console();
        private readonly PoshHostRawUserInterface _rawUi = new PoshHostRawUserInterface();
        private const int TimeoutInSeconds = 30;

        public bool StandardErrorWritten { get; set; }

        public PoshHostUserInterface(ChocolateyConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        ///   Depending on whether we allow prompting or not, we will set Console.ReadLine().
        ///   If the user has set confirm all prompts (-y), we still want to give them a
        ///   chance to make a selection, but it should ultimately time out and move on
        ///   so it doesn't break unattended operations.
        /// </summary>
        /// <returns></returns>
        public override string ReadLine()
        {
            if (!_configuration.PromptForConfirmation)
            {
                this.Log().Warn(ChocolateyLoggers.Important, @"  Confirmation (`-y`) is set.
  Respond within {0} seconds or the default selection will be chosen.".FormatWith(TimeoutInSeconds));

                return Console.ReadLine(TimeoutInSeconds * 1000);
            }

            return Console.ReadLine();
        }

        public override SecureString ReadLineAsSecureString()
        {
            if (!_configuration.PromptForConfirmation)
            {
                this.Log().Warn(ChocolateyLoggers.Important, @"  Confirmation (`-y`) is set.
  Respond within {0} seconds or the default selection will be chosen.".FormatWith(TimeoutInSeconds));
            }

            var secureStringPlainText = InteractivePrompt.GetPassword(_configuration.PromptForConfirmation);

            return secureStringPlainText.ToSecureStringSafe();
        }

        public override void Write(string value)
        {
            this.Log().Info(value.EscapeCurlyBraces());
            //Console.Write(value);
        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            var originalForegroundColor = System.Console.ForegroundColor;
            var originalBackgroundColor = System.Console.BackgroundColor;
            System.Console.ForegroundColor = foregroundColor;
            System.Console.BackgroundColor = backgroundColor;

            Console.Write(value);
            this.Log().Info(ChocolateyLoggers.LogFileOnly, value.EscapeCurlyBraces());

            System.Console.ForegroundColor = originalForegroundColor;
            System.Console.BackgroundColor = originalBackgroundColor;
        }

        public override void WriteLine()
        {
            this.Log().Info("");
        }

        public override void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            var originalForegroundColor = System.Console.ForegroundColor;
            var originalBackgroundColor = System.Console.BackgroundColor;
            System.Console.ForegroundColor = foregroundColor;
            System.Console.BackgroundColor = backgroundColor;

            Console.WriteLine(value);
            this.Log().Info(ChocolateyLoggers.LogFileOnly, value.EscapeCurlyBraces());

            System.Console.ForegroundColor = originalForegroundColor;
            System.Console.BackgroundColor = originalBackgroundColor;
        }

        public override void WriteLine(string value)
        {
            this.Log().Info(value.EscapeCurlyBraces());
        }

        public override void WriteErrorLine(string value)
        {
            StandardErrorWritten = true;
            this.Log().Error(value.EscapeCurlyBraces());
        }

        public override void WriteDebugLine(string message)
        {
            this.Log().Debug(message.EscapeCurlyBraces());
        }

        private bool _hasLoggedStartProgress = false;
        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            if (record.PercentComplete == -1) return;

            if (!_hasLoggedStartProgress)
            {
                _hasLoggedStartProgress = true;
                this.Log().Debug(record.Activity.EscapeCurlyBraces());
            }

            if (_configuration.Features.ShowDownloadProgress)
            {
                // http://stackoverflow.com/a/888569/18475
                Console.Write("\rProgress: {0}% - {1}".FormatWith(record.PercentComplete.ToStringSafe(), record.StatusDescription).PadRight(Console.WindowWidth));
            }
        }

        public override void WriteVerboseLine(string message)
        {
            this.Log().Info(ChocolateyLoggers.Verbose, "VERBOSE: " + message.EscapeCurlyBraces());
        }

        public override void WriteWarningLine(string message)
        {
            this.Log().Warn("WARNING: " + message.EscapeCurlyBraces());
        }

        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            this.Log().Info(ChocolateyLoggers.Important, caption.EscapeCurlyBraces());
            var results = new Dictionary<string, PSObject>();
            foreach (FieldDescription field in descriptions)
            {
                if (string.IsNullOrWhiteSpace(field.Label)) this.Log().Warn(field.Name.EscapeCurlyBraces());
                else
                {
                    string[] label = GetHotkeyAndLabel(field.Label);
                    this.Log().Warn(label[1].EscapeCurlyBraces());
                }

                dynamic selection = string.Empty;

                if (field.ParameterTypeFullName.IsEqualTo(typeof(SecureString).FullName))
                {
                    selection = ReadLineAsSecureString();
                }
                else
                {
                    selection = ReadLine();
                }

                if (selection == null) return null;

                results[field.Name] = PSObject.AsPSObject(selection);
            }

            return results;
        }

        /// <summary>
        ///   Parse a string containing a hotkey character.
        ///   Take a string of the form
        ///   Yes to &amp;all
        ///   and returns a two-dimensional array split out as
        ///   "A", "Yes to all".
        /// </summary>
        /// <param name="input">The string to process</param>
        /// <returns>
        ///   A two dimensional array containing the parsed components.
        /// </returns>
        private static string[] GetHotkeyAndLabel(string input)
        {
            var result = new[] { String.Empty, String.Empty };
            //Do not use StringSplitOptions.RemoveEmptyEntries, it causes issues here
            string[] fragments = input.Split('&');
            if (fragments.Length == 2)
            {
                if (fragments[1].Length > 0) result[0] = fragments[1][0].ToStringSafe().ToUpper(CultureInfo.CurrentCulture);

                result[1] = (fragments[0] + fragments[1]).Trim();
            }
            else result[1] = input;

            return result;
        }

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            if (!string.IsNullOrWhiteSpace(caption)) this.Log().Warn(caption.EscapeCurlyBraces());
            if (!string.IsNullOrWhiteSpace(message)) this.Log().Warn(ChocolateyLoggers.Important, message.EscapeCurlyBraces());

            string[,] promptData = BuildHotkeysAndLabels(choices);

            // Format the overall choice prompt string to display.
            var choicePrompt = new StringBuilder();
            for (int element = 0; element < choices.Count; element++)
            {
                choicePrompt.Append(String.Format(
                    CultureInfo.CurrentCulture,
                    "[{0}] {1} ",
                    promptData[0, element],
                    promptData[1, element]));
            }

            choicePrompt.Append(String.Format(
                CultureInfo.CurrentCulture,
                "(default is \"{0}\")",
                promptData[0, defaultChoice]));

            while (true)
            {
                this.Log().Warn(choicePrompt.ToString().EscapeCurlyBraces());
                string selection = ReadLine().TrimSafe().ToUpper(CultureInfo.CurrentCulture);

                if (selection.Length == 0) return defaultChoice;

                for (int i = 0; i < choices.Count; i++)
                {
                    if (promptData[0, i] == selection) return i;
                }

                this.Log().Warn(ChocolateyLoggers.Important, "Invalid choice: " + selection.EscapeCurlyBraces());
            }
        }

        private static string[,] BuildHotkeysAndLabels(Collection<ChoiceDescription> choices)
        {
            var choiceSelections = new string[2, choices.Count];

            for (int i = 0; i < choices.Count; ++i)
            {
                string[] hotkeyAndLabel = GetHotkeyAndLabel(choices[i].Label);
                choiceSelections[0, i] = hotkeyAndLabel[0];
                choiceSelections[1, i] = hotkeyAndLabel[1];
            }

            return choiceSelections;
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            return PromptForCredential(caption, message, userName, targetName, PSCredentialTypes.Default, PSCredentialUIOptions.Default);
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            if (!string.IsNullOrWhiteSpace(caption)) this.Log().Warn(caption.EscapeCurlyBraces());
            if (!string.IsNullOrWhiteSpace(message)) this.Log().Warn(ChocolateyLoggers.Important, message.EscapeCurlyBraces());

            if (string.IsNullOrWhiteSpace(userName))
            {
                this.Log().Warn("Please provide username:");
                string selection = ReadLine().TrimSafe().ToUpper(CultureInfo.CurrentCulture);

                if (selection.Length == 0) selection = targetName;

                if (!string.IsNullOrWhiteSpace(selection)) userName = selection;
            }

            var password = string.Empty;
            this.Log().Warn("Please provide password:");
            password = InteractivePrompt.GetPassword(_configuration.PromptForConfirmation);

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                this.Log().Warn(ChocolateyLoggers.Important, "A userName or password was not entered. This may result in future failures.");
            }

            return new PSCredential(userName, password.ToSecureStringSafe());
        }

        public override PSHostRawUserInterface RawUI { get { return _rawUi; } }

    }
}
