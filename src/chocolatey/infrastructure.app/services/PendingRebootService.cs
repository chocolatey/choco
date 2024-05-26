﻿// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

using chocolatey.infrastructure.app.configuration;
using Microsoft.Win32;
using chocolatey.infrastructure.platforms;
using System;

namespace chocolatey.infrastructure.app.services
{
    /// <summary>
    ///   Service to check for System level pending reboot request
    /// </summary>
    /// <remarks>Based on code from https://github.com/puppetlabs/puppetlabs-reboot</remarks>
    public class PendingRebootService : IPendingRebootService
    {
        private readonly IRegistryService _registryService;

        public PendingRebootService(IRegistryService registryService)
        {
            _registryService = registryService;
        }

        /// <summary>
        ///   Test to see if there are any known situations that require a system reboot.
        /// </summary>
        /// <param name="config">The current Chocolatey Configuration</param>
        /// <returns><c>true</c> if reboot is required; otherwise <c>false</c>.</returns>
        public bool IsRebootPending(ChocolateyConfiguration config)
        {
            if (config.Information.PlatformType != PlatformType.Windows)
            {
                return false;
            }

            this.Log().Debug(" Reboot Requirement Checks:");

            // note this is short-circuited, if one trips, it won't continue the checks
            return IsPendingComputerRename() ||
                   IsPendingComponentBasedServicing() ||
                   IsPendingWindowsAutoUpdate() ||
                   IsPendingFileRenameOperation() ||
                   IsPendingPackageInstaller() ||
                   IsPendingPackageInstallerSysWow64();
        }

        /// <summary>
        /// Determines whether Windows is waiting on a reboot to rename the computer.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is pending computer rename]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsPendingComputerRename()
        {
            var path = "SYSTEM\\CurrentControlSet\\Control\\ComputerName\\{0}";
            var activeName = GetRegistryKeyString(path.FormatWith("ActiveComputerName"), "ComputerName");
            var pendingName = GetRegistryKeyString(path.FormatWith("ComputerName"), "ComputerName");

            var result = !string.IsNullOrWhiteSpace(activeName) &&
                          !string.IsNullOrWhiteSpace(pendingName) &&
                          activeName != pendingName;

            this.Log().Debug(" - Pending Computer Rename = {0}".FormatWith(result ? "Flagged" : "Checked"));

            return result;
        }

        /// <summary>
        /// Determines whether Windows is waiting on a reboot for component based servicing.
        /// CBS (Component Based Servicing) is also known as trusted installer. This could also be where
        /// pending reboots related to MSI installs are recorded as of newer versions of Windows.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is pending component based servicing]; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// https://blogs.technet.microsoft.com/askperf/2008/04/23/understanding-component-based-servicing/
        /// </remarks>
        private bool IsPendingComponentBasedServicing()
        {
            if (!IsAtLeastVistaSp1())
            {
                this.Log().Trace("Not using Windows Vista SP1 or earlier, so no check for Component Based Servicing can be made.");
                return false;
            }

            var path = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Component Based Servicing\\RebootPending";
            var key = _registryService.GetKey(RegistryHive.LocalMachine, path);
            var result = key != null;

            this.Log().Debug(" - Pending Component Based Servicing = {0}".FormatWith(result ? "Flagged" : "Checked"));

            return result;
        }

        /// <summary>
        /// Determines whether Windows Automatic Update is waiting on a reboot.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if is pending windows automatic update; otherwise, <c>false</c>.
        /// </returns>
        private bool IsPendingWindowsAutoUpdate()
        {
            var path = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update\\RebootRequired";
            var key = _registryService.GetKey(RegistryHive.LocalMachine, path);
            var result = key != null;

            this.Log().Debug(" - Pending Windows Auto Update = {0}".FormatWith(result ? "Flagged" : "Checked"));

            return result;
        }

        /// <summary>
        /// Determines whether there is a pending file rename operation waiting on reboot for lock releases.
        /// </summary>
        /// <returns>
        ///   <c>false</c>, however, additional information provided in debug log to indicate if it was ignored.
        /// </returns>
        private bool IsPendingFileRenameOperation()
        {
            var path = "SYSTEM\\CurrentControlSet\\Control\\Session Manager";
            var value = GetRegistryKeyValue(path, "PendingFileRenameOperations");

            var result = false;

            if (value != null && value is string[])
            {
                result = (value as string[]).Length != 0;
            }

            this.Log().Debug(" - Pending File Rename Operations = {0}".FormatWith(result ? "Ignored" : "Checked"));

            // Always return false, as we don't want this check to result in a pending reboot warning/error.
            // Instead, this will only provide output in the debug log indicating that the check was ignored.
            return false;
        }

        /// <summary>
        /// Determines whether Windows has a pending software install waiting for reboot (MSIs typically).
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is pending package installer]; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// https://support.microsoft.com/kb/832475
        /// 0x00000000 (0)	No pending restart.
        /// </remarks>
        private bool IsPendingPackageInstaller()
        {
            var path = "SOFTWARE\\Microsoft\\Updates";
            var value = GetRegistryKeyString(path, "UpdateExeVolatile");

            var result = !string.IsNullOrWhiteSpace(value) && value != "0";

            this.Log().Debug(" - Pending Windows Package Installer = {0}".FormatWith(result ? "Flagged" : "Checked"));

            return result;
        }

        /// <summary>
        /// Determines whether Windows has a pending 32-bit software install waiting for reboot (MSIs typically).
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is pending package installer syswow64]; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// https://support.microsoft.com/kb/832475
        /// 0x00000000 (0)	No pending restart.
        /// </remarks>
        private bool IsPendingPackageInstallerSysWow64()
        {
            var path = "SOFTWARE\\Wow6432Node\\Microsoft\\Updates";
            var value = GetRegistryKeyString(path, "UpdateExeVolatile");

            var result = !string.IsNullOrWhiteSpace(value) && value != "0";

            this.Log().Debug(" - Pending Windows Package Installer SysWow64 = {0}".FormatWith(result ? "Flagged" : "Checked"));

            return result;
        }

        private string GetRegistryKeyString(string path, string value)
        {
            var key = _registryService.GetKey(RegistryHive.LocalMachine, path);

            if (key == null)
            {
                return string.Empty;
            }

            return key.GetValue(value, string.Empty).ToStringSafe();
        }

        private object GetRegistryKeyValue(string path, string value)
        {
            var key = _registryService.GetKey(RegistryHive.LocalMachine, path);

            if (key == null)
            {
                return null;
            }

            return key.GetValue(value, null);
        }

        private bool IsAtLeastVistaSp1()
        {
            var versionNumber = Platform.GetVersion();

            this.Log().Trace(" Operating System Version Number: {0}".FormatWith(versionNumber));

            return versionNumber.Build >= 6001;
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public bool is_pending_reboot(ChocolateyConfiguration config)
            => IsRebootPending(config);
#pragma warning restore IDE0022, IDE1006
    }
}
