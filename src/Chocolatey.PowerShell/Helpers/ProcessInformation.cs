// Copyright © 2017 - 2024 Chocolatey Software, Inc
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

using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Chocolatey.PowerShell.Helpers
{
    public static class ProcessInformation
    {
        /// <summary>
        /// Helper method for determining current OS platform.
        /// </summary>
        /// <returns>True if the current OS is Windows.</returns>
        public static bool IsWindows()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        /// <summary>
        /// Determines whether the current process has administrative rights.
        /// </summary>
        /// <returns>True if running on Windows and the process has administrative rights.</returns>
        public static bool IsElevated()
        {
            if (!IsWindows())
            {
                return false;
            }

            using (var identity = WindowsIdentity.GetCurrent(TokenAccessLevels.Query | TokenAccessLevels.Duplicate))
            {
                if (identity is null)
                {
                    return false;
                }

                var principal = new WindowsPrincipal(identity);

                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}
