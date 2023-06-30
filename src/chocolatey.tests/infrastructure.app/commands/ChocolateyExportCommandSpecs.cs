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
            protected ChocolateyExportCommand Command;
            protected Mock<INugetService> NugetService = new Mock<INugetService>();
            protected Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                Command = new ChocolateyExportCommand(NugetService.Object, FileSystem.Object);
            }

            public void Reset()
            {
                NugetService.ResetCalls();
                FileSystem.ResetCalls();
            }
        }

        public class When_implementing_command_for : ChocolateyExportCommandSpecsBase
        {
            private List<string> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_help()
            {
                _results.Should().Contain("export");
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyExportCommandSpecsBase
        {
            private OptionSet _optionSet;

            public override void Context()
            {
                base.Context();
                _optionSet = new OptionSet();
            }

            public override void Because()
            {
                Command.ConfigureArgumentParser(_optionSet, Configuration);
            }

            [Fact]
            public void Should_add_output_file_path_to_the_option_set()
            {
                _optionSet.Contains("output-file-path").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_output_file_path_to_the_option_set()
            {
                _optionSet.Contains("o").Should().BeTrue();
            }

            [Fact]
            public void Should_add_include_version_numbers_to_the_option_set()
            {
                _optionSet.Contains("include-version-numbers").Should().BeTrue();
            }

            [Fact]
            public void Should_add_include_version_to_the_option_set()
            {
                _optionSet.Contains("include-version").Should().BeTrue();
            }
        }

        public class When_handling_additional_argument_parsing : ChocolateyExportCommandSpecsBase
        {
            private readonly IList<string> _unparsedArgs = new List<string>();
            private Action _because;

            public override void Because()
            {
                _because = () => Command.ParseAdditionalArguments(_unparsedArgs, Configuration);
            }

            public new void Reset()
            {
                Configuration.ExportCommand.OutputFilePath = string.Empty;
                _unparsedArgs.Clear();
                base.Reset();
            }

            [Fact]
            public void Should_handle_passing_in_an_empty_string_for_output_file_path()
            {
                Reset();
                _unparsedArgs.Add(" ");
                _because();

                Configuration.ExportCommand.OutputFilePath.Should().Be("packages.config");
            }

            [Fact]
            public void Should_handle_passing_in_a_string_for_output_file_path()
            {
                Reset();
                _unparsedArgs.Add("custompackages.config");
                _because();

                Configuration.ExportCommand.OutputFilePath.Should().Be("custompackages.config");
            }
        }

        public class When_noop_is_called : ChocolateyExportCommandSpecsBase
        {
            public override void Because()
            {
                Command.DryRun(Configuration);
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
                messages.Should().ContainSingle();
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
                Command.Run(Configuration);

                NugetService.Verify(n => n.GetInstalledPackages(It.IsAny<ChocolateyConfiguration>()), Times.Once);
            }

            [Fact]
            public void Should_call_replace_file_when_file_already_exists()
            {
                FileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

                Reset();
                Command.Run(Configuration);

                FileSystem.Verify(n => n.ReplaceFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }

            [Fact]
            public void Should_not_call_replace_file_when_file_doesnt_exist()
            {
                FileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

                Reset();
                Command.Run(Configuration);

                FileSystem.Verify(n => n.ReplaceFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            }
        }
    }
}
