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

using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace chocolatey.infrastructure.adapters
{
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
                // On Windows on ARM the Chocolatey CLI runs either natively as ARM64 (Windows 11
                // 24H2+, opted into via the <supportedArchitectures> element in choco.exe.manifest)
                // or as an emulated x64 process on older hosts. Both are 64-bit, so ARM64 operating
                // systems are reported as 64-bit rather than being forced to 32-bit as they were
                // historically. The explicit check also covers the (unusual) case of the CLI running
                // 32-bit-emulated, where IntPtr.Size would otherwise report 4. See
                // https://github.com/chocolatey/choco/issues/1803 and #2172.
                if (IsArm64OperatingSystem)
                {
                    return true;
                }

                return (IntPtr.Size == 8);
            }
        }

        public ProcessorArchitectureType NativeProcessorArchitecture
        {
            get
            {
                // IsWow64Process2 is the only reliable way to determine the operating system's
                // native architecture from a process that may be running under emulation;
                // GetNativeSystemInfo and RuntimeInformation.OSArchitecture report the emulated
                // architecture on Windows on ARM. The API is Windows-only and is available on
                // Windows 10 1709+ / Windows Server 2019+.
                if (System.Environment.OSVersion.Platform != PlatformID.Win32NT)
                {
                    return ProcessorArchitectureType.Unknown;
                }

                try
                {
                    if (IsWow64Process2(GetCurrentProcess(), out _, out var nativeMachine))
                    {
                        switch (nativeMachine)
                        {
                            case ImageFileMachineArm64:
                                return ProcessorArchitectureType.Arm64;
                            case ImageFileMachineAmd64:
                                return ProcessorArchitectureType.X64;
                            case ImageFileMachineI386:
                                return ProcessorArchitectureType.X86;
                        }
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    // IsWow64Process2 does not exist before Windows 10 1709. ARM64 Windows
                    // post-dates it, so a missing entry point means the host is not ARM64.
                }
                catch (DllNotFoundException)
                {
                    // kernel32.dll is unavailable (non-Windows runtime); fall through.
                }

                return Is64BitOperatingSystem ? ProcessorArchitectureType.X64 : ProcessorArchitectureType.X86;
            }
        }

        public bool IsArm64OperatingSystem
        {
            get { return NativeProcessorArchitecture == ProcessorArchitectureType.Arm64; }
        }

        public bool UserInteractive
        {
            get { return System.Environment.UserInteractive; }
        }

        public string NewLine
        {
            get { return System.Environment.NewLine; }
        }

        public string CurrentDirectory
        {
            get { return System.Environment.CurrentDirectory; }
        }

        public string ExpandEnvironmentVariables(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

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

        // ReSharper disable InconsistentNaming
        private const ushort ImageFileMachineI386 = 0x014C;
        private const ushort ImageFileMachineAmd64 = 0x8664;
        private const ushort ImageFileMachineArm64 = 0xAA64;

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process2(IntPtr process, out ushort processMachine, out ushort nativeMachine);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();
        // ReSharper restore InconsistentNaming
    }
}
