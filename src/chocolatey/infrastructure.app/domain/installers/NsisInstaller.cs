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
    ///   NSIS Installer Options
    /// </summary>
    /// <remarks>
    ///   http://nsis.sourceforge.net/Docs/Chapter3.html#installerusage
    ///   It is impossible to look at registry and determine a NSIS installer
    ///   NSIS has no logging or language options. The command line usage is very little.
    /// </remarks>
    public class NsisInstaller : InstallerBase
    {
        public NsisInstaller()
        {
            InstallExecutable = "\"{0}\"".format_with(InstallTokens.INSTALLER_LOCATION);
            SilentInstall = "/S";
            NoReboot = "";
            LogFile = "";
            // must be last thing specified and contain no quotes, even if there are spaces
            CustomInstallLocation = "/D={0}".format_with(InstallTokens.CUSTOM_INSTALL_LOCATION); 
            Language = "";
            OtherInstallOptions = "";
            UninstallExecutable = "\"{0}\"".format_with(InstallTokens.UNINSTALLER_LOCATION);
            SilentUninstall = "/S";
            OtherUninstallOptions = "";
            ValidInstallExitCodes = new List<long> { 0 };
            ValidUninstallExitCodes = new List<long> { 0 };
        }

        public override InstallerType InstallerType
        {
            get { return InstallerType.Nsis; }
        }
    }
}