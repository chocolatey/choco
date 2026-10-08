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
using System.Net;

namespace Chocolatey.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "WebFile")]
    public class GetWebFileCommand : ChocolateyCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        [Alias("Uri")]
        public string Url { get; set; }

        [Parameter(Position = 1)]
        [Alias("FileName")]
        public string Path { get; set; } = string.Empty;

        [Parameter(Position = 2)]
        public string UserAgent { get; set; } = "chocolatey command line";

        [Parameter()]
        public SwitchParameter Passthru { get; set; }

        [Parameter()]
        public SwitchParameter Quiet { get; set; }

        [Parameter()]
        public Hashtable Options { get; set; } = new Hashtable(StringComparer.OrdinalIgnoreCase) {
            { "Headers", new Hashtable(StringComparer.OrdinalIgnoreCase) }
        };

        private WebHelper _helper;

        protected override void End()
        {
            var uri = new Uri(Url);
            if (uri.IsFile && uri.LocalPath != Path)
            {
                WriteDebug("Url is local file, setting destination");
                PSHelper.CopyFile(this, Url, Path, true);
                return;
            }

            try
            {
                var options = new WebRequestOptions
                {
                    Uri = uri,
                    UserAgent = UserAgent,
                    Headers = Options["Headers"] as IDictionary
                };

                using (_helper = new WebHelper(this, options, PipelineStopToken))
                {
                    _helper.DownloadHttpFile(Path, Passthru, Quiet);
                }
            }
            catch (WebException error)
            {
                ThrowTerminatingError(new ErrorRecord(
                    error,
                    errorId: $"{ErrorId}.WebException",
                    ErrorCategory.ResourceUnavailable,
                    targetObject: Url));
            }
            catch (OperationCanceledException)
            {
                // The PipelineStopToken that is passed into the WebHelper to handle cancellation
                // is designed to trigger only when StopProcessing() is called. As such, the
                // appropriate behaviour when the task is cancelled is to correctly wrap it
                // as a PipelineStoppedException.
                throw new PipelineStoppedException();
            }
            catch (Exception error)
            {
                ThrowTerminatingError(new ErrorRecord(
                    error,
                    errorId: $"{ErrorId}.UnknownError",
                    ErrorCategory.NotSpecified,
                    targetObject: Url));
            }
        }

        protected override void Stop()
        {
            // Ensure we dispose the helper properly if the operation is stopped,
            // since calls to Stop() happen on another thread entirely.
            _helper?.Dispose();
        }
    }
}
