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
    ///   Windows Installer (MsiExec) Options
    /// </summary>
    /// <remarks>
    ///   http://msdn.microsoft.com/en-us/library/aa367988.aspx
    ///   http://msdn.microsoft.com/en-us/library/aa372024.aspx
    ///   http://support.microsoft.com/kb/227091
    ///   http://www.advancedinstaller.com/user-guide/msiexec.html
    ///   1603 search for return value 3 http://blogs.msdn.com/b/astebner/archive/2005/08/01/446328.aspx
    /// </remarks>
    public class MsiInstaller : IInstaller
    {
        public MsiInstaller()
        {
            InstallExecutable = "msiexec.exe";
            SilentInstall = "/i \"{0}\" /qn".format_with(InstallTokens.INSTALLER_LOCATION); // /quiet
            // http://msdn.microsoft.com/en-us/library/aa371101.aspx
            NoReboot = "/norestart"; //REBOOT=ReallySuppress
            LogFile = "/l*v \"{0}\\MSI.Install.log\"".format_with(InstallTokens.PACKAGE_LOCATION);
            // http://msdn.microsoft.com/en-us/library/aa372064.aspx
            CustomInstallLocation = "TARGETDIR=\"{0}\"".format_with(InstallTokens.CUSTOM_INSTALL_LOCATION);
            // http://msdn.microsoft.com/en-us/library/aa370856.aspx
            Language = "ProductLanguage={0}".format_with(InstallTokens.LANGUAGE);
            // http://msdn.microsoft.com/en-us/library/aa367559.aspx
            OtherInstallOptions = "ALLUSERS=1 DISABLEDESKTOPSHORTCUT=1 ADDDESKTOPICON=0 ADDSTARTMENU=0";
            UninstallExecutable = "msiexec.exe";
            //SilentUninstall = "/qn /x{0}".format_with(InstallTokens.UNINSTALLER_LOCATION);
            SilentUninstall = "/qn";
            OtherUninstallOptions = "";
            ValidExitCodes = new List<int> {0, 3010};
        }

        public InstallerType InstallerType
        {
            get { return InstallerType.Msi; }
        }

        public string InstallExecutable { get; private set; }
        public string SilentInstall { get; private set; }
        public string NoReboot { get; private set; }
        public string LogFile { get; private set; }
        public string OtherInstallOptions { get; private set; }
        public string CustomInstallLocation { get; private set; }
        public string Language { get; private set; }
        public string UninstallExecutable { get; private set; }
        public string SilentUninstall { get; private set; }
        public string OtherUninstallOptions { get; private set; }
        public IEnumerable<int> ValidExitCodes { get; private set; }

        public string build_install_command_arguments()
        {
            throw new NotImplementedException();
        }

        public string build_uninstall_command_arguments()
        {
            return "{0} {1} {2}".format_with(SilentUninstall, NoReboot, OtherUninstallOptions);
        }
    }
}