namespace chocolatey.infrastructure.commands
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using adapters;
    using app;
    using filesystem;
    using Environment = System.Environment;

    public sealed class PowershellExecutor
    {
        private static readonly IList<string> _powershellLocations = new List<string>
            {
                Environment.ExpandEnvironmentVariables("%windir%\\SysNative\\WindowsPowerShell\\v1.0\\powershell.exe"),
                Environment.ExpandEnvironmentVariables("%windir%\\System32\\WindowsPowerShell\\v1.0\\powershell.exe"),
                "powershell.exe"
            };

        private static string _powershell = string.Empty;

        public static int execute(
            string command,
            IFileSystem fileSystem,
            int waitForExitSeconds,
            Action<object, DataReceivedEventArgs> stdOutAction,
            Action<object, DataReceivedEventArgs> stdErrAction
            )
        {
            if (string.IsNullOrWhiteSpace(_powershell)) _powershell = get_powershell_location(fileSystem);
            //-NoProfile -NoLogo -ExecutionPolicy unrestricted -Command "[System.Threading.Thread]::CurrentThread.CurrentCulture = ''; [System.Threading.Thread]::CurrentThread.CurrentUICulture = '';& '%DIR%chocolatey.ps1' %PS_ARGS%"
            string arguments = "-NoProfile -NoLogo -ExecutionPolicy Bypass -Command \"{0}\"".format_with(command);

            return CommandExecutor.execute(
                _powershell,
                arguments,
                waitForExitSeconds,
                workingDirectory: fileSystem.get_directory_name(Assembly.GetExecutingAssembly().Location),
                stdOutAction: stdOutAction,
                stdErrAction: stdErrAction,
                updateProcessPath: true
                );
        }

        public static string get_powershell_location(IFileSystem fileSystem)
        {
            foreach (var powershellLocation in _powershellLocations)
            {
                if (fileSystem.file_exists(powershellLocation))
                {
                    return powershellLocation;
                }
            }

            throw new FileNotFoundException("Unable to find suitable location for PowerShell. Searched the following locations: '{0}'".format_with(string.Join("; ", _powershellLocations)));
        }
    }
}