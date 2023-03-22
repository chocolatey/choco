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

namespace Chocolatey.Tests.Infrastructure.App.Commands
{
    using System.Collections.Generic;
    using System.Linq;
    using Chocolatey.Infrastructure.App.Attributes;
    using Chocolatey.Infrastructure.App.Commands;
    using Chocolatey.Infrastructure.App.Configuration;
    using Chocolatey.Infrastructure.App.Services;
    using Chocolatey.Infrastructure.CommandLine;
    using Moq;
    using Should;
    using System;

    public class ChocolateyPackCommandSpecs
    {
        [ConcernFor("pack")]
        public abstract class ChocolateyPackCommandSpecsBase : TinySpec
        {
            protected ChocolateyPackCommand command;
            protected Mock<IChocolateyPackageService> packageService = new Mock<IChocolateyPackageService>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                command = new ChocolateyPackCommand(packageService.Object);
            }
        }

        public class When_implementing_command_for : ChocolateyPackCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_pack()
            {
                results.ShouldContain("pack");
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyPackCommandSpecsBase
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
            public void Should_add_version_to_the_option_set()
            {
                optionSet.Contains("version").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_outputdirectory_to_the_option_set()
            {
                optionSet.Contains("outputdirectory").ShouldBeTrue();
            }
        }

        public class When_handling_additional_argument_parsing : ChocolateyPackCommandSpecsBase
        {
            private readonly IList<string> unparsedArgs = new List<string>();
            private const string nuspecPath = "./some/path/to.nuspec";

            public override void Context()
            {
                base.Context();
                unparsedArgs.Add(nuspecPath);
                unparsedArgs.Add("foo=1");
                unparsedArgs.Add("bar='baz'");

                // Make sure we storing only the first property name specified regardless of case.
                unparsedArgs.Add("Foo=2");
            }

            public override void Because()
            {
                command.ParseAdditionalArguments(unparsedArgs, configuration);
            }

            [Fact]
            public void Should_allow_a_path_to_the_nuspec_to_be_passed_in()
            {
                configuration.Input.ShouldEqual(nuspecPath);
            }

            [Fact]
            public void Should_property_foo_equal_1()
            {
                configuration.PackCommand.Properties["foo"].ShouldEqual("1");
            }

            [Fact]
            public void Should_property_bar_equal_baz()
            {
                configuration.PackCommand.Properties["bar"].ShouldEqual("baz");
            }

            [Fact]
            public void Should_log_warning_on_duplicate_foo()
            {
                var warnings = MockLogger.MessagesFor(LogLevel.Warn);
                warnings.Count.ShouldEqual(1);
                warnings[0].ShouldEqual("A value for 'foo' has already been added with the value '1'. Ignoring foo='2'.", StringComparer.OrdinalIgnoreCase);
            }
        }

        public class When_noop_is_called : ChocolateyPackCommandSpecsBase
        {
            public override void Because()
            {
                command.DryRun(configuration);
            }

            [Fact]
            public void Should_call_service_package_noop()
            {
                packageService.Verify(c => c.PackDryRun(configuration), Times.Once);
            }
        }

        public class When_run_is_called : ChocolateyPackCommandSpecsBase
        {
            public override void Because()
            {
                command.Run(configuration);
            }

            [Fact]
            public void Should_call_service_pack_run()
            {
                packageService.Verify(c => c.Pack(configuration), Times.Once);
            }
        }

        public class When_handling_arguments_parsing : ChocolateyPackCommandSpecsBase
        {
            private OptionSet optionSet;

            public override void Context()
            {
                base.Context();
                optionSet = new OptionSet();
                command.ConfigureArgumentParser(optionSet, configuration);
            }

            public override void Because()
            {
                optionSet.Parse(new[] { "--version", "0.42.0", "--outputdirectory", "c:\\packages" });
            }

            [Fact]
            public void Should_version_equal_to_42()
            {
                configuration.Version.ShouldEqual("0.42.0");
            }

            [Fact]
            public void Should_outputdirectory_equal_packages()
            {
                configuration.OutputDirectory.ShouldEqual("c:\\packages");
            }
        }
    }
}
