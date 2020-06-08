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

namespace chocolatey.infrastructure.app.domain.installers
{
    using System.Collections.Generic;

    /// <summary>
    ///   Windows Update Installer (msu)
    /// </summary>
    /// <remarks>
    ///   https://support.microsoft.com/en-us/kb/934307
    /// </remarks>
    public class WindowsUpdateInstaller : InstallerBase
    {
        public WindowsUpdateInstaller()
        {
            //todo: fully qualify wusa
            InstallExecutable = "wusa.exe";
            SilentInstall = "\"{0}\" /quiet".format_with(InstallTokens.INSTALLER_LOCATION);
            NoReboot = "/norestart";
            LogFile = "/log:\"{0}\\MSP_install_log.evtx\"".format_with(InstallTokens.PACKAGE_LOCATION);
            CustomInstallLocation = "";
            Language = "";
            OtherInstallOptions = "";
            UninstallExecutable = "wusa.exe";
            SilentUninstall = "\"{0}\" /quiet".format_with(InstallTokens.UNINSTALLER_LOCATION);
            OtherUninstallOptions = "";
            // https://msdn.microsoft.com/en-us/library/aa376931.aspx
            // https://support.microsoft.com/en-us/kb/290158
            // https://msdn.microsoft.com/en-us/library/windows/desktop/hh968413%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396
            ValidInstallExitCodes = new List<long>
            {
                0,
                1641,
                3010,
                2359301,
                2359302,
                2149842956
            };
            // we allow unknown 1605/1614 b/c it may have already been uninstalled 
            // and that's okay
            ValidUninstallExitCodes = new List<long>
            {
                0,
                1605,
                1614,
                1641,
                3010,
                2359301,
                2359303,
                2149842956
            };
        }

        public override InstallerType InstallerType
        {
            get { return InstallerType.HotfixOrSecurityUpdate; }
        }
    }
}
