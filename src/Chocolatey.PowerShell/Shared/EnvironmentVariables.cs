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

using System.ComponentModel;

namespace Chocolatey.PowerShell.Shared
{
    public static class EnvironmentVariables
    {
        public const string ChocolateyLastPathUpdate = "ChocolateyLastPathUpdate";
        public const string ComputerName = "COMPUTERNAME";
        public const string Path = "PATH";
        public const string ProcessorArchitecture = "PROCESSOR_ARCHITECTURE";
        public const string PSModulePath = "PSModulePath";
        public const string System = "SYSTEM";
        public const string SystemRoot = "SystemRoot";
        public const string Username = "USERNAME";

        /// <summary>
        /// The location of the current Chocolatey installation; typically defaults to <c>C:\ProgramData\chocolatey</c>.
        /// </summary>
        public const string ChocolateyInstall = nameof(ChocolateyInstall);

        /// <summary>
        /// When this environment variable is set to 'true', we are running under Chocolatey's built-in PowerShell host.
        /// Typically set when running under Chocolatey with the <c>powershellHost</c> feature enabled, if the <c>--use-system-powershell</c> flag is not provided.
        /// </summary>
        public const string ChocolateyPowerShellHost = nameof(ChocolateyPowerShellHost);

        /// <summary>
        /// When this environment variable is set to 'true', checksum validation is skipped.
        /// Typically set when running under Chocolatey if <c>--ignore-checksums</c> is passed or the feature <c>checksumFiles</c> is turned off.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        public const string ChocolateyIgnoreChecksums = nameof(ChocolateyIgnoreChecksums);

        /// <summary>
        /// When this environment variable is set to 'true', an empty checksum is treated as valid.
        /// Typically set when running under Chocolatey if <c>--allow-empty-checksums</c> is passed or the feature <c>allowEmptyChecksums</c> is turned on.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        public const string ChocolateyAllowEmptyChecksums = nameof(ChocolateyAllowEmptyChecksums);

        /// <summary>
        /// When this environment variable is set to 'true', an empty checksum is treated as valid for files downloaded from HTTPS URLs.
        /// Typically set when running under Chocolatey if <c>--allow-empty-checksums-secure</c> is passed or the feature <c>allowEmptyChecksumsSecure</c> is turned on.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        public const string ChocolateyAllowEmptyChecksumsSecure = nameof(ChocolateyAllowEmptyChecksumsSecure);

    }
}
