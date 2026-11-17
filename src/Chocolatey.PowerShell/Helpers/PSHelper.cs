// Copyright © 2017 - 2025 Chocolatey Software, Inc
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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using Microsoft.PowerShell.Commands;
using static chocolatey.StringResources;

namespace Chocolatey.PowerShell.Helpers
{
    /// <summary>
    /// Helper methods for cmdlets to make interfacing with PowerShell easier.
    /// </summary>
    public static class PSHelper
    {
        private static readonly IList<string> _powershellLocations = new List<string>
        {
            Environment.ExpandEnvironmentVariables("%systemroot%\\SysNative\\WindowsPowerShell\\v1.0\\powershell.exe"),
            Environment.ExpandEnvironmentVariables("%systemroot%\\System32\\WindowsPowerShell\\v1.0\\powershell.exe"),
            "powershell.exe"
        };

        /// <summary>
        /// Writes objects to the output pipeline of the <paramref name="cmdlet"/>, enumerating collections.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="output">The object to write to the pipeline.</param>
        public static void WriteObject(PSCmdlet cmdlet, object output)
        {
            cmdlet.WriteObject(output, enumerateCollection: true);
        }

        /// <summary>
        /// Write messages to the debug stream.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="message">The message to write to the debug stream.</param>
        public static void WriteDebug(PSCmdlet cmdlet, string message)
        {
            cmdlet.WriteDebug(message);
        }

        /// <summary>
        /// Write messages to the verbose stream.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="message">The message to write to the verbose stream.</param>
        public static void WriteVerbose(PSCmdlet cmdlet, string message)
        {
            cmdlet.WriteVerbose(message);
        }

        /// <summary>
        /// Write messages to the warning stream.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="message">The message to write to the warning stream.</param>
        public static void WriteWarning(PSCmdlet cmdlet, string message)
        {
            cmdlet.WriteWarning(message);
        }

        /// <summary>
        /// Helper method to mimic Write-Host from C#, falls back to Write-Verbose when a host is not available.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="message">The message to write to the host.</param>
        public static void WriteHost(PSCmdlet cmdlet, string message)
        {
            WriteHost(cmdlet, message, newLine: true);
        }

        /// <summary>
        /// Helper method to mimic Write-Host from C#, falls back to Write-Verbose when a host is not available.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="message">The message to write to the host.</param>
        public static void WriteHost(PSCmdlet cmdlet, string message, bool newLine)
        {
            if (!(cmdlet.Host is null))
            {
                if (newLine)
                {
                    cmdlet.Host.UI.WriteLine(message);
                }
                else
                {
                    cmdlet.Host.UI.Write(message);
                }
            }
            else
            {
                cmdlet.WriteVerbose(message);
            }
        }

        /// <summary>
        /// Prompts the user for a credential, securely storing the password.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="caption">The caption for the credential prompt.</param>
        /// <param name="message">The message to prompt the user with.</param>
        /// <param name="username">The username for the credential, if known.</param>
        /// <param name="targetName">The target host for the credential. Unless this is for a domain credential, this will be null.</param>
        /// <returns>The credentials as a PSCredential object.</returns>
        public static PSCredential GetCredential(PSCmdlet cmdlet, string caption, string message, string username, string targetName)
        {
            return cmdlet.Host.UI.PromptForCredential(caption, message, username, targetName);
        }

        /// <summary>
        /// Gets the location of the Chocolatey install location.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <returns>The path to the Chocolatey folder.</returns>
        public static string GetInstallLocation(PSCmdlet cmdlet)
        {
            if (ItemExists(cmdlet, $@"env:\{EnvironmentVariables.System.ChocolateyInstall}"))
            {
                return EnvironmentHelper.GetVariable(EnvironmentVariables.System.ChocolateyInstall);
            }

            if (ItemExists(cmdlet, $@"env:\ProgramData"))
            {
                return CombinePaths(cmdlet, EnvironmentHelper.GetVariable("ProgramData"), "chocolatey");
            }

            if (ItemExists(cmdlet, $@"env:\SystemDrive"))
            {
                return CombinePaths(cmdlet, EnvironmentHelper.GetVariable("SystemDrive"), "ProgramData", "chocolatey");
            }

            return GetParentDirectory(cmdlet, GetParentDirectory(cmdlet, typeof(PSHelper).Assembly.Location));
        }

        /// <summary>
        /// Combine the given path fragments into a single path.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="parent">The parent path to combine child fragments with.</param>
        /// <param name="childPaths">One or more child paths to combine the parent path with.</param>
        /// <returns>The completed path constructed from fragments.</returns>
        public static string CombinePaths(PSCmdlet cmdlet, string parent, params string[] childPaths)
        {
            var result = parent;
            foreach (var path in childPaths)
            {
                result = cmdlet.SessionState.Path.Combine(result, path);
            }

            return result;
        }

        /// <summary>
        /// Convert the given <paramref name="value"/> to the target type <typeparamref name="T"/>, using
        /// PowerShell's default conversion semantics.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value.</returns>
        public static T ConvertTo<T>(object value)
        {
            return (T)LanguagePrimitives.ConvertTo(value, typeof(T));
        }

