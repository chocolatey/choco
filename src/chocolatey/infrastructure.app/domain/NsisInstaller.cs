namespace chocolatey.infrastructure.app.domain
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   NSIS Installer Options
    /// </summary>
    /// <remarks>
    ///   http://nsis.sourceforge.net/Docs/Chapter3.html#installerusage
    ///   It is impossible to look at registry and determine a NSIS installer
    ///   NSIS has no logging or language options. The command line usage is very little.
    /// </remarks>
    public class NsisInstaller : IInstaller
    {
        public NsisInstaller()
        {
            InstallExecutable = "\"{0}\"".format_with(InstallTokens.INSTALLER_LOCATION);
            SilentInstall = "/S";
            NoReboot = "";
            LogFile = "";
            CustomInstallLocation = "/D={0}".format_with(InstallTokens.CUSTOM_INSTALL_LOCATION); //must be last thing specified and no quotes
            Language = "";
            OtherInstallOptions = "";
            UninstallExecutable = "\"{0}\"".format_with(InstallTokens.UNINSTALLER_LOCATION);
            SilentUninstall = "/S";
            OtherUninstallOptions = "";
            ValidExitCodes = new List<int> {0};
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