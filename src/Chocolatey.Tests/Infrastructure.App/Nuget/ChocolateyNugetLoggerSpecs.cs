// Copyright © 2022-Present Chocolatey Software, Inc
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

namespace Chocolatey.Tests.Infrastructure.App.Nuget
{
    using System;
    using System.Threading.Tasks;
    using Chocolatey.Infrastructure.App.Nuget;
    using global::NuGet.Common;
    using NUnit.Framework;
    using Should;

    using LogLevel = Chocolatey.Tests.LogLevel;
    using NuGetLogLevel = global::NuGet.Common.LogLevel;

    public class ChocolateyNugetLoggerSpecs
    {
        [Categories.Logging]
        public class When_calling_log_level_methods_should_log_with_appropriate_log_type : TinySpec
        {
            private ILogger _logger;

            public override void Because()
            {
            }

            public override void BeforeEachSpec()
            {
                base.BeforeEachSpec();
                MockLogger.Reset();
            }

            public override void Context()
            {
                _logger = new ChocolateyNugetLogger();
            }

            [Fact]
            public void Should_log_debug_level_with_nuget_prefix_on_all_lines_when_calling_LogDebug()
            {
                const string testMessage = "This should be a debug message.\r\nThis is the second line after CRLF line ending.\nThis is the third line after LF line ending.";
                var expectedMessage = "[NuGet] This should be a debug message.{0}[NuGet] This is the second line after CRLF line ending.{0}[NuGet] This is the third line after LF line ending.".FormatWith(Environment.NewLine);

                _logger.LogDebug(testMessage);

                var loggerName = LogLevel.Debug.ToStringChecked();
                MockLogger.LoggerNames.Count.ShouldEqual(1);
                MockLogger.LoggerNames.ShouldContain(typeof(ChocolateyNugetLogger).FullName);
                MockLogger.Messages.Keys.ShouldContain(loggerName);
                MockLogger.Messages[loggerName].ShouldContain(expectedMessage);
            }

            [Fact]
            public void Should_log_debug_level_with_nuget_prefix_when_calling_LogDebug()
            {
                const string testMessage = "This should be a debug message";
                const string expectedMessage = "[NuGet] " + testMessage;

                _logger.LogDebug(testMessage);

                var loggerName = LogLevel.Debug.ToStringChecked();
                MockLogger.LoggerNames.Count.ShouldEqual(1);
                MockLogger.LoggerNames.ShouldContain(typeof(ChocolateyNugetLogger).FullName);
                MockLogger.Messages.Keys.ShouldContain(loggerName);
                MockLogger.Messages[loggerName].ShouldContain(expectedMessage);
            }

            [Fact]
            public void Should_log_error_level_with_nuget_prefix_on_all_lines_when_calling_LogError()
            {
                const string testMessage = "This should be a error message.\r\nThis is the second line after CRLF line ending.\nThis is the third line after LF line ending.";
                var expectedMessage = "[NuGet] This should be a error message.{0}[NuGet] This is the second line after CRLF line ending.{0}[NuGet] This is the third line after LF line ending.".FormatWith(Environment.NewLine);

                _logger.LogError(testMessage);

                var loggerName = LogLevel.Error.ToStringChecked();
                MockLogger.LoggerNames.Count.ShouldEqual(1);
                MockLogger.LoggerNames.ShouldContain(typeof(ChocolateyNugetLogger).FullName);
                MockLogger.Messages.Keys.ShouldContain(loggerName);
                MockLogger.Messages[loggerName].ShouldContain(expectedMessage);
            }

            [Fact]
            public void Should_log_error_level_with_nuget_prefix_when_calling_LogError()
            {
                const string testMessage = "This should be a error message";
                const string expectedMessage = "[NuGet] " + testMessage;

                _logger.LogError(testMessage);

                var loggerName = LogLevel.Error.ToStringChecked();
                MockLogger.LoggerNames.Count.ShouldEqual(1);
                MockLogger.LoggerNames.ShouldContain(typeof(ChocolateyNugetLogger).FullName);
                MockLogger.Messages.Keys.ShouldContain(loggerName);
                MockLogger.Messages[loggerName].ShouldContain(expectedMessage);
            }

