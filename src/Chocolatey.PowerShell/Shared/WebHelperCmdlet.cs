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
using System;
using System.Management.Automation;
using System.Net;

namespace Chocolatey.PowerShell.Shared
{
    /// <summary>
    /// Base class for cmdlets that make web requests through a <see cref="WebHelper"/>.
    /// </summary>
    public abstract class WebHelperCmdlet : ChocolateyCmdlet
    {
        private WebHelper _helper;

        /// <summary>
        /// Runs the <paramref name="operation"/> with a <see cref="WebHelper"/> for the <paramref name="options"/>,
        /// reporting any failure as a terminating error.
        /// </summary>
        /// <param name="options">The options for the web requests the operation makes.</param>
        /// <param name="operation">The operation to run.</param>
        protected void InvokeWebHelper(WebRequestOptions options, Action<WebHelper> operation)
        {
            try
            {
                using (_helper = new WebHelper(this, options, PipelineStopToken))
                {
                    operation(_helper);
                }
            }
            catch (WebException error)
            {
                ThrowTerminatingError(new ErrorRecord(
                    error,
                    errorId: $"{ErrorId}.WebException",
                    ErrorCategory.ResourceUnavailable,
                    targetObject: options.Uri?.OriginalString));
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
                    targetObject: options.Uri?.OriginalString));
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
