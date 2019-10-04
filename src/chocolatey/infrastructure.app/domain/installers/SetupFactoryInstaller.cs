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
    ///   SetupFactory Options
    /// </summary>
    /// <remarks>
    ///   http://www.indigorose.com/webhelp/suf9/Program_Reference/Command_Line_Options.htm
    ///   While we can override the extraction path, it should already be overridden
    ///   because we are overriding the TEMP variable
    /// </remarks>
    public class SetupFactoryInstaller : InstallerBase
    {
        public SetupFactoryInstaller()
        {
            InstallExecutable = "\"{0}\"".format_with(InstallTokens.INSTALLER_LOCATION);
            SilentInstall = "/S";
            NoReboot = "";
            LogFile = "";
            // http://www.indigorose.com/forums/threads/23686-How-to-Set-the-Default-Application-Directory
            // http://www.indigorose.com/webhelp/suf70/Program_Reference/Screen_Types/Select_Install_Folder/Properties.htm
            // http://www.indigorose.com/webhelp/suf70/Program_Reference/Variables/Session_Variables.htm#AppFolder
            // todo: basically we need an environment variable for AppFolder
            CustomInstallLocation = "";
            Language = "";
            //OtherInstallOptions = "\"/T:{0}\"".format_with(InstallTokens.TEMP_LOCATION);
            OtherInstallOptions = "";
            UninstallExecutable = "\"{0}\"".format_with(InstallTokens.UNINSTALLER_LOCATION);
            SilentUninstall = "/S";
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

        public override InstallerType InstallerType
        {
            get { return InstallerType.SetupFactory; }
        }
    }
}
