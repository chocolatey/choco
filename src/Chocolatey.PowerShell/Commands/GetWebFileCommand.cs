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

using Chocolatey.PowerShell.Helpers;
using Chocolatey.PowerShell.Shared;
using System;
using System.Collections;
using System.Management.Automation;

namespace Chocolatey.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "WebFile")]
    public class GetWebFileCommand : WebHelperCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        [Alias("Uri")]
        public string Url { get; set; }

        [Parameter(Position = 1)]
        [Alias("FileName")]
        public string Path { get; set; } = string.Empty;

        [Parameter(Position = 2)]
        public string UserAgent { get; set; } = WebRequestOptions.DefaultUserAgent;

        [Parameter()]
        public SwitchParameter Passthru { get; set; }

        [Parameter()]
        public SwitchParameter Quiet { get; set; }

        [Parameter()]
        public Hashtable Options { get; set; } = new Hashtable(StringComparer.OrdinalIgnoreCase) {
            { "Headers", new Hashtable(StringComparer.OrdinalIgnoreCase) }
        };

        protected override void End()
        {
            var uri = new Uri(Url);
            if (uri.IsFile)
            {
                if (uri.LocalPath != Path)
                {
                    WriteDebug("Url is local file, setting destination");
                    CopyLocalFile(uri.LocalPath, Path);
                }

                return;
            }

            InvokeWebHelper(
                new WebRequestOptions
                {
                    Uri = uri,
                    UserAgent = UserAgent,
                    Headers = Options["Headers"] as IDictionary
                },
                helper => helper.DownloadHttpFile(Path, Passthru, Quiet));
        }

        /// <summary>
        /// Copies the local file at <paramref name="source"/> to <paramref name="destination"/>.
        /// </summary>
        /// <param name="source">The local path of the file to copy.</param>
        /// <param name="destination">The path to copy the file to.</param>
        protected virtual void CopyLocalFile(string source, string destination)
        {
            PSHelper.EnsureDirectoryExists(this, PSHelper.GetParentDirectory(this, destination));
            PSHelper.CopyFile(this, source, destination, true);
        }
    }
}
