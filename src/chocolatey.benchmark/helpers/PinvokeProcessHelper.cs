using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Security.Permissions;
using System.Security;
using chocolatey.infrastructure.information;
using chocolatey.infrastructure.platforms;

namespace chocolatey.benchmark.helpers
{
    internal class PinvokeProcessHelper
    {
        private static readonly string[] _filteredParents = new[]
        {
            "explorer",
            "powershell",
            "pwsh",
            "cmd",
            "bash",
            // The name used to launch windows services
            // in the operating system.
            "services",
            // Known Terminal Emulators
            "Tabby",
            "WindowsTerminal",
            "FireCMD",
            "ConEmu64",
            "ConEmuC64"
        };

        public static ProcessTree GetDocumentedProcessTree(Process process = null)
        {
            if (process == null)
            {
                process = Process.GetCurrentProcess();
            }

            var tree = new ProcessTree(process.ProcessName);

            if (Platform.GetPlatform() != PlatformType.Windows)
            {
                return tree;
            }

            Process nextProcess = null;

            while (true)
            {
                var foundProcess = ParentDocumentedHelper.ParentProcess(nextProcess ?? process);

                if (foundProcess == null)
                {
                    break;
                }

                nextProcess = foundProcess;
                tree.Processes.AddLast(nextProcess.ProcessName);
            }

            return tree;
        }

        public static string GetDocumentedParent(Process process = null)
        {
            if (Platform.GetPlatform() != PlatformType.Windows)
            {
                return null;
            }

            if (process == null)
            {
                process = Process.GetCurrentProcess();
            }

            Process nextProcess = null;

            while (true)
            {
                var foundProcess = ParentDocumentedHelper.ParentProcess(nextProcess ?? process);

                if (foundProcess == null)
                {
                    break;
                }

                nextProcess = foundProcess;
            }

            return nextProcess?.ProcessName;
        }

        public static string GetDocumentedParentFiltered(Process process = null)
        {
            if (Platform.GetPlatform() != PlatformType.Windows)
            {
                return null;
            }

            if (process == null)
            {
                process = Process.GetCurrentProcess();
            }

            Process nextProcess = null;
            Process selectedProcess = null;

            while (true)
            {
                var foundProcess = ParentDocumentedHelper.ParentProcess(nextProcess ?? process);

                if (foundProcess == null)
                {
                    break;
                }

                nextProcess = foundProcess;

                if (!IsIgnoredParent(nextProcess.ProcessName))
                {
                    selectedProcess = nextProcess;
                }
            }

            return selectedProcess?.ProcessName;
        }

        public static ProcessTree GetUndocumentedProcessTree(Process process = null)
        {
            if (process == null)
            {
                process = Process.GetCurrentProcess();
            }

            var tree = new ProcessTree(process.ProcessName);

            Process nextProcess = null;

            while (true)
            {
                var parentProcess = ParentProcessUtilities.GetParentProcess(nextProcess ?? process);

                if (parentProcess == null)
                {
                    break;
                }

                nextProcess = parentProcess;
                tree.Processes.AddLast(nextProcess.ProcessName);
            }

            return tree;
        }

        public static string GetUndocumentedParent(Process process = null)
        {
            if (Platform.GetPlatform() != PlatformType.Windows)
            {
                return null;
            }

            if (process == null)
            {
                process = Process.GetCurrentProcess();
            }

            Process nextProcess = null;

            while (true)
            {
                var foundProcess = ParentProcessUtilities.GetParentProcess(nextProcess ?? process);

                if (foundProcess == null)
                {
                    break;
                }

                nextProcess = foundProcess;
            }

            return nextProcess?.ProcessName;
        }

        public static string GetUndocumentedParentFiltered(Process process = null)
        {
            if (Platform.GetPlatform() != PlatformType.Windows)
            {
                return null;
            }

            if (process == null)
            {
                process = Process.GetCurrentProcess();
            }

            Process nextProcess = null;
            Process selectedProcess = null;

            while (true)
            {
                var foundProcess = ParentProcessUtilities.GetParentProcess(nextProcess ?? process);

                if (foundProcess == null)
                {
                    break;
                }

                nextProcess = foundProcess;

                if (!IsIgnoredParent(nextProcess.ProcessName))
                {
                    selectedProcess = nextProcess;
                }
            }

            return selectedProcess?.ProcessName;
        }

