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
using Chocolatey.PowerShell.Shared;
using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Threading;
using static chocolatey.StringResources;
using static chocolatey.StringResources.EnvironmentVariables;
using static System.Collections.Specialized.BitVector32;

namespace Chocolatey.PowerShell.Helpers
{
    /// <summary>
    /// The details of a file that a package needs to download.
    /// </summary>
    public class ChocolateyWebFileOptions
    {
        public string PackageName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Url64Bit { get; set; } = string.Empty;
        public string Checksum { get; set; } = string.Empty;
        public ChecksumType ChecksumType { get; set; } = ChecksumType.Md5;
        public string Checksum64 { get; set; } = string.Empty;
        public ChecksumType? ChecksumType64 { get; set; }

        /// <summary>
        /// Gets or sets any custom headers to send when downloading over HTTP.
        /// </summary>
        public IDictionary Headers { get; set; }

        /// <summary>
        /// Gets or sets whether to name the file after the name that the server gives it.
        /// </summary>
        public bool UseOriginalFileName { get; set; }

        /// <summary>
        /// Gets or sets whether to download the file even if there is already a file that looks the same.
        /// </summary>
        public bool ForceDownload { get; set; }
    }

    /// <summary>
    /// Downloads the file for a package, choosing the address and checksum for the architecture,
    /// reusing a file that was already downloaded, and verifying the result.
    /// </summary>
    public class WebFileDownloader : IDisposable
    {
        private readonly CancellationToken _cancellationToken;
        private IDisposable _helper;
        private bool _disposed;

        public WebFileDownloader(PSCmdlet cmdlet, CancellationToken cancellationToken)
        {
            Cmdlet = cmdlet;
            _cancellationToken = cancellationToken;
        }

        public WebFileDownloader(PSCmdlet cmdlet)
            : this(cmdlet, CancellationToken.None)
        {
        }

        protected PSCmdlet Cmdlet { get; }

        /// <summary>
        /// Applies any environment variable overrides to the given options, to allow Chocolatey CLI to
        /// override values with command options, if specified.
        /// </summary>
        /// <param name="options">The current options.</param>
        protected void ApplyOptionOverrides(ChocolateyWebFileOptions options)
        {
            options.Url = NormalizeUrl(GetOverrideOrDefault(Package.ChocolateyUrlOverride, options.Url));
            options.Url64Bit = NormalizeUrl(GetOverrideOrDefault(Package.ChocolateyUrl64bitOverride, options.Url64Bit));

            options.Checksum = GetOverrideOrDefault(Package.ChocolateyChecksum32, options.Checksum);
            options.ChecksumType = GetOverrideOrDefault(Package.ChocolateyChecksumType32, options.ChecksumType);
            options.Checksum64 = GetOverrideOrDefault(Package.ChocolateyChecksum64, options.Checksum64);
            options.ChecksumType64 = GetOverrideOrDefault(Package.ChocolateyChecksumType64, options.ChecksumType64);
        }

