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

using Chocolatey.PowerShell.Shared;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PackageVariables = chocolatey.StringResources.EnvironmentVariables.Package;
using SystemVariables = chocolatey.StringResources.EnvironmentVariables.System;

namespace Chocolatey.PowerShell.Helpers
{
    public class ExtractArchiveHelper
    {
        private const int BuiltinBufferSize = 1024 * 80; // 80kb (default)

        private readonly CancellationToken _cancellationToken;
        private readonly PSCmdlet _cmdlet;
        private readonly StringBuilder _extractedFilesList = new StringBuilder();

        public ExtractArchiveHelper(PSCmdlet cmdlet, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _cmdlet = cmdlet;
        }

        /// <summary>
        /// Extract files from an archive to a <paramref name="destination"/> directory.
        /// </summary>
        /// <param name="path">The path to the default or 32-bit archive to extract.</param>
        /// <param name="path64">The path to a 64-bit archive to extract.</param>
        /// <param name="packageName">The package that is being currently installed. Will default to <c>ChocolateyPackageName</c> environment variable if not provided.</param>
        /// <param name="destination">The destination directory to extract files to.</param>
        /// <param name="filesToExtract">A path or glob pattern to filter the files extracted from the archive.</param>
        /// <param name="useBuiltinCompression">If true, uses a builtin .NET extraction method rather than 7zip. Note that this method only supports zip files.</param>
        /// <param name="disableLogging">Whether to write a log of the files extracted from the archive.</param>
        /// <exception cref="ArgumentException">Thrown if incorrect combinations of arguments were given.</exception>
        /// <exception cref="InvalidDataException">Thrown if a file in the archive has an invalid relative path.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the 7-Zip executable cannot be found and <paramref name="useBuiltinCompression"/> is <c>false</c>.</exception>
        /// <exception cref="NotSupportedException">Thrown if there is no 32-bit archive path available for the given package and we are forced to use 32-bit.</exception>
        /// <exception cref="SevenZipException">Thrown if the 7-Zip executable returns a non-zero error code and <paramref name="useBuiltinCompression"/> is <c>false</c>..</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown if the 7-Zip executable cannot be opened and <paramref name="useBuiltinCompression"/> is <c>false</c>..</exception>
        public void ExtractFiles(
            string path,
            string path64,
            string packageName,
            string destination,
            string filesToExtract,
            bool useBuiltinCompression,
            bool disableLogging)
        {
            if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(path64))
            {
                throw new ArgumentException("At least one of the path or path64 values must be specified.");
            }

            var bitnessMessage = string.Empty;
            var zipFilePath = path;
            packageName = string.IsNullOrEmpty(packageName)
                ? Environment.GetEnvironmentVariable(PackageVariables.ChocolateyPackageName)
                : packageName;
            var logPath = string.Empty;

            var forceX86 = PSHelper.IsEqual(Environment.GetEnvironmentVariable(PackageVariables.ChocolateyForceX86), "true");
            if (ArchitectureWidth.Matches(32) || forceX86)
            {
                if (string.IsNullOrEmpty(path))
                {
                    throw new NotSupportedException($"32-bit archive is not supported for {packageName}");
                }

                if (!string.IsNullOrEmpty(path64))
                {
                    bitnessMessage = "32-bit ";
                }
            }
            else if (!string.IsNullOrEmpty(path64))
            {
                zipFilePath = path64;
                bitnessMessage = "64 bit ";
            }

            if (!PSHelper.FileExists(_cmdlet, zipFilePath))
            {
                throw new FileNotFoundException("The target archive could not be found at the specified path.", zipFilePath);
            }

            if (!string.IsNullOrEmpty(packageName))
            {
                var libPath = Environment.GetEnvironmentVariable(PackageVariables.ChocolateyPackageFolder);
                if (!string.IsNullOrEmpty(libPath))
                {
                    if (!PSHelper.ContainerExists(_cmdlet, libPath))
                    {
                        PSHelper.NewDirectory(_cmdlet, libPath);
                    }

                    if (!disableLogging)
                    {
                        logPath = PSHelper.CombinePaths(_cmdlet, libPath, $"{PSHelper.GetFileName(zipFilePath)}.txt");
                    }
                }
            }

