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

    public class CustomInstaller : IInstaller
    {
        public CustomInstaller()
        {
            InstallExecutable = "\"{0}\"".format_with(InstallTokens.INSTALLER_LOCATION);
            SilentInstall = "/S";
            NoReboot = "";
            LogFile = "";
            CustomInstallLocation = "";
            Language = "";
            OtherInstallOptions = "";
            UninstallExecutable = "\"{0}\"".format_with(InstallTokens.UNINSTALLER_LOCATION);
            SilentUninstall = "/S";
            OtherUninstallOptions = "";
            ValidExitCodes = new List<int> {0};
        }

        public InstallerType InstallerType
        {
            get { return InstallerType.Custom; }
        }

        public string InstallExecutable { get; private set; }
        public string SilentInstall { get; private set; }
        public string NoReboot { get; private set; }
        public string LogFile { get; private set; }
        public string CustomInstallLocation { get; private set; }
        public string Language { get; private set; }
        public string OtherInstallOptions { get; private set; }
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
            return "{0} {1}".format_with(SilentUninstall, OtherInstallOptions);
        }
    }
}