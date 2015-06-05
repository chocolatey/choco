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
    using System.Collections.Generic;
    using System.Text;

    public abstract class InstallerBase : IInstaller
    {
        public abstract InstallerType InstallerType { get; }

        public string InstallExecutable { get; protected set; }
        public string SilentInstall { get; protected set; }
        public string NoReboot { get; protected set; }
        public string LogFile { get; protected set; }
        public string OtherInstallOptions { get; protected set; }
        public string CustomInstallLocation { get; protected set; }
        public string Language { get; protected set; }
        public string UninstallExecutable { get; protected set; }
        public string SilentUninstall { get; protected set; }
        public string OtherUninstallOptions { get; protected set; }
        public IEnumerable<int> ValidInstallExitCodes { get; protected set; }
        public IEnumerable<int> ValidUninstallExitCodes { get; protected set; }

        public virtual string build_install_command_arguments(bool customInstallLocation, bool languageRequested)
        {
            var args = new StringBuilder();
            args.AppendFormat("{0} {1}", SilentInstall, NoReboot);
            //MSI may have issues with 1622 - opening a log file location
            args.AppendFormat(" {0}", LogFile);
            if (languageRequested) args.AppendFormat(" {0}", Language);
            args.AppendFormat(" {0}", OtherInstallOptions);

            // custom install location must be last for NSIS
            if (customInstallLocation) args.AppendFormat(" {0}", CustomInstallLocation);

            return args.ToString();
        }

        public virtual string build_uninstall_command_arguments()
        {
            //MSI has issues with 1622 - opening a log file location
            return "{0} {1} {2}".format_with(SilentUninstall, NoReboot, OtherUninstallOptions);
        }
    }
}