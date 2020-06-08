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
            NoReboot = "/NORESTART /RESTARTEXITCODE=3010";
            LogFile = "/LOG=\"{0}\\InnoSetup.Install.log\"".format_with(InstallTokens.PACKAGE_LOCATION);
            CustomInstallLocation = "/DIR=\"{0}\"".format_with(InstallTokens.CUSTOM_INSTALL_LOCATION);
            Language = "/LANG={0}".format_with(InstallTokens.LANGUAGE);
            OtherInstallOptions = "/SP- /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS /FORCECLOSEAPPLICATIONS /NOICONS";
            UninstallExecutable = "\"{0}\"".format_with(InstallTokens.UNINSTALLER_LOCATION);
            SilentUninstall = "/VERYSILENT";
            OtherUninstallOptions = "/SUPPRESSMSGBOXES";
            // http://www.jrsoftware.org/ishelp/index.php?topic=setupexitcodes
            ValidInstallExitCodes = new List<long> { 0, 3010 };
            ValidUninstallExitCodes = new List<long> { 0 };
        }

        public override InstallerType InstallerType
        {
            get { return InstallerType.InnoSetup; }
        }
    }
}