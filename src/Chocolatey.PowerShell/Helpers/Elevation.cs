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
using System.Management.Automation;

namespace Chocolatey.PowerShell.Helpers
{
    /// <summary>
    /// Helpers for checking and handling if the process is elevated or not.
    /// </summary>
    public static class Elevation
    {
        /// <summary>
        /// Ensures a command is run in an elevated context.
        /// </summary>
        /// <typeparam name="T">The return type of the <paramref name="action"/>.</typeparam>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="action">The C# code path that is typically executed if the process is elevated.</param>
        /// <param name="fallbackCommand">The PowerShell fallback command that will re-enter the code path after elevation.</param>
        /// <returns>The return value of the <paramref name="action"/> or the <paramref name="fallbackCommand"/> when executed.</returns>
        public static object RunElevated<T>(PSCmdlet cmdlet, Func<T> action, string fallbackCommand)
        {
            if (ProcessInformation.IsElevated())
            {
                return action();
            }

            // We're going to quote the string in order to pass it to the command,
            // so escape any double quotes passed in with PowerShell escape chars
            // to ensure they're passed in correctly.
            if (fallbackCommand.IndexOf('"') != -1)
            {
                fallbackCommand.Replace("\"", "`\"");
            }

            // This elevation path is known not to work, because this command does *not* invoke the new process with
            // BOTH UseShellExecute=true and Verb=RunAs (see https://github.com/chocolatey/choco/issues/434).
            // This code remains here to mimic the current elevation checks used for the other code path, and is
            // expected to be removed or completely overhaulted and replaced in the near-ish future.
            return cmdlet.InvokeCommand.InvokeScript($"Start-ChocolateyProcessAsAdmin -statements \"{fallbackCommand}\"");
        }

        /// <summary>
        /// Ensures a command is run in an elevated context.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="action">The C# code path that is typically executed if the process is elevated.</param>
        /// <param name="fallbackCommand">The PowerShell fallback command that will re-enter the code path after elevation.</param>
        /// <returns>The return value of the <paramref name="action"/> or the <paramref name="fallbackCommand"/> when executed.</returns>
        public static void RunElevated(PSCmdlet cmdlet, Action action, string fallbackCommand)
        {
            object wrappedAction()
            {
                action();
                return null;
            }

            RunElevated(cmdlet, wrappedAction, fallbackCommand);
        }
    }
}
