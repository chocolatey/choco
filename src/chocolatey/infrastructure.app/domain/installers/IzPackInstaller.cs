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
    ///   izPack Installer Options
    /// </summary>
    /// <remarks>
    ///   http://izpack.org/
    ///   https://izpack.atlassian.net/wiki/display/IZPACK/Installer+Runtime+Options
    ///   https://izpack.atlassian.net/wiki/display/IZPACK/Unattended+Installations
    /// </remarks>
    public class IzPackInstaller : InstallerBase
    {
        public IzPackInstaller()
        {
            InstallExecutable = "java";
            SilentInstall = "-jar \"{0}\" -options-system".format_with(InstallTokens.INSTALLER_LOCATION);
            NoReboot = "";
            LogFile = "";
            CustomInstallLocation = "-DINSTALL_PATH=\"{0}\"".format_with(InstallTokens.CUSTOM_INSTALL_LOCATION);
            Language = "";
            OtherInstallOptions = "";
            UninstallExecutable = "java"; //currently untested
            SilentUninstall = "-jar \"{0}\"".format_with(InstallTokens.UNINSTALLER_LOCATION);
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

        public override InstallerType InstallerType { get { return InstallerType.IzPack; } }
    }
}
