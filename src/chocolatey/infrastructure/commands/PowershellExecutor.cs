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

namespace chocolatey.infrastructure.commands
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using adapters;
    using filesystem;
    using Environment = System.Environment;

    public sealed class PowershellExecutor
    {
        private static bool _allowUseWindow = true;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool AllowUseWindow
        {
            get { return _allowUseWindow; }
            set { _allowUseWindow = value; }
        }

        private static readonly IList<string> _powershellLocations = new List<string>
            {
                Environment.ExpandEnvironmentVariables("%systemroot%\\SysNative\\WindowsPowerShell\\v1.0\\powershell.exe"),
                Environment.ExpandEnvironmentVariables("%systemroot%\\System32\\WindowsPowerShell\\v1.0\\powershell.exe"),
                "powershell.exe"
            };

        private static string _powershell = string.Empty;

        public static int Execute(
            string command,
            IFileSystem fileSystem,
            int waitForExitSeconds,
            Action<object, DataReceivedEventArgs> stdOutAction,
            Action<object, DataReceivedEventArgs> stdErrAction
            )
        {
            if (string.IsNullOrWhiteSpace(_powershell)) _powershell = GetPowerShellLocation(fileSystem);
            //-NoProfile -NoLogo -ExecutionPolicy unrestricted -Command "[System.Threading.Thread]::CurrentThread.CurrentCulture = ''; [System.Threading.Thread]::CurrentThread.CurrentUICulture = '';& '%DIR%chocolatey.ps1' %PS_ARGS%"
            string arguments = "-NoProfile -NoLogo -ExecutionPolicy Bypass -Command \"{0}\"".FormatWith(command);

            return CommandExecutor.ExecuteStatic(
                _powershell,
                arguments,
                waitForExitSeconds,
                workingDirectory: fileSystem.GetDirectoryName(fileSystem.GetCurrentAssemblyPath()),
                stdOutAction: stdOutAction,
                stdErrAction: stdErrAction,
                updateProcessPath: true,
                allowUseWindow: _allowUseWindow
                );
        }

        public static string GetPowerShellLocation(IFileSystem fileSystem)
        {
            foreach (var powershellLocation in _powershellLocations)
            {
                if (fileSystem.FileExists(powershellLocation))
                {
                    return powershellLocation;
                }
            }

            throw new FileNotFoundException("Unable to find suitable location for PowerShell. Searched the following locations: '{0}'".FormatWith(string.Join("; ", _powershellLocations)));
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static int execute(
            string command,
            IFileSystem fileSystem,
            int waitForExitSeconds,
            Action<object, DataReceivedEventArgs> stdOutAction,
            Action<object, DataReceivedEventArgs> stdErrAction)
            => Execute(command, fileSystem, waitForExitSeconds, stdOutAction, stdErrAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static string get_powershell_location(IFileSystem fileSystem)
            => GetPowerShellLocation(fileSystem);
#pragma warning restore IDE1006
    }
}