        /// <summary>
        /// Downloads the file for the package.
        /// </summary>
        /// <param name="options">The file to download.</param>
        /// <returns>The path of the file.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the package has no address for this architecture, or the downloaded file is not the expected length.</exception>
        /// <exception cref="NotSupportedException">Thrown if the address is not one that can be downloaded from.</exception>
        /// <exception cref="FileNotFoundException">Thrown if there is no file after the download.</exception>
        public string Download(ChocolateyWebFileOptions options)
        {
            ApplyOptionOverrides(options);

            var is64Bit = Is64BitOperatingSystem();
            PSHelper.WriteDebug(Cmdlet, $"CPU is {(is64Bit ? 64 : 32)} bit");

            var forceX86 = string.Equals(EnvironmentHelper.GetVariable(Package.ChocolateyForceX86), "true", StringComparison.OrdinalIgnoreCase);
            var selection = SelectDownload(options, is64Bit, forceX86);
            if (string.IsNullOrEmpty(selection.Url))
            {
                throw new InvalidOperationException($"This package does not support {selection.BitPackage} architecture.");
            }

            if (selection.Uses64BitUrl)
            {
                PSHelper.WriteDebug(Cmdlet, $"Setting url to '{options.Url64Bit}' and bitPackage to 64");
            }

            if (selection.ForcedX86)
            {
                PSHelper.WriteDebug(Cmdlet, "User specified -x86 so forcing 32 bit");
            }

            var url = selection.Url;
            var source = GetFileSource(url, out var uri);

            if (uri?.Scheme == Uri.UriSchemeHttp)
            {
                url = TryUseHttps(url);
            }

            var path = options.FilePath;
            if (options.UseOriginalFileName)
            {
                path = GetPathWithOriginalName(path, url);
            }

            try
            {
                EnsureParentDirectory(path);
            }
            catch (Exception error) when (CanHandle(error))
            {
                PSHelper.WriteHost(Cmdlet, $"Attempt to create directory failed for '{path}'.");
            }

            var urlIsRemote = true;
            var responseHeaders = new Hashtable(StringComparer.OrdinalIgnoreCase);

            switch (source)
            {
                case FileSource.Http:
                    responseHeaders = GetHeadersOrEmpty(url, options.Headers);
                    DownloadOrReuse(options, selection, url, path, responseHeaders);
                    break;

                case FileSource.Ftp:
                    PSHelper.WriteHost(Cmdlet, $"Ftp-ing {options.PackageName}\r\n  from '{url}'");
                    DownloadFtp(url, path);
                    break;

                default:
                    if (url.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                    {
                        url = new Uri(url).LocalPath;
                    }

                    PSHelper.WriteHost(Cmdlet, $"Copying {options.PackageName}\r\n  from '{url}'");
                    CopyFile(url, path);
                    urlIsRemote = false;
                    break;
            }

            // Validate file size and checksum; throws if something is wrong
            AssertDownloadedFileIsValid(selection, responseHeaders, path, url, urlIsRemote);
            return path;
        }

        private void AssertDownloadedFileIsValid(
            DownloadSelection selection,
            Hashtable responseHeaders,
            string path,
            string url,
            bool urlIsRemote)
        {
            var length = GetFileLength(path);
            if (length is null)
            {
                throw new FileNotFoundException($"Chocolatey expected a file to be downloaded to '{path}' but nothing exists at that location.", path);
            }

            if (responseHeaders.Count != 0 && string.IsNullOrEmpty(selection.Checksum))
            {
                PSHelper.WriteDebug(Cmdlet, $"Checking that '{path}' is the size we expect it to be.");
                if (responseHeaders.ContainsKey("Content-Length") && !IsLength(responseHeaders["Content-Length"], length.Value))
                {
                    throw new InvalidOperationException($"Chocolatey expected a file at '{path}' to be of length '{responseHeaders["Content-Length"]}' but the length was '{length}'.");
                }

                if (responseHeaders.ContainsKey("X-Checksum-Sha1"))
                {
                    var remoteChecksum = Convert.ToString(responseHeaders["X-Checksum-Sha1"]);
                    PSHelper.WriteDebug(Cmdlet, $"Verifying remote checksum of '{remoteChecksum}' for '{path}'.");
                    AssertChecksum(path, remoteChecksum, ChecksumType.Sha1, url);
                }
            }

            // Files that are not downloaded do not need a checksum, unless the package gives one.
            if (urlIsRemote || !string.IsNullOrEmpty(selection.Checksum))
            {
                PSHelper.WriteDebug(Cmdlet, $"Verifying package provided checksum of '{selection.Checksum}' for '{path}'.");
                AssertChecksum(path, selection.Checksum, selection.ChecksumType, url);
            }
        }

        /// <summary>
        /// Removes the doubled slashes from an address, and puts the second slash back in the scheme.
        /// </summary>
        public static string NormalizeUrl(string url)
        {
            return (url ?? string.Empty).Replace("//", "/").Replace(":/", "://");
        }

        /// <summary>
        /// Chooses the address, checksum and checksum type to use for the current architecture.
        /// </summary>
        internal static DownloadSelection SelectDownload(
            ChocolateyWebFileOptions options,
            bool is64Bit,
            bool forceX86)
        {
            var selection = new DownloadSelection
            {
                Url = options.Url,
                Checksum = options.Checksum,
                ChecksumType = options.ChecksumType,
            };

            var urlsDiffer = !PSHelper.IsEqual(options.Url, options.Url64Bit);

            if (forceX86)
            {
                selection.ForcedX86 = true;
                if (urlsDiffer)
                {
                    selection.BitPackage = "32 bit";
                }

                selection.Url = options.Url;
                selection.Checksum = options.Checksum;
                selection.ChecksumType = options.ChecksumType;

                return selection;
            }

            if (!string.IsNullOrEmpty(options.Url64Bit))
            {
                if (urlsDiffer)
                {
                    selection.BitPackage = "32 bit";
                }

                if (is64Bit)
                {
                    selection.BitPackage = "64 bit";
                    selection.Uses64BitUrl = true;
                    selection.Url = options.Url64Bit;

                    // The checksum only changes if the addresses are different.
                    if (urlsDiffer)
                    {
                        selection.Checksum = options.Checksum64;
                        if (options.ChecksumType64 is ChecksumType value)
                        {
                            selection.ChecksumType = value;
                        }
                    }
                }
            }

            return selection;
        }

        /// <summary>
        /// Works out how to get the file at an address, from its scheme.
        /// </summary>
        /// <param name="url">The address, or the path of a local file.</param>
        /// <param name="uri">The parsed address, or <c>null</c> for a path that is not an absolute address.</param>
        /// <exception cref="NotSupportedException">Thrown if the scheme of the address is not one that can be downloaded from.</exception>
        internal static FileSource GetFileSource(string url, out Uri uri)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                return FileSource.LocalFile;
            }