            [TestCase(NuGetLogLevel.Debug, LogLevel.Debug, "Test debug message", "[NuGet] Test debug message")]
            [TestCase(NuGetLogLevel.Error, LogLevel.Error, "Test error message", "[NuGet] Test error message")]
            [TestCase(NuGetLogLevel.Minimal, LogLevel.Info, "Test informational message", "[NuGet] Test informational message")]
            [TestCase(NuGetLogLevel.Warning, LogLevel.Warn, "Test warning message", "[NuGet] Test warning message")]
            public void Should_log_expected_log_level_when_calling_Log_with_log_message(NuGetLogLevel nugetLogLevel, LogLevel logLevel, string testMessage, string expectedMessage)
            {
                _logger.Log(new LogMessage(nugetLogLevel, testMessage));
                MockLogger.LoggerNames.Count.ShouldEqual(1);
                MockLogger.LoggerNames.ShouldContain(typeof(ChocolateyNugetLogger).FullName);
                MockLogger.Messages.Keys.ShouldContain(logLevel.ToStringChecked());
                MockLogger.Messages[logLevel.ToStringChecked()].ShouldContain(expectedMessage);
            }

            [TestCase(NuGetLogLevel.Debug, LogLevel.Debug, "Test debug message", "[NuGet] Test debug message")]
            [TestCase(NuGetLogLevel.Error, LogLevel.Error, "Test error message", "[NuGet] Test error message")]
            [TestCase(NuGetLogLevel.Minimal, LogLevel.Info, "Test informational message", "[NuGet] Test informational message")]
            [TestCase(NuGetLogLevel.Warning, LogLevel.Warn, "Test warning message", "[NuGet] Test warning message")]
            public void Should_log_expected_log_level_when_calling_Log_with_nuget_log_level(NuGetLogLevel nugetLogLevel, LogLevel logLevel, string testMessage, string expectedMessage)
            {
                _logger.Log(nugetLogLevel, testMessage);
                MockLogger.LoggerNames.Count.ShouldEqual(1);
                MockLogger.LoggerNames.ShouldContain(typeof(ChocolateyNugetLogger).FullName);
                MockLogger.Messages.Keys.ShouldContain(logLevel.ToStringChecked());
                MockLogger.Messages[logLevel.ToStringChecked()].ShouldContain(expectedMessage);
            }

            [TestCase(NuGetLogLevel.Debug, LogLevel.Debug, "Test debug message", "[NuGet] Test debug message")]
            [TestCase(NuGetLogLevel.Error, LogLevel.Error, "Test error message", "[NuGet] Test error message")]
            [TestCase(NuGetLogLevel.Minimal, LogLevel.Info, "Test informational message", "[NuGet] Test informational message")]
            [TestCase(NuGetLogLevel.Warning, LogLevel.Warn, "Test warning message", "[NuGet] Test warning message")]
            public async Task Should_log_expected_log_level_when_calling_LogAsync_with_nuget_log_level(NuGetLogLevel nugetLogLevel, LogLevel logLevel, string testMessage, string expectedMessage)
            {
                await _logger.LogAsync(nugetLogLevel, testMessage);
                MockLogger.LoggerNames.Count.ShouldEqual(1);
                MockLogger.LoggerNames.ShouldContain(typeof(ChocolateyNugetLogger).FullName);
                MockLogger.Messages.Keys.ShouldContain(logLevel.ToStringChecked());
                MockLogger.Messages[logLevel.ToStringChecked()].ShouldContain(expectedMessage);
            }

            [TestCase(NuGetLogLevel.Debug, LogLevel.Debug, "Test debug message", "[NuGet] Test debug message")]
            [TestCase(NuGetLogLevel.Error, LogLevel.Error, "Test error message", "[NuGet] Test error message")]
            [TestCase(NuGetLogLevel.Minimal, LogLevel.Info, "Test informational message", "[NuGet] Test informational message")]
            [TestCase(NuGetLogLevel.Warning, LogLevel.Warn, "Test warning message", "[NuGet] Test warning message")]
            public async Task Should_log_expected_log_level_when_calling_LogAsync_with_nuget_log_message(NuGetLogLevel nugetLogLevel, LogLevel logLevel, string testMessage, string expectedMessage)
            {
                await _logger.LogAsync(new LogMessage(nugetLogLevel, testMessage));
                MockLogger.LoggerNames.Count.ShouldEqual(1);
                MockLogger.LoggerNames.ShouldContain(typeof(ChocolateyNugetLogger).FullName);
                MockLogger.Messages.Keys.ShouldContain(logLevel.ToStringChecked());
                MockLogger.Messages[logLevel.ToStringChecked()].ShouldContain(expectedMessage);
            }

