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
    using System.Collections;
    using app;

    public sealed class Environment : IEnvironment
    {
        public OperatingSystem OSVersion
        {
            get { return System.Environment.OSVersion; }
        }

        public bool Is64BitOperatingSystem
        {
            get { return System.Environment.Is64BitOperatingSystem; }
        }

        public bool Is64BitProcess
        {
            get
            {
                // ARM64 bit architecture has a x86-32 emulator, so return false
                if (System.Environment.GetEnvironmentVariable(ApplicationParameters.Environment.ProcessorArchitecture).to_string().is_equal_to(ApplicationParameters.Environment.ARM64_PROCESSOR_ARCHITECTURE))
                {
                    return false;
                }

                return (IntPtr.Size == 8);
            }
        }

        public bool UserInteractive
        {
            get { return System.Environment.UserInteractive; }
        }

        public string NewLine
        {
            get { return System.Environment.NewLine; }
        }

        public string ExpandEnvironmentVariables(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;

            return System.Environment.ExpandEnvironmentVariables(name);
        }

        public string GetEnvironmentVariable(string variable)
        {
            return System.Environment.GetEnvironmentVariable(variable);
        }

        public IDictionary GetEnvironmentVariables()
        {
            return System.Environment.GetEnvironmentVariables();
        }

        public IDictionary GetEnvironmentVariables(EnvironmentVariableTarget target)
        {
            return System.Environment.GetEnvironmentVariables(target);
        }

        public void SetEnvironmentVariable(string variable, string value)
        {
            System.Environment.SetEnvironmentVariable(variable, value);
        }
    }
}
