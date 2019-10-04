// Copyright © 2017 - 2018 Chocolatey Software, Inc
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
    using NuGet;
    using Should;

    public class ChocolateyPinCommandSpecs
    {
        public abstract class ChocolateyPinCommandSpecsBase : TinySpec
        {
            protected ChocolateyPinCommand command;
            protected Mock<IChocolateyPackageInformationService> packageInfoService = new Mock<IChocolateyPackageInformationService>();
            protected Mock<ILogger> nugetLogger = new Mock<ILogger>();
            protected Mock<INugetService> nugetService = new Mock<INugetService>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();
            protected Mock<IPackage> package = new Mock<IPackage>();
            protected Mock<IPackage> pinnedPackage = new Mock<IPackage>();

            public override void Context()
            {
                // MockLogger = new MockLogger();
                // Log.InitializeWith(MockLogger);
                configuration.Sources = "https://localhost/somewhere/out/there";
                command = new ChocolateyPinCommand(packageInfoService.Object, nugetLogger.Object, nugetService.Object);

                package = new Mock<IPackage>();
                package.Setup(p => p.Id).Returns("regular");
                package.Setup(p => p.Version).Returns(new SemanticVersion("1.2.0"));
                packageInfoService.Setup(s => s.get_package_information(package.Object)).Returns(
                    new ChocolateyPackageInformation(package.Object)
                    {
                        IsPinned = false
                    });
                pinnedPackage = new Mock<IPackage>();
                pinnedPackage.Setup(p => p.Id).Returns("pinned");
                pinnedPackage.Setup(p => p.Version).Returns(new SemanticVersion("1.1.0"));
                packageInfoService.Setup(s => s.get_package_information(pinnedPackage.Object)).Returns(
                    new ChocolateyPackageInformation(pinnedPackage.Object)
                    {
                        IsPinned = true
                    });
            }

            public void reset()
            {
                packageInfoService.ResetCalls();
                nugetService.ResetCalls();
            }
        }

        public class when_implementing_command_for : ChocolateyPinCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void should_implement_source()
            {
                results.ShouldContain("pin");
            }
        }

        public class when_configurating_the_argument_parser : ChocolateyPinCommandSpecsBase
        {
            private OptionSet optionSet;

            public override void Context()
            {
                base.Context();
                optionSet = new OptionSet();
            }

            public override void Because()
            {
                command.configure_argument_parser(optionSet, configuration);
            }

            [Fact]
            public void should_add_name_to_the_option_set()
            {
                optionSet.Contains("name").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_name_to_the_option_set()
            {
                optionSet.Contains("n").ShouldBeTrue();
            }

            [Fact]
            public void should_add_version_to_the_option_set()
            {
                optionSet.Contains("version").ShouldBeTrue();
            }
        }

        public class when_handling_additional_argument_parsing : ChocolateyPinCommandSpecsBase
        {
            private readonly IList<string> unparsedArgs = new List<string>();
            private Action because;

            public override void Because()
            {
                because = () => command.handle_additional_argument_parsing(unparsedArgs, configuration);
            }

            public new void reset()
            {
                unparsedArgs.Clear();
                base.reset();
            }

            [Fact]
            public void should_use_the_first_unparsed_arg_as_the_subcommand()
            {
                reset();
                unparsedArgs.Add("list");
                because();

                configuration.PinCommand.Command.ShouldEqual(PinCommandType.list);
            }

            [Fact]
            public void should_throw_when_more_than_one_unparsed_arg_is_passed()
            {
                reset();
                unparsedArgs.Add("wtf");
                unparsedArgs.Add("bbq");
                var errorred = false;
                Exception error = null;

                try
                {
                    because();
                }
                catch (Exception ex)
                {
                    errorred = true;
                    error = ex;
                }

                errorred.ShouldBeTrue();
                error.ShouldNotBeNull();
                error.ShouldBeType<ApplicationException>();
                error.Message.ShouldContain("A single pin command must be listed");
            }

            [Fact]
            public void should_accept_add_as_the_subcommand()
            {
                reset();
                unparsedArgs.Add("add");
                because();

                configuration.PinCommand.Command.ShouldEqual(PinCommandType.add);
            }

            [Fact]
            public void should_accept_uppercase_add_as_the_subcommand()
            {
                reset();
                unparsedArgs.Add("ADD");
                because();

                configuration.PinCommand.Command.ShouldEqual(PinCommandType.add);
            }

            [Fact]
            public void should_remove_add_as_the_subcommand()
            {
                reset();
                unparsedArgs.Add("remove");
                because();

                configuration.PinCommand.Command.ShouldEqual(PinCommandType.remove);
            }

            [Fact]
            public void should_set_unrecognized_values_to_list_as_the_subcommand()
            {
                reset();
                unparsedArgs.Add("wtf");
                because();

                configuration.PinCommand.Command.ShouldEqual(PinCommandType.list);
            }

            [Fact]
            public void should_default_to_list_as_the_subcommand()
            {
                reset();
                because();

                configuration.PinCommand.Command.ShouldEqual(PinCommandType.list);
            }

            [Fact]
            public void should_handle_passing_in_an_empty_string()
            {
                reset();
                unparsedArgs.Add(" ");
                because();

                configuration.PinCommand.Command.ShouldEqual(PinCommandType.list);
            }

            [Fact]
            public void should_set_config_sources_to_local_only()
            {
                reset();
                because();

                configuration.Sources.ShouldEqual(ApplicationParameters.PackagesLocation);
            }

            [Fact]
            public void should_set_config_local_only_to_true()
            {
                reset();
                because();

                configuration.ListCommand.LocalOnly.ShouldBeTrue();
            }

            [Fact]
            public void should_set_config_all_versions_to_true()
            {
                reset();
                because();

                configuration.AllVersions.ShouldBeTrue();
            }
        }

        public class when_handling_validation : ChocolateyPinCommandSpecsBase
        {
            private Action because;

            public override void Because()
            {
                because = () => command.handle_validation(configuration);
            }

            [Fact]
            public void should_throw_when_command_is_not_list_and_name_is_not_set()
            {
                configuration.PinCommand.Command = PinCommandType.add;
                configuration.PinCommand.Name = "";
                var errorred = false;
                Exception error = null;

                try
                {
                    because();
                }
                catch (Exception ex)
                {
                    errorred = true;
                    error = ex;
                }

                errorred.ShouldBeTrue();
                error.ShouldNotBeNull();
                error.ShouldBeType<ApplicationException>();
                error.Message.ShouldEqual("When specifying the subcommand '{0}', you must also specify --name.".format_with(configuration.PinCommand.Command.to_string()));
            }

            [Fact]
            public void should_continue_when_command_is_list_and_name_is_not_set()
            {
                configuration.PinCommand.Command = PinCommandType.list;
                configuration.PinCommand.Name = "";
                because();
            }

            [Fact]
            public void should_continue_when_command_is_not_list_and_name_is_set()
            {
                configuration.PinCommand.Command = PinCommandType.list;
                configuration.PinCommand.Name = "bob";
                because();
            }
        }

        public class when_noop_is_called : ChocolateyPinCommandSpecsBase
        {
            public override void Because()
            {
                command.noop(configuration);
            }

            [Fact]
            public void should_log_a_message()
            {
                MockLogger.Verify(l => l.Info(It.IsAny<string>()), Times.AtLeastOnce);
            }

            [Fact]
            public void should_log_the_message_we_expect()
            {
                var messages = MockLogger.MessagesFor(LogLevel.Info);
                messages.ShouldNotBeEmpty();
                messages.Count.ShouldEqual(1);
                messages[0].ShouldContain("Pin would have called");
            }
        }

        public class when_list_is_called : ChocolateyPinCommandSpecsBase
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
                nugetService.Setup(n => n.list_run(It.IsAny<ChocolateyConfiguration>())).Returns(packageResults);
                configuration.PinCommand.Command = PinCommandType.list;
            }

            public override void Because()
            {
                command.run(configuration);
            }

            [Fact]
            public void should_list_pinned_packages()
            {
                MockLogger.Verify(l => l.Info("pinned|1.1.0"), Times.Once);
            }

            [Fact]
            public void should_not_list_unpinned_packages()
            {
                MockLogger.Verify(l => l.Info("regular|1.2.0"), Times.Never);
            }

            [Fact]
            public void should_log_a_message()
            {
                MockLogger.Verify(l => l.Info(It.IsAny<string>()), Times.Once);
            }

            [Fact]
            public void should_log_one_message()
            {
                MockLogger.Messages.Count.ShouldEqual(1);
            }
        }

        public class when_run_is_called : ChocolateyPinCommandSpecsBase
        {
            //private Action because;
            private readonly Mock<IPackageManager> packageManager = new Mock<IPackageManager>();
            private readonly Mock<IPackageRepository> localRepository = new Mock<IPackageRepository>();

            public override void Context()
            {
                base.Context();
                configuration.Sources = ApplicationParameters.PackagesLocation;
                configuration.ListCommand.LocalOnly = true;
                configuration.AllVersions = true;
            }

            public new void reset()
            {
                Context();
                base.reset();
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
            public void should_call_nuget_service_list_run_when_command_is_list()
            {
                reset();
                configuration.PinCommand.Command = PinCommandType.list;
                command.run(configuration);

                nugetService.Verify(n => n.list_run(It.IsAny<ChocolateyConfiguration>()), Times.Once);
            }

            [Pending("NuGet is killing me with extension methods. Need to find proper item to mock out to return the package object.")]
            [Fact]
            public void should_set_pin_when_command_is_add()
            {
                reset();

                configuration.PinCommand.Name = "regular";
                packageManager.Setup(pm => pm.LocalRepository).Returns(localRepository.Object);
                SemanticVersion semanticVersion = null;
                //nuget woes
                localRepository.Setup(r => r.FindPackage(configuration.PinCommand.Name, semanticVersion)).Returns(package.Object);
                configuration.PinCommand.Command = PinCommandType.add;

                command.set_pin(packageManager.Object, configuration);

                packageInfoService.Verify(s => s.save_package_information(It.IsAny<ChocolateyPackageInformation>()), Times.Once);
            }

            [Pending("NuGet is killing me with extension methods. Need to find proper item to mock out to return the package object.")]
            [Fact]
            public void should_remove_pin_when_command_is_remove()
            {
                reset();
                configuration.PinCommand.Name = "pinned";
                packageManager.Setup(pm => pm.LocalRepository).Returns(localRepository.Object);
                configuration.PinCommand.Command = PinCommandType.remove;

                command.set_pin(packageManager.Object, configuration);

                packageInfoService.Verify(s => s.save_package_information(It.IsAny<ChocolateyPackageInformation>()), Times.Once);
            }
        }
    }
}