            [Fact]
            public void Should_log_info_level_with_nuget_prefix_on_all_lines_when_calling_LogInformationSummary()
            {
                const string testMessage = "This should be a error message.\r\nThis is the second line after CRLF line ending.\nThis is the third line after LF line ending.";
                var expectedMessage = "[NuGet] This should be a error message.{0}[NuGet] This is the second line after CRLF line ending.{0}[NuGet] This is the third line after LF line ending.".FormatWith(Environment.NewLine);

                _logger.LogInformationSummary(testMessage);

                var loggerName = LogLevel.Info.ToStringChecked();
                MockLogger.LoggerNames.Count.ShouldEqual(1);
                MockLogger.LoggerNames.ShouldContain(typeof(ChocolateyNugetLogger).FullName);
                MockLogger.Messages.Keys.ShouldContain(loggerName);
                MockLogger.Messages[loggerName].ShouldContain(expectedMessage);
            }

            [Fact]
            public void Should_log_info_level_with_nuget_prefix_when_calling_LogInformationSummary()
            {
                const string testMessage = "This should be a informational message";
                const string expectedMessage = "[NuGet] " + testMessage;

                _logger.LogInformationSummary(testMessage);

                var loggerName = LogLevel.Info.ToStringChecked();
                MockLogger.LoggerNames.Count.ShouldEqual(1);
                MockLogger.LoggerNames.ShouldContain(typeof(ChocolateyNugetLogger).FullName);
                MockLogger.Messages.Keys.ShouldContain(loggerName);
                MockLogger.Messages[loggerName].ShouldContain(expectedMessage);
            }

            [TestCase(NuGetLogLevel.Verbose, "Test verbose message", "[NuGet] Test verbose message")]
            [TestCase(NuGetLogLevel.Information, "Test informational verbose message", "[NuGet] Test informational verbose message")]
            public void Should_log_verbose_level_when_calling_Log_with_nuget_log_level(NuGetLogLevel nuGetLogLevel, string testMessage, string expectedMessage)
            {
                _logger.Log(nuGetLogLevel, testMessage);
                MockLogger.LoggerNames.Count.ShouldEqual(2);
                MockLogger.LoggerNames.ShouldContain("Verbose");
                MockLogger.Messages.Keys.ShouldContain("Info");
                MockLogger.Messages["Info"].ShouldContain(expectedMessage);
            }

            [TestCase(NuGetLogLevel.Verbose, "Test verbose message", "[NuGet] Test verbose message")]
            [TestCase(NuGetLogLevel.Information, "Test informational verbose message", "[NuGet] Test informational verbose message")]
            public void Should_log_verbose_level_when_calling_Log_with_nuget_log_message(NuGetLogLevel nuGetLogLevel, string testMessage, string expectedMessage)
            {
                _logger.Log(new LogMessage(nuGetLogLevel, testMessage));
                MockLogger.LoggerNames.Count.ShouldEqual(2);
                MockLogger.LoggerNames.ShouldContain("Verbose");
                MockLogger.Messages.Keys.ShouldContain("Info");
                MockLogger.Messages["Info"].ShouldContain(expectedMessage);
            }

            [TestCase(NuGetLogLevel.Verbose, "Test verbose message", "[NuGet] Test verbose message")]
            [TestCase(NuGetLogLevel.Information, "Test informational verbose message", "[NuGet] Test informational verbose message")]
            public async Task Should_log_verbose_level_when_calling_LogAsync_with_nuget_log_level(NuGetLogLevel nuGetLogLevel, string testMessage, string expectedMessage)
            {
                await _logger.LogAsync(nuGetLogLevel, testMessage);
                MockLogger.LoggerNames.Count.ShouldEqual(2);
                MockLogger.LoggerNames.ShouldContain("Verbose");
                MockLogger.Messages.Keys.ShouldContain("Info");
                MockLogger.Messages["Info"].ShouldContain(expectedMessage);
            }

