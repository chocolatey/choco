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
using System.IO;
using System.Management.Automation;
using System.Net;
using System.Threading;
using static chocolatey.StringResources.EnvironmentVariables;

namespace Chocolatey.PowerShell.Helpers
{
    /// <summary>
    /// Downloads files over FTP.
    /// </summary>
    public class FtpHelper : IDisposable
    {
        private readonly string _userName;
        private readonly string _password;
        private WebRequest _request;
        private bool _disposed;

        public FtpHelper(PSCmdlet cmdlet, Uri uri, string userName, string password, CancellationToken cancellationToken)
            : this(cmdlet, uri, userName, password)
        {
            CancellationToken = cancellationToken;
        }

        public FtpHelper(PSCmdlet cmdlet, Uri uri, string userName, string password)
        {
            Cmdlet = cmdlet;
            Uri = uri;
            _userName = userName;
            _password = password;
        }

        protected PSCmdlet Cmdlet { get; }
        protected CancellationToken? CancellationToken { get; }
        protected Uri Uri { get; }

        /// <summary>
        /// Download the file at the address to the target <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">The path of the file to create.</param>
        /// <param name="quiet">Whether to suppress the usual progress display.</param>
        /// <exception cref="WebException">Thrown if the file could not be downloaded for any reason.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the cancellation token is triggered.</exception>
        public void DownloadFile(string filePath, bool quiet)
        {
            var request = CreateRequest(Uri);
            _request = request;

            // A stop that arrived before the request was registered could not abort it.
            CancellationToken?.ThrowIfCancellationRequested();

            ConfigureRequest(request);

            try
            {
                var path = GetTargetPath(filePath);
                long goal;
                string goalFormatted;

                using (var response = request.GetResponse())
                {
                    goal = response.ContentLength;
                    goalFormatted = goal.AsFileSizeString();

                    using (var reader = response.GetResponseStream())
                    using (var writer = CreateTargetStream(path))
                    {
                        Copy(reader, writer, goal, goalFormatted, path, quiet);
                    }
                }

                PSHelper.WriteHost(Cmdlet, string.Empty);
                PSHelper.WriteHost(Cmdlet, $"Download of '{Path.GetFileName(path)}' ({goalFormatted}) completed.");
            }
            catch (Exception error) when (CancellationToken?.IsCancellationRequested == true)
            {
                request.Abort();

                // If this class is disposed from another thread before this thread registers it,
                // this *could* be false, and it will fall through to below; this ensures that we
                // still treat those edge cases correctly as a cancelled task.
                if (error is OperationCanceledException)
                {
                    throw;
                }

                throw new OperationCanceledException("The download was cancelled.", error, CancellationToken.Value);
            }
            catch (Exception error)
            {
                request.Abort();
                SetExitCode(404);

                var errorMessage = $"The remote file either doesn't exist, is unauthorized, or is forbidden for url '{Uri.OriginalString}'. {error.Message}";
                // This would be a lie: licensed does not actually have caching for FTP downloads.
                // Leaving this here for possible future enhancement similar to HttpHelper, but for now it serves no purpose.
                //if (EnvironmentHelper.GetVariable(Package.DownloadCacheAvailable) == "true")
                //{
                //    errorMessage += " \nThis package is likely not broken for licensed users - see https://docs.chocolatey.org/en-us/features/private-cdn.";
                //}

                throw new WebException(errorMessage, error);
            }
        }

        /// <summary>
        /// Creates the request for the address.
        /// </summary>
        /// <param name="uri">The address to request.</param>
        /// <returns>The request.</returns>
        /// <exception cref="ArgumentException">Thrown if the address is not an FTP address.</exception>
        protected virtual WebRequest CreateRequest(Uri uri)
        {
            var request = WebRequest.Create(uri);
            if (!(request is FtpWebRequest))
            {
                throw new ArgumentException($"The address '{uri.OriginalString}' is not an FTP address.", nameof(uri));
            }

            return request;
        }

