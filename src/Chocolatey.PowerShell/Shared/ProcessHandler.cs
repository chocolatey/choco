// Copyright © 2017 - 2025 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Chocolatey.PowerShell.Helpers;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Threading;

namespace Chocolatey.PowerShell.Shared
{
    /// <summary>
    /// Base class for handling console applications that need to be called from PowerShell,
    /// ensuring that their output is redirected correctly.
    /// 
    /// Each instance of this class may be used to handle operations for a single process.
    /// </summary>
    public abstract class ProcessHandler
    {
        private bool _started;

        /// <summary>
        /// The underlying <see cref="System.Diagnostics.Process" /> object.
        /// </summary>
        protected Process Process { get; private set; }

        /// <summary>
        /// The blocking collection used to handle output messages from the process.
        /// In order to customise how these messages are handled, override the <see cref="HandleProcessMessages"/> virtual method.
        /// </summary>
        protected BlockingCollection<ProcessOutput> ProcessMessages;

        /// <summary>
        /// The original cmdlet used to instantiate and invoke the process.
        /// </summary>
        protected readonly PSCmdlet Cmdlet;

        /// <summary>
        /// The cancellation token that indicates when to stop processing messages from the process. This will be set by
        /// the calling cmdlet in order to properly respond to Ctrl+C or <see cref="Cmdlet.StopProcessing"/> requests.
        /// Triggering this token will cause the underlying process to be disposed.
        /// </summary>
        protected readonly CancellationToken CancellationToken;

        /// <summary>
        /// Instantiates a new <see cref="ProcessHandler"/> for a given <paramref name="cmdlet"/> using its <paramref name="pipelineStopToken"/>.
        /// </summary>
        /// <param name="cmdlet">The cmdlet invoking the process.</param>
        /// <param name="pipelineStopToken">The cmdlet's <see cref="ChocolateyCmdlet.PipelineStopToken"/>.</param>
        public ProcessHandler(PSCmdlet cmdlet, CancellationToken pipelineStopToken)
        {
            Cmdlet = cmdlet;
            CancellationToken = pipelineStopToken;
        }

        /// <summary>
        /// Starts the given process by name or path, with the provided arguments. Does not return until the process terminates.
        /// </summary>
        /// <param name="processName">The name or path of the process to start.</param>
        /// <param name="workingDirectory">The working directory to start the process in.</param>
        /// <param name="arguments">Arguments to pass to the process. These will be logged.</param>
        /// <param name="sensitiveStatements">Sensitive arguments to pass to the process. These will not be logged.</param>
        /// <param name="elevated">Whether to attempt elevation. This currently cannot elevate processes from a non-elevated context.</param>
        /// <param name="windowStyle">Whether to show windows for the process.</param>
        /// <param name="noNewWindow">Whether to run in the current window.</param>
        /// <returns>The exit code from the process.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the process has already been started.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="processName"/> is not provided or empty.</exception>
        /// <exception cref="Win32Exception">Thrown if the executable could not be opened.</exception>
        protected int Run(string processName, string workingDirectory, string arguments, string sensitiveStatements, bool elevated, ProcessWindowStyle windowStyle, bool noNewWindow)
        {
            if (_started)
            {
                throw new InvalidOperationException("A process has already been started.");
            }

            if (string.IsNullOrWhiteSpace(processName))
            {
                throw new ArgumentNullException(nameof(processName), "No process name was provided.");
            }

            _started = true;

            var alreadyElevated = ProcessInformation.IsElevated();

            var exitCode = 0;
            Process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    FileName = processName,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory,
                    WindowStyle = windowStyle,
                    CreateNoWindow = noNewWindow,
                },
            };

