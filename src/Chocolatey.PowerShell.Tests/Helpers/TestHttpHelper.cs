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
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Net;
using System.Net.Http;
using System.Threading;
using Chocolatey.PowerShell.Helpers;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    public class TestHttpHelper : HttpHelper
    {
        private HttpClient _testClient;

        public TestHttpHelper(PSCmdlet cmdlet, WebRequestOptions options)
            : base(cmdlet, options)
        {
        }

        public TestHttpHelper(PSCmdlet cmdlet, WebRequestOptions options, CancellationToken cancellationToken)
            : base(cmdlet, options, cancellationToken)
        {
        }

        /// <summary>
        /// The proxy reported as the system proxy, so that specs do not depend on the machine's settings.
        /// </summary>
        public IWebProxy SystemProxy { get; set; }

        /// <summary>
        /// When set, requests are sent through this handler instead of the network.
        /// </summary>
        public HttpMessageHandler MessageHandler { get; set; }

        /// <summary>
        /// The cookies for this helper only, so that specs do not share state through the process-wide container.
        /// </summary>
        public CookieContainer CookieJar { get; } = new CookieContainer();

        protected override CookieContainer Cookies => CookieJar;

        /// <summary>
        /// The exit code that the helper reported, if it reported one.
        /// </summary>
        public int? ReportedExitCode { get; private set; }

        protected override void SetExitCode(int exitCode)
        {
            ReportedExitCode = exitCode;
        }

        /// <summary>
        /// When set, chooses the address that each request is sent to.
        /// </summary>
        public Func<bool, Uri> RequestUriChooser { get; set; }

        /// <summary>
        /// The addresses that requests were sent to, in order.
        /// </summary>
        public List<Uri> ChosenUris { get; } = new List<Uri>();

        /// <summary>
        /// When set, decides whether a failed request is sent again.
        /// </summary>
        public Func<Exception, bool, bool> Recovery { get; set; }

        /// <summary>
        /// The errors that recovery was offered for, in order.
        /// </summary>
        public List<Exception> RecoveryErrors { get; } = new List<Exception>();

        /// <summary>
        /// When set, decides whether the message of a failed download mentions the private CDN, instead of the default.
        /// </summary>
        public bool? DownloadCacheHint { get; set; }

        protected override bool ShowDownloadCacheHint => DownloadCacheHint ?? base.ShowDownloadCacheHint;

        /// <summary>
        /// When set, the size of the buffer that downloads read into.
        /// </summary>
        public int? BufferSize { get; set; }

        /// <summary>
        /// Whether to record the number of bytes that each read of the download stream asks for.
        /// </summary>
        public bool RecordReads { get; set; }

        /// <summary>
        /// The number of bytes that each read of the download stream asked for, when <see cref="RecordReads"/> is set.
        /// </summary>
        public List<int> ReadSizes { get; } = new List<int>();

        protected override Uri GetRequestUri(bool isDownload)
        {
            var uri = RequestUriChooser?.Invoke(isDownload) ?? base.GetRequestUri(isDownload);
            ChosenUris.Add(uri);

            return uri;
        }

        protected override bool TryRecover(Exception error, bool isDownload)
        {
            RecoveryErrors.Add(error);

            return Recovery?.Invoke(error, isDownload) ?? false;
        }

        protected override int GetBufferSize()
        {
            return BufferSize ?? base.GetBufferSize();
        }

        protected override Stream GetDownloadStream()
        {
            var stream = base.GetDownloadStream();

            return RecordReads ? new ReadSizeRecordingStream(stream, ReadSizes) : stream;
        }

        public int Read(Stream stream, byte[] buffer)
        {
            return ReadBuffer(stream, buffer);
        }

        public void SetReadTimeout(TimeSpan? timeout)
        {
            ReadTimeout = timeout;
        }

        public HttpClientHandler CreateRequestHandler(IWebProxy proxy)
        {
            return CreateHandler(proxy);
        }

        public IWebProxy ProxyFor(Uri requestUri)
        {
            return ResolveProxy(requestUri);
        }

        public string ProxyKey(IWebProxy proxy)
        {
            return GetProxyKey(proxy);
        }

        public HttpRequestMessage CreateMessage()
        {
            return CreateRequestMessage(Options);
        }

        public HttpClient SharedHttpClient(IWebProxy proxy)
        {
            return base.GetHttpClient(proxy);
        }

        public TimeSpan RequestTimeout()
        {
            return GetRequestTimeout();
        }

        public TimeSpan? ResponseTimeout()
        {
            return GetReadTimeout();
        }

        public Dictionary<string, string> ReadResponseHeaders(HttpResponseMessage response)
        {
            Response = response;
            return GetResponseHeaders();
        }

        protected override IWebProxy GetSystemProxy()
        {
            return SystemProxy;
        }

        protected override HttpClient GetHttpClient(IWebProxy proxy)
        {
            if (MessageHandler is null)
            {
                return base.GetHttpClient(proxy);
            }

            if (_testClient is null)
            {
                _testClient = new HttpClient(MessageHandler)
                {
                    Timeout = Timeout.InfiniteTimeSpan
                };
            }

            return _testClient;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _testClient?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
