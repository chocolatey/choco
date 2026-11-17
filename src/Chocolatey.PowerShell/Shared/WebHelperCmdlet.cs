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
    /// Base class for cmdlets that make web requests through a <see cref="HttpHelper"/> or an <see cref="FtpHelper"/>.
    /// </summary>
    public abstract class WebHelperCmdlet : ChocolateyCmdlet
    {
        private IDisposable _helper;

        /// <summary>
        /// Runs the <paramref name="operation"/> with a <see cref="HttpHelper"/> for the <paramref name="options"/>,
        /// reporting any failure as a terminating error.
        /// </summary>
        /// <param name="options">The options for the web requests the operation makes.</param>
        /// <param name="operation">The operation to run.</param>
        protected void InvokeHttpHelper(WebRequestOptions options, Action<HttpHelper> operation)
        {
            Run(
                options.Uri?.OriginalString,
                () =>
                {
                    using (var helper = new HttpHelper(this, options, PipelineStopToken))
                    {
                        _helper = helper;
                        operation(helper);
                    }
                });
        }

        /// <summary>
        /// Runs the <paramref name="operation"/> with an <see cref="FtpHelper"/> for the address,
        /// reporting any failure as a terminating error.
        /// </summary>
        /// <param name="uri">The FTP address to make requests to.</param>
        /// <param name="userName">The user name to connect with.</param>
        /// <param name="password">The password to connect with.</param>
        /// <param name="operation">The operation to run.</param>
        protected void InvokeFtpHelper(Uri uri, string userName, string password, Action<FtpHelper> operation)
        {
            Run(
                uri.OriginalString,
                () =>
                {
                    using (var helper = new FtpHelper(this, uri, userName, password, PipelineStopToken))
                    {
                        _helper = helper;
                        operation(helper);
                    }
                });
        }

        /// <summary>
        /// Runs the <paramref name="operation"/> with the <paramref name="helper"/>, disposing the helper when it is done
        /// or when the command is stopped, and reporting any failure as a terminating error.
        /// </summary>
        /// <typeparam name="THelper">The type of the helper.</typeparam>
        /// <param name="target">The target of any error, such as the address being requested.</param>
        /// <param name="helper">The helper to run the operation with.</param>
        /// <param name="operation">The operation to run.</param>
        protected void InvokeHelper<THelper>(string target, THelper helper, Action<THelper> operation)
            where THelper : IDisposable
        {
            Run(
                target,
                () =>
                {
                    using (helper)
                    {
                        _helper = helper;
                        operation(helper);
                    }
                });
        }

        /// <summary>
        /// Copies a local file to the destination when the address is for a local file.
        /// </summary>
        /// <param name="uri">The address that was given.</param>
        /// <param name="destination">The path to copy the file to.</param>
        /// <returns><c>true</c> if the address is for a local file, in which case it has been dealt with, otherwise <c>false</c>.</returns>
        protected bool TryCopyLocalFile(Uri uri, string destination)
        {
            if (!uri.IsFile)
            {
                return false;
            }

            if (uri.LocalPath != destination)
            {
                WriteDebug("Url is local file, setting destination");
                CopyLocalFile(uri.LocalPath, destination);
            }

            return true;
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

        private void Run(string target, Action operation)
        {
            try
            {
                operation();
            }
            catch (WebException error)
            {
                ThrowTerminatingError(new ErrorRecord(
                    error,
                    errorId: $"{ErrorId}.WebException",
                    ErrorCategory.ResourceUnavailable,
                    targetObject: target));
            }
            catch (OperationCanceledException)
            {
                // The PipelineStopToken that is passed into the helper to handle cancellation
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
                    targetObject: target));
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
