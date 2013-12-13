using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace chocolatey.infrastructure.commands
{
    public class CommandExecutor
    {
        public static int execute(string process, string arguments, bool wait_for_exit)
        {
            return execute(process, arguments, wait_for_exit, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }

        public static int execute(string process, string arguments, bool wait_for_exit, string working_directory)
        {
            return execute(process, arguments, wait_for_exit, working_directory, update_process_path: true);
        }

        public static int execute(string process, string arguments, bool wait_for_exit, string working_directory, bool update_process_path)
        {
            int exit_code = -1;
            if (update_process_path)
            {
                process = Path.GetFullPath(process);
            }

            ProcessStartInfo psi = new ProcessStartInfo(process, arguments)
                                       {
                                           UseShellExecute = false,
                                           WorkingDirectory = working_directory,
                                           RedirectStandardOutput = true,
                                           RedirectStandardError = true,
                                           CreateNoWindow = true
                                       };

            StreamReader standard_output;
            StreamReader error_output;

            using (Process p = new Process())
            {
                p.StartInfo = psi;
                p.ErrorDataReceived += log_output;
                p.OutputDataReceived += log_output;
                p.EnableRaisingEvents = true;
                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                
                if (wait_for_exit)
                {
                    p.WaitForExit();
                }
                exit_code = p.ExitCode;
            }
            
            return exit_code;
        }

        private static void log_output(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }
    }
}