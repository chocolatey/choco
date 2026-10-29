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
using Chocolatey.PowerShell.Helpers;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    /// <summary>
    /// A request that answers with whatever the supplied function returns, and records the settings it is given.
    /// </summary>
    public class FakeFtpRequest : WebRequest
    {
        private readonly Func<WebResponse> _respond;
        private int _timeout;

        public FakeFtpRequest(Func<WebResponse> respond)
        {
            _respond = respond;
        }

        public override ICredentials Credentials { get; set; }

        public override IWebProxy Proxy { get; set; }

        public override string Method { get; set; }

        public bool TimeoutWasSet { get; private set; }

        public bool Aborted { get; private set; }

        public override int Timeout
        {
            get { return _timeout; }
            set
            {
                _timeout = value;
                TimeoutWasSet = true;
            }
        }

        public override WebResponse GetResponse()
        {
            return _respond();
        }

        public override void Abort()
        {
            Aborted = true;
        }
    }

    /// <summary>
    /// A response with the given content and length.
    /// </summary>
    public class FakeFtpResponse : WebResponse
    {
        private readonly byte[] _content;
        private readonly long _length;
        private readonly Stream _stream;

        public FakeFtpResponse(byte[] content)
            : this(content, content.Length)
        {
        }

        public FakeFtpResponse(byte[] content, long length)
        {
            _content = content;
            _length = length;
        }

        public FakeFtpResponse(Stream stream, long length)
        {
            _stream = stream;
            _length = length;
        }

        public override long ContentLength
        {
            get { return _length; }
            set { throw new NotSupportedException(); }
        }

        public override Stream GetResponseStream()
        {
            return _stream ?? new MemoryStream(_content);
        }

        public override void Close()
        {
        }
    }

    /// <summary>
    /// A stream whose reads always fail with an IO error.
    /// </summary>
    public class FaultingReadStream : MemoryStream
    {
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new IOException("The read failed.");
        }
    }

    public class TestFtpHelper : FtpHelper
    {
        public TestFtpHelper(PSCmdlet cmdlet, Uri uri, string userName, string password)
            : base(cmdlet, uri, userName, password)
        {
        }

        public TestFtpHelper(PSCmdlet cmdlet, Uri uri, string userName, string password, CancellationToken cancellationToken)
            : base(cmdlet, uri, userName, password, cancellationToken)
        {
        }

        /// <summary>
        /// The request that is used instead of creating one for the address.
        /// </summary>
        public FakeFtpRequest Request { get; set; }

        /// <summary>
        /// The stream that downloads are written to instead of a file.
        /// </summary>
        public MemoryStream Written { get; set; } = new MemoryStream();

        /// <summary>
        /// The exit code that the helper reported, if it reported one.
        /// </summary>
        public int? ReportedExitCode { get; private set; }

        protected override WebRequest CreateRequest(Uri uri)
        {
            return Request ?? base.CreateRequest(uri);
        }

        protected override string GetTargetPath(string filePath)
        {
            return filePath;
        }

        protected override Stream CreateTargetStream(string path)
        {
            return Written;
        }

        protected override void SetExitCode(int exitCode)
        {
            ReportedExitCode = exitCode;
        }
    }
}