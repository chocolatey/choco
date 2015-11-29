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

namespace chocolatey.infrastructure.registration
{
    using System.Net;
    using logging;

    public sealed class SecurityProtocol
    {
        public static void set_protocol()
        {
#if NETFX_45
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3; 
#else
            ServicePointManager.SecurityProtocol =  SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
            "chocolatey".Log().Warn(ChocolateyLoggers.Important,
@" !!WARNING!!
Choco prefers to use TLS v1.2 if it is available, but this client is 
 built on .NET 4.0, which uses an older SSL. It's using TLS 1.0 or 
 earlier, which makes it susceptible to BEAST and also doesn't 
 implement the 1/n-1 record splitting mitigation for Cipher-Block 
 Chaining. 

 For more information you should visit https://www.howsmyssl.com/");
#endif 
            }
    }
}
