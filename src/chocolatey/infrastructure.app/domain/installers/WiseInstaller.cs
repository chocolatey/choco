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
    ///   WISE Options
    /// </summary>
    /// <remarks>
    ///   https://support.symantec.com/en_US/article.HOWTO5865.html
    ///   http://www.itninja.com/blog/view/wise-setup-exe-switches
    ///   While we can override the extraction path, it should already be overridden
    ///   because we are overriding the TEMP variable
    /// </remarks>
    public class WiseInstaller : InstallerBase
    {
        public WiseInstaller()
        {
            InstallExecutable = "\"{0}\"".format_with(InstallTokens.INSTALLER_LOCATION);
            SilentInstall = "/s";
            NoReboot = "";
            LogFile = "";
            // http://www.itninja.com/question/wise-package-install-switches-for-install-path
            CustomInstallLocation = "";
            Language = "";
            OtherInstallOptions = "";
            UninstallExecutable = "\"{0}\"".format_with(InstallTokens.UNINSTALLER_LOCATION);
            SilentUninstall = "/s";
            // http://www.symantec.com/connect/blogs/wisescript-command-line-options
            OtherUninstallOptions = "\"{0}\\Uninstall.Log\"".format_with(InstallTokens.TEMP_LOCATION);
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
            get { return InstallerType.Wise; }
        }
    }
}
