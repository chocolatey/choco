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
using System.Linq;
using System.Management.Automation;
using Chocolatey.PowerShell.Shared;
using Chocolatey.PowerShell.Win32;
using Microsoft.Win32;

namespace Chocolatey.PowerShell.Helpers
{
    public static class EnvironmentHelper
    {
        private const string MachineEnvironmentRegistryKeyName = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment\";
        private const string UserEnvironmentRegistryKeyName = "Environment";

        /// <summary>
        /// Get an environment variable from the current process scope by name.
        /// </summary>
        /// <param name="name">The name of the variable to retrieve.</param>
        /// <returns>The value of the environment variable.</returns>
        public static string GetVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }

        /// <summary>
        /// Gets the value of the environment variable with the target <paramref name="name"/>, expanding environment names that may be present in the value.
        /// </summary>
        /// <param name="cmdlet">The cmdlet running the method.</param>
        /// <param name="name">The name of the environment variable to retrieve.</param>
        /// <param name="scope">The scope to look in for the environment variable.</param>
        /// <returns>The value of the environment variable as a string.</returns>
        public static string GetVariable(PSCmdlet cmdlet, string name, EnvironmentVariableTarget scope)
        { 
            return GetVariable(cmdlet, name, scope, preserveVariables: false);
        }

        /// <summary>
        /// Gets the value of the environment variable with the target <paramref name="name"/>.
        /// </summary>
        /// <param name="cmdlet">The cmdlet running the method.</param>
        /// <param name="name">The name of the environment variable to retrieve.</param>
        /// <param name="scope">The scope to look in for the environment variable.</param>
        /// <param name="preserveVariables">Whether to preserve environment variable names in the retrieved value. If false, environment names will be expanded.</param>
        /// <returns>The value of the environment variable as a string.</returns>
        public static string GetVariable(PSCmdlet cmdlet, string name, EnvironmentVariableTarget scope, bool preserveVariables)
        {
            if (scope == EnvironmentVariableTarget.Process)
            {
                return GetVariable(name);
            }

            var value = string.Empty;

            try
            {
                using (var registryKey = GetEnvironmentKey(scope))
                {
                    var options = preserveVariables ? RegistryValueOptions.DoNotExpandEnvironmentNames : RegistryValueOptions.None;
                    if (!(registryKey is null))
                    {
                        value = (string)registryKey.GetValue(name, string.Empty, options);
                    }
                }
            }
            catch (Exception error)
            {
                cmdlet.WriteDebug($"Unable to retrieve the {name} environment variable. Details: {error.Message}");
            }

            if (string.IsNullOrEmpty(value))
            {
                value = Environment.GetEnvironmentVariable(name, scope);
            }

            return value ?? string.Empty;
        }

        /// <summary>
        /// Gets the registry key associated with the targeted scope of Environment variables.
        /// </summary>
        /// <param name="scope">The scope of the environment variables to look up.</param>
        /// <returns>The registry key associated with the targeted <paramref name="scope"/> of environment variables.</returns>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="scope"/> is not <see cref="EnvironmentVariableTarget.User"/> or <see cref="EnvironmentVariableTarget.Machine"/>.</exception>
        private static RegistryKey GetEnvironmentKey(EnvironmentVariableTarget scope, bool writable = false)
        {
            switch (scope)
            {
                case EnvironmentVariableTarget.User:
                    return Registry.CurrentUser.OpenSubKey(UserEnvironmentRegistryKeyName, writable);
                case EnvironmentVariableTarget.Machine:
                    return Registry.LocalMachine.OpenSubKey(MachineEnvironmentRegistryKeyName, writable);
                default:
                    throw new NotSupportedException($"The environment variable scope value '{scope}' is not supported.");
            }
        }


