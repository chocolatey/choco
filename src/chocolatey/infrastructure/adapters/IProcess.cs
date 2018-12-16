// Copyright © 2017 - 2018 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.adapters
{
    using System;
    using System.Diagnostics;

    // ReSharper disable InconsistentNaming

    public interface IProcess : IDisposable
    {
        event EventHandler<DataReceivedEventArgs> OutputDataReceived;
        event EventHandler<DataReceivedEventArgs> ErrorDataReceived;

        /// <summary>
        ///   Gets or sets the properties to pass into the <see cref='System.Diagnostics.Process.Start' /> method for the
        ///   <see
        ///     cref='System.Diagnostics.Process' />
        ///   .
        /// </summary>
        /// <value>
        ///   The start information.
        /// </value>
        ProcessStartInfo StartInfo { get; set; }

        /// <summary>
        ///   Gets or sets whether the <see cref='System.Diagnostics.Process.Exited' /> event is fired when the process terminates.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [enable raising events]; otherwise, <c>false</c>.
        /// </value>
        bool EnableRaisingEvents { get; set; }

        /// <summary>
        ///   Gets the value that was specified by the associated process when it was terminated.
        /// </summary>
        /// <value>
        ///   The exit code.
        /// </value>
        int ExitCode { get; }

        /// <summary>
        ///   Starts a process specified by the <see cref='System.Diagnostics.Process.StartInfo' /> property of this
        ///   <see
        ///     cref='System.Diagnostics.Process' />
        ///   component and associates it with the
        ///   <see cref='System.Diagnostics.Process' /> . If a process resource is reused
        ///   rather than started, the reused process is associated with this <see cref='System.Diagnostics.Process' />
        ///   component.
        /// </summary>
        void Start();

        /// <summary>
        ///   Instructs the <see cref='System.Diagnostics.Process' /> component to start
        ///   reading the StandardError stream asynchronously. The user can register a callback
        ///   that will be called when a line of data terminated by \n,\r or \r\n is reached, or the end of stream is reached
        ///   then the remaining information is returned. The user can add an event handler to ErrorDataReceived.
        /// </summary>
        void BeginErrorReadLine();

        /// <summary>
        ///   Instructs the <see cref='System.Diagnostics.Process' /> component to start
        ///   reading the StandardOutput stream asynchronously. The user can register a callback
        ///   that will be called when a line of data terminated by \n,\r or \r\n is reached, or the end of stream is reached
        ///   then the remaining information is returned. The user can add an event handler to OutputDataReceived.
        /// </summary>
        void BeginOutputReadLine();

        /// <summary>
        ///   Instructs the <see cref='System.Diagnostics.Process' /> component to wait
        ///   indefinitely for the associated process to exit.
        /// </summary>
        void WaitForExit();

        /// <summary>
        ///   Instructs the <see cref='System.Diagnostics.Process' /> component to wait the specified number of milliseconds for the associated process to exit.
        /// </summary>
        /// <param name="milliseconds">The milliseconds.</param>
        /// <returns></returns>
        bool WaitForExit(int milliseconds);
    }

    // ReSharper restore InconsistentNaming
}