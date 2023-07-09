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
            protected ChocolateyPinCommand Command;
            protected Mock<IChocolateyPackageInformationService> PackageInfoService = new Mock<IChocolateyPackageInformationService>();
            protected Mock<ILogger> NugetLogger = new Mock<ILogger>();
            protected Mock<INugetService> NugetService = new Mock<INugetService>();
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();
            protected Mock<IPackageMetadata> Package = new Mock<IPackageMetadata>();
            protected Mock<IPackageMetadata> PinnedPackage = new Mock<IPackageMetadata>();
            protected Mock<IPackageMetadata> MingwPackage = new Mock<IPackageMetadata>();
            protected Mock<IPackageMetadata> GstreamerMingwPackage = new Mock<IPackageMetadata>();

            public override void Context()
            {
                //MockLogger = new MockLogger();
                //Log.InitializeWith(MockLogger);
                Configuration.Sources = "https://localhost/somewhere/out/there";
                Command = new ChocolateyPinCommand(PackageInfoService.Object, NugetLogger.Object, NugetService.Object);

                Package = new Mock<IPackageMetadata>();
                Package.Setup(p => p.Id).Returns("regular");
                Package.Setup(p => p.Version).Returns(new NuGetVersion("1.2.0"));
                PackageInfoService.Setup(s => s.Get(Package.Object)).Returns(
                    new ChocolateyPackageInformation(Package.Object)
                    {
                        IsPinned = false
                    });
                PinnedPackage = new Mock<IPackageMetadata>();
                PinnedPackage.Setup(p => p.Id).Returns("pinned");
                PinnedPackage.Setup(p => p.Version).Returns(new NuGetVersion("1.1.0"));
                PackageInfoService.Setup(s => s.Get(PinnedPackage.Object)).Returns(
                    new ChocolateyPackageInformation(PinnedPackage.Object)
                    {
                        IsPinned = true
                    });

                MingwPackage = new Mock<IPackageMetadata>();
                MingwPackage.Setup(p => p.Id).Returns("mingw");
                MingwPackage.Setup(p => p.Version).Returns(new NuGetVersion("1.0.0"));
                PackageInfoService.Setup(s => s.Get(MingwPackage.Object)).Returns(
                    new ChocolateyPackageInformation(MingwPackage.Object)
                    {
                        IsPinned = true
                    });
                GstreamerMingwPackage = new Mock<IPackageMetadata>();
                GstreamerMingwPackage.Setup(p => p.Id).Returns("gstreamer-mingw");
                GstreamerMingwPackage.Setup(p => p.Version).Returns(new NuGetVersion("1.0.0"));
                PackageInfoService.Setup(s => s.Get(GstreamerMingwPackage.Object)).Returns(
                    new ChocolateyPackageInformation(GstreamerMingwPackage.Object)
                    {
                        IsPinned = true
                    });
            }

            public void Reset()
            {
                PackageInfoService.ResetCalls();
                NugetService.ResetCalls();
            }
        }

        public class When_implementing_command_for : ChocolateyPinCommandSpecsBase
        {
            private List<string> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_source()
            {
                _results.Should().Contain("pin");
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyPinCommandSpecsBase
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
            public void Should_add_name_to_the_option_set()
            {
                _optionSet.Contains("name").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_name_to_the_option_set()
            {
                _optionSet.Contains("n").Should().BeTrue();
            }

            [Fact]
            public void Should_add_version_to_the_option_set()
            {
                _optionSet.Contains("version").Should().BeTrue();
            }
        }

        public class When_handling_additional_argument_parsing : ChocolateyPinCommandSpecsBase
        {
            private readonly IList<string> _unparsedArgs = new List<string>();
            private Action _because;

            public override void Because()
            {
                _because = () => Command.ParseAdditionalArguments(_unparsedArgs, Configuration);
            }

            public new void Reset()
            {
                _unparsedArgs.Clear();
                base.Reset();
            }

            [Fact]
            public void Should_use_the_first_unparsed_arg_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("list");
                _because();

                Configuration.PinCommand.Command.Should().Be(PinCommandType.List);
            }

            [Fact]
            public void Should_throw_when_more_than_one_unparsed_arg_is_passed()
            {
                Reset();
                _unparsedArgs.Add("wtf");
                _unparsedArgs.Add("bbq");
                var errored = false;
                Exception error = null;

                try
                {
                    _because();
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
                _unparsedArgs.Add("add");
                _because();

                Configuration.PinCommand.Command.Should().Be(PinCommandType.Add);
            }

            [Fact]
            public void Should_accept_uppercase_add_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("ADD");
                _because();

                Configuration.PinCommand.Command.Should().Be(PinCommandType.Add);
            }

            [Fact]
            public void Should_remove_add_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("remove");
                _because();

                Configuration.PinCommand.Command.Should().Be(PinCommandType.Remove);
            }

            [Fact]
            public void Should_set_unrecognized_values_to_list_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("wtf");
                _because();

                Configuration.PinCommand.Command.Should().Be(PinCommandType.List);
            }

            [Fact]
            public void Should_default_to_list_as_the_subcommand()
            {
                Reset();
                _because();

                Configuration.PinCommand.Command.Should().Be(PinCommandType.List);
            }

            [Fact]
            public void Should_handle_passing_in_an_empty_string()
            {
                Reset();
                _unparsedArgs.Add(" ");
                _because();

                Configuration.PinCommand.Command.Should().Be(PinCommandType.List);
            }

            [Fact]
            public void Should_set_config_sources_to_local_only()
            {
                Reset();
                _because();

                Configuration.Sources.Should().Be(ApplicationParameters.PackagesLocation);
            }

            [Fact]
            public void Should_set_config_local_only_to_true()
            {
                Reset();
                _because();

                Configuration.ListCommand.LocalOnly.Should().BeTrue();
            }

            [Fact]
            public void Should_set_config_all_versions_to_true()
            {
                Reset();
                _because();

                Configuration.AllVersions.Should().BeTrue();
            }
        }

        public class When_validating : ChocolateyPinCommandSpecsBase
        {
            private Action _because;

            public override void Because()
            {
                _because = () => Command.Validate(Configuration);
            }

            [Fact]
            public void Should_throw_when_command_is_not_list_and_name_is_not_set()
            {
                Configuration.PinCommand.Command = PinCommandType.Add;
                Configuration.PinCommand.Name = "";
                var errored = false;
                Exception error = null;

                try
                {
                    _because();
                }
                catch (Exception ex)
                {
                    errored = true;
                    error = ex;
                }

                errored.Should().BeTrue();
                error.Should().NotBeNull();
                error.Should().BeOfType<ApplicationException>();
                error.Message.Should().Be("When specifying the subcommand '{0}', you must also specify --name.".FormatWith(Configuration.PinCommand.Command.ToStringSafe().ToLower()));
            }

            [Fact]
            public void Should_continue_when_command_is_list_and_name_is_not_set()
            {
                Configuration.PinCommand.Command = PinCommandType.List;
                Configuration.PinCommand.Name = "";
                _because();
            }

            [Fact]
            public void Should_continue_when_command_is_not_list_and_name_is_set()
            {
                Configuration.PinCommand.Command = PinCommandType.List;
                Configuration.PinCommand.Name = "bob";
                _because();
            }
        }

        public class When_noop_is_called : ChocolateyPinCommandSpecsBase
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
                Configuration.Sources = ApplicationParameters.PackagesLocation;
                Configuration.ListCommand.LocalOnly = true;
                Configuration.AllVersions = true;
                var packageResults = new[]
                {
                    new PackageResult(Package.Object, null),
                    new PackageResult(PinnedPackage.Object, null)
                };
                NugetService.Setup(n => n.List(It.IsAny<ChocolateyConfiguration>())).Returns(packageResults);
                Configuration.PinCommand.Command = PinCommandType.List;
            }

            public override void Because()
            {
                Command.Run(Configuration);
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
                Configuration.Sources = ApplicationParameters.PackagesLocation;
                Configuration.ListCommand.LocalOnly = true;
                Configuration.AllVersions = true;

                var packageResults = new[]
                {
                    new PackageResult(Package.Object, null),
                    new PackageResult(PinnedPackage.Object, null)
                };
                NugetService.Setup(n => n.List(It.IsAny<ChocolateyConfiguration>())).Returns(packageResults);
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
                Configuration.PinCommand.Command = PinCommandType.List;
                Command.Run(Configuration);

                NugetService.Verify(n => n.List(It.IsAny<ChocolateyConfiguration>()), Times.Once);
            }

            [Fact]
            public void Should_set_pin_when_command_is_add()
            {
                Reset();

                Configuration.PinCommand.Name = "regular";
                Configuration.PinCommand.Command = PinCommandType.Add;

                Command.SetPin(Configuration);

                PackageInfoService.Verify(s => s.Save(It.IsAny<ChocolateyPackageInformation>()), Times.Once);
            }

            [Fact]
            public void Should_remove_pin_when_command_is_remove()
            {
                Reset();
                Configuration.PinCommand.Name = "pinned";
                Configuration.PinCommand.Command = PinCommandType.Remove;

                Command.SetPin(Configuration);

                PackageInfoService.Verify(s => s.Save(It.IsAny<ChocolateyPackageInformation>()), Times.Once);
            }
        }

        public class When_run_is_called_with_similarly_named_package_installed : ChocolateyPinCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Sources = ApplicationParameters.PackagesLocation;
                Configuration.ListCommand.LocalOnly = true;
                Configuration.AllVersions = true;

                var packageResults = new[]
                {
                    new PackageResult(MingwPackage.Object, null),
                    new PackageResult(GstreamerMingwPackage.Object, null)
                };
                NugetService.Setup(n => n.List(It.IsAny<ChocolateyConfiguration>())).Returns(packageResults);
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
            }

            [Fact]
            public void Should_call_nuget_service_list_run_when_command_is_list()
            {
                Reset();
                Configuration.PinCommand.Command = PinCommandType.List;
                Command.Run(Configuration);

                NugetService.Verify(n => n.List(It.IsAny<ChocolateyConfiguration>()), Times.Once);
            }

            [Fact]
            public void Should_remove_pin_from_correct_package()
            {
                Reset();
                Configuration.PinCommand.Name = "mingw";
                Configuration.PinCommand.Command = PinCommandType.Remove;

                Command.SetPin(Configuration);

                PackageInfoService.Verify(s =>
                    s.Save(It.Is<ChocolateyPackageInformation>(n =>
                        n.Package.Id.Equals("mingw"))), Times.Once);
            }
        }
    }
}
