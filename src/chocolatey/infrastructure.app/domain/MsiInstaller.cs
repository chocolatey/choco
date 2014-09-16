namespace chocolatey.infrastructure.app.domain
{
    using System.Collections.Generic;

    /// <summary>
    /// Windows Installer (MsiExec) Options
    /// </summary>
    /// <remarks>
    /// http://msdn.microsoft.com/en-us/library/aa367988.aspx
    /// http://msdn.microsoft.com/en-us/library/aa372024.aspx
    /// http://support.microsoft.com/kb/227091
    /// http://www.advancedinstaller.com/user-guide/msiexec.html
    /// 
    /// </remarks>
    public class MsiInstaller : IInstaller
    {
        public MsiInstaller()
        {
            SilentInstall = "/i \"{{INSTALLER_LOCATION}}\" /qn"; // /quiet
            // http://msdn.microsoft.com/en-us/library/aa371101.aspx
            NoReboot = "/norestart"; //REBOOT=ReallySuppress
            LogFile = "/l*v \"{{PACKAGE_LOCATION}}\\MSI.Install.log\"";
            // http://msdn.microsoft.com/en-us/library/aa372064.aspx
            CustomInstallLocation = "TARGETDIR=\"{{INSTALL_LOCATION}}\"";
            // http://msdn.microsoft.com/en-us/library/aa370856.aspx
            Language = "ProductLanguage={{LANGUAGE}}";
            // http://msdn.microsoft.com/en-us/library/aa367559.aspx
            OtherInstallOptions = "ALLUSERS=1 DISABLEDESKTOPSHORTCUT=1 ADDDESKTOPICON=0 ADDSTARTMENU=0";
            SilentUninstall = "/x {{UNINSTALL_LOCATION}} /qn";
            OtherUninstallOptions = "";
            ValidExitCodes = new List<int> {0, 3010};
        }

        public string SilentInstall { get; private set; }
        public string NoReboot { get; private set; }
        public string LogFile { get; private set; }
        public string OtherInstallOptions { get; private set; }
        public string CustomInstallLocation { get; private set; }
        public string Language { get; private set; }
        public string SilentUninstall { get; private set; }
        public string OtherUninstallOptions { get; private set; }
        public IEnumerable<int> ValidExitCodes { get; private set; }
    }
}