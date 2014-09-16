namespace chocolatey.infrastructure.app.domain
{
    using System.Collections.Generic;

    /// <summary>
    /// InnoSetup Installer Options
    /// </summary>
    /// <remarks>
    ///   http://www.jrsoftware.org/ishelp/index.php?topic=setupcmdline
    /// </remarks>
    public class InnoSetupInstaller : IInstaller
    {
        public InnoSetupInstaller()
        {
            SilentInstall = "\"{{INSTALLER_LOCATION}}\" /VERYSILENT";
            NoReboot = "/NORESTART";
            LogFile = "/LOG=\"{{PACKAGE_LOCATION}}\\InnoSetup.Install.log\"";
            CustomInstallLocation = "/DIR=\"{{INSTALL_LOCATION}}\"";
            Language = "/LANG={{LANGUAGE}}";
            OtherInstallOptions = "/SP- /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS /NOICONS";
            SilentUninstall = "{{UNINSTALL_LOCATION}} /VERYSILENT";
            OtherUninstallOptions = "/SUPPRESSMSGBOXES";
            ValidExitCodes = new List<int> {0};
        }

        public string SilentInstall { get; private set; }
        public string NoReboot { get; private set; }
        public string LogFile { get; private set; }
        public string CustomInstallLocation { get; private set; }
        public string Language { get; private set; }
        public string OtherInstallOptions { get; private set; }
        public string SilentUninstall { get; private set; }
        public string OtherUninstallOptions { get; private set; }
        public IEnumerable<int> ValidExitCodes { get; private set; }
    }
}