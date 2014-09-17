namespace chocolatey.infrastructure.app.domain
{
    using System.Collections.Generic;

    public interface IInstaller
    {
        string InstallExecutable { get; }
        string SilentInstall { get; }
        string NoReboot { get; }
        string LogFile { get; }
        string CustomInstallLocation { get; }
        string Language { get; }
        string OtherInstallOptions { get; }
        string UninstallExecutable { get; }
        string SilentUninstall { get; }
        string OtherUninstallOptions { get; }
        IEnumerable<int> ValidExitCodes { get; }

        string build_install_command_arguments();
        string build_uninstall_command_arguments();
    }
}