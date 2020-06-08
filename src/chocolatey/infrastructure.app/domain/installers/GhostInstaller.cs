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
    ///   Ghost Installer Options
    /// </summary>
    /// <remarks>
    ///   http://www.ethalone.com/products.html /
    ///   https://web.archive.org/web/20070812133050/http://www.ethalone.com/cgi-bin/ib/ikonboard.cgi?act=ST;f=2;t=195
    ///   Ghost has no logging or language options. The command line usage is very little.
    /// </remarks>
    public class GhostInstaller : InstallerBase
    {
        public GhostInstaller()
        {
            InstallExecutable = "\"{0}\"".format_with(InstallTokens.INSTALLER_LOCATION);
            SilentInstall = "-s";
            NoReboot = "";
            LogFile = "";
            CustomInstallLocation = "";
            Language = "";
            OtherInstallOptions = "";
            UninstallExecutable = "\"{0}\"".format_with(InstallTokens.UNINSTALLER_LOCATION);
            SilentUninstall = "-u -s";
            OtherUninstallOptions = "";
            ValidInstallExitCodes = new List<long> { 0 };
            ValidUninstallExitCodes = new List<long> { 0 };
        }

        public override InstallerType InstallerType { get { return InstallerType.Ghost; } }
    }
}