            switch (uri.Scheme)
            {
                case "http":
                case "https":
                    return FileSource.Http;
                case "ftp":
                    return FileSource.Ftp;
                case "file":
                    return FileSource.LocalFile;
                default:
                    throw new NotSupportedException($"The address '{url}' cannot be downloaded from, as the scheme '{uri.Scheme}' is not supported. Packages can use http, https, ftp and file addresses, or the path to a local file.");
            }
        }

        /// <summary>
        /// Replaces the scheme at the start of an http address with https.
        /// </summary>
        internal static string ToHttps(string url)
        {
            return "https" + url.Substring("http".Length);
        }

        /// <summary>
        /// Gets whether the architecture width is 64 bit.
        /// </summary>
        protected virtual bool Is64BitOperatingSystem()
        {
            return ArchitectureWidth.Matches(64);
        }

        /// <summary>
        /// Gets the response headers for an address.
        /// </summary>
        protected virtual Hashtable GetHeaders(string url, IDictionary headers)
        {
            using (var helper = new HttpHelper(Cmdlet, WebRequestOptions.ForUrl(url, WebRequestOptions.DefaultUserAgent), _cancellationToken))
            {
                _helper = helper;

                return helper.GetHeaders();
            }
        }

        /// <summary>
        /// Downloads a file over HTTP or HTTPS.
        /// </summary>
        protected virtual void DownloadHttp(string url, string path, IDictionary headers)
        {
            var options = new WebRequestOptions
            {
                Uri = new Uri(url),
                UserAgent = WebRequestOptions.DefaultUserAgent,
                Headers = headers,
            };

            using (var helper = new HttpHelper(Cmdlet, options, _cancellationToken))
            {
                _helper = helper;
                helper.DownloadFile(path, passthru: false, quiet: false);
            }
        }

        /// <summary>
        /// Downloads a file over FTP.
        /// </summary>
        protected virtual void DownloadFtp(string url, string path)
        {
            using (var helper = new FtpHelper(Cmdlet, new Uri(url), string.Empty, string.Empty, _cancellationToken))
            {
                _helper = helper;
                helper.DownloadFile(path, quiet: false);
            }
        }

        /// <summary>
        /// Gets the name of the file that the server would give to a download.
        /// </summary>
        protected virtual string GetRemoteFileName(string url, string defaultName)
        {
            using (var helper = new HttpHelper(Cmdlet, WebRequestOptions.ForUrl(url, WebRequestOptions.DefaultUserAgent), _cancellationToken))
            {
                _helper = helper;

                return helper.GetRemoteFileName(defaultName);
            }
        }

        protected virtual string ResolvePath(string path)
        {
            return PSHelper.GetUnresolvedPath(Cmdlet, path);
        }

        protected virtual void EnsureParentDirectory(string path)
        {
            PSHelper.EnsureDirectoryExists(Cmdlet, PSHelper.GetParentDirectory(Cmdlet, path));
        }

        protected virtual void CopyFile(string source, string destination)
        {
            PSHelper.CopyFile(Cmdlet, source, destination, force: true);
        }

        /// <summary>
        /// Gets the length of a file, or <c>null</c> if there is no file.
        /// </summary>
        protected virtual long? GetFileLength(string path)
        {
            var fullPath = ResolvePath(path);

            return File.Exists(fullPath) ? new FileInfo(fullPath).Length : (long?)null;
        }

        /// <summary>
        /// Checks the file against a checksum.
        /// </summary>
        /// <exception cref="ChecksumVerificationFailedException">When the checksums don't match.</exception>
        /// <exception cref="FileNotFoundException">When checksum.exe or the file to verify is missing.</exception>
        /// <exception cref="ChecksumMissingException">When no checksum is provided and is required.</exception>
        protected virtual void AssertChecksum(string path, string checksum, ChecksumType checksumType, string url)
        {
            ChecksumValidator.AssertChecksumValid(Cmdlet, path, checksum, checksumType, url);
        }

        private static string GetOverrideOrDefault(string variableName, string defaultValue)
        {
            var overrideValue = EnvironmentHelper.GetVariable(variableName);

            return string.IsNullOrEmpty(overrideValue) ? defaultValue : overrideValue;
        }