            var envChocolateyPackageName = Environment.GetEnvironmentVariable(PackageVariables.ChocolateyPackageName);
            var envChocolateyInstallDirectoryPackage = Environment.GetEnvironmentVariable(PackageVariables.ChocolateyInstallDirectoryPackage);

            if (!string.IsNullOrEmpty(envChocolateyPackageName) && PSHelper.IsEqual(envChocolateyPackageName, envChocolateyInstallDirectoryPackage))
            {
                _cmdlet.WriteWarning("Install Directory override not available for zip packages at this time. If this package also runs a native installer using Chocolatey functions, the directory will be honored.");
            }

            PSHelper.WriteHost(_cmdlet, $"Extracting {bitnessMessage}{zipFilePath} to {destination}...");

            PSHelper.EnsureDirectoryExists(_cmdlet, destination);

            var filesToExtractMessage = string.IsNullOrEmpty(filesToExtract) ? string.Empty : $" matching pattern {filesToExtract}";
            try
            {
                if (useBuiltinCompression)
                {
                    if (_cmdlet.ShouldProcess(zipFilePath, $"Extract zip file contents{filesToExtractMessage} to '{destination}' with built-in decompression"))
                    {
                        ExtractWithBuiltin(zipFilePath, destination, filesToExtract, disableLogging);
                    }
                }
                else if (_cmdlet.ShouldProcess(zipFilePath, $"Extract zip file contents{filesToExtractMessage} to '{destination}' with 7-Zip"))
                {
                    var helper = new SevenZipExtractionHelper(_cmdlet, _cancellationToken, _extractedFilesList);
                    helper.ExtractFiles(zipFilePath, destination, filesToExtract, disableLogging);
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(logPath))
                {
                    try
                    {
                        PSHelper.SetContent(_cmdlet, logPath, _extractedFilesList.ToString(), Encoding.UTF8);
                    }
                    catch (IOException error)
                    {
                        // Non-terminating error, because this doesn't mean the operation actually failed,
                        // it just means we couldn't write the log file.
                        _cmdlet.WriteError(new RuntimeException($"There was an error recording the zip file extraction log: {error.Message}", error).ErrorRecord);
                    }
                }
            }

            EnvironmentHelper.SetVariable(PackageVariables.ChocolateyPackageInstallLocation, destination);
        }

        /// <summary>
        /// Use builtin zip archive methods to extract files.
        /// </summary>
        /// <param name="path">The path to the archive to extract.</param>
        /// <param name="destination">The destination directory to extract files to.</param>
        /// <param name="filesToExtract">A path or glob pattern to filter the files extracted from the archive.</param>
        /// <param name="disableLogging">Whether to write a log of the files extracted from the archive.</param>
        /// <returns>The destination path where the files were extracted to.</returns>
        /// <exception cref="InvalidDataException">Thrown if a file in the archive has an invalid relative path.</exception>
        private void ExtractWithBuiltin(string path, string destination, string filesToExtract, bool disableLogging)
        {
            var fullDestination = PSHelper.GetUnresolvedPath(_cmdlet, destination);
            using (var file = File.OpenRead(path))
            using (var zipArchive = new ZipArchive(file, ZipArchiveMode.Read))
            {
                BuiltinExtractToDirectory(zipArchive, fullDestination, filesToExtract, disableLogging);
            }
        }

        private void BuiltinExtractToDirectory(ZipArchive source, string resolvedDirectoryName, string filesToExtract, bool disableLogging)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (resolvedDirectoryName == null)
            {
                throw new ArgumentNullException(nameof(resolvedDirectoryName));
            }

            // Wildcard patterns we support are just * and ?, so convert those and escape everything else as literal.
            // We anchor to the start of the path, leaving the end un-anchored so that providing a directory name
            // will extract the directory along with its contents.
            var filter = string.IsNullOrEmpty(filesToExtract)
                ? null
                : new Regex("^" + Regex.Escape(filesToExtract).Replace(@"\\*", ".*").Replace(@"\\?", "."), RegexOptions.IgnoreCase);

