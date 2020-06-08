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
    ///   InstallShield Installer Options
    /// </summary>
    /// <remarks>
    ///   http://helpnet.installshield.com/installshield18helplib/ihelpsetup_execmdline.htm
    /// </remarks>
    public class InstallShieldInstaller : InstallerBase
    {
        public InstallShieldInstaller()
        {
            InstallExecutable = "\"{0}\"".format_with(InstallTokens.INSTALLER_LOCATION);
            SilentInstall = "/s /v\"/qn\"";
            NoReboot = "/v\"REBOOT=ReallySuppress\"";
            LogFile = "/f2\"{0}\\MSI.Install.log\"".format_with(InstallTokens.PACKAGE_LOCATION);
            CustomInstallLocation = "/v\"INSTALLDIR=\\\"{0}\\\"".format_with(InstallTokens.CUSTOM_INSTALL_LOCATION);
            Language = "/l\"{0}\"".format_with(InstallTokens.LANGUAGE);
            OtherInstallOptions = "/sms"; // pause
            UninstallExecutable = "\"{0}\"".format_with(InstallTokens.UNINSTALLER_LOCATION);
            SilentUninstall = "/uninst /s";
            OtherUninstallOptions = "/sms";
            // http://helpnet.installshield.com/installshield18helplib/IHelpSetup_EXEErrors.htm
            ValidInstallExitCodes = new List<long> { 0, 1641, 3010 };
            ValidUninstallExitCodes = new List<long> { 0, 1605, 1614, 1641, 3010 };
        }

        public override InstallerType InstallerType
        {
            get { return InstallerType.InstallShield; }
        }
    }
}