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
    ///   QT Installer Options
    /// </summary>
    /// <remarks>
    ///   https://doc.qt.io/qtinstallerframework/index.html
    ///   http://doc.qt.io/qtinstallerframework/operations.html
    /// </remarks>
    public class QtInstaller : InstallerBase
    {
        public QtInstaller()
        {
            InstallExecutable = "\"{0}\"".format_with(InstallTokens.INSTALLER_LOCATION);
            SilentInstall = "";
            NoReboot = "";
            LogFile = "";
            //http://doc.qt.io/qtinstallerframework/ifw-globalconfig.html
            //CustomInstallLocation = "targetdir={0}".format_with(InstallTokens.CUSTOM_INSTALL_LOCATION);
            CustomInstallLocation = "";
            Language = "";
            OtherInstallOptions = "";
            UninstallExecutable = "\"{0}\"".format_with(InstallTokens.UNINSTALLER_LOCATION);
            SilentUninstall = "";
            OtherUninstallOptions = "";
            ValidInstallExitCodes = new List<long>
            {
                0
            };
            ValidUninstallExitCodes = new List<long>
            {
                0
            };
        }

        public override InstallerType InstallerType { get { return InstallerType.QtInstaller; } }
    }
}
