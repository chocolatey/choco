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

namespace chocolatey.tests.infrastructure.app.nuget
{
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.nuget;
    using Moq;
    using NuGet.Common;
    using NuGet.ProjectManagement;
    using NUnit.Framework;
    using Should;

    public class ChocolateyNuGetProjectContextSpecs
    {
        public abstract class ChocolateyNuGetProjectContextSpecsBase : TinySpec
        {
            protected ChocolateyConfiguration Configuration;
            protected Mock<ILogger> Logger = new Mock<ILogger>();
            protected ChocolateyNuGetProjectContext Service;

            public override void Context()
            {
                Configuration = new ChocolateyConfiguration();
                Service = new ChocolateyNuGetProjectContext(Configuration, Logger.Object);
            }
        }

        [Categories.Logging, Parallelizable(ParallelScope.Self)]
        public class when_calling_logging_methods_the_passed_in_logger_is_used : ChocolateyNuGetProjectContextSpecsBase
        {
            public override void Because()
            { }

            public override void BeforeEachSpec()
            {
                base.BeforeEachSpec();

                Logger.ResetCalls();
            }

            [Fact]
            public void should_log_debug_information_in_child_logger()
            {
                Service.Log(MessageLevel.Debug, "Some {0} message", "DEBUG");

                Logger.Verify(l => l.LogDebug("Some DEBUG message"), Times.Once);

                // TODO: Uncomment once Moq is upgrade to v4.8 or later.
                //Logger.VerifyNoOtherCalls();
            }

            [Fact]
            public void should_log_error_information_in_child_logger()
            {
                Service.Log(MessageLevel.Error, "Some {0} message", "ERROR");

                Logger.Verify(l => l.LogError("Some ERROR message"), Times.Once);

                // TODO: Uncomment once Moq is upgrade to v4.8 or later.
                //Logger.VerifyNoOtherCalls();
            }

            [Fact]
            public void should_log_info_information_in_child_logger()
            {
                Service.Log(MessageLevel.Info, "Some {0} message", "INFO");

                Logger.Verify(l => l.LogInformation("Some INFO message"), Times.Once);

                // TODO: Uncomment once Moq is upgrade to v4.8 or later.
                //Logger.VerifyNoOtherCalls();
            }

            [TestCase(LogLevel.Debug)]
            public void should_log_to_child_logger_and_pass_along_original_message(LogLevel logLevel)
            {
                var logMessage = new LogMessage(logLevel, "My awesome message");

                Service.Log(logMessage);

                Logger.Verify(l => l.Log(logMessage), Times.Once);

                // TODO: Uncomment once Moq is upgrade to v4.8 or later.
                //Logger.VerifyNoOtherCalls();
            }

            [Fact]
            public void should_log_warning_information_in_child_logger()
            {
                Service.Log(MessageLevel.Warning, "Some {0} message", "WARNING");

                Logger.Verify(l => l.LogWarning("Some WARNING message"), Times.Once);

                // TODO: Uncomment once Moq is upgrade to v4.8 or later.
                //Logger.VerifyNoOtherCalls();
            }

            [Fact]
            public void should_report_errors_to_child_logger()
            {
                Service.ReportError("Some kind of error!");

                Logger.Verify(l => l.LogError("Some kind of error!"), Times.Once);

                // TODO: Uncomment once Moq is upgrade to v4.8 or later.
                //Logger.VerifyNoOtherCalls();
            }

            [Fact]
            public void should_report_errors_with_message_to_child_logger()
            {
                var logMessage = new LogMessage(LogLevel.Debug, "Some message");

                Service.ReportError(logMessage);

                Logger.Verify(l => l.Log(logMessage), Times.Once);

                // TODO: Uncomment once Moq is upgrade to v4.8 or later.
                //Logger.VerifyNoOtherCalls();
            }

            [Fact]
            public void should_report_warning_when_resolving_file_conflicts()
            {
                var message = "Some kind of message";

                var result = Service.ResolveFileConflict(message);

                result.ShouldEqual(FileConflictAction.OverwriteAll);

                Logger.Verify(l => l.LogWarning("File conflict, overwriting all: Some kind of message"), Times.Once);

                // TODO: Uncomment once Moq is upgrade to v4.8 or later.
                //Logger.VerifyNoOtherCalls();
            }
        }
    }
}
