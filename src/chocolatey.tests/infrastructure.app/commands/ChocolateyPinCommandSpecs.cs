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
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commandline;
    using chocolatey.infrastructure.results;
    using Moq;
    using NuGet.Common;
    using NuGet.Packaging;
    using NuGet.Versioning;

    using NUnit.Framework;

    using FluentAssertions;

    public class ChocolateyPinCommandSpecs
    {
        [ConcernFor("pin")]
        public abstract class ChocolateyPinCommandSpecsBase : TinySpec
        {
            protected ChocolateyPinCommand command;
            protected Mock<IChocolateyPackageInformationService> packageInfoService = new Mock<IChocolateyPackageInformationService>();
            protected Mock<ILogger> nugetLogger = new Mock<ILogger>();
            protected Mock<INugetService> nugetService = new Mock<INugetService>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();
            protected Mock<IPackageMetadata> package = new Mock<IPackageMetadata>();
            protected Mock<IPackageMetadata> pinnedPackage = new Mock<IPackageMetadata>();

            public override void Context()
            {
                //MockLogger = new MockLogger();
                //Log.InitializeWith(MockLogger);
                configuration.Sources = "https://localhost/somewhere/out/there";
                command = new ChocolateyPinCommand(packageInfoService.Object, nugetLogger.Object, nugetService.Object);

                package = new Mock<IPackageMetadata>();
                package.Setup(p => p.Id).Returns("regular");
                package.Setup(p => p.Version).Returns(new NuGetVersion("1.2.0"));
                packageInfoService.Setup(s => s.Get(package.Object)).Returns(
                    new ChocolateyPackageInformation(package.Object)
                    {
                        IsPinned = false
                    });
                pinnedPackage = new Mock<IPackageMetadata>();
                pinnedPackage.Setup(p => p.Id).Returns("pinned");
                pinnedPackage.Setup(p => p.Version).Returns(new NuGetVersion("1.1.0"));
                packageInfoService.Setup(s => s.Get(pinnedPackage.Object)).Returns(
                    new ChocolateyPackageInformation(pinnedPackage.Object)
                    {
                        IsPinned = true
                    });
            }

            public void Reset()
            {
                packageInfoService.ResetCalls();
                nugetService.ResetCalls();
            }
        }

        public class When_implementing_command_for : ChocolateyPinCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_source()
            {
                results.Should().Contain("pin");
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyPinCommandSpecsBase
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
            public void Should_add_name_to_the_option_set()
            {
                optionSet.Contains("name").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_name_to_the_option_set()
            {
                optionSet.Contains("n").Should().BeTrue();
            }

            [Fact]
            public void Should_add_version_to_the_option_set()
            {
                optionSet.Contains("version").Should().BeTrue();
            }
        }

        public class When_handling_additional_argument_parsing : ChocolateyPinCommandSpecsBase
        {
            private readonly IList<string> unparsedArgs = new List<string>();
            private Action because;

            public override void Because()
            {
                because = () => command.ParseAdditionalArguments(unparsedArgs, configuration);
            }

            public new void Reset()
            {
                unparsedArgs.Clear();
                base.Reset();
            }

            [Fact]
            public void Should_use_the_first_unparsed_arg_as_the_subcommand()
            {
                Reset();
                unparsedArgs.Add("list");
                because();

                configuration.PinCommand.Command.Should().Be(PinCommandType.List);
            }

            [Fact]
            public void Should_throw_when_more_than_one_unparsed_arg_is_passed()
            {
                Reset();
                unparsedArgs.Add("wtf");
                unparsedArgs.Add("bbq");
                var errored = false;
                Exception error = null;

                try
                {
                    because();
                }
                catch (Exception ex)
                {
                    errored = true;
                    error = ex;
                }

                errored.Should().BeTrue();
                error.Should().NotBeNull();
                error.Should().BeOfType<ApplicationException>();
                error.Message.Should().Contain("A single pin command must be listed");
            }

            [Fact]
            public void Should_accept_add_as_the_subcommand()
            {
                Reset();
                unparsedArgs.Add("add");
                because();

                configuration.PinCommand.Command.Should().Be(PinCommandType.Add);
            }

            [Fact]
            public void Should_accept_uppercase_add_as_the_subcommand()
            {
                Reset();
                unparsedArgs.Add("ADD");
                because();

                configuration.PinCommand.Command.Should().Be(PinCommandType.Add);
            }

            [Fact]
            public void Should_remove_add_as_the_subcommand()
            {
                Reset();
                unparsedArgs.Add("remove");
                because();

                configuration.PinCommand.Command.Should().Be(PinCommandType.Remove);
            }

            [Fact]
            public void Should_set_unrecognized_values_to_list_as_the_subcommand()
            {
                Reset();
                unparsedArgs.Add("wtf");
                because();

                configuration.PinCommand.Command.Should().Be(PinCommandType.List);
            }

            [Fact]
            public void Should_default_to_list_as_the_subcommand()
            {
                Reset();
                because();

                configuration.PinCommand.Command.Should().Be(PinCommandType.List);
            }

            [Fact]
            public void Should_handle_passing_in_an_empty_string()
            {
                Reset();
                unparsedArgs.Add(" ");
                because();

                configuration.PinCommand.Command.Should().Be(PinCommandType.List);
            }

            [Fact]
            public void Should_set_config_sources_to_local_only()
            {
                Reset();
                because();

                configuration.Sources.Should().Be(ApplicationParameters.PackagesLocation);
            }

            [Fact]
            public void Should_set_config_local_only_to_true()
            {
                Reset();
                because();

                configuration.ListCommand.LocalOnly.Should().BeTrue();
            }

            [Fact]
            public void Should_set_config_all_versions_to_true()
            {
                Reset();
                because();

                configuration.AllVersions.Should().BeTrue();
            }
        }

        public class When_validating : ChocolateyPinCommandSpecsBase
        {
            private Action because;

            public override void Because()
            {
                because = () => command.Validate(configuration);
            }

            [Fact]
            public void Should_throw_when_command_is_not_list_and_name_is_not_set()
            {
                configuration.PinCommand.Command = PinCommandType.Add;
                configuration.PinCommand.Name = "";
                var errored = false;
                Exception error = null;

                try
                {
                    because();
                }
                catch (Exception ex)
                {
                    errored = true;
                    error = ex;
                }

                errored.Should().BeTrue();
                error.Should().NotBeNull();
                error.Should().BeOfType<ApplicationException>();
                error.Message.Should().Be("When specifying the subcommand '{0}', you must also specify --name.".FormatWith(configuration.PinCommand.Command.ToStringSafe().ToLower()));
            }

            [Fact]
            public void Should_continue_when_command_is_list_and_name_is_not_set()
            {
                configuration.PinCommand.Command = PinCommandType.List;
                configuration.PinCommand.Name = "";
                because();
            }

            [Fact]
            public void Should_continue_when_command_is_not_list_and_name_is_set()
            {
                configuration.PinCommand.Command = PinCommandType.List;
                configuration.PinCommand.Name = "bob";
                because();
            }
        }

        public class When_noop_is_called : ChocolateyPinCommandSpecsBase
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
                var messages = MockLogger.MessagesFor(tests.LogLevel.Info);
                messages.Should().NotBeEmpty()
                    .And.ContainSingle();
                messages[0].Should().Contain("Pin would have called");
            }
        }

        public class When_list_is_called : ChocolateyPinCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                configuration.Sources = ApplicationParameters.PackagesLocation;
                configuration.ListCommand.LocalOnly = true;
                configuration.AllVersions = true;
                var packageResults = new[]
                {
                    new PackageResult(package.Object, null),
                    new PackageResult(pinnedPackage.Object, null)
                };
                nugetService.Setup(n => n.List(It.IsAny<ChocolateyConfiguration>())).Returns(packageResults);
                configuration.PinCommand.Command = PinCommandType.List;
            }

            public override void Because()
            {
                command.Run(configuration);
            }

            [Fact]
            public void Should_list_pinned_packages()
            {
                MockLogger.Verify(l => l.Info("pinned|1.1.0"), Times.Once);
            }

            [Fact]
            public void Should_not_list_unpinned_packages()
            {
                MockLogger.Verify(l => l.Info("regular|1.2.0"), Times.Never);
            }

            [Fact]
            public void Should_log_a_message()
            {
                MockLogger.Verify(l => l.Info(It.IsAny<string>()), Times.Once);
            }

            [Fact]
            public void Should_log_one_message()
            {
                MockLogger.Messages.Should().ContainSingle();
            }
        }

        public class When_run_is_called : ChocolateyPinCommandSpecsBase
        {
            //private Action because;

            public override void Context()
            {
                base.Context();
                configuration.Sources = ApplicationParameters.PackagesLocation;
                configuration.ListCommand.LocalOnly = true;
                configuration.AllVersions = true;

                var packageResults = new[]
                {
                    new PackageResult(package.Object, null),
                    new PackageResult(pinnedPackage.Object, null)
                };
                nugetService.Setup(n => n.List(It.IsAny<ChocolateyConfiguration>())).Returns(packageResults);
            }

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
            public void Should_call_nuget_service_list_run_when_command_is_list()
            {
                Reset();
                configuration.PinCommand.Command = PinCommandType.List;
                command.Run(configuration);

                nugetService.Verify(n => n.List(It.IsAny<ChocolateyConfiguration>()), Times.Once);
            }

            [Fact]
            public void Should_set_pin_when_command_is_add()
            {
                Reset();

                configuration.PinCommand.Name = "regular";
                configuration.PinCommand.Command = PinCommandType.Add;

                command.SetPin(configuration);

                packageInfoService.Verify(s => s.Save(It.IsAny<ChocolateyPackageInformation>()), Times.Once);
            }

            [Fact]
            public void Should_remove_pin_when_command_is_remove()
            {
                Reset();
                configuration.PinCommand.Name = "pinned";
                configuration.PinCommand.Command = PinCommandType.Remove;

                command.SetPin(configuration);

                packageInfoService.Verify(s => s.Save(It.IsAny<ChocolateyPackageInformation>()), Times.Once);
            }
        }

    }
}
