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
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static chocolatey.StringResources.EnvironmentVariables;

namespace Chocolatey.PowerShell.Helpers
{
    public class WebHelper : IDisposable
    {
        private const int DefaultRequestTimeoutMilliseconds = 30000;

        // One client (and one cookie container) is shared by every (PowerShell) download in the process.
        // The client is created again only when the proxy to use changes, and every client uses the same cookie container.
        private static readonly object _clientLock = new object();
        private static readonly CookieContainer _cookieContainer = new CookieContainer();
        private static HttpClient _httpClient;
        private static string _clientKey;

        private bool disposedValue;
        private bool _proxyMessagesAreDebug;

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
        protected HttpResponseMessage Response { get; set; }
        protected TimeSpan? ReadTimeout { get; set; }

        /// <summary>
        /// Gets the address the <see cref="Response"/> came from, which is the final address if any redirects
        /// were followed.
        /// </summary>
        protected Uri ResponseUri => Response?.RequestMessage?.RequestUri ?? RequestUri ?? Options.Uri;

        /// <summary>
        /// Gets the address that the current request is being sent to, as chosen by <see cref="GetRequestUri(bool)"/>.
        /// This is <c>null</c> until a request has been started.
        /// </summary>
        protected Uri RequestUri { get; private set; }

        /// <summary>
        /// Gets the cookie container that requests made through the shared client use.
        /// </summary>
        protected virtual CookieContainer Cookies => _cookieContainer;

        /// <summary>
        /// Gets whether the message of a failed download mentions the private CDN. By default it does when the
        /// download cache is available. Override this to return <c>false</c> where the hint does not apply.
        /// </summary>
        protected virtual bool ShowDownloadCacheHint => EnvironmentHelper.GetVariable(Package.DownloadCacheAvailable) == "true";

        /// <summary>
        /// Gets the number of bytes to read from the download stream at a time.
        /// Override this to change how much is read and written in each step of a download.
        /// </summary>
        /// <returns>The size of the buffer, which must be greater than zero.</returns>
        protected virtual int GetBufferSize()
        {
            return 1048576;
        }

        /// <summary>
        /// Gets the proxy that the system would use for a request when no explicit proxy is configured.
        /// </summary>
        /// <returns>The system's default proxy, or <c>null</c> if there is none.</returns>
        protected virtual IWebProxy GetSystemProxy()
        {
            return WebRequest.DefaultWebProxy;
        }

        /// <summary>
        /// Works out the proxy to send a request to the given address through, according to the configured
        /// environment variables.
        /// </summary>
        /// <param name="requestUri">The address that the request will be sent to.</param>
        /// <returns>The proxy to use, or <c>null</c> to leave the choice to the handler's default.</returns>
        protected virtual IWebProxy ResolveProxy(Uri requestUri)
        {
            if (WebProxyConfiguration.IsEnabled)
            {
                var proxy = new WebProxy(WebProxyConfiguration.Url, WebProxyConfiguration.BypassOnLocal);
                if (!string.IsNullOrEmpty(WebProxyConfiguration.Password))
                {
                    proxy.Credentials = new NetworkCredential(WebProxyConfiguration.Username, WebProxyConfiguration.Password);
                }

                proxy.BypassList = WebProxyConfiguration.BypassList;

                WriteProxyMessage($"Using explicit proxy server '{proxy.Address}'.");

                return proxy;
            }

            var systemProxy = GetSystemProxy();
            if (systemProxy?.IsBypassed(requestUri) == false)
            {
                //var proxyAddress = webClient.Proxy.GetProxy(uri).Authority;
                var proxyAddress = systemProxy.GetProxy(requestUri).Authority;
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

                WriteProxyMessage($"Using system proxy server '{proxyAddress}'.");

                return new WebProxy(proxyAddress)
                {
                    Credentials = credentials,
                    BypassProxyOnLocal = true
                };
            }

            return null;
        }