        private static bool IsIgnoredParent(string processName)
        {
            return _filteredParents.Contains(processName, StringComparer.OrdinalIgnoreCase);
        }

        private class ParentDocumentedHelper
        {
            public static Process ParentProcess(Process process)
            {
                try
                {
                    var processId = ParentProcessId(process.Id);

                    if (processId == -1)
                    {
                        return null;
                    }
                    else
                    {
                        return Process.GetProcessById(processId);
                    }
                }
                catch
                {
                    return null;
                }
            }

            private static int ParentProcessId(int id)
            {
                var pe32 = new PROCESSENTRY32
                {
                    dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32))
                };

                using (var hSnapshot = CreateToolhelp32Snapshot(SnapshotFlags.Process, (uint)id))
                {
                    if (hSnapshot.IsInvalid)
                    {
                        throw new Win32Exception();
                    }

                    if (!Process32First(hSnapshot, ref pe32))
                    {
                        var errno = Marshal.GetLastWin32Error();

                        if (errno == ERROR_NO_MORE_FILES)
                        {
                            return -1;
                        }

                        throw new Win32Exception(errno);
                    }

                    do
                    {
                        if (pe32.th32ProcessID == (uint)id)
                        {
                            return (int)pe32.th32ParentProcessID;
                        }
                    } while (Process32Next(hSnapshot, ref pe32));
                }

                return -1;
            }

            private const int ERROR_NO_MORE_FILES = 0x12;
            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern SafeSnapshotHandle CreateToolhelp32Snapshot(SnapshotFlags flags, uint id);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool Process32First(SafeSnapshotHandle hSnapshot, ref PROCESSENTRY32 lppe);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool Process32Next(SafeSnapshotHandle hSnapshot, ref PROCESSENTRY32 lppe);

            [Flags]
            private enum SnapshotFlags : uint
            {
                HeapList = 0x00000001,
                Process = 0x00000002,
                Thread = 0x00000004,
                Module = 0x00000008,
                Module32 = 0x00000010,
                All = (HeapList | Process | Thread | Module),
                Inherit = 0x80000000,
                NoHeaps = 0x40000000
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct PROCESSENTRY32
            {
#pragma warning disable IDE1006 // Naming Styles
                public uint dwSize;
                public uint cntUsage;
                public uint th32ProcessID;
                public IntPtr th32DefaultHeapID;
                public uint th32ModuleID;
                public uint cntThreads;
                public uint th32ParentProcessID;
                public int pcPriClassBase;
                public uint dwFlags;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                public string szExeFile;
#pragma warning restore IDE1006 // Naming Styles
            }

            [SuppressUnmanagedCodeSecurity, HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
            internal sealed class SafeSnapshotHandle : SafeHandleMinusOneIsInvalid
            {
                internal SafeSnapshotHandle()
                    : base(true)
                {
                }

                [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
                internal SafeSnapshotHandle(IntPtr handle)
                    : base(true)
                {
                    SetHandle(handle);
                }

                protected override bool ReleaseHandle()
                {
                    return CloseHandle(handle);
                }

                [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
                [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
                private static extern bool CloseHandle(IntPtr handle);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ParentProcessUtilities
        {
            internal IntPtr Reserved1;
            internal IntPtr PebBaseAddress;
            internal IntPtr Reserved2_0;
            internal IntPtr Reselved2_1;
            internal IntPtr UniqueProcessId;
            internal IntPtr InheritedFromUniqueProcessId;

            [DllImport("ntdll.dll")]
            private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ParentProcessUtilities processInformaiton, int processInformationLength, out int returnLength);

            public static Process GetParentProcess(Process process)
            {
                return GetParentProcess(process.Handle);
            }

            public static Process GetParentProcess(IntPtr handle)
            {
                var processUtilities = new ParentProcessUtilities();
                var status = NtQueryInformationProcess(handle, 0, ref processUtilities, Marshal.SizeOf(processUtilities), out _);

                if (status != 0)
                {
                    return null;
                }

                try
                {
                    return Process.GetProcessById(processUtilities.InheritedFromUniqueProcessId.ToInt32());
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }
        }
    }
}
