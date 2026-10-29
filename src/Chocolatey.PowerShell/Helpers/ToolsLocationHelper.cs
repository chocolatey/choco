// Copyright © 2017 - 2026 Chocolatey Software, Inc
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
using System.Text.RegularExpressions;
using static chocolatey.StringResources.EnvironmentVariables.System;

namespace Chocolatey.PowerShell.Helpers
{
    /// <summary>
    /// Gets the top level location for tools and software that are installed outside of package folders, and keeps the
    /// <c>ChocolateyToolsLocation</c> environment variable up to date with it.
    /// Used by <see cref="Commands.GetToolsLocationCommand"/>, and any other commands that need the location.
    /// </summary>
    public class ToolsLocationHelper
    {
        private static readonly Regex DriveLetterPattern = new Regex(@"^\w:", RegexOptions.IgnoreCase);

        private readonly PSCmdlet _cmdlet;

        /// <summary>
        /// Creates a helper that runs on behalf of the <paramref name="cmdlet"/>.
        /// </summary>
        /// <param name="cmdlet">The cmdlet to report the environment changes and errors through.</param>
        public ToolsLocationHelper(PSCmdlet cmdlet)
        {
            _cmdlet = cmdlet;
        }

        /// <summary>
        /// Gets the tools location.
        /// This is <c>ChocolateyToolsLocation</c> when that is set, otherwise the value of the older
        /// <c>ChocolateyBinRoot</c> (or older still, <c>chocolatey_bin_root</c>), otherwise <c>tools</c>
        /// on the system drive.
        /// The older variables are removed from the user's environment when they are used, and the result is
        /// saved to <c>ChocolateyToolsLocation</c> when that is not already set to it.
        /// </summary>
        /// <returns>The full path of the tools location.</returns>
        public string Get()
        {
            var toolsLocation = EnvironmentHelper.GetVariable(ChocolateyToolsLocation);

            if (toolsLocation is null)
            {
                var binRoot = EnvironmentHelper.GetVariable(ChocolateyBinRoot);
                var legacyRoot = EnvironmentHelper.GetVariable(ChocolateyBinRootLegacy);

                if (binRoot is null && legacyRoot is null)
                {
                    toolsLocation = CombinePaths(EnvironmentHelper.GetVariable(SystemDrive), "tools");
                }
                else
                {
                    if (!(legacyRoot is null))
                    {
                        if (binRoot is null)
                        {
                            binRoot = legacyRoot;
                        }

                        ClearVariable(ChocolateyBinRootLegacy);
                    }

                    toolsLocation = binRoot;
                    ClearVariable(ChocolateyBinRoot);
                }
            }

            if (!DriveLetterPattern.IsMatch(toolsLocation))
            {
                toolsLocation = CombinePaths(EnvironmentHelper.GetVariable(SystemDrive), toolsLocation);
            }

            if (!string.Equals(EnvironmentHelper.GetVariable(ChocolateyToolsLocation), toolsLocation, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    SetVariable(ChocolateyToolsLocation, toolsLocation, EnvironmentVariableTarget.User);
                }
                catch (Exception error) when (!(error is PipelineStoppedException))
                {
                    if (!IsElevated())
                    {
                        throw;
                    }

                    // The user scope was inaccessible, but we are elevated, so we can try writing to the
                    // machine scope instead.
                    SetVariable(
                        ChocolateyToolsLocation,
                        toolsLocation,
                        EnvironmentVariableTarget.Machine);
                }
            }

            return toolsLocation;
        }

        /// <summary>
        /// Joins two parts of a path the way a path is joined in PowerShell, so that a drive such as <c>C:</c> and
        /// <c>tools</c> give <c>C:\tools</c>.
        /// </summary>
        protected virtual string CombinePaths(string parent, string child)
        {
            return PSHelper.CombinePaths(_cmdlet, parent, child);
        }

        /// <summary>
        /// Sets an environment variable in the target scope.
        /// </summary>
        protected virtual void SetVariable(string name, string value, EnvironmentVariableTarget scope)
        {
            EnvironmentHelper.SetVariable(_cmdlet, name, scope, value);
        }

        /// <summary>
        /// Gets whether the process has administrative rights.
        /// </summary>
        protected virtual bool IsElevated()
        {
            return ProcessInformation.IsElevated();
        }

        /// <summary>
        /// Removes a variable that is no longer used from the user's environment.
        /// Failure to clear the value is ignored.
        /// </summary>
        private void ClearVariable(string name)
        {
            try
            {
                SetVariable(name, string.Empty, EnvironmentVariableTarget.User);
            }
            catch (Exception error) when (!(error is PipelineStoppedException))
            {
                // This is only used to clear out old variables, it's not
                // critical for anything to work.
            }
        }
    }
}