        /// <summary>
        /// Creates the handler that requests are sent through, configured with the Chocolatey defaults,
        /// the default credentials, and the given <paramref name="proxy"/>.
        /// </summary>
        /// <param name="proxy">
        /// The proxy to send requests through, or <c>null</c> to leave the choice to the handler's default.
        /// </param>
        /// <returns>The configured handler.</returns>
        protected virtual HttpClientHandler CreateHandler(IWebProxy proxy)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 20,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                // http://stackoverflow.com/questions/518181/too-many-automatic-redirections-were-attempted-error-message-when-using-a-httpw
                CookieContainer = Cookies
            };

            var defaultCredentials = CredentialCache.DefaultCredentials;
            if (!(defaultCredentials is null))
            {
                handler.Credentials = defaultCredentials;
            }

            if (!(proxy is null))
            {
                handler.Proxy = proxy;
            }

            return handler;
        }

        /// <summary>
        /// Gets a key that identifies the configuration of the given <paramref name="proxy"/>.
        /// </summary>
        /// <param name="proxy">The proxy to describe, or <c>null</c> for the handler's default.</param>
        /// <returns>A hash of the proxy's configuration.</returns>
        protected static string GetProxyKey(IWebProxy proxy)
        {
            string description;
            if (proxy is null)
            {
                description = "default";
            }
            else if (proxy is WebProxy webProxy)
            {
                var credentials = webProxy.Credentials?.GetCredential(webProxy.Address, "Basic");
                description = string.Join(
                    "\n",
                    webProxy.Address.AbsoluteUri,
                    webProxy.BypassProxyOnLocal,
                    string.Join(",", webProxy.BypassList),
                    ReferenceEquals(webProxy.Credentials, CredentialCache.DefaultCredentials),
                    credentials?.UserName,
                    credentials?.Domain,
                    credentials?.Password);
            }
            else
            {
                description = $"{proxy.GetType().FullName}:{RuntimeHelpers.GetHashCode(proxy)}";
            }

            using (var hash = SHA256.Create())
            {
                return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(description)));
            }
        }

        /// <summary>
        /// Gets the client that requests are sent through. The client is shared by every download in the process,
        /// and is created again only when the proxy to use is not the one it was created for.
        /// Override this to change how requests are sent.
        /// </summary>
        /// <param name="proxy">
        /// The proxy that requests will be sent through, or <c>null</c> for the handler's default.
        /// </param>
        /// <returns>The client.</returns>
        protected virtual HttpClient GetHttpClient(IWebProxy proxy)
        {
            var key = GetProxyKey(proxy);

            lock (_clientLock)
            {
                // If the proxy key has changed, it means that someone has done something weird to
                // change the proxy configuration *during* a Chocolatey run. This handles that
                // edge case, but given we do not expect this to be a typical use case, we do
                // not want the overhead of maintaining multiple separate HttpClient instances.
                if (_httpClient is null || _clientKey != key)
                {
                    // Each request applies its own timeout, so the client itself never times out.
                    _httpClient = new HttpClient(CreateHandler(proxy))
                    {
                        Timeout = Timeout.InfiniteTimeSpan
                    };
                    _clientKey = key;
                }

                return _httpClient;
            }
        }

        /// <summary>
        /// Gets how long to wait for the response headers to arrive, taking any configured environment variable
        /// into account.
        /// </summary>
        /// <returns>The request timeout.</returns>
        protected TimeSpan GetRequestTimeout()
        {
            var requestTimeout = EnvironmentHelper.GetVariable(Package.ChocolateyRequestTimeout);
            if (PSHelper.TryConvertTo<int>(requestTimeout, out var milliseconds)
                && milliseconds > 0)
            {
                PSHelper.WriteDebug(Cmdlet, $"Setting request timeout to {requestTimeout}");
                return TimeSpan.FromMilliseconds(milliseconds);
            }

            return TimeSpan.FromMilliseconds(DefaultRequestTimeoutMilliseconds);
        }

        /// <summary>
        /// Gets how long to wait for each read of the response body, taking any configured environment variable
        /// into account.
        /// </summary>
        /// <returns>The read timeout, or <c>null</c> if reads should wait indefinitely.</returns>
        protected TimeSpan? GetReadTimeout()
        {
            var responseTimeout = EnvironmentHelper.GetVariable(Package.ChocolateyResponseTimeout);
            if (PSHelper.TryConvertTo<int>(responseTimeout, out var milliseconds)
                && milliseconds > 0)
            {
                PSHelper.WriteDebug(Cmdlet, $"Setting read/write timeout to {responseTimeout}");
                return TimeSpan.FromMilliseconds(milliseconds);
            }

            return null;
        }

        /// <summary>
        /// Creates the request message for the given <paramref name="options"/>, including any custom headers.
        /// </summary>
        /// <param name="options">The web request options.</param>
        /// <returns>The request message.</returns>
        protected virtual HttpRequestMessage CreateRequestMessage(WebRequestOptions options)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, options.Uri);
            SetRequestHeader(message, "Accept", "*/*", replace: true);

            if (!string.IsNullOrEmpty(options.UserAgent))
            {
                PSHelper.WriteDebug(Cmdlet, $"Setting the UserAgent to '{options.UserAgent}'");
                SetRequestHeader(message, "User-Agent", options.UserAgent, replace: true);
            }

            if (options.Headers?.Count > 0)
            {
                PSHelper.WriteDebug(Cmdlet, "Setting custom headers");
                foreach (var key in options.Headers.Keys)
                {
                    // Normalise the key into a standardised header name, ensuring raw HttpRequestHeader enum
                    // values are correctly handled.
                    var name = key is string text
                        ? text
                        : GetHeaderName(PSHelper.ConvertTo<HttpRequestHeader>(key));
                    var value = PSHelper.ConvertTo<string>(options.Headers[key]);

                    switch (name.ToLowerInvariant())
                    {
                        case "cookie":
                            Cookies.SetCookies(options.Uri, value);
                            break;
                        case "accept":
                        case "referer":
                        case "user-agent":
                            SetRequestHeader(message, name, value, replace: true);
                            break;
                        default:
                            // If this was already set with one of the explicit options,
                            // that should take precedence over values in the header dictionary.
                            SetRequestHeader(message, name, value, replace: false);
                            break;
                    }
                }
            }

            return message;
        }

        /// <summary>
        /// Retrieves the headers from the <see cref="Response"/>.
        /// </summary>
        /// <returns>A Dictionary containing the headers and their values.</returns>
        protected Dictionary<string, string> GetResponseHeaders()
        {
            return ConsolidateHeaders(Response.Headers, Response.Content?.Headers);
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
                    var contentType = responseHeaders["Content-Type"].ToLowerInvariant();
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
            GetResponseHeaders().TryGetValue("Content-Disposition", out var contentDisposition);
            var fileName = regex.Match(contentDisposition ?? string.Empty)
                    .Groups[1]?.Value?
                    .Trim(new char[] { '/', '\\', '"', '\'' });

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = ResponseUri.Segments[ResponseUri.Segments.Length - 1];

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
                        GetMediaType().Split('/')[1]);
                }
            }

            return PSHelper.GetUnresolvedPath(Cmdlet, fileName);
        }

        /// <summary>
        /// Gets the download stream from the HTTP response. Override this to wrap the stream if needed.
        /// </summary>
        /// <returns>A <see cref="Stream"/> implementation which retrieves data from the <see cref="Response"/>.</returns>
        protected virtual Stream GetDownloadStream()
        {
            return Response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Reads from the <paramref name="stream"/> into the <paramref name="buffer"/>, returning as soon as the
        /// cancellation token is triggered even if the read is still pending.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">The buffer to fill.</param>
        /// <returns>The number of bytes read, or zero when the end of the stream is reached.</returns>
        /// <exception cref="OperationCanceledException">Thrown if cancellation is requested while waiting for the read.</exception>
        /// <exception cref="WebException">Thrown with a status of <see cref="WebExceptionStatus.Timeout"/> if the <see cref="ReadTimeout"/> elapses.</exception>
        protected int ReadBuffer(Stream stream, byte[] buffer)
        {
            if (!CancellationToken.HasValue && !ReadTimeout.HasValue)
            {
                return stream.Read(buffer, 0, buffer.Length);
            }

            var token = CancellationToken.GetValueOrDefault();
            using (var readSource = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                if (ReadTimeout.HasValue)
                {
                    readSource.CancelAfter(ReadTimeout.Value);
                }

                try
                {
                    var read = stream.ReadAsync(buffer, 0, buffer.Length, readSource.Token);

                    // This ensures that we can bail out regardless of whether the underlying stream implementation
                    // cancels as expected.
                    Task.WaitAny(new Task[] { read }, readSource.Token);

                    return read.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested && readSource.IsCancellationRequested)
                {
                    throw new WebException("The operation has timed out.", WebExceptionStatus.Timeout);
                }
            }
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

            var goal = Response.Content.Headers.ContentLength ?? -1;
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

                var buffer = new byte[GetBufferSize()];
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
                                    $"Downloading {ResponseUri} to {path}",
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
                                    $"Completed download of {ResponseUri}.",
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
                PSHelper.WriteHost(Cmdlet, $"Download of '{Path.GetFileName(path)}' ({goalFormatted}) completed.");
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
            ReadTimeout = GetReadTimeout();

            Execute(
                () =>
                {
                    WriteCheckFileFor(filePath);

                    if ((!passthru && string.IsNullOrEmpty(filePath))
                        || (!string.IsNullOrEmpty(filePath) && PSHelper.ContainerExists(Cmdlet, filePath)))
                    {
                        filePath = GenerateFilePath();
                    }

                    switch (Response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            if (passthru)
                            {
                                Content = new StringBuilder();
                                ContentEncoding = GetResponseEncoding();
                            }

                            DownloadFromResponse(filePath, quiet);

                            if (passthru)
                            {
                                PSHelper.WriteObject(Cmdlet, Content.ToString());
                            }

                            break;
                    }
                },
                isDownload: true);
        }

        /// <summary>
        /// Gets the headers that the server responds with for the request described by the options.
        /// </summary>
        /// <returns>
        /// A case-insensitive table of the response headers that have a value, or an empty table if the options
        /// have no address to request.
        /// </returns>
        /// <exception cref="WebException">Thrown if the server could not be reached, or it responded with an error.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the cancellation token is triggered.</exception>
        public Hashtable GetWebHeaders()
        {
            var headers = new Hashtable(StringComparer.OrdinalIgnoreCase);
            if (Options.Uri is null)
            {
                return headers;
            }

            Execute(
                () =>
                {
                    PSHelper.WriteDebug(Cmdlet, "Response Headers:");
                    foreach (var header in GetResponseHeaders())
                    {
                        if (!string.IsNullOrEmpty(header.Value))
                        {
                            headers[header.Key] = header.Value;
                            PSHelper.WriteDebug(Cmdlet, $"  '{header.Key}':'{header.Value}'");
                        }
                    }
                },
                isDownload: false,
                beforeSend: WriteRequestHeaders);

            return headers;
        }

        /// <summary>
        /// Gets the name of the file that the request described by the options would download, using what the
        /// server responds with, and falling back to the <paramref name="defaultName"/> when a name cannot be determined.
        /// </summary>
        /// <param name="defaultName">The name to use when a name cannot be determined.</param>
        /// <returns>The name of the file.</returns>
        /// <exception cref="OperationCanceledException">Thrown if the cancellation token is triggered.</exception>
        public string GetRemoteFileName(string defaultName)
        {
            if (Options.Uri is null)
            {
                PSHelper.WriteDebug(Cmdlet, "Url was null, using default name.");
                return defaultName;
            }

            if (Options.Uri.IsFile)
            {
                PSHelper.WriteDebug(Cmdlet, "Url is local file, returning fileName");
                return Path.GetFileName(Options.Uri.LocalPath);
            }

            if (Options.Uri.OriginalString.StartsWith("ftp", StringComparison.OrdinalIgnoreCase))
            {
                PSHelper.WriteDebug(Cmdlet, "Url is FTP, using default name.");
                return defaultName;
            }

            // Working out a file name is a quiet step on the way to a download, so it keeps the proxy
            // messages out of the host output.
            _proxyMessagesAreDebug = true;

            var fileName = defaultName;
            try
            {
                Execute(
                    () =>
                    {
                        var headers = GetResponseHeaders();
                        headers.TryGetValue("Content-Disposition", out var contentDisposition);
                        headers.TryGetValue("Location", out var location);

                        fileName = RemoteFileNameResolver.Resolve(
                            defaultName,
                            RequestUri.OriginalString,
                            ResponseUri,
                            contentDisposition,
                            location,
                            message => PSHelper.WriteDebug(Cmdlet, message));
                    },
                    isDownload: false);
            }
            catch (WebException error)
            {
                PSHelper.WriteDebug(Cmdlet, $"Url request/response failed - file name will be '{defaultName}':  {error.InnerException?.Message ?? error.Message}");
                return defaultName;
            }

            return fileName;
        }

        /// <summary>
        /// Sends the request described by the options, and runs the <paramref name="onResponse"/> action once the
        /// response headers have arrived and the response is known to be a success.
        /// </summary>
        /// <param name="onResponse">The action to run with the successful <see cref="Response"/>.</param>
        /// <param name="isDownload">
        /// Whether the request is for a download, in which case a failure also reports an exit code and
        /// mentions the private CDN when it is available.
        /// </param>
        protected void Execute(Action onResponse, bool isDownload)
        {
            Execute(onResponse, isDownload, beforeSend: null);
        }

        /// <summary>
        /// Sends the request described by the options, and runs the <paramref name="onResponse"/> action once the
        /// response headers have arrived and the response is known to be a success.
        /// </summary>
        /// <param name="onResponse">The action to run with the successful <see cref="Response"/>.</param>
        /// <param name="isDownload">
        /// Whether the request is for a download, in which case a failure also reports an exit code and
        /// mentions the private CDN when it is available.
        /// </param>
        /// <param name="beforeSend">An action to run on the request message before it is sent.</param>
        protected void Execute(Action onResponse, bool isDownload, Action<HttpRequestMessage> beforeSend)
        {
            // A failure is recovered from at most once, so that a request cannot be retried indefinitely.
            if (!TryExecute(onResponse, isDownload, beforeSend, allowRecovery: true))
            {
                TryExecute(onResponse, isDownload, beforeSend, allowRecovery: false);
            }
        }

        /// <summary>
        /// Gets the address that a request is sent to. By default this is the address from the options.
        /// Override this to send the request somewhere else, such as a cache of the file.
        /// </summary>
        /// <param name="isDownload">Whether the request is for a download.</param>
        /// <returns>The address to send the request to.</returns>
        protected virtual Uri GetRequestUri(bool isDownload)
        {
            return Options.Uri;
        }

        /// <summary>
        /// Gives a chance to recover from a failed request, by returning <c>true</c> to have the request sent again.
        /// <see cref="GetRequestUri(bool)"/> is called again for the new attempt, so an override that returns
        /// <c>true</c> should arrange for that to give a different address. A failure is recovered from at most once.
        /// </summary>
        /// <remarks>
        /// The error is whatever the failed request raised: a <see cref="WebException"/> for an error status or a
        /// timeout, or an <see cref="HttpRequestException"/> when the server could not be reached.
        /// Cancellation is never passed to this method.
        /// </remarks>
        /// <param name="error">The error that the request raised.</param>
        /// <param name="isDownload">Whether the request is for a download.</param>
        /// <returns><c>true</c> to send the request again, otherwise <c>false</c>.</returns>
        protected virtual bool TryRecover(Exception error, bool isDownload)
        {
            return false;
        }

        /// <summary>
        /// Reports the exit code for the host to use when the session ends.
        /// </summary>
        /// <remarks>
        /// This is virtual to enable overriding for unit tests; it is unlikely that any other code
        /// will need to override this method.
        /// </remarks>
        /// <param name="exitCode">The exit code to report.</param>
        protected virtual void SetExitCode(int exitCode)
        {
            PSHelper.SetExitCode(Cmdlet, exitCode);
        }

        private void WriteProxyMessage(string message)
        {
            if (_proxyMessagesAreDebug)
            {
                PSHelper.WriteDebug(Cmdlet, message);
            }
            else
            {
                PSHelper.WriteHost(Cmdlet, message);
            }
        }

        /// <summary>
        /// Add the header to the <paramref name="message"/>.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown if the header cannot be added to the request message.
        /// </exception>
        private static void SetRequestHeader(HttpRequestMessage message, string name, string value, bool replace)
        {
            if (replace)
            {
                message.Headers.Remove(name);
            }

            if (!message.Headers.TryAddWithoutValidation(name, value))
            {
                throw new ArgumentException($"The '{name}' header cannot be set on the request.", nameof(name));
            }
        }

        private static string GetHeaderName(HttpRequestHeader header)
        {
            var headers = new WebHeaderCollection
            {
                { header, "value" }
            };

            return headers.AllKeys[0];
        }

        /// <summary>
        /// Consolidates the headers from all sources into a single header dictionary.
        /// </summary>
        private static Dictionary<string, string> ConsolidateHeaders(params IEnumerable<KeyValuePair<string, IEnumerable<string>>>[] sources)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var source in sources)
            {
                AddHeaders(headers, source);
            }

            return headers;
        }

        private static void AddHeaders(Dictionary<string, string> target, IEnumerable<KeyValuePair<string, IEnumerable<string>>> source)
        {
            if (source is null)
            {
                return;
            }

            foreach (var header in source)
            {
                target[header.Key] = string.Join(GetValueSeparator(header.Key), header.Value);
            }
        }

        /// <summary>
        /// Some header values are handed back in multiple pieces; for some of these it makes sense to normalise
        /// them by joining on space (such as User-Agent); others tend to be a list of entries.
        /// </summary>
        private static string GetValueSeparator(string headerName)
        {
            switch (headerName.ToLowerInvariant())
            {
                case "user-agent":
                case "server":
                    return " ";
                default:
                    return ",";
            }
        }

        private string GetMediaType()
        {
            return Response.Content?.Headers.ContentType?.MediaType ?? string.Empty;
        }

        private Encoding GetResponseEncoding()
        {
            var charset = Response.Content?.Headers.ContentType?.CharSet?.Trim('"', '\'');

            return Encoding.GetEncoding(string.IsNullOrEmpty(charset) ? "ISO-8859-1" : charset);
        }

        /// <summary>
        /// Sends the request once, and runs the <paramref name="onResponse"/> action if it succeeds.
        /// </summary>
        /// <returns><c>false</c> if the request failed and <see cref="TryRecover(Exception, bool)"/> asked for it to be sent again, otherwise <c>true</c>.</returns>
        private bool TryExecute(Action onResponse, bool isDownload, Action<HttpRequestMessage> beforeSend, bool allowRecovery)
        {
            RequestUri = GetRequestUri(isDownload);

            var client = GetHttpClient(ResolveProxy(RequestUri));
            var requestTimeout = GetRequestTimeout();

            using (var message = CreateRequestMessage(Options.WithUri(RequestUri)))
            {
                beforeSend?.Invoke(message);

                try
                {
                    Response = SendHttpRequest(client, message, requestTimeout);

                    if (!Response.IsSuccessStatusCode)
                    {
                        throw new WebException($"The remote server returned an error: ({(int)Response.StatusCode}) {Response.ReasonPhrase}.");
                    }

                    onResponse();

                    return true;
                }
                catch (Exception error) when (CancellationToken?.IsCancellationRequested == true)
                {
                    AbortRequest();

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
                    AbortRequest();

                    if (allowRecovery && TryRecover(error, isDownload))
                    {
                        return false;
                    }

                    if (isDownload)
                    {
                        SetExitCode(404);
                    }

                    var errorMessage = $"The remote file either doesn't exist, is unauthorized, or is forbidden for url '{RequestUri.OriginalString}'. {error.Message}";
                    if (isDownload && ShowDownloadCacheHint)
                    {
                        errorMessage += "\nThis package is likely not broken for licensed users - see https://docs.chocolatey.org/en-us/features/private-cdn.";
                    }

                    throw new WebException(errorMessage, error);
                }
            }
        }

        private void WriteRequestHeaders(HttpRequestMessage message)
        {
            PSHelper.WriteDebug(Cmdlet, "Request Headers:");
            foreach (var header in ConsolidateHeaders(message.Headers, message.Content?.Headers))
            {
                PSHelper.WriteDebug(
                    Cmdlet,
                    string.IsNullOrEmpty(header.Value) ? $"  '{header.Key}'" : $"  '{header.Key}':'{header.Value}'");
            }
        }

        private HttpResponseMessage SendHttpRequest(HttpClient client, HttpRequestMessage message, TimeSpan timeout)
        {
            var token = CancellationToken.GetValueOrDefault();
            using (var sendSource = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                // This mirrors the original behaviour with a default timeout, without binding all
                // operations to the exact same default timeout.
                sendSource.CancelAfter(timeout);

                try
                {
                    var send = client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, sendSource.Token);

                    // This ensures that we can bail out regardless of whether the underlying handler
                    // cancels as expected.
                    Task.WaitAny(new Task[] { send }, sendSource.Token);

                    return send.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested && sendSource.IsCancellationRequested)
                {
                    throw new WebException("The operation has timed out.", WebExceptionStatus.Timeout);
                }
            }
        }

        private void AbortRequest()
        {
            Response?.Dispose();
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
        /// <summary>
        /// The user agent that requests are made with unless one is given.
        /// </summary>
        public const string DefaultUserAgent = "chocolatey command line";

        public Uri Uri { get; set; }
        public string UserAgent { get; set; } = DefaultUserAgent;
        public IDictionary Headers { get; set; }

        /// <summary>
        /// Creates options for the given address.
        /// </summary>
        /// <param name="url">The address to request. When this is null or empty the options have no address.</param>
        /// <param name="userAgent">The user agent to make the request with.</param>
        /// <returns>The options.</returns>
        /// <exception cref="UriFormatException">Thrown if the <paramref name="url"/> is not a valid address.</exception>
        public static WebRequestOptions ForUrl(string url, string userAgent)
        {
            return new WebRequestOptions
            {
                Uri = string.IsNullOrEmpty(url) ? null : new Uri(url),
                UserAgent = userAgent
            };
        }

        /// <summary>
        /// Creates a copy of these options that requests a different address.
        /// </summary>
        /// <param name="uri">The address to request.</param>
        /// <returns>The copy of the options.</returns>
        public WebRequestOptions WithUri(Uri uri)
        {
            return new WebRequestOptions
            {
                Uri = uri,
                UserAgent = UserAgent,
                Headers = Headers
            };
        }
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
