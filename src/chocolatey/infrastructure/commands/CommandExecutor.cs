namespace chocolatey.infrastructure.commands
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using adapters;
    using filesystem;
    using platforms;
    using Process = adapters.Process;

    public sealed class CommandExecutor
    {
        private static Lazy<IFileSystem> file_system_initializer = new Lazy<IFileSystem>(() => new DotNetFileSystem());

        private static IFileSystem file_system
        {
            get { return file_system_initializer.Value; }
        }

        private static Func<IProcess> initialize_process = () => new Process();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void initialize_with(Lazy<IFileSystem> file_system, Func<IProcess> process_initializer)
        {
            file_system_initializer = file_system;
            initialize_process = process_initializer;
        }

        public static int execute(string process, string arguments, int waitForExitInSeconds)
        {
            return execute(process, arguments, waitForExitInSeconds, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }

        public static int execute(
            string process,
            string arguments,
            int waitForExitInSeconds,
            Action<object, DataReceivedEventArgs> stdOutAction,
            Action<object, DataReceivedEventArgs> stdErrAction
            )
        {
            return execute(process,
                           arguments,
                           waitForExitInSeconds,
                           file_system.get_directory_name(Assembly.GetExecutingAssembly().Location),
                           stdOutAction,
                           stdErrAction,
                           updateProcessPath: true
                );
        }

        public static int execute(string process, string arguments, int waitForExitInSeconds, string workingDirectory)
        {
            return execute(process, arguments, waitForExitInSeconds, workingDirectory, null, null, updateProcessPath: true);
        }

        public static int execute(string process,
                                  string arguments,
                                  int waitForExitInSeconds,
                                  string workingDirectory,
                                  Action<object, DataReceivedEventArgs> stdOutAction,
                                  Action<object, DataReceivedEventArgs> stdErrAction,
                                  bool updateProcessPath
            )
        {
            int exitCode = -1;
            if (updateProcessPath)
            {
                process = file_system.get_full_path(process);
            }

            if (Platform.get_platform() != PlatformType.Windows)
            {
                arguments = process + " " + arguments;
                process = "mono";
            }

            "chocolatey".Log().Debug(() => "Calling command ['\"{0}\" {1}']".format_with(process, arguments));

            var psi = new ProcessStartInfo(process, arguments)
                {
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

            using (var p = initialize_process())
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

                if (waitForExitInSeconds > 0)
                {
                    var exited = p.WaitForExit((int) TimeSpan.FromSeconds(waitForExitInSeconds).TotalMilliseconds);
                    if (exited)
                    {
                        exitCode = p.ExitCode;
                    }
                }
            }

            "chocolatey".Log().Debug(() => "Command ['\"{0}\" {1}'] exited with '{2}'".format_with(process, arguments, exitCode));
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