        /// <summary>
        /// Gets the full path of the file to create.
        /// </summary>
        /// <param name="filePath">The path that was asked for, which may be relative to the PowerShell location.</param>
        /// <returns>The full path.</returns>
        protected virtual string GetTargetPath(string filePath)
        {
            return PSHelper.GetUnresolvedPath(Cmdlet, filePath);
        }

        /// <summary>
        /// Creates the file that the download is written to, creating its folder if that does not exist.
        /// </summary>
        /// <param name="path">The full path of the file to create.</param>
        /// <returns>The stream to write the download to.</returns>
        protected virtual Stream CreateTargetStream(string path)
        {
            PSHelper.EnsureDirectoryExists(Cmdlet, PSHelper.GetParentDirectory(Cmdlet, path));

            return new FileStream(path, FileMode.Create);
        }

        /// <summary>
        /// Reports the exit code for the host to use when the session ends.
        /// </summary>
        /// <param name="exitCode">The exit code to report.</param>
        protected virtual void SetExitCode(int exitCode)
        {
            PSHelper.SetExitCode(Cmdlet, exitCode);
        }

        private void ConfigureRequest(WebRequest request)
        {
            if (WebProxyConfiguration.IsEnabled)
            {
                PSHelper.WriteHost(Cmdlet, $"Using explicit proxy server '{WebProxyConfiguration.Url}'.");
                request.Proxy = WebProxyConfiguration.CreateProxy();
            }

            request.Credentials = new NetworkCredential(_userName, _password);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            var requestTimeout = GetTimeout(Package.ChocolateyRequestTimeout);
            if (requestTimeout is int t)
            {
                request.Timeout = t;
            }

            if (request is FtpWebRequest ftpRequest)
            {
                ftpRequest.UseBinary = true;
                ftpRequest.KeepAlive = false;

                var responseTimeout = GetTimeout(Package.ChocolateyResponseTimeout);
                if (responseTimeout.HasValue)
                {
                    ftpRequest.ReadWriteTimeout = responseTimeout.Value;
                }
            }
        }

        /// <summary>
        /// Gets a timeout in milliseconds from an environment variable. Values at or below zero,
        /// or invalid values, return null.
        /// </summary>
        private static int? GetTimeout(string variable)
        {
            if (PSHelper.TryConvertTo<int>(EnvironmentHelper.GetVariable(variable), out var milliseconds)
                && milliseconds > 0)
            {
                return milliseconds;
            }

            return null;
        }

        private void Copy(Stream reader, Stream writer, long goal, string goalFormatted, string path, bool quiet)
        {
            var buffer = new byte[1048576];
            long total = 0;
            int count;

            do
            {
                // Terminate early if the pipeline is stopped (usually from Ctrl+C)
                CancellationToken?.ThrowIfCancellationRequested();

                count = reader.Read(buffer, 0, buffer.Length);
                writer.Write(buffer, 0, count);

                if (!quiet)
                {
                    total += count;
                    WriteProgress(total, goal, goalFormatted, path, count);
                }
            } while (count != 0);

            writer.Flush();
        }

        private void WriteProgress(long total, long goal, string goalFormatted, string path, int count)
        {
            var activity = $"Downloading {Uri.OriginalString} to {path}";

            if (goal > 0)
            {
                var percentComplete = Math.Min(100, (int)Math.Truncate((double)total / goal * 100));
                Cmdlet.WriteProgress(new ProgressRecord(0, activity, $"Saving {total.AsFileSizeString()} of {goalFormatted} ({total}/{goal})")
                {
                    PercentComplete = percentComplete
                });
            }
            else
            {
                Cmdlet.WriteProgress(new ProgressRecord(0, activity, $"Saving {total} bytes...")
                {
                    RecordType = ProgressRecordType.Completed
                });
            }

            if (total == goal && count == 0)
            {
                Cmdlet.WriteProgress(new ProgressRecord(0, $"Completed download of {Uri.OriginalString}.", $"Completed a total of {total} bytes of {path}")
                {
                    RecordType = ProgressRecordType.Completed,
                    PercentComplete = 100
                });
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _request?.Abort();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}