            [TestCase(NuGetLogLevel.Verbose, "Test verbose message", "[NuGet] Test verbose message")]
            [TestCase(NuGetLogLevel.Information, "Test informational verbose message", "[NuGet] Test informational verbose message")]
            public async Task Should_log_verbose_level_when_calling_LogAsync_with_nuget_log_message(NuGetLogLevel nuGetLogLevel, string testMessage, string expectedMessage)
            {
                await _logger.LogAsync(new LogMessage(nuGetLogLevel, testMessage));
                MockLogger.LoggerNames.Count.ShouldEqual(2);
                MockLogger.LoggerNames.ShouldContain("Verbose");
                MockLogger.Messages.Keys.ShouldContain("Info");
                MockLogger.Messages["Info"].ShouldContain(expectedMessage);
            }

            [Fact]
            public void Should_log_verbose_level_with_nuget_prefix_on_all_lines_when_calling_LogInformation()
            {
                const string testMessage = "This should be a informational verbose message.\r\nThis is the second line after CRLF line ending.\nThis is the third line after LF line ending.";
                var expectedMessage = "[NuGet] This should be a informational verbose message.{0}[NuGet] This is the second line after CRLF line ending.{0}[NuGet] This is the third line after LF line ending.".FormatWith(Environment.NewLine);

                _logger.LogInformation(testMessage);

                var loggerName = LogLevel.Info.ToStringChecked();
                MockLogger.LoggerNames.Count.ShouldEqual(2);
                MockLogger.LoggerNames.ShouldContain("Verbose");
                MockLogger.Messages.Keys.ShouldContain(loggerName);
                MockLogger.Messages[loggerName].ShouldContain(expectedMessage);
            }

            [Fact]
            public void Should_log_verbose_level_with_nuget_prefix_on_all_lines_when_calling_LogMinimal()
            {
                const string testMessage = "This should be a error message.\r\nThis is the second line after CRLF line ending.\nThis is the third line after LF line ending.";
                var expectedMessage = "[NuGet] This should be a error message.{0}[NuGet] This is the second line after CRLF line ending.{0}[NuGet] This is the third line after LF line ending.".FormatWith(Environment.NewLine);

                _logger.LogMinimal(testMessage);

                var loggerName = LogLevel.Info.ToStringChecked();
                MockLogger.LoggerNames.Count.ShouldEqual(2);
                MockLogger.LoggerNames.ShouldContain("Verbose");
                MockLogger.Messages.Keys.ShouldContain(loggerName);
                MockLogger.Messages[loggerName].ShouldContain(expectedMessage);
            }

            [Fact]
            public void Should_log_verbose_level_with_nuget_prefix_on_all_lines_when_calling_LogVerbose()
            {
                const string testMessage = "This should be a verbose message.\r\nThis is the second line after CRLF line ending.\nThis is the third line after LF line ending.";
                var expectedMessage = "[NuGet] This should be a verbose message.{0}[NuGet] This is the second line after CRLF line ending.{0}[NuGet] This is the third line after LF line ending.".FormatWith(Environment.NewLine);

                _logger.LogVerbose(testMessage);

                var loggerName = LogLevel.Info.ToStringChecked();
                MockLogger.LoggerNames.Count.ShouldEqual(2);
                MockLogger.LoggerNames.ShouldContain("Verbose");
                MockLogger.Messages.Keys.ShouldContain(loggerName);
                MockLogger.Messages[loggerName].ShouldContain(expectedMessage);
            }

            [Fact]
            public void Should_log_verbose_level_with_nuget_prefix_when_calling_LogInformation()
            {
                const string testMessage = "This should be a informational verbose message";
                const string expectedMessage = "[NuGet] " + testMessage;

                _logger.LogInformation(testMessage);

                var loggerName = LogLevel.Info.ToStringChecked();
                MockLogger.LoggerNames.Count.ShouldEqual(2);
                MockLogger.LoggerNames.ShouldContain("Verbose");
                MockLogger.Messages.Keys.ShouldContain(loggerName);
                MockLogger.Messages[loggerName].ShouldContain(expectedMessage);
            }

