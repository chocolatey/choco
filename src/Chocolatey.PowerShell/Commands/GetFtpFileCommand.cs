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
using System;
using System.Management.Automation;
using Chocolatey.PowerShell.Shared;

namespace Chocolatey.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "FtpFile")]
    public class GetFtpFileCommand : WebHelperCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        [Alias("Uri")]
        public string Url { get; set; }

        [Parameter(Position = 1, Mandatory = true)]
        [Alias("FileName")]
        public string Path { get; set; }

        [Parameter(Position = 2)]
        public string UserName { get; set; } = string.Empty;

        [Parameter(Position = 3)]
        public string Password { get; set; } = string.Empty;

        [Parameter()]
        public SwitchParameter Quiet { get; set; }

        protected override void End()
        {
            var uri = new Uri(Url);
            if (TryCopyLocalFile(uri, Path))
            {
                return;
            }

            InvokeFtpHelper(uri, UserName, Password, helper => helper.DownloadFile(Path, Quiet));
        }
    }
}