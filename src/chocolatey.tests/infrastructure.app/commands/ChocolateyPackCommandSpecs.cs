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
    using System.Collections.Generic;
    using System.Linq;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commandline;
    using Moq;
    using FluentAssertions;
    using System;

    public class ChocolateyPackCommandSpecs
    {
        [ConcernFor("pack")]
        public abstract class ChocolateyPackCommandSpecsBase : TinySpec
        {
            protected ChocolateyPackCommand Command;
            protected Mock<IChocolateyPackageService> PackageService = new Mock<IChocolateyPackageService>();
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                Command = new ChocolateyPackCommand(PackageService.Object);
            }
        }

        public class When_implementing_command_for : ChocolateyPackCommandSpecsBase
        {
            private List<string> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_pack()
            {
                _results.Should().Contain("pack");
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyPackCommandSpecsBase
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
            public void Should_add_version_to_the_option_set()
            {
                _optionSet.Contains("version").Should().BeTrue();
            }

            [Fact]
            public void Should_add_outputdirectory_to_the_option_set()
            {
                _optionSet.Contains("outputdirectory").Should().BeTrue();
            }
        }

        public class When_handling_additional_argument_parsing : ChocolateyPackCommandSpecsBase
        {
            private readonly IList<string> _unparsedArgs = new List<string>();
            private const string NuspecPath = "./some/path/to.nuspec";

            public override void Context()
            {
                base.Context();
                _unparsedArgs.Add(NuspecPath);
                _unparsedArgs.Add("foo=1");
                _unparsedArgs.Add("bar='baz'");

                // Make sure we storing only the first property name specified regardless of case.
                _unparsedArgs.Add("Foo=2");
            }

            public override void Because()
            {
                Command.ParseAdditionalArguments(_unparsedArgs, Configuration);
            }

            [Fact]
            public void Should_allow_a_path_to_the_nuspec_to_be_passed_in()
            {
                Configuration.Input.Should().Be(NuspecPath);
            }

            [Fact]
            public void Should_property_foo_equal_1()
            {
                Configuration.PackCommand.Properties["foo"].Should().Be("1");
            }

            [Fact]
            public void Should_property_bar_equal_baz()
            {
                Configuration.PackCommand.Properties["bar"].Should().Be("baz");
            }

            [Fact]
            public void Should_log_warning_on_duplicate_foo()
            {
                var warnings = MockLogger.MessagesFor(LogLevel.Warn);
                warnings.Should().HaveCount(1);
                warnings[0].Should().BeEquivalentTo("A value for 'foo' has already been added with the value '1'. Ignoring foo='2'.");
            }
        }

        public class When_noop_is_called : ChocolateyPackCommandSpecsBase
        {
            public override void Because()
            {
                Command.DryRun(Configuration);
            }

            [Fact]
            public void Should_call_service_package_noop()
            {
                PackageService.Verify(c => c.PackDryRun(Configuration), Times.Once);
            }
        }

        public class When_run_is_called : ChocolateyPackCommandSpecsBase
        {
            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_call_service_pack_run()
            {
                PackageService.Verify(c => c.Pack(Configuration), Times.Once);
            }
        }

        public class When_handling_arguments_parsing : ChocolateyPackCommandSpecsBase
        {
            private OptionSet _optionSet;

            public override void Context()
            {
                base.Context();
                _optionSet = new OptionSet();
                Command.ConfigureArgumentParser(_optionSet, Configuration);
            }

            public override void Because()
            {
                _optionSet.Parse(new[] { "--version", "0.42.0", "--outputdirectory", "c:\\packages" });
            }

            [Fact]
            public void Should_version_equal_to_42()
            {
                Configuration.Version.Should().Be("0.42.0");
            }

            [Fact]
            public void Should_outputdirectory_equal_packages()
            {
                Configuration.OutputDirectory.Should().Be("c:\\packages");
            }
        }
    }
}
