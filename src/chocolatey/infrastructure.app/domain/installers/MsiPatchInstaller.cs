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

    public class MsiPatchInstaller : InstallerBase
    {
        public MsiPatchInstaller()
        {
            //todo: fully qualify msiexec
            InstallExecutable = "msiexec.exe";
            SilentInstall = "/p \"{0}\" /qn".format_with(InstallTokens.INSTALLER_LOCATION);
            // http://msdn.microsoft.com/en-us/library/aa371101.aspx
            NoReboot = "/norestart";
            LogFile = "/l*v \"{0}\\MSP.Install.log\"".format_with(InstallTokens.PACKAGE_LOCATION);
            // https://msdn.microsoft.com/en-us/library/aa372064.aspx
            // http://apprepack.blogspot.com/2012/08/installdir-vs-targetdir.html
            CustomInstallLocation = "";
            // http://msdn.microsoft.com/en-us/library/aa370856.aspx
            Language = "";
            // http://msdn.microsoft.com/en-us/library/aa367559.aspx
            OtherInstallOptions = "REINSTALLMODE=sumo REINSTALL=ALL";
            UninstallExecutable = "msiexec.exe";
            SilentUninstall = "/package {0} /qn".format_with(InstallTokens.UNINSTALLER_LOCATION);
            OtherUninstallOptions = "MSIPATCHREMOVE={PATCH_GUID_HERE}";
            // https://msdn.microsoft.com/en-us/library/aa376931.aspx
            // https://support.microsoft.com/en-us/kb/290158
            ValidInstallExitCodes = new List<long>
            {
                0,
                1641,
                3010
            };
            // we allow unknown 1605/1614 b/c it may have already been uninstalled 
            // and that's okay
            ValidUninstallExitCodes = new List<long>
            {
                0,
                1605,
                1614,
                1641,
                3010
            };
        }

        public override InstallerType InstallerType
        {
            get { return InstallerType.MsiPatch; }
        }
    }
}
