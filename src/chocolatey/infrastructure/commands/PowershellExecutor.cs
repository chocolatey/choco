// Copyright © 2017 - 2018 Chocolatey Software, Inc
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

        public static int execute(
            string command,
            IFileSystem fileSystem,
            int waitForExitSeconds,
            Action<object, DataReceivedEventArgs> stdOutAction,
            Action<object, DataReceivedEventArgs> stdErrAction
            )
        {
            if (string.IsNullOrWhiteSpace(_powershell)) _powershell = get_powershell_location(fileSystem);
            //-NoProfile -NoLogo -ExecutionPolicy unrestricted -Command "[System.Threading.Thread]::CurrentThread.CurrentCulture = ''; [System.Threading.Thread]::CurrentThread.CurrentUICulture = '';& '%DIR%chocolatey.ps1' %PS_ARGS%"
            string arguments = "-NoProfile -NoLogo -ExecutionPolicy Bypass -Command \"{0}\"".format_with(command);

            return CommandExecutor.execute_static(
                _powershell,
                arguments,
                waitForExitSeconds,
                workingDirectory: fileSystem.get_directory_name(fileSystem.get_current_assembly_path()),
                stdOutAction: stdOutAction,
                stdErrAction: stdErrAction,
                updateProcessPath: true,
                allowUseWindow: _allowUseWindow
                );
        }

        public static string get_powershell_location(IFileSystem fileSystem)
        {
            foreach (var powershellLocation in _powershellLocations)
            {
                if (fileSystem.file_exists(powershellLocation))
                {
                    return powershellLocation;
                }
            }

            throw new FileNotFoundException("Unable to find suitable location for PowerShell. Searched the following locations: '{0}'".format_with(string.Join("; ", _powershellLocations)));
        }
    }
}