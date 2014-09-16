namespace chocolatey.infrastructure.app.domain
{
    using System.Collections.Generic;

    /// <summary>
    /// NSIS Installer Options
    /// </summary>
    /// <remarks>
    ///   http://nsis.sourceforge.net/Docs/Chapter3.html#installerusage
    /// It is impossible to look at registry and determine a NSIS installer
    /// NSIS has no logging or language options. The command line usage is very little.
    /// </remarks>
    public class NsisInstaller : IInstaller
    {
        public NsisInstaller()
        {
            SilentInstall = "\"{{INSTALLER_LOCATION}}\" /S ";
            NoReboot = "";
            LogFile = ""; // "\"{{PACKAGE_LOCATION}}\\NSIS.Install.log\"";
            CustomInstallLocation = "/D={{INSTALL_LOCATION}}"; //must be last thing specified and no quotes
            Language = "";
            OtherInstallOptions = "";
            SilentUninstall = "{{UNINSTALL_LOCATION}} /S";
            OtherUninstallOptions = "";
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