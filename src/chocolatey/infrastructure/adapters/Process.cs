// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

using System;
using System.Diagnostics;

namespace chocolatey.infrastructure.adapters
{
    public sealed class Process : IProcess
    {
        public event EventHandler<DataReceivedEventArgs> OutputDataReceived;
        public event EventHandler<DataReceivedEventArgs> ErrorDataReceived;

        public Process()
        {
            UnderlyingType = new System.Diagnostics.Process();
            UnderlyingType.ErrorDataReceived += (sender, args) => ErrorDataReceived.Invoke(sender, args);
            UnderlyingType.OutputDataReceived += (sender, args) => OutputDataReceived.Invoke(sender, args);
        }

        public ProcessStartInfo StartInfo
        {
            get { return UnderlyingType.StartInfo; }
            set { UnderlyingType.StartInfo = value; }
        }

        public bool EnableRaisingEvents
        {
            get { return UnderlyingType.EnableRaisingEvents; }
            set { UnderlyingType.EnableRaisingEvents = value; }
        }

        public int ExitCode
        {
            get { return UnderlyingType.ExitCode; }
        }

        public System.Diagnostics.Process UnderlyingType { get; }

        public void Start()
        {
            UnderlyingType.Start();
        }

        public void BeginErrorReadLine()
        {
            UnderlyingType.BeginErrorReadLine();
        }

        public void BeginOutputReadLine()
        {
            UnderlyingType.BeginOutputReadLine();
        }

        public void WaitForExit()
        {
            UnderlyingType.WaitForExit();
        }

        public bool WaitForExit(int milliseconds)
        {
            return UnderlyingType.WaitForExit(milliseconds);
        }

        public void Dispose()
        {
            UnderlyingType.Dispose();
        }
    }
}
