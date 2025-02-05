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
using System.IO;
using System.Net;

namespace chocolatey.tests
{
    public static class UNCHelper
    {
        public static string ConvertLocalFolderPathToIpBasedUncPath(string localFolderName)
        {
            var rootFolder = Path.GetPathRoot(localFolderName);
            var ipAddress = ReturnMachineId();

            return string.Format("\\\\{0}\\\\{1}$\\{2}", ipAddress, rootFolder.TrimEnd('\\', ':'), localFolderName.Substring(rootFolder.Length));
        }

        private static IPAddress ReturnMachineId()
        {
            var hostName = Dns.GetHostName();
            var ipEntry = Dns.GetHostEntry(hostName);
            var addr = ipEntry.AddressList;
            IPAddress ipV4 = null;

            foreach (var item in addr)
            {
                if (item.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ipV4 = item;
                    break;
                }
            }

            if (ipV4 == null)
            {
                throw new ApplicationException("You have no IP of Version 4.");
            }

            return ipV4;
        }
    }
}
