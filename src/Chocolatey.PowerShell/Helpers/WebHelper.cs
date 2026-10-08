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
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static chocolatey.StringResources.EnvironmentVariables;

namespace Chocolatey.PowerShell.Helpers
{
    public class WebHelper : IDisposable
    {
        private bool disposedValue;

        /// <summary>
        /// Initializes a <see cref="WebHelper"/> with a cancellation token, to enable cancelling any download
        /// operations if needed.
        /// </summary>
        /// <param name="cmdlet">The cmdlet responsible for the operation.</param>
        /// <param name="options">The options used for the web request.</param>
        /// <param name="cancellationToken">A cancellation token, typically managed by the cmdlet to handle pipeline stops.</param>
        public WebHelper(PSCmdlet cmdlet, WebRequestOptions options, CancellationToken cancellationToken)
            : this(cmdlet, options)
        {
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Initializes a <see cref="WebHelper"/>.
        /// </summary>
        /// <param name="cmdlet">The cmdlet responsible for the operation.</param>
        /// <param name="options">The options used for the web request.</param>
        public WebHelper(PSCmdlet cmdlet, WebRequestOptions options)
        {
            Cmdlet = cmdlet;
            Options = options;
        }

        protected PSCmdlet Cmdlet { get; }
        protected CancellationToken? CancellationToken { get; }
        protected WebRequestOptions Options { get; }
        protected StringBuilder Content { get; set; }
        protected Encoding ContentEncoding { get; set; }
        protected HttpWebResponse Response { get; set; }

        /// <summary>
        /// Sets the proxy configuration on the given <paramref name="request"/> according to the configured environment variables.
        /// </summary>
        /// <param name="request">The web request to configure the proxy for.</param>
        protected virtual void SetProxyConfiguration(WebRequest request)
        {
            if (WebProxyConfiguration.IsEnabled)
            {
                var proxy = new WebProxy(WebProxyConfiguration.Url, WebProxyConfiguration.BypassOnLocal);
                if (!string.IsNullOrEmpty(WebProxyConfiguration.Password))
                {
                    proxy.Credentials = new NetworkCredential(WebProxyConfiguration.Username, WebProxyConfiguration.Password);
                }

                proxy.BypassList = WebProxyConfiguration.BypassList;

                PSHelper.WriteHost(Cmdlet, $"Using explicit proxy server '{proxy.Address}'.");

                request.Proxy = proxy;
            }
            else if (request.Proxy?.IsBypassed(request.RequestUri) == false)
            {
                //var proxyAddress = webClient.Proxy.GetProxy(uri).Authority;
                var proxyAddress = request.Proxy.GetProxy(request.RequestUri).Authority;
                var credentials = CredentialCache.DefaultCredentials;
                if (credentials is null)
                {
                    PSHelper.WriteDebug(Cmdlet, "Default credentials were null. Attempting backup method");
                    credentials = PSHelper.GetCredential(
                        Cmdlet,
                        $"Credential request for '{proxyAddress}'",
                        "Enter your credentials: ",
                        WebProxyConfiguration.Username,
                        targetName: null) // used only for domain qualifiers
                        .GetNetworkCredential();
                }

                PSHelper.WriteHost(Cmdlet, $"Using system proxy server '{proxyAddress}'.");
                request.Proxy = new WebProxy(proxyAddress)
                {
                    Credentials = credentials,
                    BypassProxyOnLocal = true
                };
            }
        }

        /// <summary>
        /// Initializes a web request according to the given <paramref name="options"/>, the Chocolatey defaults,
        /// and any configured environment variables.
        /// </summary>
        /// <param name="options">The web request options.</param>
        /// <returns>The HttpWebRequest object.</returns>
        protected virtual HttpWebRequest CreateWebRequest(WebRequestOptions options)
        {
            var request = WebRequest.Create(options.Uri) as HttpWebRequest;
            var defaultCredentials = CredentialCache.DefaultCredentials;

            if (!(defaultCredentials is null))
            {
                request.Credentials = defaultCredentials;
            }

            SetProxyConfiguration(request);

            request.Accept = "*/*";
            request.AllowAutoRedirect = true;
            request.MaximumAutomaticRedirections = 20;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            request.Timeout = 30000;
            var requestTimeout = EnvironmentHelper.GetVariable(Package.ChocolateyRequestTimeout);
            if (!string.IsNullOrEmpty(requestTimeout))
            {
                PSHelper.WriteDebug(Cmdlet, $"Setting request timeout to {requestTimeout}");
                request.Timeout = PSHelper.ConvertTo<int>(requestTimeout);
            }

            var responseTimeout = EnvironmentHelper.GetVariable(Package.ChocolateyResponseTimeout);
            if (!string.IsNullOrEmpty(responseTimeout))
            {
                PSHelper.WriteDebug(Cmdlet, $"Setting read/write timeout to {responseTimeout}");
                request.ReadWriteTimeout = PSHelper.ConvertTo<int>(responseTimeout);
            }

            // http://stackoverflow.com/questions/518181/too-many-automatic-redirections-were-attempted-error-message-when-using-a-httpw
            request.CookieContainer = new CookieContainer();

            if (!string.IsNullOrEmpty(options.UserAgent))
            {
                PSHelper.WriteDebug(Cmdlet, $"Setting the UserAgent to '{options.UserAgent}'");
                request.UserAgent = options.UserAgent;
            }

            if (options.Headers?.Count > 0)
            {
                PSHelper.WriteDebug(Cmdlet, "Setting custom headers");
                foreach (var key in options.Headers.Keys)
                {
                    var value = PSHelper.ConvertTo<string>(options.Headers[key]);
                    var header = key is string name
                        ? GetDedicatedHeader(name)
                        : PSHelper.ConvertTo<HttpRequestHeader>(key);

                    switch (header)
                    {
                        case HttpRequestHeader.Accept:
                            request.Accept = value;
                            break;
                        case HttpRequestHeader.Cookie:
                            request.CookieContainer.SetCookies(options.Uri, value);
                            break;
                        case HttpRequestHeader.Referer:
                            request.Referer = value;
                            break;
                        case HttpRequestHeader.UserAgent:
                            request.UserAgent = value;
                            break;
                        default:
                            if (header.HasValue)
                            {
                                request.Headers.Add(header.Value, value);
                            }
                            else
                            {
                                request.Headers.Add((string)key, value);
                            }

                            break;
                    }
                }
            }

            return request;
        }

        private static HttpRequestHeader? GetDedicatedHeader(string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "accept":
                    return HttpRequestHeader.Accept;
                case "cookie":
                    return HttpRequestHeader.Cookie;
                case "referer":
                    return HttpRequestHeader.Referer;
                case "user-agent":
                    return HttpRequestHeader.UserAgent;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Retrieves the headers from the <see cref="Response"/>.
        /// </summary>
        /// <returns>A Dictionary containing the headers and their values.</returns>
        protected Dictionary<string, string> GetResponseHeaders()
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in Response.Headers)
            {
                var value = Response.Headers[key];
                if (!(value is null))
                {
                    headers.Add(key.ToString(), value.ToString());
                }
            }

            return headers;
        }

