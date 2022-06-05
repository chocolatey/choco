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

    public sealed class SecurityProtocol
    {
        private const SecurityProtocolType SYSTEM_DEFAULT = (SecurityProtocolType)(0);
        private const SecurityProtocolType SSL_3 = (SecurityProtocolType)(48);
        private const SecurityProtocolType TLS_1_0 = (SecurityProtocolType)(192);
        private const SecurityProtocolType TLS_1_1 = (SecurityProtocolType)(768);
        private const SecurityProtocolType TLS_1_2 = (SecurityProtocolType)(3072);
        private const SecurityProtocolType TLS_1_3 = (SecurityProtocolType)(12288);

        public static void set_protocol(ChocolateyConfiguration config, bool provideWarning)
        {
            try
            {
                // We can't address the protocols directly when built with .NET 
                // Framework 4.0. However if someone is running .NET 4.5 or 
                // greater, they have in-place upgrades for System.dll, which
                // will allow us to set these protocols directly.
                SecurityProtocolType allowedProtocolsList = SYSTEM_DEFAULT;
                foreach (string protocol in config.SecurityProtocols.AllowedSecurityProtocol.Split(','))
                {
                    switch (protocol.to_lower())
                    {
                        case "ssl3":
                            allowedProtocolsList = allowedProtocolsList | SSL_3;
                            break;
                        case "tls":
                            allowedProtocolsList = allowedProtocolsList | TLS_1_0;
                            break;
                        case "tls11":
                            allowedProtocolsList = allowedProtocolsList | TLS_1_1;
                            break;
                        case "tls12":
                            allowedProtocolsList = allowedProtocolsList | TLS_1_2;
                            break;
                        case "tls13":
                            allowedProtocolsList = allowedProtocolsList | TLS_1_3;
                            break;
                        default:
                            break;
                    }
                }
                if (allowedProtocolsList == SYSTEM_DEFAULT)
                    ServicePointManager.SecurityProtocol = TLS_1_3 | TLS_1_2 | TLS_1_1 | TLS_1_0 | SSL_3;
                else
                    ServicePointManager.SecurityProtocol = allowedProtocolsList;
            }
            catch (Exception)
            {
                if (string.IsNullOrWhiteSpace(config.SecurityProtocols.AllowedSecurityProtocol)){
                    ServicePointManager.SecurityProtocol = TLS_1_0 | SSL_3;
                }
                //todo: provide this warning with the ability to opt out of seeing it again so we can move it up to more prominent visibility and not just the verbose log
                if (provideWarning)
                {
                "chocolatey".Log().Warn(ChocolateyLoggers.Verbose,
@" !!WARNING!!
Choco prefers to use TLS v1.2 if it is available, but this client is 
 running on .NET 4.0, which uses an older SSL. It's using TLS 1.0 or 
 earlier, which makes it susceptible to BEAST and also doesn't 
 implement the 1/n-1 record splitting mitigation for Cipher-Block 
 Chaining. Upgrade to at least .NET 4.5 at your earliest convenience.

 For more information you should visit https://www.howsmyssl.com/");
                }
            }

            try
            {
                if (ServicePointManager.ServerCertificateValidationCallback != null)
                {
                    "chocolatey".Log().Warn("ServerCertificateValidationCallback was set to '{0}' Removing.".format_with(System.Net.ServicePointManager.ServerCertificateValidationCallback));
                    ServicePointManager.ServerCertificateValidationCallback = null;
                }
            }
            catch (Exception ex)
            {
                "chocolatey".Log().Warn("Error resetting ServerCertificateValidationCallback: {0}".format_with(ex.Message));
            }

        }
    }
}