            DirectoryInfo directoryInfo = Directory.CreateDirectory(resolvedDirectoryName);
            var destination = directoryInfo.FullName;

            var length = destination.Length;
            if (length != 0 && destination[length - 1] != Path.DirectorySeparatorChar)
            {
                destination += Path.DirectorySeparatorChar;
            }

            foreach (ZipArchiveEntry entry in source.Entries)
            {
                if (!(filter is null))
                {
                    if (!filter.IsMatch(entry.FullName))
                    {
                        _cmdlet.WriteDebug($"Skipping zipfile entry {entry.FullName} as it does not match the pattern {filesToExtract}");
                        continue;
                    }
                }

                var fullPath = Path.GetFullPath(Path.Combine(destination, entry.FullName));
                if (!fullPath.StartsWith(destination, StringComparison.OrdinalIgnoreCase))
                {
                    throw new IOException($"Invalid data encountered in the archive; entry's relative path '{entry.FullName}' would cause it to be extracted outside the destination directory.");
                }

                if (Path.GetFileName(fullPath).Length == 0)
                {
                    if (entry.Length != 0)
                    {
                        throw new IOException($"Invalid data encountered for entry {entry.FullName}; a directory entry containing file data cannot be extracted.");
                    }

                    Directory.CreateDirectory(fullPath);
                }
                else
                {
                    ExtractZipArchiveFile(fullPath, entry, disableLogging, _cancellationToken).GetAwaiter().GetResult();
                }
            }
        }

        // NOTE: async method used here to make use of the cancellation token
        // SAFETY: do not call Cmdlet or PSHelper methods from async methods when possible;
        //         some calls, most notable the Cmdlet.Write*() methods will throw exceptions.
        private async Task ExtractZipArchiveFile(string fullPath, ZipArchiveEntry entry, bool disableLogging, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            // Create causes overwrite if the file already exists, this is intentional.
            using (var target = File.Create(fullPath))
            using (var entryStream = entry.Open())
            {
                await entryStream.CopyToAsync(target, BuiltinBufferSize, cancellationToken);

                await target.FlushAsync(cancellationToken);
                if (!disableLogging)
                {
                    _extractedFilesList.AppendLine(fullPath);
                }
            }
        }

        /// <summary>
        /// Helper class for handling decompression of archives with 7-Zip.
        /// This helper relies on 7z.exe being present on the system.
        /// </summary>
        private sealed class SevenZipExtractionHelper : ProcessHandler
        {
            private const string ErrorMessageAddendum = "This is most likely an issue with the '{0}' package and not with Chocolatey itself. Please follow up with the package maintainer(s) directly.";

            private string _destinationFolder = string.Empty;

            /// <summary>
            /// Instantiates a new <see cref="SevenZipExtractionHelper"/>.
            /// </summary>
            /// <param name="cmdlet">The calling cmdlet.</param>
            /// <param name="pipelineStopToken">The calling cmdlet's pipeline stop token. This is used to ensure we correctly dispose of resources when a user cancels an operation.</param>
            /// <param name="extractedFilesList">A stringbuilder to append a log of extracted files to.</param>
            internal SevenZipExtractionHelper(PSCmdlet cmdlet, CancellationToken pipelineStopToken, StringBuilder extractedFilesList)
                : base(cmdlet, pipelineStopToken)
            {
                ProcessOutputReceived += (sender, output) =>
                {
                    if (output.Message.StartsWith("- "))
                    {
                        extractedFilesList.AppendLine(_destinationFolder + '\\' + output.Message.Substring(2));
                    }
                };
            }

            /// <summary>
            /// Run 7-Zip to extract files to the <paramref name="destination"/> directory.
            /// </summary>
            /// <param name="zipFilePath">The path to the archive to extract.</param>
            /// <param name="destination">The destination directory to extract files to.</param>
            /// <param name="filesToExtract">A path or glob pattern to filter the files extracted from the archive.</param>
            /// <param name="disableLogging">Whether to write a log of the files extracted from the archive.</param>
            /// <returns>The destination path where the files were extracted to.</returns>
            /// <exception cref="InvalidOperationException">Thrown if the 7-Zip executable cannot be found.</exception>
            /// <exception cref="SevenZipException">Thrown if the 7-Zip executable returns a non-zero error code.</exception>
            /// <exception cref="System.ComponentModel.Win32Exception">Thrown if the 7-Zip executable cannot be opened.</exception>
            internal void ExtractFiles(string zipFilePath, string destination, string filesToExtract, bool disableLogging)
            {
                var exePath = PSHelper.CombinePaths(Cmdlet, PSHelper.GetInstallLocation(Cmdlet), "tools", "7z.exe");

                if (!PSHelper.ItemExists(Cmdlet, exePath))
                {
                    EnvironmentHelper.UpdateSession(Cmdlet);
                    exePath = PSHelper.CombinePaths(Cmdlet, EnvironmentHelper.GetVariable(SystemVariables.ChocolateyInstall), "tools", "7zip.exe");
                }

                exePath = PSHelper.GetUnresolvedPath(Cmdlet, exePath);

                if (!PSHelper.ItemExists(Cmdlet, exePath))
                {
                    throw new InvalidOperationException("Could not locate the 7z.exe or 7zip.exe executables.");
                }

                Cmdlet.WriteDebug($"7zip found at '{exePath}'");

                // 32-bit 7z would not find C:\Windows\System32\config\systemprofile\AppData\Local\Temp,
                // because it gets translated to C:\Windows\SysWOW64\... by the WOW redirection layer.
                // Replace System32 with sysnative, which does not get redirected.
                // 32-bit 7z is required so it can see both architectures
                if (ArchitectureWidth.Matches(64))
                {
                    var systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
                    var sysNativePath = PSHelper.CombinePaths(Cmdlet, EnvironmentHelper.GetVariable("SystemRoot"), "SysNative");
                    zipFilePath = PSHelper.Replace(zipFilePath, Regex.Escape(systemPath), sysNativePath);
                    destination = PSHelper.Replace(destination, Regex.Escape(systemPath), sysNativePath);
                }

                var workingDirectory = PSHelper.GetCurrentDirectory(Cmdlet);
                if (string.IsNullOrEmpty(workingDirectory))
                {
                    Cmdlet.WriteDebug("Unable to use current location for Working Directory. Using Cache Location instead.");
                    workingDirectory = EnvironmentHelper.GetVariable("TEMP");
                }

                var loggingOption = disableLogging ? "-bb0" : "-bb1";

                var options = $"x -aoa -bd {loggingOption} -o\"{destination}\" -y \"{zipFilePath}\"";
                if (!string.IsNullOrEmpty(filesToExtract))
                {
                    options += $" \"{filesToExtract}\"";
                }

                Cmdlet.WriteDebug($"Executing command ['{exePath}' {options}]");

                _destinationFolder = destination;

                var exitCode = Run(exePath, workingDirectory, options, sensitiveStatements: null, elevated: false, ProcessWindowStyle.Hidden, noNewWindow: true);

                PSHelper.SetExitCode(Cmdlet, exitCode);

                Cmdlet.WriteDebug($"7z exit code: {exitCode}");

                if (exitCode == 0)
                {
                    return;
                }

                var error = GetExitCodeException(exitCode);
                var disclaimer = string.Format(ErrorMessageAddendum, EnvironmentHelper.GetVariable(PackageVariables.ChocolateyPackageName));

                throw new SevenZipException($"{error.Message} {disclaimer}", error);
            }

            private Exception GetExitCodeException(int exitCode)
            {
                switch (exitCode)
                {
                    case 1:
                        return new ApplicationFailedException($"Some files could not be extracted. (Code {exitCode})");
                    case 2:
                        return new ApplicationException($"7-Zip encountered a fatal error while extracting the files. (Code {exitCode})");
                    case 7:
                        return new ArgumentException($"7-Zip command line error. (Code {exitCode})");
                    case 8:
                        return new OutOfMemoryException($"7-Zip exited with an out of memory error. (Code {exitCode})");
                    case 255:
                        return new OperationCanceledException($"7-Zip extraction was cancelled by the user. (Code {exitCode})");
                    default:
                        return new Exception($"7-Zip exited with an unknown error. (Code {exitCode})");
                };
            }
        }
    }
}
