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

using Chocolatey.PowerShell.Helpers;
using Chocolatey.PowerShell.Shared;
using System.Management.Automation;

namespace Chocolatey.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "WebFileName")]
    public class GetWebFileNameCommand : WebHelperCmdlet
    {
        [Parameter(Position = 0)]
        [Alias("Uri")]
        public string Url { get; set; } = string.Empty;

        [Parameter(Position = 1, Mandatory = true)]
        public string DefaultName { get; set; }

        [Parameter()]
        public string UserAgent { get; set; } = WebRequestOptions.DefaultUserAgent;

        protected override void End()
        {
            InvokeWebHelper(
                WebRequestOptions.ForUrl(Url, UserAgent),
                helper => WriteObject(helper.GetRemoteFileName(DefaultName)));
        }
    }
}
