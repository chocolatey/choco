﻿// Copyright © 2011 - Present RealDimensions Software, LLC
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
    using logging;
    using Console = adapters.Console;

    public class PoshHostUserInterface : PSHostUserInterface
    {
        private readonly ChocolateyConfiguration _configuration;
        protected readonly Console Console = new Console();
        private readonly PoshHostRawUserInterface _rawUi = new PoshHostRawUserInterface();
        private const int TIMEOUT_IN_SECONDS = 30;

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
  Respond within {0} seconds or the default selection will be chosen.".format_with(TIMEOUT_IN_SECONDS));

                return Console.ReadLine(TIMEOUT_IN_SECONDS * 1000);
            }

            return Console.ReadLine();
        }
        
        public override void Write(string value)
        {
            this.Log().Info(value);
            //Console.Write(value);
        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            var originalForegroundColor = System.Console.ForegroundColor;
            var originalBackgroundColor = System.Console.BackgroundColor;
            System.Console.ForegroundColor = foregroundColor;
            System.Console.BackgroundColor = backgroundColor;

            this.Log().Info(value);

            //Console.Write(value);

            System.Console.ForegroundColor = originalForegroundColor;
            System.Console.BackgroundColor = originalBackgroundColor;
        }

        public override void WriteLine()
        {
            base.WriteLine();
        }

        public override void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            var originalForegroundColor = System.Console.ForegroundColor;
            var originalBackgroundColor = System.Console.BackgroundColor;
            System.Console.ForegroundColor = foregroundColor;
            System.Console.BackgroundColor = backgroundColor;

            this.Log().Info(value);

            //Console.Write(value);

            System.Console.ForegroundColor = originalForegroundColor;
            System.Console.BackgroundColor = originalBackgroundColor;
        }

        public override void WriteLine(string value)
        {
            this.Log().Info(value);
        }

        public override void WriteErrorLine(string value)
        {
            StandardErrorWritten = true;
            this.Log().Error(value);
        }

        public override void WriteDebugLine(string message)
        {
            this.Log().Debug(message);
        }

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            if (record.PercentComplete == -1) return;

            if (record.PercentComplete == 100) this.Log().Debug(() => "Progress: 100%{0}".format_with(" ".PadRight(20)));

            // http://stackoverflow.com/a/888569/18475
            Console.Write("\rProgress: {0}%{1}".format_with(record.PercentComplete, " ".PadRight(20)));
        }

        public override void WriteVerboseLine(string message)
        {
            this.Log().Info(ChocolateyLoggers.Verbose, "VERBOSE: " + message);
        }

        public override void WriteWarningLine(string message)
        {
            this.Log().Warn("WARNING: " + message);
        }

        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            this.Log().Info(ChocolateyLoggers.Important, caption);
            var results = new Dictionary<string, PSObject>();
            foreach (FieldDescription field in descriptions)
            {
                if (string.IsNullOrWhiteSpace(field.Label)) this.Log().Warn(field.Name);
                else
                {
                    string[] label = get_hotkey_and_label(field.Label);
                    this.Log().Warn(label[1]);
                }

                string selection = ReadLine();
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
        private static string[] get_hotkey_and_label(string input)
        {
            var result = new[] { String.Empty, String.Empty };
            string[] fragments = input.Split('&');
            if (fragments.Length == 2)
            {
                if (fragments[1].Length > 0) result[0] = fragments[1][0].to_string().ToUpper(CultureInfo.CurrentCulture);

                result[1] = (fragments[0] + fragments[1]).Trim();
            }
            else result[1] = input;

            return result;
        }

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            this.Log().Warn(caption);

            string[,] promptData = build_hotkeys_and_plain_labels(choices);

            // Format the overall choice prompt string to display.
            var choicePrompt = new StringBuilder();
            for (int element = 0; element < choices.Count; element++)
            {
                choicePrompt.Append(String.Format(
                    CultureInfo.CurrentCulture,
                    "|{0}> {1} ",
                    promptData[0, element],
                    promptData[1, element]));
            }

            choicePrompt.Append(String.Format(
                CultureInfo.CurrentCulture,
                "[Default is ({0}]",
                promptData[0, defaultChoice]));

            while (true)
            {
                this.Log().Warn(choicePrompt.ToString());
                string selection = ReadLine().trim_safe().ToUpper(CultureInfo.CurrentCulture);

                if (selection.Length == 0) return defaultChoice;

                for (int i = 0; i < choices.Count; i++)
                {
                    if (promptData[0, i] == selection) return i;
                }

                this.Log().Warn(ChocolateyLoggers.Important, "Invalid choice: " + selection);
            }
        }

        private static string[,] build_hotkeys_and_plain_labels(Collection<ChoiceDescription> choices)
        {
            var choiceSelections = new string[2, choices.Count];

            for (int i = 0; i < choices.Count; ++i)
            {
                string[] hotkeyAndLabel = get_hotkey_and_label(choices[i].Label);
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
            this.Log().Warn(caption);
            if (string.IsNullOrWhiteSpace(userName))
            {
                this.Log().Warn("Please provide username:");
                string selection = ReadLine().trim_safe().ToUpper(CultureInfo.CurrentCulture);

                if (selection.Length == 0) selection = targetName;

                if (!string.IsNullOrWhiteSpace(selection)) userName = selection;
            }

            var password = string.Empty;
            this.Log().Warn("Please provide password:");
            var possibleNonInteractive = !_configuration.PromptForConfirmation;
            ConsoleKeyInfo info = possibleNonInteractive ? Console.ReadKey(TIMEOUT_IN_SECONDS * 1000) : Console.ReadKey(true);
            while (info.Key != ConsoleKey.Enter)
            {
                if (info.Key != ConsoleKey.Backspace)
                {
                    Console.Write("*");
                    password += info.KeyChar;
                    info = possibleNonInteractive ? Console.ReadKey(TIMEOUT_IN_SECONDS * 1000) : Console.ReadKey(true);
                }
                else if (info.Key == ConsoleKey.Backspace)
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        password = password.Substring(0, password.Length - 1);
                        // get the location of the cursor
                        int pos = System.Console.CursorLeft;
                        // move the cursor to the left by one character
                        System.Console.SetCursorPosition(pos - 1, System.Console.CursorTop);
                        // replace it with space
                        Console.Write(" ");
                        // move the cursor to the left by one character again
                        System.Console.SetCursorPosition(pos - 1, System.Console.CursorTop);
                    }
                    info = possibleNonInteractive ? Console.ReadKey(TIMEOUT_IN_SECONDS * 1000) : Console.ReadKey(true);
                }
            }
            for (int i = 0; i < password.Length; i++) Console.Write("*");
            System.Console.WriteLine("");

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            {
                this.Log().Warn(ChocolateyLoggers.Important, "A userName or password was not entered. This may result in future failures.");
            }

            return new PSCredential(userName, password.to_secure_string());
        }

        public override PSHostRawUserInterface RawUI { get { return _rawUi; } }

        #region Not Implemented / Empty

        public override SecureString ReadLineAsSecureString()
        {
            throw new NotImplementedException("Reading secure strings is not implemented.");
        }
        
        #endregion
    }
}
