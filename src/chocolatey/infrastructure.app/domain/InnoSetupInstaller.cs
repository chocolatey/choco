// Copyright © 2011 - Present RealDimensions Software, LLC
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

namespace chocolatey.infrastructure.app.domain
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   InnoSetup Installer Options
    /// </summary>
    /// <remarks>
    ///   http://www.jrsoftware.org/ishelp/index.php?topic=setupcmdline
    /// </remarks>
    public class InnoSetupInstaller : InstallerBase
    {
        public InnoSetupInstaller()
        {
            InstallExecutable = "\"{0}\" ".format_with(InstallTokens.INSTALLER_LOCATION);
            SilentInstall = "/VERYSILENT";
            NoReboot = "/NORESTART";
            LogFile = "/LOG=\"{0}\\InnoSetup.Install.log\"".format_with(InstallTokens.PACKAGE_LOCATION);
            CustomInstallLocation = "/DIR=\"{0}\"".format_with(InstallTokens.CUSTOM_INSTALL_LOCATION);
            Language = "/LANG={0}".format_with(InstallTokens.LANGUAGE);
            OtherInstallOptions = "/SP- /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS /NOICONS";
            UninstallExecutable = "\"{0}\"".format_with(InstallTokens.UNINSTALLER_LOCATION);
            SilentUninstall = "/VERYSILENT";
            OtherUninstallOptions = "/SUPPRESSMSGBOXES";
            // http://www.jrsoftware.org/ishelp/index.php?topic=setupexitcodes
            ValidInstallExitCodes = new List<int> { 0 };
            ValidUninstallExitCodes = new List<int> { 0 };
        }

        public override InstallerType InstallerType
        {
            get { return InstallerType.InnoSetup; }
        }
    }
}