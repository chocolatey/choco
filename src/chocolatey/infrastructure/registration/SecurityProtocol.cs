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

namespace chocolatey.infrastructure.registration
{
    using System;
    using System.Net;
    using app.configuration;
    using logging;
    using platforms;

    public sealed class SecurityProtocol
    {
        public static void SetProtocol(ChocolateyConfiguration config)
        {
            if (config.Information.PlatformVersion < Version.Parse("6.2") && config.Information.PlatformType == PlatformType.Windows)
            {
                // If the Windows version is less than 8.0/Server 2012, explicitly enable TLS 1.2
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
            }
            else
            {
                // Otherwise, let the OS handle it as per Microsoft best practices
                // https://web.archive.org/web/20230321032852/https://learn.microsoft.com/en-us/dotnet/framework/network-programming/tls

                // Windows 10 and Server 1019 do not support TLS 1.3
                // But TLS 1.3 is both available and enabled by default in Windows 11 and Server 2022, so no need to explicitly enable it (unlike TLS 1.2 in Windows 7)
                // https://web.archive.org/web/20230321032830/https://learn.microsoft.com/en-us/windows/win32/secauthn/protocols-in-tls-ssl--schannel-ssp-

                ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;
            }

            try
            {
                if (ServicePointManager.ServerCertificateValidationCallback != null)
                {
                    "chocolatey".Log().Warn("ServerCertificateValidationCallback was set to '{0}' Removing.".FormatWith(System.Net.ServicePointManager.ServerCertificateValidationCallback));
                    ServicePointManager.ServerCertificateValidationCallback = null;
                }
            }
            catch (Exception ex)
            {
                "chocolatey".Log().Warn("Error resetting ServerCertificateValidationCallback: {0}".FormatWith(ex.Message));
            }
        }


#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static void set_protocol(ChocolateyConfiguration config, bool provideWarning)
            => SetProtocol(config);
#pragma warning restore IDE1006
    }
}