            using (Process)
            {
                if (!string.IsNullOrWhiteSpace(arguments))
                {
                    Process.StartInfo.Arguments = arguments;
                }

                if (!string.IsNullOrWhiteSpace(sensitiveStatements))
                {
                    PSHelper.WriteHost(Cmdlet, "Sensitive arguments have been passed. Adding to arguments.");
                    Process.StartInfo.Arguments += " " + sensitiveStatements;
                }

                if (elevated && !alreadyElevated && Environment.OSVersion.Version > new Version(6, 0))
                {
                    // SELF-ELEVATION: This currently doesn't work as we're not using ShellExecute
                    Cmdlet.WriteDebug("Setting RunAs for elevation");
                    Process.StartInfo.Verb = "RunAs";
                }

                Process.OutputDataReceived += ProcessOutputHandler;
                Process.ErrorDataReceived += ProcessErrorHandler;

                // process.WaitForExit() is a bit unreliable, we use the Exiting event handler to register when
                // the process exits.
                Process.Exited += ProcessExitingHandler;

                try
                {
                    ProcessMessages = new BlockingCollection<ProcessOutput>();
                    Process.Start();
                    Process.BeginOutputReadLine();
                    Process.BeginErrorReadLine();

                    Cmdlet.WriteDebug("Waiting for process to exit");

                    // This will handle dispatching output/error messages until either the process has exited or the pipeline
                    // has been cancelled.
                    HandleProcessMessages();

                    exitCode = Process.ExitCode;

                    Cmdlet.WriteDebug($"Command [\"{Process}\" {arguments}] exited with '{exitCode}'.");

                    return exitCode;
                }
                catch (Win32Exception error)
                {
                    throw new IOException($"There was an error starting the target process '{processName}': {error.Message}", error);
                }
                catch (ObjectDisposedException error)
                {
                    // This means that something has disposed the process object before we could start it.
                    // This would typically mean that Ctrl+C / StopProcessing() has been called on the
                    // cmdlet before we got here, but after we created the Process object.
                    throw new OperationCanceledException($"The current operation was cancelled before the process could be started.", error);
                }
            }
        }

        /// <summary>
        /// This method is called after the process has been started and should not return until the
        /// <see cref="ProcessMessages"/> collection has been completely exhausted, or cancelled via
        /// the <see cref="CancellationToken"/>.
        /// </summary>
        /// <remarks>
        /// In most cases, overriding this should not be necessary. Additional logic for handling
        /// stdout or stderr can be implemented by registering handlers to <see cref="ProcessOutputReceived"/>
        /// or <see cref="ProcessErrorDataReceived"/>, which will be invoked automatically as those messages
        /// are emitted. Override this only if you need to disable or modify the behaviour of writing the
        /// process' messages to Error or Verbose streams.
        /// </remarks>
        protected virtual void HandleProcessMessages()
        {
            if (ProcessMessages is null)
            {
                return;
            }

            // Use of the CancellationToken allows us to respect calls for StopProcessing() correctly.
            foreach (var item in ProcessMessages.GetConsumingEnumerable(CancellationToken))
            {
                if (item.StdErr)
                {
                    Cmdlet.WriteError(new RuntimeException(item.Message).ErrorRecord);
                }
                else
                {
                    Cmdlet.WriteVerbose(item.Message);
                }
            }
        }

        /// <summary>
        /// Raised when the underlying <see cref="Process"> exits.
        /// </summary>
        protected event EventHandler ProcessExited;

        /// <summary>
        /// Raised when the underlying <see cref="Process"> emits non-null/empty data to stdout.
        /// </summary>
        protected event EventHandler<ProcessOutput> ProcessOutputReceived;

        /// <summary>
        /// Raised when the underlying <see cref="Process"> emits non-null/empty data to stderr.
        /// </summary>
        protected event EventHandler<ProcessOutput> ProcessErrorDataReceived;

        private void ProcessExitingHandler(object sender, EventArgs e)
        {
            ProcessMessages?.CompleteAdding();
            ProcessExited?.Invoke(sender, e);
        }

        private void ProcessOutputHandler(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                var message = new ProcessOutput(e.Data, isStdErr: false);
                ProcessMessages?.Add(message);
                ProcessOutputReceived?.Invoke(sender, message);
            }
        }

        private void ProcessErrorHandler(object sender, DataReceivedEventArgs e)
        {
            if (!(e.Data is null))
            {
                var message = new ProcessOutput(e.Data, isStdErr: true);
                ProcessMessages?.Add(message);
                ProcessErrorDataReceived?.Invoke(sender, message);
            }
        }

        /// <summary>
        /// The event args passed to <see cref="ProcessOutputReceived"/> and <see cref="ProcessErrorDataReceived"/>.
        /// </summary>
        protected class ProcessOutput : EventArgs
        {
            public ProcessOutput(string message, bool isStdErr)
            {
                Message = message;
                StdErr = isStdErr;
            }

            /// <summary>
            /// The message string passed to stdout or stderr.
            /// </summary>
            public string Message { get; private set; }

            /// <summary>
            /// True if the message came from stderr.
            /// </summary>
            public bool StdErr { get; private set; }
        }
    }
}
