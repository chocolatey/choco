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
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security;
using System.Security.Principal;
using chocolatey.infrastructure.platforms;
using Microsoft.Win32.SafeHandles;

namespace chocolatey.infrastructure.information
{
    public sealed class ProcessInformation
    {
        public static bool UserIsAdministrator()
        {
            if (Platform.GetPlatform() != PlatformType.Windows)
            {
                return false;
            }

            var isAdmin = false;

            using (var identity = WindowsIdentity.GetCurrent())
            {
                if (identity != null)
                {
                    var principal = new WindowsPrincipal(identity);
                    isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

                    // Any version of Windows less than 6 does not have UAC
                    // so bail with the answer from the above check
                    if (Platform.GetVersion().Major < 6)
                    {
                        return isAdmin;
                    }

                    if (!isAdmin)
                    {
                        // Processes subject to UAC actually have the Administrators group
                        // stripped out from the process, and will return false for any
                        // check about being an administrator, including a check against
                        // the native `CheckTokenMembership` or `UserIsAdmin`. Instead we
                        // need to perform a not 100% answer on whether they are an admin
                        // based on if we have a split token.
                        // Crediting http://www.davidmoore.info/blog/2011/06/20/how-to-check-if-the-current-user-is-an-administrator-even-if-uac-is-on/
                        // and http://blogs.msdn.com/b/cjacks/archive/2006/10/09/how-to-determine-if-a-user-is-a-member-of-the-administrators-group-with-uac-enabled-on-windows-vista.aspx
                        // NOTE: from the latter (the original) -
                        //    Note that this technique detects if the token is split or not.
                        //    In the vast majority of situations, this will determine whether
                        //    the user is running as an administrator. However, there are
                        //    other user types with advanced permissions which may generate a
                        //    split token during an interactive login (for example, the
                        //    Network Configuration Operators group). If you are using one of
                        //    these advanced permission groups, this technique will determine
                        //    the elevation type, and not the presence (or absence) of the
                        //    administrator credentials.
                        "chocolatey".Log().Debug(@"User may be subject to UAC, checking for a split token (not 100%
 effective).");

                        var tokenInfLength = Marshal.SizeOf(typeof(int));
                        IntPtr tokenInformation = Marshal.AllocHGlobal(tokenInfLength);

                        try
                        {
                            var token = identity.Token;
                            var successfulCall = GetTokenInformation(token, TokenInformationType.TokenElevationType, tokenInformation, tokenInfLength, out tokenInfLength);

                            if (!successfulCall)
                            {
                                "chocolatey".Log().Warn("Error during native GetTokenInformation call - {0}".FormatWith(Marshal.GetLastWin32Error()));
                                if (tokenInformation != IntPtr.Zero)
                                {
                                    Marshal.FreeHGlobal(tokenInformation);
                                }
                            }

                            var elevationType = (TokenElevationType)Marshal.ReadInt32(tokenInformation);

                            switch (elevationType)
                            {
                                // TokenElevationTypeFull - User has a split token, and the process is running elevated. Assuming they're an administrator.
                                case TokenElevationType.TokenElevationTypeFull:
                                // TokenElevationTypeLimited - User has a split token, but the process is not running elevated. Assuming they're an administrator.
                                case TokenElevationType.TokenElevationTypeLimited:
                                    isAdmin = true;
                                    break;
                            }
                        }
                        finally
                        {
                            if (tokenInformation != IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(tokenInformation);
                            }
                        }
                    }
                }
            }

            return isAdmin;
        }

        public static bool IsElevated()
        {
            if (Platform.GetPlatform() != PlatformType.Windows)
            {
                return false;
            }

            using (var identity = WindowsIdentity.GetCurrent(TokenAccessLevels.Query | TokenAccessLevels.Duplicate))
            {
                if (identity != null)
                {
                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }

            return false;
        }

        public static bool UserIsTerminalServices()
        {
            return Environment.GetEnvironmentVariable("SESSIONNAME").ToStringSafe().ContainsSafe("rdp-");
        }

        public static bool UserIsRemote()
        {
            return UserIsTerminalServices() || Environment.GetEnvironmentVariable("SESSIONNAME").ToStringSafe() == string.Empty;
        }

        public static bool UserIsSystem()
        {
            if (Platform.GetPlatform() != PlatformType.Windows)
            {
                return false;
            }

            var isSystem = false;

            using (var identity = WindowsIdentity.GetCurrent())
            {
                isSystem = identity.IsSystem;
            }

            return isSystem;
        }

        public static ProcessTree GetProcessTree()
        {
            return GetProcessTree(null);
        }

        public static ProcessTree GetProcessTree(Process process)
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

            try
            {
                try
                {
                    tree = PopulateProcessTreeInternal(tree, process);
                }
                catch (TypeLoadException ex) when (ex is DllNotFoundException || ex is EntryPointNotFoundException)
                {
                    try
                    {
                        // These exceptions mean the lookup failed because the DLL is missing or the entry point is no longer present.
                        // Ignore these and fall back to the alternative p/invoke method if we haven't already.
                        tree = PopulateProcessTreeStable(tree, process);
                    }
                    catch (TypeLoadException)
                    {
                        "chocolatey".Log().Warn(logging.ChocolateyLoggers.LogFileOnly, "All available methods of querying processes from the win32 APIs are broken or critical DLLs are missing.");
                    }
                }
            }
            catch (Win32Exception ex)
            {
                "chocolatey".Log().Warn(logging.ChocolateyLoggers.LogFileOnly, "Unhandled Win32Exception ({0}) in finding parent processes.", ex.Message);
            }

            return tree;
        }

