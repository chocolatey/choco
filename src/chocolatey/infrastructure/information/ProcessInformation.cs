// Copyright © 2011 - Present RealDimensions Software, LLC
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

namespace chocolatey.infrastructure.information
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security.Principal;
    using platforms;

    public sealed class ProcessInformation
    {
        public static bool user_is_administrator()
        {
            var isAdmin = false;

            using (var identity = WindowsIdentity.GetCurrent())
            {
                if (identity != null)
                {
                    var principal = new WindowsPrincipal(identity);
                    isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

                    if (!isAdmin && Platform.get_platform() == PlatformType.Windows)
                    {
                        // We could be subject to a user token under UAC. If we are on Windows, we can perform
                        // a native call to CheckTokenMembership to determine if the user is an admin, 
                        // even when the process is not elevated
                        "chocolatey".Log().Debug("User may be subject to UAC, checking against native CheckTokenMembership.");

                        var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                        IntPtr adminSidPtr = new IntPtr();
                        var success = ConvertStringSidToSid(adminSid.Value, out adminSidPtr);
                        if (!success)
                        {
                            "chocolatey".Log().Warn("Error during native ConvertStringSidToSid call - {0}".format_with(Marshal.GetLastWin32Error()));
                        }

                        success = CheckTokenMembership(IntPtr.Zero, adminSidPtr, out isAdmin);
                        if (!success)
                        {
                            "chocolatey".Log().Warn("Error during native CheckTokenMembership call - {0}".format_with(Marshal.GetLastWin32Error()));
                        }
                    }
                }
            }

            return isAdmin;
        }

        public static bool process_is_elevated()
        {
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
    }
}