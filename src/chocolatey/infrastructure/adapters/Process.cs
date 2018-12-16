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

    public sealed class Process : IProcess
    {
        private readonly System.Diagnostics.Process _process;
        public event EventHandler<DataReceivedEventArgs> OutputDataReceived;
        public event EventHandler<DataReceivedEventArgs> ErrorDataReceived;

        public Process()
        {
            _process = new System.Diagnostics.Process();
            _process.ErrorDataReceived += (sender, args) => ErrorDataReceived.Invoke(sender, args);
            _process.OutputDataReceived += (sender, args) => OutputDataReceived.Invoke(sender, args);
        }

        public ProcessStartInfo StartInfo
        {
            get { return _process.StartInfo; }
            set { _process.StartInfo = value; }
        }

        public bool EnableRaisingEvents
        {
            get { return _process.EnableRaisingEvents; }
            set { _process.EnableRaisingEvents = value; }
        }

        public int ExitCode
        {
            get { return _process.ExitCode; }
        }

        public System.Diagnostics.Process UnderlyingType
        {
            get { return _process; }
        }

        public void Start()
        {
            _process.Start();
        }

        public void BeginErrorReadLine()
        {
            _process.BeginErrorReadLine();
        }

        public void BeginOutputReadLine()
        {
            _process.BeginOutputReadLine();
        }

        public void WaitForExit()
        {
            _process.WaitForExit();
        }

        public bool WaitForExit(int milliseconds)
        {
            return _process.WaitForExit(milliseconds);
        }

        public void Dispose()
        {
            _process.Dispose();
        }
    }
}