        /// <summary>
        /// Attempt to convert the given <paramref name="value"/> to the target type <typeparamref name="T"/>,
        /// using PowerShell's default conversion semantics.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="result">The converted value if the conversion succeeds, otherwise the default value for the type.</param>
        /// <returns>True if the value successfully converts, otherwise false.</returns>
        public static bool TryConvertTo<T>(object value, out T result)
        {
            return LanguagePrimitives.TryConvertTo<T>(value, out result);
        }

        /// <summary>
        /// Checks for the existence of the target <paramref name="directory"/>, creating it if it doesn't exist.
        /// </summary>
        /// <param name="cmdlet">The cmdlet running the method.</param>
        /// <param name="directory">The directory to look for or create.</param>
        public static void EnsureDirectoryExists(PSCmdlet cmdlet, string directory)
        {
            if (!ContainerExists(cmdlet, directory))
            {
                NewDirectory(cmdlet, directory);
            }
        }

        /// <summary>
        /// Test the equality of two values, based on PowerShell's equality checks, case insensitive for string values.
        /// Equivalent to <c>-eq</c> in PowerShell.
        /// </summary>
        /// <param name="first">The first (LHS) value to compare.</param>
        /// <param name="second">The second (RHS) vale to compare.</param>
        /// <returns>True if PowerShell considers the values equial, false otherwise.</returns>
        public static bool IsEqual(object first, object second)
        {
            return LanguagePrimitives.Equals(first, second, ignoreCase: true);
        }

        /// <summary>
        /// Test the equality of two values, based on PowerShell's equality checks, optionally case insensitive.
        /// Equivalent to <c>-eq</c> in PowerShell if <paramref name="ignoreCase"/> is <c>true</c>, otherwise equivalent to <c>-ceq</c>.
        /// </summary>
        /// <param name="first">The first (LHS) value to compare.</param>
        /// <param name="second">The second (RHS) vale to compare.</param>
        /// <param name="ignoreCase">Whether to ignore case in the comparison for string values.</param>
        /// <returns>True if PowerShell considers the values equial, false otherwise.</returns>
        public static bool IsEqual(object first, object second, bool ignoreCase)
        {
            return LanguagePrimitives.Equals(first, second, ignoreCase);
        }

        /// <summary>
        /// Test whether an item at the given path exists.
        /// Equivalent to <c>Test-Path</c>.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="path">The path to look for an item at.</param>
        /// <returns>True if the item exists, otherwise false.</returns>
        public static bool ItemExists(PSCmdlet cmdlet, string path)
        {
            return cmdlet.InvokeProvider.Item.Exists(path);
        }

        /// <summary>
        /// Test whether a non-container item at the given path exists.
        /// Equivalent to <c>Test-Path -PathType Leaf</c>.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="path">The path to look for a non-container item at.</param>
        /// <returns>True if a file exists at the given path, otherwise false.</returns>
        public static bool FileExists(PSCmdlet cmdlet, string path)
        {
            return ItemExists(cmdlet, path) && !ContainerExists(cmdlet, path);
        }

        /// <summary>
        /// Copies the target file to the destination, resolving paths according to PowerShell's path resolution behaviour.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="path">The source file to copy.</param>
        /// <param name="destinationPath">The destination file.</param>
        /// <param name="force">Whether to overwrite existing files.</param>
        public static void CopyFile(PSCmdlet cmdlet, string path, string destinationPath, bool force)
        {
            var source = ResolveFilePath(cmdlet, path);
            var destination = GetUnresolvedPath(cmdlet, destinationPath);

            cmdlet.InvokeProvider.Item.Copy(source.ToArray(), destination, recurse: false, CopyContainers.CopyTargetContainer, force, literalPath: false);
        }

        /// <summary>
        /// Test whether a container item at the given path exists.
        /// Equivalent to <c>Test-Path -PathType Container</c>.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="path">The path to look for a container item at.</param>
        /// <returns>True if a container exists at the given path, otherwise false.</returns>
        public static bool ContainerExists(PSCmdlet cmdlet, string path)
        {
            return cmdlet.InvokeProvider.Item.IsContainer(path);
        }

        /// <summary>
        /// Gets the parent directory of a given path.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="path">The path to find the parent container for.</param>
        /// <returns>The path to the parent container of the provided path.</returns>
        public static string GetParentDirectory(PSCmdlet cmdlet, string path)
        {
            return cmdlet.SessionState.Path.ParseParent(GetUnresolvedPath(cmdlet, path), string.Empty);
        }

        /// <summary>
        /// Gets the file name segment of a provided file path.
        /// </summary>
        /// <param name="path">The path to take the file name from.</param>
        /// <returns>The file name and extension.</returns>
        public static string GetFileName(string path)
        {
            return Path.GetFileName(path);
        }

