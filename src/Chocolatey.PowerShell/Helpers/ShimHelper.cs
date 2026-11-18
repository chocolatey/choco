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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace Chocolatey.PowerShell.Helpers
{
    /// <summary>
    /// Helpers for creating and removing the shims (and batch redirects) in the Chocolatey bin folder.
    /// </summary>
    public static class ShimHelper
    {
        private const string BinFolderName = "bin";

        /// <summary>
        /// Creates a shim in the Chocolatey bin folder for the file at <paramref name="path"/>, falling back to
        /// batch and bash redirect files if the shim cannot be generated.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="name">The name of the shim, without an extension.</param>
        /// <param name="path">The path to the file the shim runs.</param>
        /// <param name="useStart">Whether the shim should start the file without waiting for it to exit.</param>
        /// <param name="command">Additional arguments the shim always passes to the file.</param>
        public static void Create(PSCmdlet cmdlet, string name, string path, bool useStart, string command)
        {
            Create(cmdlet, name, path, useStart, command, GetShimGenConfig(cmdlet));
        }

        internal static void Create(PSCmdlet cmdlet, string name, string path, bool useStart, string command, ShimGenConfig config)
        {
            var installRoot = config.InstallRoot;
            var shimGenPath = config.Path;
            var binFolder = Path.Combine(installRoot, BinFolderName);
            var batchFile = Path.Combine(binFolder, $"{name}.bat");
            var bashFile = Path.Combine(binFolder, name);
            var shimFile = Path.Combine(binFolder, $"{name}.exe");

            if (!cmdlet.ShouldProcess(shimFile, "Create shim"))
            {
                return;
            }

            ForceRemoveFile(batchFile);
            ForceRemoveFile(bashFile);

            var target = GetRelativeTarget(installRoot, path);

            if (File.Exists(shimGenPath))
            {
                var arguments = GetShimGenArguments(shimFile, target, path, useStart, command, config.Debug);
                var exitCode = CreateShim(cmdlet, shimGenPath, arguments, config);

                if (exitCode == 0 && File.Exists(shimFile))
                {
                    PSHelper.WriteHost(cmdlet, $"Added {shimFile} shim pointed to '{target}'.");
                    return;
                }
                else
                {
                    cmdlet.WriteDebug($"ShimGen exited with code {exitCode}.");
                }
            }

            cmdlet.WriteWarning("An error occurred generating shim, using old method.");
            WriteRedirectFiles(cmdlet, name, target, useStart, batchFile, bashFile);
        }

        private static int CreateShim(PSCmdlet cmdlet, string path, string arguments, ShimGenConfig config)
        {
            cmdlet.WriteDebug($"ShimGen found at '{path}'");
            cmdlet.WriteDebug($"Calling {path} {arguments}");

            var (exitCode, output) = config.RunAction(path, arguments);

            foreach (var line in output)
            {
                cmdlet.WriteDebug($" [ShimGen] {line}");
            }

            return exitCode;
        }

        /// <summary>
        /// Removes the shim and any batch and bash redirect files with the given <paramref name="name"/> from the
        /// Chocolatey bin folder.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="name">The name of the shim, without an extension.</param>
        /// <param name="path">The path to the file the shim runs, used only in messages.</param>
        public static void Remove(PSCmdlet cmdlet, string name, string path)
        {
            Remove(cmdlet, name, path, GetInstallRoot(cmdlet));
        }

        internal static void Remove(PSCmdlet cmdlet, string name, string path, string installRoot)
        {
            var binFolder = Path.Combine(installRoot, BinFolderName);
            var batchFile = Path.Combine(binFolder, $"{name}.bat");
            var bashFile = Path.Combine(binFolder, name);
            var shimFile = Path.Combine(binFolder, $"{name}.exe");

            path = path ?? string.Empty;
            var relativeTarget = GetRelativeTarget(installRoot, path);
            var target = relativeTarget == path ? path : "%DIR%" + relativeTarget;

            cmdlet.WriteDebug($"Attempting to remove the batch and bash shortcuts: {batchFile} and {bashFile}");
            RemoveFile(cmdlet, batchFile, "batch file", target);
            RemoveFile(cmdlet, bashFile, "bash file", target);

            cmdlet.WriteDebug($"Attempting to remove the shim: {shimFile}");
            RemoveFile(cmdlet, shimFile, "shim", target);
        }

        /// <summary>
        /// Makes <paramref name="path"/> relative to the bin folder when it is inside the Chocolatey install
        /// folder; otherwise returns it unchanged.
        /// </summary>
        internal static string GetRelativeTarget(string installRoot, string path)
        {
            var root = installRoot.TrimEnd('\\') + @"\";
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            var pathTail = path.Substring(root.Length);
            return $@"..\{pathTail}".Replace(@"\\", @"\");
        }

        internal static string GetShimGenArguments(string shimFile, string target, string iconPath, bool useStart, string command, bool debug)
        {
            var arguments = new StringBuilder($"-o \"{shimFile}\" -p \"{target}\" -i \"{iconPath}\"");

            if (!string.IsNullOrEmpty(command))
            {
                arguments.Append($" -c {command}");
            }

            if (useStart)
            {
                arguments.Append(" -gui");
            }

            if (debug)
            {
                arguments.Append(" -debug");
            }

            return arguments.ToString();
        }

        private static void WriteRedirectFiles(PSCmdlet cmdlet, string name, string target, bool useStart, string batchFile, string bashFile)
        {
            var isRelative = !Path.IsPathRooted(target);
            var batchTarget = isRelative ? "%DIR%" + target : target;
            var bashTarget = (isRelative ? "$DIR/" + target : target).Replace('\\', '/');

            PSHelper.WriteHost(cmdlet, $"Adding {batchFile} and pointing to '{batchTarget}'.");
            PSHelper.WriteHost(cmdlet, $"Adding {bashFile} and pointing to '{batchTarget}'.");

            string batchContent;
            string bashContent;

            if (useStart)
            {
                PSHelper.WriteHost(cmdlet, $"Setting up {name} as a non-command line application.");
                batchContent = $"@echo off\r\nSET DIR=%~dp0%\r\nstart \"\" \"{batchTarget}\" %*\r\n";
                bashContent = $"#!/bin/sh\nDIR=${{0%/*}}\n\"{bashTarget}\" \"$@\" &\n";
            }
            else
            {
                batchContent = $"@echo off\r\nSET DIR=%~dp0%\r\ncmd /c \"\"{batchTarget}\" %*\"\r\nexit /b %ERRORLEVEL%\r\n";
                bashContent = $"#!/bin/sh\nDIR=${{0%/*}}\n\"{bashTarget}\" \"$@\"\nexit $?\n";
            }

            File.WriteAllText(batchFile, batchContent, Encoding.ASCII);
            File.WriteAllText(bashFile, bashContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        private static void RemoveFile(PSCmdlet cmdlet, string file, string description, string target)
        {
            if (!File.Exists(file))
            {
                cmdlet.WriteDebug($"Tried to remove {description} {file} but it was already removed.");
                return;
            }

            if (cmdlet.ShouldProcess(file, $"Remove {description}"))
            {
                PSHelper.WriteHost(cmdlet, $"Removing {description} {file} which pointed to '{target}'.");
                File.Delete(file);
            }
        }

        private static void ForceRemoveFile(string file)
        {
            if (File.Exists(file))
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
        }

        private static string GetInstallRoot(PSCmdlet cmdlet)
        {
            return PSHelper.ResolvePath(cmdlet, PSHelper.GetInstallLocation(cmdlet)).First();
        }

        private static ShimGenConfig GetShimGenConfig(PSCmdlet cmdlet)
        {
            var installRoot = GetInstallRoot(cmdlet);

            return new ShimGenConfig
            {
                InstallRoot = installRoot,
                Path = Path.Combine(installRoot, "tools", "shimgen.exe"),
                Debug = PSHelper.IsDebug(cmdlet),
                RunAction = RunShimGen,
            };
        }

        private static (int ExitCode, IList<string> Output) RunShimGen(string shimGenPath, string arguments)
        {
            var startInfo = new ProcessStartInfo(shimGenPath, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            using (var process = Process.Start(startInfo))
            {
                var error = process.StandardError.ReadToEndAsync();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var lines = $"{output}\n{error.Result}"
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();

                return (process.ExitCode, lines);
            }
        }
    }
}
