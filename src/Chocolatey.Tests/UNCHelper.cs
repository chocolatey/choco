namespace Chocolatey.Tests
{
    using System;
    using System.IO;
    using System.Net;

    public static class UNCHelper
    {
        public static string convert_local_folder_path_to_ip_based_unc_path(string localFolderName)
        {
            var rootFolder = Path.GetPathRoot(localFolderName);
            var ipAddress = return_machine_ip();

            return string.Format("\\\\{0}\\\\{1}$\\{2}", ipAddress, rootFolder.TrimEnd('\\', ':'), localFolderName.Substring(rootFolder.Length));
        }

        private static IPAddress return_machine_ip()
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
