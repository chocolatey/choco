// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

namespace chocolatey.tests.infrastructure.app.commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commandline;
    using chocolatey.infrastructure.filesystem;
    using Moq;
    using FluentAssertions;

    public class ChocolateyExportCommandSpecs
    {
        [ConcernFor("export")]
        public abstract class ChocolateyExportCommandSpecsBase : TinySpec
        {
            protected ChocolateyExportCommand command;
            protected Mock<INugetService> nugetService = new Mock<INugetService>();
            protected Mock<IFileSystem> fileSystem = new Mock<IFileSystem>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                command = new ChocolateyExportCommand(nugetService.Object, fileSystem.Object);
            }

            public void Reset()
            {
                nugetService.ResetCalls();
                fileSystem.ResetCalls();
            }
        }

        public class When_implementing_command_for : ChocolateyExportCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_help()
            {
                results.Should().Contain("export");
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyExportCommandSpecsBase
        {
            private OptionSet optionSet;

            public override void Context()
            {
                base.Context();
                optionSet = new OptionSet();
            }

            public override void Because()
            {
                command.ConfigureArgumentParser(optionSet, configuration);
            }

            [Fact]
            public void Should_add_output_file_path_to_the_option_set()
            {
                optionSet.Contains("output-file-path").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_output_file_path_to_the_option_set()
            {
                optionSet.Contains("o").Should().BeTrue();
            }

            [Fact]
            public void Should_add_include_version_numbers_to_the_option_set()
            {
                optionSet.Contains("include-version-numbers").Should().BeTrue();
            }

            [Fact]
            public void Should_add_include_version_to_the_option_set()
            {
                optionSet.Contains("include-version").Should().BeTrue();
            }
        }

        public class When_handling_additional_argument_parsing : ChocolateyExportCommandSpecsBase
        {
            private readonly IList<string> unparsedArgs = new List<string>();
            private Action because;

            public override void Because()
            {
                because = () => command.ParseAdditionalArguments(unparsedArgs, configuration);
            }

            public new void Reset()
            {
                configuration.ExportCommand.OutputFilePath = string.Empty;
                unparsedArgs.Clear();
                base.Reset();
            }

            [Fact]
            public void Should_handle_passing_in_an_empty_string_for_output_file_path()
            {
                Reset();
                unparsedArgs.Add(" ");
                because();

                configuration.ExportCommand.OutputFilePath.Should().Be("packages.config");
            }

            [Fact]
            public void Should_handle_passing_in_a_string_for_output_file_path()
            {
                Reset();
                unparsedArgs.Add("custompackages.config");
                because();

                configuration.ExportCommand.OutputFilePath.Should().Be("custompackages.config");
            }
        }

        public class When_noop_is_called : ChocolateyExportCommandSpecsBase
        {
            public override void Because()
            {
                command.DryRun(configuration);
            }

            [Fact]
            public void Should_log_a_message()
            {
                MockLogger.Verify(l => l.Info(It.IsAny<string>()), Times.AtLeastOnce);
            }

            [Fact]
            public void Should_log_the_message_we_expect()
            {
                var messages = MockLogger.MessagesFor(LogLevel.Info);
                messages.Should().NotBeEmpty();
                messages.Count.Should().Be(1);
                messages[0].Should().Contain("Export would have been with options");
            }
        }

        public class When_run_is_called : ChocolateyExportCommandSpecsBase
        {
            public new void Reset()
            {
                Context();
                base.Reset();
            }

            public override void AfterEachSpec()
            {
                base.AfterEachSpec();
                MockLogger.Messages.Clear();
            }

            public override void Because()
            {
                // because = () => command.run(configuration);
            }

            [Fact]
            public void Should_call_nuget_service_get_all_installed_packages()
            {
                Reset();
                command.Run(configuration);

                nugetService.Verify(n => n.GetInstalledPackages(It.IsAny<ChocolateyConfiguration>()), Times.Once);
            }

            [Fact]
            public void Should_call_replace_file_when_file_already_exists()
            {
                fileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

                Reset();
                command.Run(configuration);

                fileSystem.Verify(n => n.ReplaceFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }

            [Fact]
            public void Should_not_call_replace_file_when_file_doesnt_exist()
            {
                fileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

                Reset();
                command.Run(configuration);

                fileSystem.Verify(n => n.ReplaceFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            }
        }
    }
}
