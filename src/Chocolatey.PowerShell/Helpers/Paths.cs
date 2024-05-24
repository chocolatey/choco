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
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using Chocolatey.PowerShell.Shared;

namespace Chocolatey.PowerShell.Helpers
{
    internal static class Paths
    {
        private const string PathSplitPattern = "(?<=\\G(?:[^;\"]|\"[^\"]*\")*);";

        private static readonly Regex _pathSplitRegex = new Regex(PathSplitPattern, RegexOptions.Compiled);

        /// <summary>
        /// Split the given <paramref name="pathString"/> at each semicolon, excepting if the semicolon is in double quotes.
        /// Blank entries are preserved, such as those caused by a trailing semicolon.
        /// </summary>
        /// <param name="pathString">A PATH string to parse and split into a list of entries.</param>
        /// <returns>The list of entries, with any surrounding quotes trimmed from each entry.</returns>
        internal static string[] ParsePathString(string pathString)
        {
            var pathList = _pathSplitRegex.Split(pathString ?? string.Empty);

            // Strip any quotes from the PATH entries, if present
            for (var i = 0; i < pathList.Length; i++)
            {
                var entry = pathList[i];
                if (entry.Length >= 2 && entry.StartsWith("\"", StringComparison.Ordinal) && entry.EndsWith("\"", StringComparison.Ordinal))
                {
                    pathList[i] = entry.Substring(1, entry.Length - 2);
                }
            }

            return pathList;
        }

        /// <summary>
        /// Case-insensitively find the index of the given <paramref name="value"/> in the list of <paramref name="paths"/>.
        /// </summary>
        /// <param name="paths">The list of paths to look in.</param>
        /// <param name="value">The value to search for.</param>
        /// <returns></returns>
        internal static int FindPathIndex(List<string> paths, string value)
        {
            // Ensure we trim any trailing directory separators (slashes) off the end of both the input value to find and the values to compare against.
            var valueWithoutTrailingSlash = value.TrimEnd(Path.DirectorySeparatorChar);

            return paths.FindIndex(s => s.TrimEnd(Path.DirectorySeparatorChar).Equals(valueWithoutTrailingSlash, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Construct a PATH string from a list of entries and add quotes around any entries that contain PATH-reserved characters (; on Windows).
        /// </summary>
        /// <param name="paths">The list of paths to format.</param>
        /// <returns>A correctly-formatted PATH string containing all the input paths, each quoted if necessary.</returns>
        internal static string GetPathString(IList<string> paths)
        {
            var result = new StringBuilder();

            // Quote paths if necessary
            for (var i = 0; i < paths.Count; i++)
            {
                if (result.Length > 0)
                {
                    result.Append(Path.PathSeparator);
                }

                var path = paths[i];
                if (path?.IndexOf(Path.PathSeparator) > -1 && !path.StartsWith("\"") && !path.EndsWith("\""))
                {
                    result.Append($"\"{path}\"");
                }
                else
                {
                    result.Append(path);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Installs/adds a new PATH entry at the target <paramref name="scope"/>.
        /// </summary>
        /// <param name="cmdlet">The cmdlet running the method.</param>
        /// <param name="pathEntry">The path entry to add/install.</param>
        /// <param name="scope">The target scope of the PATH variable to modify.</param>
        public static void InstallPathEntry(PSCmdlet cmdlet, string pathEntry, EnvironmentVariableTarget scope)
        {
            var pathEntries = new List<string>(ParsePathString(EnvironmentHelper.GetVariable(cmdlet, EnvironmentVariables.Path, scope, preserveVariables: true)));
            if (FindPathIndex(pathEntries, pathEntry) == -1)
            {
                PSHelper.WriteHost(cmdlet, $"PATH environment variable does not have {pathEntry} in it. Adding...");

                pathEntries.Add(pathEntry);
                var newPath = GetPathString(pathEntries);

                void updatePath()
                {
                    EnvironmentHelper.SetVariable(cmdlet, EnvironmentVariables.Path, scope, newPath);
                }

                if (scope == EnvironmentVariableTarget.Machine)
                {
                    Elevation.RunElevated(cmdlet, updatePath, $"Install-ChocolateyPath -PathToInstall '{pathEntry}' -PathType {scope}");
                }
                else
                {
                    updatePath();
                }
            }
        }

        /// <summary>
        /// Uninstalls/removes a PATH entry at the target <paramref name="scope"/>.
        /// </summary>
        /// <param name="cmdlet">The cmdlet running the method.</param>
        /// <param name="pathEntry">The path entry to remove/uninstall.</param>
        /// <param name="scope">The target scope of the PATH variable to modify.</param>
        public static void UninstallPathEntry(PSCmdlet cmdlet, string pathEntry, EnvironmentVariableTarget scope)
        {
            var pathEntries = new List<string>(ParsePathString(EnvironmentHelper.GetVariable(cmdlet, EnvironmentVariables.Path, scope, preserveVariables: true)));
            var removeIndex = FindPathIndex(pathEntries, pathEntry);
            if (removeIndex >= 0)
            {
                PSHelper.WriteHost(cmdlet, $"Found {pathEntry} in PATH environment variable. Removing...");

                pathEntries.RemoveAt(removeIndex);
                var newPath = GetPathString(pathEntries);

                void updatePath()
                {
                    EnvironmentHelper.SetVariable(cmdlet, EnvironmentVariables.Path, scope, newPath);
                }

                if (scope == EnvironmentVariableTarget.Machine)
                {
                    Elevation.RunElevated(cmdlet, updatePath, $"Uninstall-ChocolateyPath -PathToInstall '{pathEntry}' -PathType {scope}");
                }
                else
                {
                    updatePath();
                }
            }
        }
    }
}
