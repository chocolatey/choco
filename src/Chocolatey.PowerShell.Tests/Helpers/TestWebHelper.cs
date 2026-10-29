// Copyright © 2017 - 2026 Chocolatey Software, Inc
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

using System.IO;
using System.Management.Automation;
using System.Net;
using System.Threading;
using Chocolatey.PowerShell.Helpers;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    public class TestWebHelper : WebHelper
    {
        public TestWebHelper(PSCmdlet cmdlet, WebRequestOptions options)
            : base(cmdlet, options)
        {
        }

        public TestWebHelper(PSCmdlet cmdlet, WebRequestOptions options, CancellationToken cancellationToken)
            : base(cmdlet, options, cancellationToken)
        {
        }

        public int Read(Stream stream, byte[] buffer)
        {
            return ReadBuffer(stream, buffer);
        }

        public HttpWebRequest CreateRequest()
        {
            return CreateWebRequest(Options);
        }

        public void ApplyProxyConfiguration(WebRequest request)
        {
            base.SetProxyConfiguration(request);
        }

        protected override void SetProxyConfiguration(WebRequest request)
        {
        }
    }
}
