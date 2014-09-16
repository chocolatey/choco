namespace chocolatey.infrastructure.app.domain
{
    using System.Collections.Generic;

    /// <summary>
    /// InstallShield Installer Options
    /// </summary>
    /// <remarks>
    /// http://helpnet.installshield.com/installshield18helplib/ihelpsetup_execmdline.htm
    /// </remarks>
    public class InstallShieldInstaller : IInstaller
    {
        public InstallShieldInstaller()
        {
            SilentInstall = "\"{{INSTALLER_LOCATION}}\" /s /v\"/qn\"";
            NoReboot = "/v\"REBOOT=ReallySuppress\"";
            LogFile = "/f2\"{{PACKAGE_LOCATION}}\\MSI.Install.log\"";
            CustomInstallLocation = "/v\"INSTALLDIR=\\\"{{INSTALL_LOCATION}}\\\"";
            Language = "/l\"{{LANGUAGE}}\"";
            OtherInstallOptions = "/sms"; // pause
            SilentUninstall = "/uninst {{UNINSTALL_LOCATION}} /s";
            OtherUninstallOptions = "/sms";
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