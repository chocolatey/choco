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
    ///   BitRock Installer Options
    /// </summary>
    /// <remarks>
    ///   http://blog.bitrock.com/2009/10/unattended-mode.html
    ///   http://installbuilder.bitrock.com/docs/installbuilder-userguide.html#_unattended_mode
    ///   http://installbuilder.bitrock.com/docs/installbuilder-userguide/ar01s13.html
    /// </remarks>
    public class BitRockInstaller : InstallerBase
    {
        public BitRockInstaller()
        {
            InstallExecutable = "\"{0}\"".format_with(InstallTokens.INSTALLER_LOCATION);
            SilentInstall = "--mode unattended";
            // http://answers.bitrock.com/questions/215/how-can-i-restart-the-computer-after-installation-has-completed
            NoReboot = "";
            LogFile = "";
            // http://installbuilder.bitrock.com/docs/installbuilder-userguide.html#_command_line_parameters
            // http://answers.bitrock.com/questions/57/how-do-i-specify-a-different-default-installation-directory-for-unix-and-windows
            CustomInstallLocation = "--installdir {0}".format_with(InstallTokens.CUSTOM_INSTALL_LOCATION);
            Language = "";
            OtherInstallOptions = "--unattendedmodeui none";
            UninstallExecutable = "\"{0}\"".format_with(InstallTokens.UNINSTALLER_LOCATION);
            SilentUninstall = "--mode unattended";
            OtherUninstallOptions = "--unattendedmodeui none";
            ValidInstallExitCodes = new List<long>
            {
                0
            };
            ValidUninstallExitCodes = new List<long>
            {
                0
            };
        }

        public override InstallerType InstallerType { get { return InstallerType.BitRock; } }
    }
}
