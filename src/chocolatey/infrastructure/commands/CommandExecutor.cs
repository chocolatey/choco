namespace chocolatey.infrastructure.commands
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    public sealed class CommandExecutor
    {
        public static int execute(string process, string arguments, bool waitForExit)
        {
            return execute(process, arguments, waitForExit, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }

        public static int execute(string process, string arguments, bool waitForExit, string workingDirectory)
        {
            return execute(process, arguments, waitForExit, workingDirectory, updateProcessPath: true);
        }

        public static int execute(string process, string arguments, bool waitForExit, string workingDirectory, bool updateProcessPath)
        {
            int exitCode = -1;
            if (updateProcessPath)
            {
                process = Path.GetFullPath(process);
            }

            var psi = new ProcessStartInfo(process, arguments)
                {
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

            StreamReader standardOutput;
            StreamReader errorOutput;

            using (var p = new Process())
            {
                p.StartInfo = psi;
                p.ErrorDataReceived += log_output;
                p.OutputDataReceived += log_output;
                p.EnableRaisingEvents = true;
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();

                if (waitForExit)
                {
                    p.WaitForExit();
                }
                exitCode = p.ExitCode;
            }

            return exitCode;
        }

        private static void log_output(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }
    }
}