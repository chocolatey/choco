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
using System.Collections;
using System.Management.Automation;
using Chocolatey.PowerShell.Helpers;
using Chocolatey.PowerShell.Shared;

namespace Chocolatey.PowerShell.Commands
{
    [Cmdlet(VerbsData.Save, "ChocolateyWebFile")]
    [OutputType(typeof(string))]
    public class SaveChocolateyWebFileCommand : WebHelperCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string PackageName { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        [Alias("FileFullPath")]
        public string Path { get; set; }

        [Parameter(Position = 2)]
        public string Url { get; set; } = string.Empty;

        [Parameter(Position = 3)]
        [Alias("Url64")]
        public string Url64Bit { get; set; } = string.Empty;

        [Parameter()]
        public string Checksum { get; set; } = string.Empty;

        [Parameter()]
        public ChecksumType ChecksumType { get; set; } = ChecksumType.Md5;

        [Parameter()]
        public string Checksum64 { get; set; } = string.Empty;

        [Parameter()]
        public ChecksumType? ChecksumType64 { get; set; }

        [Parameter()]
        public Hashtable Options { get; set; } = new Hashtable(StringComparer.OrdinalIgnoreCase) {
            { "Headers", new Hashtable(StringComparer.OrdinalIgnoreCase) }
        };

        [Parameter()]
        [Alias("GetOriginalFileName")]
        public SwitchParameter UseOriginalFileName { get; set; }

        [Parameter()]
        [Alias("ForceDownload")]
        public SwitchParameter Force { get; set; }

        protected override void End()
        {
            var options = new ChocolateyWebFileOptions
            {
                PackageName = PackageName,
                FilePath = Path,
                Url = Url,
                Url64Bit = Url64Bit,
                Checksum = Checksum,
                ChecksumType = ChecksumType,
                Checksum64 = Checksum64,
                ChecksumType64 = ChecksumType64 ?? ChecksumType,
                Headers = Options["Headers"] as IDictionary,
                UseOriginalFileName = UseOriginalFileName,
                ForceDownload = Force,
            };

            InvokeHelper(
                Path,
                new WebFileDownloader(this, PipelineStopToken),
                downloader => WriteObject(downloader.Download(options)));
        }
    }
}