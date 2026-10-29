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
using System.Management.Automation;
using Chocolatey.PowerShell.Helpers;
using Chocolatey.PowerShell.Shared;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    /// <summary>
    /// A downloader that records what it is asked to do, and keeps its files in memory.
    /// </summary>
    public class TestChocolateyWebFileDownloader : WebFileDownloader
    {
        public TestChocolateyWebFileDownloader(PSCmdlet cmdlet)
            : base(cmdlet)
        {
        }

        public bool Is64Bit { get; set; } = true;

        /// <summary>
        /// The files that exist, by path, with their lengths.
        /// </summary>
        public Dictionary<string, long> Files { get; } = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The length of a file that is downloaded or copied.
        /// </summary>
        public long DownloadedLength { get; set; } = 10;

        /// <summary>
        /// When set, downloads and copies do not leave a file behind.
        /// </summary>
        public bool LeaveNoFile { get; set; }

        /// <summary>
        /// Gets the headers for an address, or throws if the headers cannot be retrieved.
        /// </summary>
        public Func<string, Hashtable> HeadersFor { get; set; } = url => new Hashtable(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the exception that a checksum check fails with, or <c>null</c> if it passes.
        /// </summary>
        public Func<string, string, string, string, Exception> ChecksumFailure { get; set; } = (path, checksum, type, url) => null;

        public Func<string, string, string> RemoteFileName { get; set; } = (url, defaultName) => defaultName;

        public Action<string> OnEnsureParentDirectory { get; set; } = path => { };

        public List<string> HeaderRequests { get; } = new List<string>();
        public List<string> HttpDownloads { get; } = new List<string>();
        public List<IDictionary> HttpDownloadHeaders { get; } = new List<IDictionary>();
        public List<string> FtpDownloads { get; } = new List<string>();
        public List<KeyValuePair<string, string>> Copies { get; } = new List<KeyValuePair<string, string>>();
        public List<string[]> ChecksumChecks { get; } = new List<string[]>();
        public List<string> Calls { get; } = new List<string>();

        protected override bool Is64BitOperatingSystem()
        {
            return Is64Bit;
        }

        protected override Hashtable GetHeaders(string url, IDictionary headers)
        {
            HeaderRequests.Add(url);
            Calls.Add("headers " + url);

            return HeadersFor(url);
        }

        protected override void DownloadHttp(string url, string path, IDictionary headers)
        {
            HttpDownloads.Add(url);
            HttpDownloadHeaders.Add(headers);
            Calls.Add("download " + url);
            AddFile(path);
        }

        protected override void DownloadFtp(string url, string path)
        {
            FtpDownloads.Add(url);
            Calls.Add("ftp " + url);
            AddFile(path);
        }

        protected override string GetRemoteFileName(string url, string defaultName)
        {
            return RemoteFileName(url, defaultName);
        }

        protected override string ResolvePath(string path)
        {
            return path;
        }

        protected override void EnsureParentDirectory(string path)
        {
            OnEnsureParentDirectory(path);
        }

        protected override void CopyFile(string source, string destination)
        {
            Copies.Add(new KeyValuePair<string, string>(source, destination));
            Calls.Add("copy " + source);
            AddFile(destination);
        }

        protected override long? GetFileLength(string path)
        {
            return Files.TryGetValue(path, out var length) ? length : (long?)null;
        }

        protected override void AssertChecksum(string path, string checksum, ChecksumType checksumType, string url)
        {
            var type = checksumType.ToString().ToLowerInvariant();
            ChecksumChecks.Add(new[] { path, checksum, type, url });
            Calls.Add("checksum " + checksum);

            var failure = ChecksumFailure(path, checksum, type, url);
            if (!(failure is null))
            {
                throw failure;
            }
        }

        private void AddFile(string path)
        {
            if (!LeaveNoFile)
            {
                Files[path] = DownloadedLength;
            }
        }
    }
}
