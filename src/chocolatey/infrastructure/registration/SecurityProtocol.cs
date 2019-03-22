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

namespace chocolatey.infrastructure.registration
{
    using System;
    using System.Net;
    using app.configuration;
    using logging;

    public sealed class SecurityProtocol
    {
        private const int TLS_1_1 = 768;
        private const int TLS_1_2 = 3072;

        public static void set_protocol(ChocolateyConfiguration config, bool provideWarning)
        {
            try
            {
                // We can't address the protocols directly when built with .NET 
                // Framework 4.0. However if someone is running .NET 4.5 or 
                // greater, they have in-place upgrades for System.dll, which
                // will allow us to set these protocols directly.
                const SecurityProtocolType tls11 = (SecurityProtocolType)TLS_1_1;
                const SecurityProtocolType tls12 = (SecurityProtocolType)TLS_1_2;
                ServicePointManager.SecurityProtocol = tls12 | tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
            }
            catch (Exception)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
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
