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
    ///   Squirrel Installer Options
    /// </summary>
    /// <remarks>
    ///   https://github.com/Squirrel/Squirrel.Windows/blob/92e7af66b1593f951527dd88289a4ed1bee4bcdd/src/Update/Program.cs#L109
    /// </remarks>
    public class SquirrelInstaller : InstallerBase
    {
        public SquirrelInstaller()
        {
            InstallExecutable = "\"{0}\"".format_with(InstallTokens.INSTALLER_LOCATION);
            SilentInstall = "-s";
            NoReboot = "";
            LogFile = "";
            CustomInstallLocation = "";
            Language = "";
            OtherInstallOptions = "";
            UninstallExecutable = "\"{0}\"".format_with(InstallTokens.UNINSTALLER_LOCATION);
            SilentUninstall = "-s";
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

        public override InstallerType InstallerType { get { return InstallerType.Squirrel; } }
    }
}
