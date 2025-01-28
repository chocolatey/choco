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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using Chocolatey.PowerShell.Shared;
using static Chocolatey.PowerShell.Helpers.PSHelper;

namespace Chocolatey.PowerShell.Helpers
{
    /// <summary>
    /// Helper class to validate checksums. Used by <see cref="Commands.AssertValidChecksumCommand"/>, and any other commands that need to validate checksums.
    /// </summary>
    public static class ChecksumValidator
    {
        /// <summary>
        /// Tests whether a given <paramref name="checksum"/> matches the checksum of a given file.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="path">The path to the file to verify the checksum of.</param>
        /// <param name="checksum">The checksum value to validate against.</param>
        /// <param name="checksumType">The type of the checksum.</param>
        /// <param name="url">The original url that the file was downloaded from, if any.</param>
        /// <param name="error">If this method returns false, this will contain an exception that can be raised if needed.</param>
        /// <returns>True if the actual checksum of the file matches the given checksum, otherwise False.</returns>
        public static bool IsValid(PSCmdlet cmdlet, string path, string checksum, ChecksumType? checksumType, string url, out Exception error)
        {
            if (checksumType is null)
            {
                checksumType = ChecksumType.Md5;
            }

            if (IsEqual(Environment.GetEnvironmentVariable(EnvironmentVariables.ChocolateyIgnoreChecksums), "true"))
            {
                cmdlet.WriteWarning("Ignoring checksums due to feature checksumFiles turned off or option --ignore-checksums set.");
                error = null;
                return true;
            }

            if (string.IsNullOrWhiteSpace(checksum))
            {
                if (IsEqual(Environment.GetEnvironmentVariable(EnvironmentVariables.ChocolateyAllowEmptyChecksums), "true"))
                {
                    cmdlet.WriteDebug("Empty checksums are allowed due to allowEmptyChecksums feature or option.");
                    error = null;
                    return true;
                }

                var isHttpsUrl = !string.IsNullOrEmpty(url) && url.ToLower().StartsWith("https");

                if (isHttpsUrl && IsEqual(Environment.GetEnvironmentVariable(EnvironmentVariables.ChocolateyAllowEmptyChecksumsSecure), "true"))
                {
                    cmdlet.WriteDebug("Download from HTTPS source with feature 'allowEmptyChecksumsSecure' enabled.");
                    error = null;
                    return true;
                }

                cmdlet.WriteWarning("Missing package checksums are not allowed (by default for HTTP/FTP, \n HTTPS when feature 'allowEmptyChecksumsSecure' is disabled) for \n safety and security reasons. Although we strongly advise against it, \n if you need this functionality, please set the feature \n 'allowEmptyChecksums' ('choco feature enable -n \n allowEmptyChecksums') \n or pass in the option '--allow-empty-checksums'. You can also pass \n checksums at runtime (recommended). See `choco install -?` for details.");
                cmdlet.WriteDebug("If you are a maintainer attempting to determine the checksum for packaging purposes, please run \n 'choco install checksum' and run 'checksum -t sha256 -f $file' \n Ensure you do this for all remote resources.");

                if (GetPSVersion().Major >= 4)
                {
                    cmdlet.WriteDebug("Because you are running PowerShell with a major version of v4 or greater, you could also opt to run \n '(Get-FileHash -Path $file -Algorithm SHA256).Hash' \n rather than install a separate tool.");
                }

                if (IsEqual(Environment.GetEnvironmentVariable(EnvironmentVariables.ChocolateyPowerShellHost), "true")
                    && !(cmdlet.Host is null))
                {
                    const string prompt = "Do you wish to allow the install to continue (not recommended)?";
                    var info = string.Format(
                        "The integrity of the file '{0}'{1} has not been verified by a checksum in the package scripts",
                        GetFileName(path),
                        string.IsNullOrWhiteSpace(url) ? string.Empty : $" from '{url}'");

                    var choices = new Collection<ChoiceDescription>
                    {
                        new ChoiceDescription("&Yes"),
                        new ChoiceDescription("&No"),
                    };

                    var selection = cmdlet.Host.UI.PromptForChoice(info, prompt, choices, defaultChoice: 1);

                    if (selection == 0)
                    {
                        error = null;
                        return true;
                    }
                }

                var errorMessage = isHttpsUrl
                    ? "This package downloads over HTTPS but does not yet have package checksums to verify the package. We recommend asking the maintainer to add checksums to this package. In the meantime if you need this package to work correctly, please enable the feature allowEmptyChecksumsSecure, provide the runtime switch '--allow-empty-checksums-secure', or pass in checksums at runtime (recommended - see 'choco install -?' / 'choco upgrade -?' for details)."
                    : "Empty checksums are no longer allowed by default for non-secure sources. Please ask the maintainer to add checksums to this package. In the meantime if you need this package to work correctly, please enable the feature allowEmptyChecksums, provide the runtime switch '--allow-empty-checksums', or pass in checksums at runtime (recommended - see 'choco install -?' / 'choco upgrade -?' for details). It is strongly advised against allowing empty checksums for non-internal HTTP/FTP sources.";

                error = new ChecksumMissingException(errorMessage);
                return false;
            }

            if (!FileExists(cmdlet, path))
            {
                error = new FileNotFoundException($"Unable to checksum a file that doesn't exist - Could not find file '{path}'", path);
                return false;
            }

            var checksumExe = CombinePaths(cmdlet, GetInstallLocation(cmdlet), "tools", "checksum.exe");
            if (!FileExists(cmdlet, checksumExe))
            {
                error = new FileNotFoundException("Unable to locate 'checksum.exe', your Chocolatey installation may be incomplete or damaged. Try reinstalling chocolatey with 'choco install chocolatey --force'.", checksumExe);
                return false;
            }

            cmdlet.WriteDebug($"checksum.exe found at '{checksumExe}'");
            var arguments = string.Format(
                "-c=\"{0}\" -t=\"{1}\" -f=\"{2}\"",
                checksum,
                checksumType.ToString().ToLower(),
                path);

            cmdlet.WriteDebug($"Executing command ['{checksumExe}' {arguments}]");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo(checksumExe, arguments)
                {
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                },
            };

            process.Start();
            process.WaitForExit();

            var exitCode = process.ExitCode;
            process.Dispose();

            cmdlet.WriteDebug($"Command ['{checksumExe}' {arguments}] exited with '{exitCode}'");

            if (exitCode != 0)
            {
                error = new ChecksumVerificationFailedException($"Checksum for '{path}' did not match '{checksum}' for checksum type '{checksumType}'. Consider passing the actual checksums through with `--checksum --checksum64` once you validate the checksums are appropriate. A less secure option is to pass `--ignore-checksums` if necessary.", checksum, path);
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Validate the checksum of a file against an expected <paramref name="checksum"/> and throw if the checksum does not match.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="path">The path to the file to verify the checksum for.</param>
        /// <param name="checksum">The expected checksum value.</param>
        /// <param name="checksumType">The type of the checksum to look for.</param>
        /// <param name="url">The url the file was downloaded from originally, if any.</param>
        public static void AssertChecksumValid(PSCmdlet cmdlet, string path, string checksum, ChecksumType? checksumType, string url)
        {
            if (!IsValid(cmdlet, path, checksum, checksumType, url, out var exception))
            {
                throw exception;
            }
        }
    }
}