        private static T GetOverrideOrDefault<T>(string variableName, T defaultValue)
        {
            var overrideValue = EnvironmentHelper.GetVariable(variableName);
            if (!string.IsNullOrEmpty(overrideValue)
                && PSHelper.TryConvertTo<T>(overrideValue, out var result))
            {
                return result;
            }

            return defaultValue;
        }

        private static bool IsLength(object expected, long length)
        {
            return PSHelper.TryConvertTo<long>(expected, out var expectedLength) && expectedLength == length;
        }

        private string TryUseHttps(string url)
        {
            var httpsUrl = ToHttps(url);
            try
            {
                GetHeaders(httpsUrl, headers: null);
            }
            catch (Exception) when (!_cancellationToken.IsCancellationRequested)
            {
                PSHelper.WriteDebug(Cmdlet, "Url does not have HTTPS available");

                return url;
            }

            Cmdlet.WriteWarning("Url has SSL/TLS available, switching to HTTPS for download");

            return httpsUrl;
        }

        private string GetPathWithOriginalName(string path, string url)
        {
            try
            {
                // remove \chocolatey\chocolatey\
                path = Regex.Replace(path, @"\\chocolatey\\chocolatey\\", @"\chocolatey\", RegexOptions.IgnoreCase);
                var directory = Path.GetDirectoryName(path);
                var originalFileName = Path.GetFileName(path);

                return ResolvePath(Path.Combine(directory, GetRemoteFileName(url, originalFileName)));
            }
            catch (Exception error) when (CanHandle(error))
            {
                PSHelper.WriteHost(Cmdlet, $"Attempt to use original download file name failed for '{url}'.");

                return path;
            }
        }

        private Hashtable GetHeadersOrEmpty(string url, IDictionary headers)
        {
            try
            {
                return GetHeaders(url, headers);
            }
            catch (Exception error) when (CanHandle(error))
            {
                PSHelper.WriteHost(Cmdlet, $"Attempt to get headers for {url} failed.\n  {error.Message}");

                return new Hashtable(StringComparer.OrdinalIgnoreCase);
            }
        }

        private bool IsCachedFileValid(
            ChocolateyWebFileOptions options,
            DownloadSelection selection,
            string url,
            string path,
            Hashtable responseHeaders)
        {
            var cachedLength = GetFileLength(path);

            if (cachedLength.HasValue && !options.ForceDownload)
            {
                if (!string.IsNullOrEmpty(selection.Checksum))
                {
                    try
                    {
                        PSHelper.WriteHost(Cmdlet, "File appears to be downloaded already. Verifying with package checksum to determine if it needs to be redownloaded.");
                        AssertChecksum(path, selection.Checksum, selection.ChecksumType, url);
                        return true;
                    }
                    catch (Exception) when (!_cancellationToken.IsCancellationRequested)
                    {
                        PSHelper.WriteDebug(Cmdlet, "Existing file failed checksum. Will be redownloaded from url.");
                    }
                }
                else if (responseHeaders.Count != 0 && responseHeaders.ContainsKey("Content-Length"))
                {
                    // If the file already exists there is no reason to download it again.
                    return IsLength(responseHeaders["Content-Length"], cachedLength.Value);
                }
            }

            return false;
        }

        private void DownloadOrReuse(
            ChocolateyWebFileOptions options,
            DownloadSelection selection,
            string url,
            string path,
            Hashtable responseHeaders)
        {
            if (!IsCachedFileValid(options, selection, url, path, responseHeaders))
            {
                PSHelper.WriteHost(Cmdlet, $"Downloading {options.PackageName} {selection.BitPackage}\r\n  from '{url}'");
                DownloadHttp(url, path, options.Headers);
            }
            else
            {
                PSHelper.WriteDebug(Cmdlet, $"{options.PackageName}'s requested file has already been downloaded. Using cached copy at\r\n '{path}'.");
            }
        }

        /// <summary>
        /// Errors that come from Ctrl+C or other cmdlets in a pipeline throwing exceptions should not be
        /// handled; this determines whether we should rethrow exceptions or whether we can handle them.
        /// </summary>
        /// <param name="error">The exception to check.</param>
        /// <returns></returns>
        private bool CanHandle(Exception error)
        {
            return !(error is OperationCanceledException) && !_cancellationToken.IsCancellationRequested;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _helper?.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal enum FileSource
        {
            Http,
            Ftp,
            LocalFile,
        }

        internal sealed class DownloadSelection
        {
            public string Url { get; set; }
            public string Checksum { get; set; }
            public ChecksumType ChecksumType { get; set; }
            public string BitPackage { get; set; } = string.Empty;
            public bool Uses64BitUrl { get; set; }
            public bool ForcedX86 { get; set; }
        }
    }
}