            [Fact]
            public void Should_log_verbose_level_with_nuget_prefix_when_calling_LogMinimal()
            {
                const string testMessage = "This should be a informational message";
                const string expectedMessage = "[NuGet] " + testMessage;

                _logger.LogMinimal(testMessage);

                var loggerName = LogLevel.Info.ToStringChecked();
                MockLogger.LoggerNames.Count.ShouldEqual(2);
                MockLogger.LoggerNames.ShouldContain("Verbose");
                MockLogger.Messages.Keys.ShouldContain(loggerName);
                MockLogger.Messages[loggerName].ShouldContain(expectedMessage);
            }

            [Fact]
            public void Should_log_verbose_level_with_nuget_prefix_when_calling_LogVerbose()
            {
                const string testMessage = "This should be a verbose message";
                const string expectedMessage = "[NuGet] " + testMessage;

                _logger.LogVerbose(testMessage);

                var loggerName = LogLevel.Info.ToStringChecked();
                MockLogger.LoggerNames.Count.ShouldEqual(2);
                MockLogger.LoggerNames.ShouldContain("Verbose");
                MockLogger.Messages.Keys.ShouldContain(loggerName);
                MockLogger.Messages[loggerName].ShouldContain(expectedMessage);
            }

            [Fact]
            public void Should_log_warn_level_with_nuget_prefix_on_all_lines_when_calling_LogWarning()
            {
                const string testMessage = "This should be a warning message.\r\nThis is the second line after CRLF line ending.\nThis is the third line after LF line ending.";
                var expectedMessage = "[NuGet] This should be a warning message.{0}[NuGet] This is the second line after CRLF line ending.{0}[NuGet] This is the third line after LF line ending.".FormatWith(Environment.NewLine);

                _logger.LogWarning(testMessage);

                var loggerName = LogLevel.Warn.ToStringChecked();
                MockLogger.LoggerNames.Count.ShouldEqual(1);
                MockLogger.LoggerNames.ShouldContain(typeof(ChocolateyNugetLogger).FullName);
                MockLogger.Messages.Keys.ShouldContain(loggerName);
                MockLogger.Messages[loggerName].ShouldContain(expectedMessage);
            }

            [Fact]
            public void Should_log_warn_level_with_nuget_prefix_when_calling_LogWarning()
            {
                const string testMessage = "This should be a warning message";
                const string expectedMessage = "[NuGet] " + testMessage;

                _logger.LogWarning(testMessage);

                var loggerName = LogLevel.Warn.ToStringChecked();
                MockLogger.LoggerNames.Count.ShouldEqual(1);
                MockLogger.LoggerNames.ShouldContain(typeof(ChocolateyNugetLogger).FullName);
                MockLogger.Messages.Keys.ShouldContain(loggerName);
                MockLogger.Messages[loggerName].ShouldContain(expectedMessage);
            }

            [TestCase("")]
            [TestCase("       ")]
            public void Should_not_output_whitespace_only_line_in_multiline_logging(string testType)
            {
                var testValue = "I will be containing\n{0}\nsome whitespace".FormatWith(testType);
                var expectedValue = "[NuGet] I will be containing{0}[NuGet]{0}[NuGet] some whitespace".FormatWith(Environment.NewLine);

                _logger.Log(NuGetLogLevel.Minimal, testValue);
                MockLogger.Messages.Keys.ShouldContain("Info");
                MockLogger.Messages["Info"].ShouldContain(expectedValue);
            }

            [TestCase(null)]
            [TestCase("")]
            [TestCase("    ")]
            public void Should_only_output_prefix_for_null_or_empty_values(string testValue)
            {
                _logger.Log(NuGetLogLevel.Minimal, testValue);

                MockLogger.Messages.Keys.ShouldContain("Info");
                MockLogger.Messages["Info"].ShouldContain("[NuGet]");
            }

            [TestCase("\n\n\n\n\n")]
            [TestCase("\r\n\r\n\r\n\r\n\r\n")]
            public void Should_only_output_prefixes_on_every_line(string testValue)
            {
                var expectedValue = "[NuGet]{0}[NuGet]{0}[NuGet]{0}[NuGet]{0}[NuGet]".FormatWith(Environment.NewLine);

                _logger.Log(NuGetLogLevel.Information, testValue);
                MockLogger.Messages.Keys.ShouldContain("Info");
                MockLogger.Messages["Info"].ShouldContain(expectedValue);
            }
        }
    }
}
