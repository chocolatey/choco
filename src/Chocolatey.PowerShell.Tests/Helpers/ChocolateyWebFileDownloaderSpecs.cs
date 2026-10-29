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
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using Chocolatey.PowerShell.Helpers;
using Chocolatey.PowerShell.Shared;
using FluentAssertions;
using Moq;
using static chocolatey.StringResources.EnvironmentVariables;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    // ReSharper disable InconsistentNaming

    public class ChocolateyWebFileDownloaderSpecs
    {
        public class When_normalizing_an_address : TinySpec
        {
            public override void Context()
            {
            }

            public override void Because()
            {
            }

            [InlineData("https://example.com/a//b", "https://example.com/a/b")]
            [InlineData("https://example.com//a", "https://example.com/a")]
            [InlineData("https://example.com/a?u=http://b/c", "https://example.com/a?u=http://b/c")]
            [InlineData("http://example.com/a.exe?x=1&y=//z", "http://example.com/a.exe?x=1&y=/z")]
            [InlineData("ftp://example.com/a.exe", "ftp://example.com/a.exe")]
            [InlineData(@"C:\tools\a.exe", @"C:\tools\a.exe")]
            [InlineData(@"file:///\\fileshare\location\bob.exe", @"file:///\\fileshare\location\bob.exe")]
            [InlineData("", "")]
            [InlineData(null, "")]
            public void Should_collapse_repeated_slashes_after_the_scheme(string url, string expected)
            {
                WebFileDownloader.NormalizeUrl(url).Should().Be(expected);
            }
        }

        public class When_choosing_what_to_download : TinySpec
        {
            public override void Context()
            {
            }

            public override void Because()
            {
            }

            // A 64 bit address, with its own checksum.
            [InlineData("http://a/32", "http://a/64", "c32", "md5", "c64", "sha256", true, false, "http://a/64", "c64", "sha256", "64 bit")]
            // The type of checksum falls back to the 32 bit one.
            [InlineData("http://a/32", "http://a/64", "c32", "md5", "c64", "", true, false, "http://a/64", "c64", "md5", "64 bit")]
            // The same address for both keeps the 32 bit checksum.
            [InlineData("http://a/x", "http://a/x", "c32", "md5", "c64", "sha256", true, false, "http://a/x", "c32", "md5", "64 bit")]
            // Addresses that only differ by case are the same address.
            [InlineData("HTTP://A/X", "http://a/x", "c32", "md5", "c64", "sha256", true, false, "http://a/x", "c32", "md5", "64 bit")]
            // A 32 bit system uses the 32 bit address.
            [InlineData("http://a/32", "http://a/64", "c32", "md5", "c64", "sha256", false, false, "http://a/32", "c32", "md5", "32 bit")]
            // Only a 32 bit address.
            [InlineData("http://a/32", "", "c32", "md5", "", "", true, false, "http://a/32", "c32", "md5", "")]
            [InlineData("http://a/32", "", "c32", "md5", "", "", false, false, "http://a/32", "c32", "md5", "")]
            // Only a 64 bit address.
            [InlineData("", "http://a/64", "c32", "md5", "c64", "sha256", true, false, "http://a/64", "c64", "sha256", "64 bit")]
            [InlineData("", "http://a/64", "c32", "md5", "c64", "sha256", false, false, "", "c32", "md5", "32 bit")]
            // Forcing 32 bit.
            [InlineData("http://a/32", "http://a/64", "c32", "md5", "c64", "sha256", true, true, "http://a/32", "c32", "md5", "32 bit")]
            [InlineData("http://a/x", "http://a/x", "c32", "md5", "c64", "sha256", true, true, "http://a/x", "c32", "md5", "")]
            public void Should_choose_the_address_and_checksum(
                string url32,
                string url64,
                string checksum32,
                string checksumType32,
                string checksum64,
                string checksumType64,
                bool is64Bit,
                bool forceX86,
                string expectedUrl,
                string expectedChecksum,
                string expectedChecksumType,
                string expectedBitPackage)
            {
                var options = new ChocolateyWebFileOptions
                {
                    Url = url32,
                    Url64Bit = url64,
                    Checksum = checksum32,
                    ChecksumType = ParseChecksumType(checksumType32) ?? ChecksumType.Md5,
                    Checksum64 = checksum64,
                    ChecksumType64 = ParseChecksumType(checksumType64),
                };

                var selection = WebFileDownloader.SelectDownload(options, is64Bit, forceX86);

                selection.Url.Should().Be(expectedUrl);
                selection.Checksum.Should().Be(expectedChecksum);
                selection.ChecksumType.ToString().ToLowerInvariant().Should().Be(expectedChecksumType);
                selection.BitPackage.Should().Be(expectedBitPackage);
            }

            private static ChecksumType? ParseChecksumType(string value)
            {
                return string.IsNullOrEmpty(value) ? (ChecksumType?)null : (ChecksumType)Enum.Parse(typeof(ChecksumType), value, ignoreCase: true);
            }
        }

        public class When_classifying_an_address : TinySpec
        {
            public override void Context()
            {
            }

            public override void Because()
            {
            }

            [InlineData("http://example.com/a.exe", "Http")]
            [InlineData("HTTP://example.com/a.exe", "Http")]
            [InlineData("https://example.com/a.exe", "Http")]
            [InlineData("Https://example.com/a.exe", "Http")]
            [InlineData("ftp://example.com/a.exe", "Ftp")]
            [InlineData("FTP://example.com/a.exe", "Ftp")]
            [InlineData("file:///tmp/tools/a.exe", "LocalFile")]
            [InlineData("FILE:///tmp/tools/a.exe", "LocalFile")]
            [InlineData("a.exe", "LocalFile")]
            [InlineData("../tools/a.exe", "LocalFile")]
            public void Should_choose_how_to_get_the_file(string url, string expected)
            {
                WebFileDownloader.GetFileSource(url, out var uri).ToString().Should().Be(expected);
            }

            [InlineData(@"C:\tools\a.exe")]
            [InlineData(@"\\server\share\a.exe")]
            [NUnit.Framework.Platform("Win")]
            public void Should_treat_a_windows_path_as_a_local_file(string path)
            {
                WebFileDownloader.GetFileSource(path, out var uri).ToString().Should().Be("LocalFile");
            }

            [InlineData("ftps://example.com/a.exe", "ftps")]
            [InlineData("httpx://example.com/a.exe", "httpx")]
            [InlineData("sftp://example.com/a.exe", "sftp")]
            [InlineData("mailto:someone@example.com", "mailto")]
            public void Should_not_support_any_other_scheme(string url, string scheme)
            {
                Action classify = () => WebFileDownloader.GetFileSource(url, out var uri);

                classify.Should().Throw<NotSupportedException>().Which.Message.Should().Contain($"the scheme '{scheme}' is not supported");
            }
        }

        public class When_switching_an_address_to_https : TinySpec
        {
            public override void Context()
            {
            }

            public override void Because()
            {
            }

            [InlineData("http://example.com/a.exe", "https://example.com/a.exe")]
            [InlineData("HTTP://example.com/a.exe", "https://example.com/a.exe")]
            [InlineData("http://example.com:8080/a.exe?u=http://other/b", "https://example.com:8080/a.exe?u=http://other/b")]
            public void Should_only_change_the_scheme(string url, string expected)
            {
                WebFileDownloader.ToHttps(url).Should().Be(expected);
            }
        }

        public abstract class DownloadSpecsBase : EnvironmentSpecsBase
        {
            protected Mock<ICommandRuntime> CommandRuntime;
            protected TestCmdlet Cmdlet;
            protected TestChocolateyWebFileDownloader Downloader;
            protected ChocolateyWebFileOptions Options;
            protected string Result;
            protected Exception Error;

            protected override string[] ManagedVariables { get; } =
            {
                Package.ChocolateyUrlOverride,
                Package.ChocolateyUrl64bitOverride,
                Package.ChocolateyChecksum32,
                Package.ChocolateyChecksumType32,
                Package.ChocolateyChecksum64,
                Package.ChocolateyChecksumType64,
                Package.ChocolateyForceX86,
            };

            public override void Context()
            {
                base.Context();

                CommandRuntime = new Mock<ICommandRuntime>();
                Cmdlet = new TestCmdlet { CommandRuntime = CommandRuntime.Object };
                Downloader = new TestChocolateyWebFileDownloader(Cmdlet);
                Options = new ChocolateyWebFileOptions
                {
                    PackageName = "package",
                    FilePath = "file.exe",
                    Url = "https://example.com/file.exe",
                };
            }

            public override void Because()
            {
                try
                {
                    Result = Downloader.Download(Options);
                }
                catch (Exception error)
                {
                    Error = error;
                }
            }

            public override void AfterObservations()
            {
                Downloader.Dispose();
                base.AfterObservations();
            }

            protected void VerifyHost(string message)
            {
                CommandRuntime.Verify(r => r.WriteVerbose(message), Times.Once);
            }

            protected void VerifyDebug(string message)
            {
                CommandRuntime.Verify(r => r.WriteDebug(message), Times.Once);
            }
        }

        public class When_downloading_a_file_over_https : DownloadSpecsBase
        {
            [Fact]
            public void Should_not_fail()
            {
                Error.Should().BeNull();
            }

            [Fact]
            public void Should_return_the_path_of_the_file()
            {
                Result.Should().Be("file.exe");
            }

            [Fact]
            public void Should_download_from_the_address()
            {
                Downloader.HttpDownloads.Should().Equal("https://example.com/file.exe");
            }

            [Fact]
            public void Should_get_the_headers_once()
            {
                Downloader.HeaderRequests.Should().Equal("https://example.com/file.exe");
            }

            [Fact]
            public void Should_say_what_is_being_downloaded()
            {
                VerifyHost("Downloading package \r\n  from 'https://example.com/file.exe'");
            }

            [Fact]
            public void Should_verify_the_checksum_of_the_remote_file()
            {
                Downloader.ChecksumChecks.Should().ContainSingle().Which.Should().Equal("file.exe", string.Empty, "md5", "https://example.com/file.exe");
            }

            [Fact]
            public void Should_say_that_the_package_checksum_is_being_verified()
            {
                VerifyDebug("Verifying package provided checksum of '' for 'file.exe'.");
            }
        }

        public class When_the_address_is_http_and_https_is_available : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Url = "http://example.com/file.exe";
            }

            [Fact]
            public void Should_switch_to_https_for_the_download()
            {
                Downloader.HttpDownloads.Should().Equal("https://example.com/file.exe");
            }

            [Fact]
            public void Should_warn_about_the_switch()
            {
                CommandRuntime.Verify(r => r.WriteWarning("Url has SSL/TLS available, switching to HTTPS for download"), Times.Once);
            }

            [Fact]
            public void Should_get_the_headers_for_the_https_address_both_times()
            {
                Downloader.HeaderRequests.Should().Equal("https://example.com/file.exe", "https://example.com/file.exe");
            }

            [Fact]
            public void Should_verify_the_checksum_against_the_https_address()
            {
                Downloader.ChecksumChecks.Single()[3].Should().Be("https://example.com/file.exe");
            }
        }

        public class When_the_address_is_http_and_https_is_not_available : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Url = "http://example.com/file.exe";
                Downloader.HeadersFor = url =>
                {
                    if (url.StartsWith("https"))
                    {
                        throw new WebException("Unable to connect to the remote server");
                    }

                    return new Hashtable(StringComparer.OrdinalIgnoreCase);
                };
            }

            [Fact]
            public void Should_download_from_the_http_address()
            {
                Downloader.HttpDownloads.Should().Equal("http://example.com/file.exe");
            }

            [Fact]
            public void Should_say_that_https_is_not_available_in_the_debug_output()
            {
                VerifyDebug("Url does not have HTTPS available");
            }

            [Fact]
            public void Should_not_warn()
            {
                CommandRuntime.Verify(r => r.WriteWarning(It.IsAny<string>()), Times.Never);
            }
        }

        public class When_the_address_has_an_upper_case_scheme : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Url = "HTTP://example.com/file.exe";
            }

            [Fact]
            public void Should_download_it_over_https()
            {
                Downloader.HttpDownloads.Should().Equal("https://example.com/file.exe");
            }

            [Fact]
            public void Should_not_copy_it_as_a_file()
            {
                Downloader.Copies.Should().BeEmpty();
            }
        }

        public class When_the_headers_cannot_be_retrieved : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Downloader.HeadersFor = url => throw new WebException("The remote server returned an error: (403) Forbidden.");
            }

            [Fact]
            public void Should_still_download_the_file()
            {
                Downloader.HttpDownloads.Should().Equal("https://example.com/file.exe");
            }

            [Fact]
            public void Should_say_that_the_headers_failed()
            {
                VerifyHost("Attempt to get headers for https://example.com/file.exe failed.\n  The remote server returned an error: (403) Forbidden.");
            }

            [Fact]
            public void Should_not_check_the_length()
            {
                CommandRuntime.Verify(r => r.WriteDebug(It.Is<string>(m => m.StartsWith("Checking that"))), Times.Never);
            }
        }

        public abstract class ExistingFileSpecsBase : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Downloader.Files["file.exe"] = 10;
            }
        }

        public class When_the_file_exists_and_the_length_matches_the_headers : ExistingFileSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Downloader.HeadersFor = url => new Hashtable(StringComparer.OrdinalIgnoreCase) { { "Content-Length", "10" } };
            }

            [Fact]
            public void Should_not_download_the_file_again()
            {
                Downloader.HttpDownloads.Should().BeEmpty();
            }

            [Fact]
            public void Should_say_that_the_cached_copy_is_used()
            {
                VerifyDebug("package's requested file has already been downloaded. Using cached copy at\r\n 'file.exe'.");
            }
        }

        public class When_the_file_exists_and_the_length_does_not_match_the_headers : ExistingFileSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Downloader.HeadersFor = url => new Hashtable(StringComparer.OrdinalIgnoreCase) { { "Content-Length", "99" } };
                Downloader.DownloadedLength = 99;
            }

            [Fact]
            public void Should_download_the_file_again()
            {
                Downloader.HttpDownloads.Should().Equal("https://example.com/file.exe");
            }
        }

        public class When_the_file_exists_and_a_download_is_forced : ExistingFileSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.ForceDownload = true;
                Downloader.HeadersFor = url => new Hashtable(StringComparer.OrdinalIgnoreCase) { { "Content-Length", "10" } };
            }

            [Fact]
            public void Should_download_the_file_again()
            {
                Downloader.HttpDownloads.Should().Equal("https://example.com/file.exe");
            }
        }

        public class When_the_file_exists_and_matches_the_package_checksum : ExistingFileSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Checksum = "abc123";
                Options.ChecksumType = ChecksumType.Sha256;
            }

            [Fact]
            public void Should_not_download_the_file_again()
            {
                Downloader.HttpDownloads.Should().BeEmpty();
            }

            [Fact]
            public void Should_say_that_it_is_verifying_the_existing_file()
            {
                VerifyHost("File appears to be downloaded already. Verifying with package checksum to determine if it needs to be redownloaded.");
            }

            [Fact]
            public void Should_check_the_checksum_of_the_existing_file()
            {
                Downloader.ChecksumChecks.First().Should().Equal("file.exe", "abc123", "sha256", "https://example.com/file.exe");
            }
        }

        public class When_the_file_exists_and_does_not_match_the_package_checksum : ExistingFileSpecsBase
        {
            private int _checks;

            public override void Context()
            {
                base.Context();
                Options.Checksum = "abc123";
                Options.ChecksumType = ChecksumType.Sha256;

                // The existing file fails, and the new one passes.
                Downloader.ChecksumFailure = (path, checksum, type, url) => _checks++ == 0 ? new InvalidOperationException("The checksum did not match.") : null;
            }

            [Fact]
            public void Should_download_the_file_again()
            {
                Downloader.HttpDownloads.Should().Equal("https://example.com/file.exe");
            }

            [Fact]
            public void Should_say_why_in_the_debug_output()
            {
                VerifyDebug("Existing file failed checksum. Will be redownloaded from url.");
            }

            [Fact]
            public void Should_verify_the_new_file()
            {
                Downloader.ChecksumChecks.Should().HaveCount(2);
            }
        }

        public class When_the_downloaded_file_is_not_the_length_in_the_headers : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Downloader.HeadersFor = url => new Hashtable(StringComparer.OrdinalIgnoreCase) { { "Content-Length", "99" } };
                Downloader.DownloadedLength = 10;
            }

            [Fact]
            public void Should_fail_naming_the_expected_and_actual_lengths()
            {
                Error.Should().BeOfType<InvalidOperationException>()
                    .Which.Message.Should().Be("Chocolatey expected a file at 'file.exe' to be of length '99' but the length was '10'.");
            }

            [Fact]
            public void Should_not_check_the_checksum()
            {
                Downloader.ChecksumChecks.Should().BeEmpty();
            }
        }

        public class When_the_package_gives_a_checksum_and_the_headers_have_a_different_length : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Checksum = "abc123";
                Downloader.HeadersFor = url => new Hashtable(StringComparer.OrdinalIgnoreCase) { { "Content-Length", "99" } };
            }

            [Fact]
            public void Should_not_compare_the_length()
            {
                Error.Should().BeNull();
            }

            [Fact]
            public void Should_verify_the_package_checksum_with_the_default_checksum_type()
            {
                Downloader.ChecksumChecks.Should().ContainSingle().Which.Should().Equal("file.exe", "abc123", "md5", "https://example.com/file.exe");
            }
        }

        public class When_the_server_gives_a_sha1_checksum : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Downloader.HeadersFor = url => new Hashtable(StringComparer.OrdinalIgnoreCase)
                {
                    { "Content-Length", "10" },
                    { "X-Checksum-Sha1", "da39a3ee5e6b4b0d3255bfef95601890afd80709" },
                };
            }

            [Fact]
            public void Should_verify_the_remote_checksum_as_sha1_before_the_package_checksum()
            {
                Downloader.ChecksumChecks.Should().HaveCount(2);
                Downloader.ChecksumChecks[0].Should().Equal("file.exe", "da39a3ee5e6b4b0d3255bfef95601890afd80709", "sha1", "https://example.com/file.exe");
            }

            [Fact]
            public void Should_say_that_it_is_verifying_the_remote_checksum()
            {
                VerifyDebug("Verifying remote checksum of 'da39a3ee5e6b4b0d3255bfef95601890afd80709' for 'file.exe'.");
            }
        }

        public class When_the_checksum_does_not_match : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Checksum = "abc123";
                Downloader.ChecksumFailure = (path, checksum, type, url) => new InvalidOperationException("The checksum did not match.");
            }

            [Fact]
            public void Should_fail_with_the_checksum_error()
            {
                Error.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("The checksum did not match.");
            }
        }

        public class When_the_address_is_an_ftp_address : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Url = "ftp://example.com/files/file.exe";
            }

            [Fact]
            public void Should_download_over_ftp()
            {
                Downloader.FtpDownloads.Should().Equal("ftp://example.com/files/file.exe");
            }

            [Fact]
            public void Should_say_what_is_being_downloaded()
            {
                VerifyHost("Ftp-ing package\r\n  from 'ftp://example.com/files/file.exe'");
            }

            [Fact]
            public void Should_not_make_a_web_request()
            {
                Downloader.HeaderRequests.Should().BeEmpty();
                Downloader.HttpDownloads.Should().BeEmpty();
            }

            [Fact]
            public void Should_verify_the_checksum()
            {
                Downloader.ChecksumChecks.Should().ContainSingle();
            }
        }

        public class When_the_address_is_a_file_address : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Url = "file:///tmp/tools/source.exe";
            }

            [Fact]
            public void Should_copy_the_local_file()
            {
                Downloader.Copies.Should().ContainSingle().Which.Key.Should().Be(new Uri("file:///tmp/tools/source.exe").LocalPath);
            }

            [Fact]
            public void Should_copy_it_to_the_file_path()
            {
                Downloader.Copies.Single().Value.Should().Be("file.exe");
            }

            [Fact]
            public void Should_say_what_is_being_copied()
            {
                VerifyHost($"Copying package\r\n  from '{new Uri("file:///tmp/tools/source.exe").LocalPath}'");
            }

            [Fact]
            public void Should_not_make_a_web_request()
            {
                Downloader.HeaderRequests.Should().BeEmpty();
            }

            [Fact]
            public void Should_not_verify_a_checksum_when_the_package_gives_none()
            {
                Downloader.ChecksumChecks.Should().BeEmpty();
            }
        }

        public class When_the_address_is_a_file_address_and_the_package_gives_a_checksum : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Url = "file:///tmp/tools/source.exe";
                Options.Checksum = "abc123";
                Options.ChecksumType = ChecksumType.Sha256;
            }

            [Fact]
            public void Should_verify_the_checksum_against_the_local_path()
            {
                Downloader.ChecksumChecks.Should().ContainSingle().Which.Should().Equal("file.exe", "abc123", "sha256", new Uri("file:///tmp/tools/source.exe").LocalPath);
            }
        }

        public class When_the_address_is_a_local_path : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Url = "tools/source.exe";
            }

            [Fact]
            public void Should_copy_from_the_path_as_given()
            {
                Downloader.Copies.Should().ContainSingle().Which.Key.Should().Be("tools/source.exe");
            }
        }

        public class When_the_scheme_of_the_address_is_not_supported : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Url = "ftps://example.com/file.exe";
            }

            [Fact]
            public void Should_fail_saying_that_the_scheme_is_not_supported()
            {
                Error.Should().BeOfType<NotSupportedException>().Which.Message.Should().Contain("the scheme 'ftps' is not supported");
            }

            [Fact]
            public void Should_not_do_anything_else()
            {
                Downloader.Calls.Should().BeEmpty();
            }
        }

        public class When_the_package_has_no_address_for_the_architecture : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Url = string.Empty;
                Options.Url64Bit = string.Empty;
            }

            [Fact]
            public void Should_fail_saying_that_the_architecture_is_not_supported()
            {
                Error.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("This package does not support  architecture.");
            }
        }

        public class When_a_32_bit_system_has_only_a_64_bit_address : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Downloader.Is64Bit = false;
                Options.Url = string.Empty;
                Options.Url64Bit = "https://example.com/file64.exe";
            }

            [Fact]
            public void Should_fail_naming_the_architecture()
            {
                Error.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("This package does not support 32 bit architecture.");
            }
        }

        public class When_the_package_has_both_a_32_bit_and_a_64_bit_address : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Url = "https://example.com/file32.exe";
                Options.Url64Bit = "https://example.com/file64.exe";
                Options.Checksum = "c32";
                Options.Checksum64 = "c64";
                Options.ChecksumType = ChecksumType.Md5;
                Options.ChecksumType64 = ChecksumType.Sha256;
            }

            [Fact]
            public void Should_download_the_64_bit_file()
            {
                Downloader.HttpDownloads.Should().Equal("https://example.com/file64.exe");
            }

            [Fact]
            public void Should_say_which_package_is_downloaded()
            {
                VerifyHost("Downloading package 64 bit\r\n  from 'https://example.com/file64.exe'");
            }

            [Fact]
            public void Should_verify_the_64_bit_checksum()
            {
                Downloader.ChecksumChecks.Should().ContainSingle().Which.Should().Equal("file.exe", "c64", "sha256", "https://example.com/file64.exe");
            }

            [Fact]
            public void Should_say_what_it_chose_in_the_debug_output()
            {
                VerifyDebug("CPU is 64 bit");
                VerifyDebug("Setting url to 'https://example.com/file64.exe' and bitPackage to 64");
            }
        }

        public class When_a_32_bit_download_is_forced : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Url = "https://example.com/file32.exe";
                Options.Url64Bit = "https://example.com/file64.exe";
                Environment.SetEnvironmentVariable(Package.ChocolateyForceX86, "true");
            }

            [Fact]
            public void Should_download_the_32_bit_file()
            {
                Downloader.HttpDownloads.Should().Equal("https://example.com/file32.exe");
            }

            [Fact]
            public void Should_say_that_it_is_forcing_32_bit()
            {
                VerifyDebug("User specified -x86 so forcing 32 bit");
            }
        }

        public class When_the_force_x86_variable_is_set_to_something_other_than_true : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Url = "https://example.com/file32.exe";
                Options.Url64Bit = "https://example.com/file64.exe";
                Environment.SetEnvironmentVariable(Package.ChocolateyForceX86, "false");
            }

            [Fact]
            public void Should_download_the_64_bit_file()
            {
                Downloader.HttpDownloads.Should().Equal("https://example.com/file64.exe");
            }
        }

        public class When_the_address_and_checksum_are_overridden_by_environment_variables : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Url = "https://example.com/original.exe";
                Options.Checksum = "original";
                Options.ChecksumType = ChecksumType.Md5;
                Environment.SetEnvironmentVariable(Package.ChocolateyUrlOverride, "https://override.example.com//file.exe");
                Environment.SetEnvironmentVariable(Package.ChocolateyChecksum32, "overridden");
                Environment.SetEnvironmentVariable(Package.ChocolateyChecksumType32, "sha512");
            }

            [Fact]
            public void Should_download_from_the_override_address_with_repeated_slashes_collapsed()
            {
                Downloader.HttpDownloads.Should().Equal("https://override.example.com/file.exe");
            }

            [Fact]
            public void Should_use_the_override_checksum_and_type()
            {
                Downloader.ChecksumChecks.Should().ContainSingle().Which.Should().Equal("file.exe", "overridden", "sha512", "https://override.example.com/file.exe");
            }
        }

        public class When_the_64_bit_address_and_checksum_are_overridden : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Url = "https://example.com/file32.exe";
                Options.Url64Bit = "https://example.com/file64.exe";
                Options.Checksum64 = "original64";
                Options.ChecksumType64 = ChecksumType.Md5;
                Environment.SetEnvironmentVariable(Package.ChocolateyUrl64bitOverride, "https://override.example.com/file64.exe");
                Environment.SetEnvironmentVariable(Package.ChocolateyChecksum64, "overridden64");
                Environment.SetEnvironmentVariable(Package.ChocolateyChecksumType64, "sha256");
            }

            [Fact]
            public void Should_download_the_override_address()
            {
                Downloader.HttpDownloads.Should().Equal("https://override.example.com/file64.exe");
            }

            [Fact]
            public void Should_verify_the_override_checksum()
            {
                Downloader.ChecksumChecks.Should().ContainSingle().Which.Should().Equal("file.exe", "overridden64", "sha256", "https://override.example.com/file64.exe");
            }
        }

        public class When_no_file_exists_after_the_download : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Downloader.LeaveNoFile = true;
            }

            [Fact]
            public void Should_fail_saying_that_the_file_is_missing()
            {
                Error.Should().BeOfType<FileNotFoundException>().Which.Message.Should().Be("Chocolatey expected a file to be downloaded to 'file.exe' but nothing exists at that location.");
            }

            [Fact]
            public void Should_not_check_a_checksum()
            {
                Downloader.ChecksumChecks.Should().BeEmpty();
            }
        }

        public class When_the_folder_for_the_file_cannot_be_created : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Downloader.OnEnsureParentDirectory = path => throw new UnauthorizedAccessException("Access to the path is denied.");
            }

            [Fact]
            public void Should_say_so_and_carry_on()
            {
                VerifyHost("Attempt to create directory failed for 'file.exe'.");
                Error.Should().BeNull();
            }
        }

        public class When_the_original_file_name_is_not_wanted : DownloadSpecsBase
        {
            [Fact]
            public void Should_use_the_file_path_as_given()
            {
                Downloader.Files.Keys.Should().Equal("file.exe");
            }
        }

        public class When_using_the_original_file_name : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.UseOriginalFileName = true;
                Options.FilePath = @"C:\tools\package\default.exe";
                Downloader.RemoteFileName = (url, defaultName) => "installer.msi";
            }

            [Fact]
            [NUnit.Framework.Platform("Win")]
            public void Should_use_the_name_from_the_server_in_the_same_folder()
            {
                Result.Should().Be(@"C:\tools\package\installer.msi");
            }

            [Fact]
            [NUnit.Framework.Platform("Win")]
            public void Should_download_to_that_file()
            {
                Downloader.Files.Keys.Should().Equal(@"C:\tools\package\installer.msi");
            }
        }

        public class When_the_original_file_name_cannot_be_found : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.UseOriginalFileName = true;
                Options.FilePath = @"C:\tools\package\default.exe";
                Downloader.RemoteFileName = (url, defaultName) => throw new InvalidOperationException("No name.");
            }

            [Fact]
            public void Should_say_so_and_keep_the_file_path()
            {
                VerifyHost("Attempt to use original download file name failed for 'https://example.com/file.exe'.");
                Result.Should().Be(@"C:\tools\package\default.exe");
            }
        }

        public class When_the_file_is_in_a_doubled_chocolatey_folder : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.UseOriginalFileName = true;
                Options.FilePath = @"C:\Chocolatey\chocolatey\lib\package\default.exe";
                Downloader.RemoteFileName = (url, defaultName) => defaultName;
            }

            [Fact]
            [NUnit.Framework.Platform("Win")]
            public void Should_remove_the_doubled_folder_when_using_the_original_file_name()
            {
                Result.Should().Be(@"C:\chocolatey\lib\package\default.exe");
            }
        }
    }

    // ReSharper restore InconsistentNaming
}
