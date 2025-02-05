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
using System.Diagnostics;
using System.Linq;
using chocolatey.infrastructure.information;

namespace chocolatey.benchmark.helpers
{
    internal static class ManagedProcessHelper
    {
        private static readonly string[] _filteredParents = new[]
        {
            "explorer",
            "powershell",
            "pwsh",
            "cmd",
            "bash",
            // The name used to launch windows services
            // in the operating system.
            "services",
            // Known Terminal Emulators
            "Tabby",
            "WindowsTerminal",
            "FireCMD",
            "ConEmu64",
            "ConEmuC64"
        };

        public static ProcessTree GetProcessTree(Process process = null)
        {
            if (process == null)
            {
                process = Process.GetCurrentProcess();
            }

            var tree = new ProcessTree(process.ProcessName);

            Process nextProcess = null;

            while (true)
            {
                var processId = nextProcess?.Id ?? process.Id;
                var processName = FindIndexedProcessName(processId);

                if (string.IsNullOrEmpty(processName))
                {
                    break;
                }

                var foundProcess = FindPidFromIndexedProcessName(processName);

                if (foundProcess == null)
                {
                    break;
                }

                nextProcess = foundProcess;
                tree.Processes.AddLast(nextProcess.ProcessName);
            }

            return tree;
        }

        public static string GetParent(Process process = null)
        {
            if (process == null)
            {
                process = Process.GetCurrentProcess();
            }

            Process nextProcess = null;

            while (true)
            {
                var processId = nextProcess?.Id ?? process.Id;

                var processName = FindIndexedProcessName(processId);

                if (string.IsNullOrEmpty(processName))
                {
                    break;
                }

                var foundProcess = FindPidFromIndexedProcessName(processName);

                if (foundProcess == null)
                {
                    break;
                }

                nextProcess = foundProcess;
            }

            return nextProcess?.ProcessName;
        }

        public static string GetParentFiltered(Process process = null)
        {
            if (process == null)
            {
                process = Process.GetCurrentProcess();
            }

            Process nextProcess = null;
            Process selectedProcess = null;

            while (true)
            {
                var processId = nextProcess?.Id ?? process.Id;

                var processName = FindIndexedProcessName(processId);

                if (string.IsNullOrEmpty(processName))
                {
                    break;
                }

                var foundProcess = FindPidFromIndexedProcessName(processName);

                if (foundProcess == null)
                {
                    break;
                }

                nextProcess = foundProcess;

                if (!IsIgnoredParent(nextProcess.ProcessName))
                {
                    selectedProcess = nextProcess;
                }
            }

            return selectedProcess?.ProcessName;
        }

        private static bool IsIgnoredParent(string processName)
        {
            return _filteredParents.Contains(processName, StringComparer.OrdinalIgnoreCase);
        }

        private static Process FindPidFromIndexedProcessName(string indexedProcessName)
        {
            try
            {
                var parentId = new PerformanceCounter("Process", "Creating Process ID", indexedProcessName);
                return Process.GetProcessById((int)parentId.NextValue());
            }
            catch
            {
                return null;
            }
        }

        private static string FindIndexedProcessName(int pid)
        {
            var processName = Process.GetProcessById(pid).ProcessName;
            var processByName = Process.GetProcessesByName(processName);
            string processIndexedName = null;

            for (var i = 0; i < processByName.Length; i++)
            {
                try
                {
                    processIndexedName = i == 0 ? processName : processName + "#" + i;
                    var processId = new PerformanceCounter("Process", "ID Process", processIndexedName);

                    if ((int)processId.NextValue() == pid)
                    {
                        return processIndexedName;
                    }
                }
                catch
                {
                    // Empty on purpose
                }
            }

            return processIndexedName;
        }
    }
}
