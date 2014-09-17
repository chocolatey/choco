namespace chocolatey.infrastructure.app.domain
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   InstallShield Installer Options
    /// </summary>
    /// <remarks>
    ///   http://helpnet.installshield.com/installshield18helplib/ihelpsetup_execmdline.htm
    /// </remarks>
    public class InstallShieldInstaller : IInstaller
    {
        public InstallShieldInstaller()
        {
            InstallExecutable = "\"{0}\"".format_with(InstallTokens.INSTALLER_LOCATION);
            SilentInstall = "/s /v\"/qn\"";
            NoReboot = "/v\"REBOOT=ReallySuppress\"";
            LogFile = "/f2\"{0}\\MSI.Install.log\"".format_with(InstallTokens.PACKAGE_LOCATION);
            CustomInstallLocation = "/v\"INSTALLDIR=\\\"{0}\\\"".format_with(InstallTokens.CUSTOM_INSTALL_LOCATION);
            Language = "/l\"{0}\"".format_with(InstallTokens.LANGUAGE);
            OtherInstallOptions = "/sms"; // pause
            UninstallExecutable = "\"{0}\"".format_with(InstallTokens.UNINSTALLER_LOCATION);
            SilentUninstall = "/uninst /s";
            OtherUninstallOptions = "/sms";
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
            return "{0} {1} {2}".format_with(SilentUninstall, NoReboot, OtherUninstallOptions);
        }
    }
}