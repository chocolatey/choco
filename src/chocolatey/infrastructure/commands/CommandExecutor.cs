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

        public static int execute(
            string process,
            string arguments,
            bool waitForExit,
            Action<object, DataReceivedEventArgs> stdOutAction,
            Action<object, DataReceivedEventArgs> stdErrAction
            )
        {
            return execute(process,
                           arguments,
                           waitForExit,
                           Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                           stdOutAction,
                           stdErrAction,
                           updateProcessPath: true
                );
        }

        public static int execute(string process, string arguments, bool waitForExit, string workingDirectory)
        {
            return execute(process, arguments, waitForExit, workingDirectory, null, null, updateProcessPath: true);
        }

        public static int execute(string process,
                                  string arguments,
                                  bool waitForExit,
                                  string workingDirectory,
                                  Action<object, DataReceivedEventArgs> stdOutAction,
                                  Action<object, DataReceivedEventArgs> stdErrAction,
                                  bool updateProcessPath
            )
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

            using (var p = new Process())
            {
                p.StartInfo = psi;
                if (stdOutAction == null)
                {
                    p.OutputDataReceived += log_output;
                }
                else
                {
                    p.OutputDataReceived += (s, e) => stdOutAction(s, e);
                }
                if (stdErrAction == null)
                {
                    p.ErrorDataReceived += log_error;
                }
                else
                {
                    p.ErrorDataReceived += (s, e) => stdErrAction(s, e);
                }

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

            "chocolatey".Log().Debug(() => "Command '\"{0}\" {1}' exited with '{2}'".format_with(process, arguments, exitCode));
            return exitCode;
        }

        private static void log_output(object sender, DataReceivedEventArgs e)
        {
            "chocolatey".Log().Info(e.Data);
        }

        private static void log_error(object sender, DataReceivedEventArgs e)
        {
            "chocolatey".Log().Error(e.Data);
        }
    }
}