        /// <summary>
        /// Writes a warning and a <c>filename.istext</c> check file if the target file has a content-type
        /// that indicates it is plaintext.
        /// </summary>
        /// <param name="downloadPath">The path that we're downloading the final file to; the checkfile will be this path with <c>.istext</c> appended.</param>
        protected void WriteCheckFileFor(string downloadPath)
        {
            if (string.IsNullOrEmpty(downloadPath))
            {
                return;
            }

            var binaryIsTextCheckFile = PSHelper.GetUnresolvedPath(Cmdlet, $"{downloadPath}.istext");
            if (PSHelper.FileExists(Cmdlet, binaryIsTextCheckFile))
            {
                try
                {
                    File.Delete(binaryIsTextCheckFile);
                }
                catch
                {
                    // Suppress errors, this is not critical.
                }
            }

            try
            {
                var responseHeaders = GetResponseHeaders();
                if (responseHeaders.ContainsKey("Content-Type"))
                {
                    var contentType = responseHeaders["Content-Type"].ToLower();
                    if (!(contentType is null))
                    {
                        if (contentType.Contains("text/html") || contentType.Contains("text/plain"))
                        {
                            var warning = $"{downloadPath} is of content type {contentType}";
                            PSHelper.WriteWarning(Cmdlet, warning);
                            File.WriteAllText(binaryIsTextCheckFile, warning);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                // Unable to get content-type header
                PSHelper.WriteDebug(Cmdlet, $"Error getting content type - {err.Message}");
            }
        }

        protected virtual string GenerateFilePath()
        {
            const string fileNameHeaderPattern = @"(?i)filename=(.*)$";
            Regex regex = new Regex(fileNameHeaderPattern);
            var fileName = regex.Match(Response.Headers["Content-Disposition"] ?? string.Empty)
                    .Groups[1]?.Value?
                    .Trim(new char[] { '/', '\\', '"', '\'' });

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = Response.ResponseUri.Segments[Response.ResponseUri.Segments.Length - 1];

                if (string.IsNullOrEmpty(fileName))
                {
                    PSHelper.WriteHost(Cmdlet, "Please enter a filename: ");
                    fileName = Cmdlet.Host.UI.ReadLine();
                }

                fileName = fileName.Trim(new char[] { '/', '\\', '"', '\'' });

                if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
                {
                    fileName = string.Format(
                        "{0}.{1}",
                        fileName,
                        Response.ContentType.Split(';')[0].Split('/')[1]);
                }
            }

            return PSHelper.GetUnresolvedPath(Cmdlet, fileName);
        }

        /// <summary>
        /// Extension point to allow inheritance and adjustments to the stream implementation for the download.
        /// </summary>
        /// <returns>A <see cref="Stream"/> implementation which retrieves data from the <see cref="Response"/>.</returns>
        protected virtual Stream GetDownloadStream()
        {
            return Response.GetResponseStream();
        }

        /// <summary>
        /// Reads from the <paramref name="stream"/> into the <paramref name="buffer"/>, returning as soon as the
        /// cancellation token is triggered even if the read is still pending.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">The buffer to fill.</param>
        /// <returns>The number of bytes read, or zero when the end of the stream is reached.</returns>
        /// <exception cref="OperationCanceledException">Thrown if cancellation is requested while waiting for the read.</exception>
        protected int ReadBuffer(Stream stream, byte[] buffer)
        {
            if (!CancellationToken.HasValue)
            {
                return stream.Read(buffer, 0, buffer.Length);
            }

            var token = CancellationToken.Value;
            var read = stream.ReadAsync(buffer, 0, buffer.Length, token);

            // This ensures that we can bail out regardless of whether the underlying stream implementation
            // cancels as expected.
            Task.WaitAny(new Task[] { read }, token);

            return read.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Downloads the file from the <see cref="Response"/>.
        /// </summary>
        /// <param name="path">The path to save the file as.</param>
        /// <param name="quiet">Whether to suppress the usual progress output from the download.</param>
        protected void DownloadFromResponse(string path, bool quiet)
        {
            if (!string.IsNullOrEmpty(path))
            {
                path = PSHelper.GetUnresolvedPath(Cmdlet, path);
            }

            var goal = Response.ContentLength;
            var goalFormatted = goal.AsFileSizeString();

            using (var reader = GetDownloadStream())
            {
                FileStream writer = null;
                if (!string.IsNullOrEmpty(path))
                {
                    var directory = PSHelper.GetParentDirectory(Cmdlet, path);
                    PSHelper.EnsureDirectoryExists(Cmdlet, directory);

                    writer = new FileStream(path, FileMode.Create);
                }

                var buffer = new byte[1048576];
                long total = 0;
                int count, iterLoop = 0;

                try
                {
                    do
                    {
                        // Terminate early if the pipeline is stopped (usually from Ctrl+C)
                        CancellationToken?.ThrowIfCancellationRequested();

                        count = ReadBuffer(reader, buffer);
                        if (!string.IsNullOrEmpty(path))
                        {
                            writer.Write(buffer, 0, count);
                        }

                        if (!(Content is null))
                        {
                            Content.Append(ContentEncoding.GetString(buffer, 0, count));
                        }
                        else if (!quiet)
                        {
                            total += count;
                            var totalFormatted = total.AsFileSizeString();

                            if (goal > 0 && (++iterLoop % 10 == 0))
                            {
                                var percentComplete = (int)Math.Truncate((decimal)total / goal * 100);
                                var record = new ProgressRecord(
                                    activityId: 0,
                                    $"Downloading {Response.ResponseUri} to {path}",
                                    $"Saving {totalFormatted} of {goalFormatted}")
                                {
                                    PercentComplete = percentComplete
                                };

                                Cmdlet.WriteProgress(record);
                            }

                            if (total == goal && count == 0)
                            {
                                var record = new ProgressRecord(
                                    activityId: 0,
                                    $"Completed download of {Response.ResponseUri}.",
                                    $"Completed download of {path} ({goalFormatted}).")
                                {
                                    RecordType = ProgressRecordType.Completed
                                };
                                Cmdlet.WriteProgress(record);
                            }
                        }
                    } while (count > 0);
                }
                finally
                {
                    if (!(writer is null))
                    {
                        writer.Flush();
                        writer.Dispose();
                    }
                }

                PSHelper.WriteHost(Cmdlet, string.Empty);
                PSHelper.WriteHost(Cmdlet, $"Download of {Path.GetFileName(path)} ({goalFormatted}) completed.");
            }
        }

        /// <summary>
        /// Download a file over HTTP to the target <paramref name="filePath"/>.
        /// </summary>
        /// <param name="Options">The web request options specifying how to retrieve the file.</param>
        /// <param name="filePath">The file path to download the result to. May be null if <paramref name="passthru"/> is <c>true</c>.</param>
        /// <param name="passthru">Whether to write the resulting file data to the output stream.</param>
        /// <param name="quiet">Whether to suppress the usual progress display when downloading a file.</param>
        /// <exception cref="WebException">Thrown if the file could not be downloaded for any reason.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the cancellation token is triggered.</exception>
        public void DownloadHttpFile(
            string filePath,
            bool passthru,
            bool quiet)
        {
            var request = CreateWebRequest(Options);
            try
            {
                Response = request.GetResponse() as HttpWebResponse;
                
                WriteCheckFileFor(filePath);

                if ((!passthru && string.IsNullOrEmpty(filePath))
                    || (!string.IsNullOrEmpty(filePath) && PSHelper.ContainerExists(Cmdlet, filePath)))
                {
                    filePath = GenerateFilePath();
                }

                switch (Response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                    case HttpStatusCode.NotFound:
                        EnvironmentHelper.SetVariable(Package.ChocolateyExitCode, Response.StatusCode.ToString());
                        throw new WebException($"Remote file either doesn't exist, is unauthorized, or is forbidden for '{Options.Uri.OriginalString}'.");
                    case HttpStatusCode.OK:
                        if (passthru)
                        {
                            Content = new StringBuilder();
                            ContentEncoding = Encoding.GetEncoding(Response.CharacterSet);
                        }

                        DownloadFromResponse(filePath, quiet);
                        
                        if (passthru)
                        {
                            PSHelper.WriteObject(Cmdlet, Content.ToString());
                        }

                        break;
                }
                
            }
            catch (Exception error) when (error is OperationCanceledException || CancellationToken?.IsCancellationRequested == true)
            {
                if (!(request is null))
                {
                    request.ServicePoint.MaxIdleTime = 0;
                    request.Abort();
                }

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
                if (!(request is null))
                {
                    request.ServicePoint.MaxIdleTime = 0;
                    request.Abort();
                }

                PSHelper.SetExitCode(Cmdlet, 404);

                var message = $"The remote file either doesn't exist, is unauthorized, or is forbidden for url '{Options.Uri.OriginalString}'. {error.Message}";
                if (EnvironmentHelper.GetVariable(Package.DownloadCacheAvailable) == "true")
                {
                    message += "\nThis package is likely not broken for licensed users - see https://docs.chocolatey.org/en-us/features/private-cdn.";
                }

                throw new WebException(message, error);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Response?.Dispose();
                    Content?.Clear();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Base options for making a web request.
    /// </summary>
    public class WebRequestOptions
    {
        public Uri Uri { get; set; }
        public string UserAgent { get; set; }
        public IDictionary Headers { get; set; }
    }

    /// <summary>
    /// Helper class for retrieving the proxy configuration from the process environment variables defined by Chocolatey CLI.
    /// </summary>
    public static class WebProxyConfiguration
    {
        public static bool IsEnabled
        {
            get => !string.IsNullOrEmpty(Url);
        }

        public static string Url
        {
            get => EnvironmentHelper.GetVariable(Package.ChocolateyProxyLocation);
        }

        public static bool BypassOnLocal
        {
            get => EnvironmentHelper.GetVariable(Package.ChocolateyProxyBypassOnLocal) == "true";
        }

        public static string Username
        {
            get => EnvironmentHelper.GetVariable(Package.ChocolateyProxyUser);
        }

        public static string Password
        {
            get => EnvironmentHelper.GetVariable(Package.ChocolateyProxyPassword);
        }

        public static string[] BypassList
        {
            get => EnvironmentHelper.GetVariable(Package.ChocolateyProxyBypassList)?
                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                ?? Array.Empty<string>();
        }
    }
}