        /// <summary>
        /// Gets the list of environment variables in the specified <paramref name="scope"/>.
        /// </summary>
        /// <param name="scope">The scope to lookup environment variable names in.</param>
        /// <returns></returns>
        public static string[] GetVariableNames(EnvironmentVariableTarget scope)
        {
            if (scope == EnvironmentVariableTarget.Process)
            {
                return Environment.GetEnvironmentVariables().Keys.Cast<string>().ToArray();
            }

            try
            {
                using (var registryKey = GetEnvironmentKey(scope))
                {
                    return registryKey.GetValueNames();
                }
            }
            catch
            {
                // HKCU:\Environment may not exist in all Windows OSes
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Sets the value of an environment variable for the current process only.
        /// </summary>
        /// <param name="name">The name of the environment variable to set.</param>
        /// <param name="value">The value to set the environment variable to.</param>
        public static void SetVariable(string name, string value)
        {
            Environment.SetEnvironmentVariable(name, value);
        }

        /// <summary>
        /// Sets the value of an environment variable at the target <paramref name="scope"/>, and updates the current session environment.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="name">The name of the environment variable to set.</param>
        /// <param name="scope">The scope to set the environment variable in.</param>
        /// <param name="value">The value to set the environment variable to.</param>
        public static void SetVariable(PSCmdlet cmdlet, string name, EnvironmentVariableTarget scope, string value)
        {
            if (scope == EnvironmentVariableTarget.Process)
            {
                SetVariable(name, value);
                return;
            }

            using (var registryKey = GetEnvironmentKey(scope, writable: true))
            {
                var registryType = RegistryValueKind.String;

                try
                {
                    if (registryKey.GetValueNames().Contains(name))
                    {
                        registryType = registryKey.GetValueKind(name);
                    }
                }
                catch
                {
                    // The value doesn't exist yet, suppress the error.
                }

                if (name.ToUpper() == EnvironmentVariables.Path)
                {
                    registryType = RegistryValueKind.ExpandString;
                }

                cmdlet.WriteDebug($"Registry type for {name} is/will be {registryType}");

                if (string.IsNullOrEmpty(value))
                {
                    registryKey.DeleteValue(name, throwOnMissingValue: false);
                }
                else
                {
                    registryKey.SetValue(name, value, registryType);
                }
            }

            try
            {
                // Trigger environment refresh in explorer.exe:
                // 1. Notify all windows of environment block change
                NativeMethods.SendMessageTimeout(
                    hWnd: (IntPtr)NativeMethods.HWND_BROADCAST,
                    Msg: NativeMethods.WM_SETTINGCHANGE,
                    wParam: UIntPtr.Zero,
                    lParam: "Environment",
                    fuFlags: 2,
                    uTimeout: 5000,
                    out UIntPtr result);

                // 2. Set a user environment variable making the system refresh
                var setxPath = string.Format(@"{0}\System32\setx.exe", GetVariable(cmdlet, EnvironmentVariables.SystemRoot, EnvironmentVariableTarget.Process));
                cmdlet.InvokeCommand.InvokeScript($"& \"{setxPath}\" {EnvironmentVariables.ChocolateyLastPathUpdate} \"{DateTime.Now.ToFileTime()}\"");
            }
            catch (Exception error)
            {
                cmdlet.WriteWarning($"Failure attempting to let Explorer know about updated environment settings.\n  {error.Message}");
            }

            UpdateSession(cmdlet);
        }

        /// <summary>
        /// Updates the current session environment, ensuring environment changes are reflected in the current session.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        public static void UpdateSession(PSCmdlet cmdlet)
        {
            var userName = GetVariable(cmdlet, EnvironmentVariables.Username, EnvironmentVariableTarget.Process);
            var architecture = GetVariable(cmdlet, EnvironmentVariables.ProcessorArchitecture, EnvironmentVariableTarget.Process);
            var psModulePath = GetVariable(cmdlet, EnvironmentVariables.PSModulePath, EnvironmentVariableTarget.Process);

            var scopeList = new List<EnvironmentVariableTarget>() { EnvironmentVariableTarget.Process, EnvironmentVariableTarget.Machine };

            var computerName = GetVariable(cmdlet, EnvironmentVariables.ComputerName, EnvironmentVariableTarget.Process);

            // User scope should override (be checked after) machine scope, but only if we're not running as SYSTEM
            if (userName != computerName && userName != EnvironmentVariables.System)
            {
                scopeList.Add(EnvironmentVariableTarget.User);
            }

            foreach (var scope in scopeList)
            {
                foreach (var name in GetVariableNames(scope))
                {
                    var value = GetVariable(cmdlet, name, scope);
                    if (!string.IsNullOrEmpty(value))
                    {
                        SetVariable(cmdlet, name, EnvironmentVariableTarget.Process, value);
                    }
                }
            }

            // Update PATH, combining both scopes' values.
            var paths = new string[2];
            paths[0] = GetVariable(cmdlet, EnvironmentVariables.Path, EnvironmentVariableTarget.Machine);
            paths[1] = GetVariable(cmdlet, EnvironmentVariables.Path, EnvironmentVariableTarget.User);

            SetVariable(cmdlet, EnvironmentVariables.Path, EnvironmentVariableTarget.Process, string.Join(";", paths));

            // Preserve PSModulePath as it's almost always updated by process, preserve it
            SetVariable(cmdlet, EnvironmentVariables.PSModulePath, EnvironmentVariableTarget.Process, psModulePath);

            // Preserve user and architecture
            if (!string.IsNullOrEmpty(userName))
            {
                SetVariable(cmdlet, EnvironmentVariables.Username, EnvironmentVariableTarget.Process, userName);
            }

            if (!string.IsNullOrEmpty(architecture))
            {
                SetVariable(cmdlet, EnvironmentVariables.ProcessorArchitecture, EnvironmentVariableTarget.Process, architecture);
            }
        }
    }
}