        private static ProcessTree PopulateProcessTreeInternal(ProcessTree tree, Process currentProcess)
        {
            Process nextProcess = null;
            try
            {
                while (true)
                {
                    var parentProcess = ParentProcessHelperInternal.GetParentProcess(nextProcess ?? currentProcess);

                    if (parentProcess is null)
                    {
                        break;
                    }

                    nextProcess = parentProcess;
                    tree.Processes.AddLast(nextProcess.ProcessName);
                }
            }
            catch (Win32Exception ex)
            {
                // Native error code 5 is access denied.
                // This usually happens if the parent executable
                // is running as a different user, in which case
                // we are not able to get the necessary handle for
                // the process.
                if (ex.NativeErrorCode != 5)
                {
                    throw;
                }
                else
                {
                    "chocolatey".Log().Debug(logging.ChocolateyLoggers.LogFileOnly, "Unable to get parent process for '{0}'. Ignoring...", currentProcess.ProcessName);
                }
            }

            return tree;
        }

        private static ProcessTree PopulateProcessTreeStable(ProcessTree tree, Process currentProcess)
        {
            Process nextProcess = null;
            try
            {
                while (true)
                {
                    var parentProcess = ParentProcessHelperStable.GetParentProcess(nextProcess ?? currentProcess);

                    if (parentProcess is null)
                    {
                        break;
                    }

                    nextProcess = parentProcess;
                    tree.Processes.AddLast(nextProcess.ProcessName);
                }
            }
            catch (Win32Exception ex)
            {
                // Native error code 5 is access denied.
                // This usually happens if the parent executable
                // is running as a different user, in which case
                // we are not able to get the necessary handle for
                // the process.
                if (ex.NativeErrorCode != 5)
                {
                    throw;
                }
                else
                {
                    "chocolatey".Log().Debug(logging.ChocolateyLoggers.LogFileOnly, "Unable to get parent process for '{0}'. Ignoring...", currentProcess.ProcessName);
                }
            }

            return tree;
        }


        /*
         https://msdn.microsoft.com/en-us/library/windows/desktop/aa376402.aspx
         BOOL WINAPI ConvertStringSidToSid(
           _In_   LPCTSTR StringSid,
           _Out_  PSID *Sid
         );
         */

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool ConvertStringSidToSid(string stringSid, out IntPtr sid);

        /*
         https://msdn.microsoft.com/en-us/library/windows/desktop/aa376389.aspx
         BOOL WINAPI CheckTokenMembership(
            _In_opt_  HANDLE TokenHandle,
            _In_      PSID SidToCheck,
            _Out_     PBOOL IsMember
         );

         */

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CheckTokenMembership(IntPtr tokenHandle, IntPtr sidToCheck, out bool isMember);

        /*
          https://msdn.microsoft.com/en-us/library/windows/desktop/aa446671.aspx
          BOOL WINAPI GetTokenInformation(
            _In_       HANDLE TokenHandle,
            _In_       TOKEN_INFORMATION_CLASS TokenInformationClass,
            _Out_opt_  LPVOID TokenInformation,
            _In_       DWORD TokenInformationLength,
            _Out_      PDWORD ReturnLength
          );


        */
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation(IntPtr tokenHandle, TokenInformationType tokenInformationClass, IntPtr tokenInformation, int tokenInformationLength, out int returnLength);

        /// <summary>
        /// Passed to <see cref="GetTokenInformation"/> to specify what
        /// information about the token to return.
        /// </summary>
        private enum TokenInformationType
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUiAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        /// <summary>
        /// The elevation type for a user token.
        /// </summary>
        private enum TokenElevationType
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ParentProcessHelperInternal
        {
            internal IntPtr Reserved1;
            internal IntPtr PebBaseAddress;
            internal IntPtr Reserved2_0;
            internal IntPtr Reselved2_1;
            internal IntPtr UniqueProcessId;
            internal IntPtr InheritedFromUniqueProcessId;

            [DllImport("ntdll.dll")]
            private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ParentProcessHelperInternal processInformation, int processInformationLength, out int returnLength);

            public static Process GetParentProcess(Process process)
            {
                return GetParentProcess(process.Handle);
            }

            public static Process GetParentProcess(IntPtr handle)
            {
                try
                {
                    var processUtilities = new ParentProcessHelperInternal();

                    // https://learn.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntqueryinformationprocess#process_basic_information
                    // Retrieves a pointer to a PEB structure that can be used to determine whether the specified process is being debugged,
                    // and a unique value used by the system to identify the specified process. 
                    // It also includes the `InheritedFromUniqueProcessId` value which we can use to look up the parent process directly.
                    const int processBasicInformation = 0;

                    var status = NtQueryInformationProcess(handle, processBasicInformation, ref processUtilities, Marshal.SizeOf(processUtilities), out _);

                    if (status != 0)
                    {
                        return null;
                    }

                    return Process.GetProcessById(processUtilities.InheritedFromUniqueProcessId.ToInt32());
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }
        }


        private static class ParentProcessHelperStable
        {
            public static Process GetParentProcess(Process process)
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

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static bool user_is_administrator()
            => UserIsAdministrator();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static bool process_is_elevated()
            => IsElevated();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static bool user_is_terminal_services()
            => UserIsTerminalServices();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static bool user_is_remote()
            => UserIsRemote();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static bool user_is_system()
            => UserIsSystem();
#pragma warning restore IDE0022, IDE1006
    }
}