        /// <summary>
        /// Gets the current PowerShell version of the running PowerShell assemblies.
        /// Equivalent to <c>$PSVersionTable.PSVersion</c>.
        /// </summary>
        /// <returns>The current PowerShell version.</returns>
        public static Version GetPSVersion()
        {
            Version result = null;
            var assembly = Assembly.GetAssembly(typeof(Cmdlet));

            // This type is public in PS v6.2+, this reflection will not be needed once we're using newer assemblies.
            var psVersionInfo = assembly.GetType("System.Management.Automation.PSVersionInfo");
            var versionProperty = psVersionInfo?.GetProperty("PSVersion", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            var getter = versionProperty?.GetGetMethod(true);
            result = (Version)getter?.Invoke(null, Array.Empty<object>());

            // Assume absolute minimum version if we can't determine the version.
            return result ?? new Version(2, 0);
        }

        /// <summary>
        /// Turns a relative path into a full path based on the current context the <paramref name="cmdlet"/> is running in,
        /// without ensuring that the path actually exists.
        /// 
        /// Similar to <c>Resolve-Path</c>, but will not error on a path that does not exist.
        /// </summary>
        /// <param name="cmdlet">The cmdlet running the method.</param>
        /// <param name="path">The relative path to transform into a full path.</param>
        /// <returns>The full path to the item.</returns>
        public static string GetUnresolvedPath(PSCmdlet cmdlet, string path)
        {
            return cmdlet.SessionState.Path.GetUnresolvedProviderPathFromPSPath(path);
        }

        /// <summary>
        /// Resolves a path in the current PowerShell FileSystem provider context to one or more files.
        /// Wildcards are supported.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="path">The unresolved path to fully resolve.</param>
        /// <returns>A collection of resolved paths on the filesystem.</returns>
        public static Collection<string> ResolveFilePath(PSCmdlet cmdlet, string path)
        {
            return cmdlet.SessionState.Path.GetResolvedProviderPathFromProviderPath(path, FileSystemProvider.ProviderName);
        }

        /// <summary>
        /// Creates a new item at the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="path">The path to the new item.</param>
        /// <param name="name">The name for the new item, which may be null if the <paramref name="path"/> contains the name already.</param>
        /// <param name="itemType">The name for the item type to create. For the FileSystem provider, this will be File or Directory.</param>
        /// <returns>A <see cref="Collection{PSObject}"/> containing the references to the item(s) created.</returns>
        public static Collection<PSObject> NewItem(PSCmdlet cmdlet, string path, string name, string itemType)
        {
            return cmdlet.InvokeProvider.Item.New(path, name, itemType, content: string.Empty);
        }

        /// <summary>
        /// Creates a new item at the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="path">The path to the new item.</param>
        /// <param name="name">The name for the new item, which may be null if the <paramref name="path"/> contains the name already.</param>
        /// <param name="itemType">The name for the item type to create. For the FileSystem provider, this will be File or Directory.</param>
        /// <returns>A <see cref="Collection{PSObject}"/> containing the references to the item(s) created.</returns>
        public static Collection<PSObject> NewItem(PSCmdlet cmdlet, string path, string itemType)
        {
            return NewItem(cmdlet, path, name: null, itemType);
        }

        /// <summary>
        /// Creates a new file at the designated <paramref name="path"/>.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="path">The path to the file to be created.</param>
        /// <returns>A <see cref="Collection{PSObject}"/> containing the references to the item(s) created.</returns>
        public static Collection<PSObject> NewFile(PSCmdlet cmdlet, string path)
        {
            return NewItem(cmdlet, path, itemType: "File");
        }

        /// <summary>
        /// Creates a new directory at the designated <paramref name="path"/>.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="path">The path to the directory to be created.</param>
        /// <returns>A <see cref="Collection{PSObject}"/> containing the references to the item(s) created.</returns>
        public static Collection<PSObject> NewDirectory(PSCmdlet cmdlet, string path)
        {
            return NewItem(cmdlet, path, itemType: "Directory");
        }

        /// <summary>
        /// Sets the exit code for when the PowerShell host exits. Note that this does not terminate the process, just updates the
        /// exit code for when it is exited.
        /// </summary>
        /// <param name="cmdlet">The calling cmdlet.</param>
        /// <param name="exitCode">The exit code to set.</param>
        public static void SetExitCode(PSCmdlet cmdlet, int exitCode)
        {
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyExitCode, exitCode.ToString());
            cmdlet.Host.SetShouldExit(exitCode);
        }

        /// <summary>
        /// Gets the path to the location of <c>powershell.exe</c>.
        /// </summary>
        /// <returns>The path where <c>powershell.exe</c> is found.</returns>
        /// <exception cref="FileNotFoundException">Thrown if <c>powershell.exe</c> cannot be located.</exception>
        public static string GetPowerShellLocation()
        {
            foreach (var powershellLocation in _powershellLocations)
            {
                if (File.Exists(powershellLocation))
                {
                    return powershellLocation;
                }
            }

            throw new FileNotFoundException(string.Format("Unable to find suitable location for PowerShell. Searched the following locations: '{0}'", string.Join("; ", _powershellLocations)));
        }
    }
}

