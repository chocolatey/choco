// Copyright © 2023-Present Chocolatey Software, Inc
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
    using Moq;
    using Should;

    public static class ChocolateyListCommandSpecs
    {
        [ConcernFor("list")]
        public abstract class ChocolateyListCommandSpecsBase : TinySpec
        {
            protected ChocolateyListCommand command;
            protected Mock<IChocolateyPackageService> packageService = new Mock<IChocolateyPackageService>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                command = new ChocolateyListCommand(packageService.Object);
            }
        }

        public class when_implementing_command_for : ChocolateyListCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void should_implement_list()
            {
                results.ShouldContain("list");
            }

            [Fact]
            public void should_not_implement_search()
            {
                results.ShouldNotContain("search");
            }

            [Fact]
            public void should_not_implement_find()
            {
                results.ShouldNotContain("find");
            }

            public class when_configurating_the_argument_parser : ChocolateyListCommandSpecsBase
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

                [NUnit.Framework.TestCase("prerelease")]
                [NUnit.Framework.TestCase("pre")]
                [NUnit.Framework.TestCase("includeprograms")]
                [NUnit.Framework.TestCase("i")]
                public void should_add_to_option_set(string option)
                {
                    optionSet.Contains(option).ShouldBeTrue();
                }

                [NUnit.Framework.TestCase("source")]
                [NUnit.Framework.TestCase("s")]
                [NUnit.Framework.TestCase("localonly")]
                [NUnit.Framework.TestCase("l")]
                [NUnit.Framework.TestCase("user")]
                [NUnit.Framework.TestCase("u")]
                [NUnit.Framework.TestCase("password")]
                [NUnit.Framework.TestCase("p")]
                [NUnit.Framework.TestCase("allversions")]
                [NUnit.Framework.TestCase("a")]
                public void should_not_add_to_option_set(string option)
                {
                    optionSet.Contains(option).ShouldBeFalse();
                }
            }

            public class when_handling_additional_argument_parsing : ChocolateyListCommandSpecsBase
            {
                private readonly IList<string> unparsedArgs = new List<string>();
                private readonly string source = "https://somewhereoutthere";
                private Action because;

                public override void Context()
                {
                    base.Context();
                    unparsedArgs.Add("pkg1");
                    unparsedArgs.Add("pkg2");
                    unparsedArgs.Add("-l");
                    unparsedArgs.Add("--local-only");
                    unparsedArgs.Add("--localonly");
                    configuration.Sources = source;
                }

                public override void Because()
                {
                    because = () => command.handle_additional_argument_parsing(unparsedArgs, configuration);
                }

                [Fact]
                public void should_set_unparsed_arguments_to_configuration_input()
                {
                    because();
                    configuration.Input.ShouldEqual("pkg1 pkg2");
                }

                [NUnit.Framework.TestCase("-l")]
                [NUnit.Framework.TestCase("--local-only")]
                [NUnit.Framework.TestCase("--localonly")]
                public void should_output_warning_message_about_unsupported_argument(string argument)
                {
                    because();
                    MockLogger.Messages.Keys.ShouldContain("Warn");
                    MockLogger.Messages["Warn"].ShouldContain(@"
UNSUPPORTED ARGUMENT: Ignoring the argument {0}. This argument is unsupported for locally installed packages, and will be treated as a package name in Chocolatey CLI v3!".format_with(argument));
                }
            }

            public class when_noop_is_called_with_list_command : ChocolateyListCommandSpecsBase
            {
                public override void Context()
                {
                    base.Context();
                    configuration.CommandName = "search";
                    configuration.ListCommand.LocalOnly = false;
                }

                public override void Because()
                {
                    command.noop(configuration);
                }

                [Fact]
                public void should_call_service_list_noop()
                {
                    packageService.Verify(c => c.list_noop(configuration), Times.Once);
                }

                [Fact]
                public void should_not_report_any_warning_messages()
                {
                    MockLogger.Messages.Keys.ShouldNotContain("Warn");
                }
            }

            public class when_run_is_called_with_search_command_and_local_only : ChocolateyListCommandSpecsBase
            {
                public override void Context()
                {
                    base.Context();
                    configuration.CommandName = "list";
                }

                public override void Because()
                {
                    command.run(configuration);
                }

                [Fact]
                public void should_call_service_list_run()
                {
                    packageService.Verify(c => c.list_run(configuration), Times.Once);
                }

                [Fact]
                public void should_not_report_any_warning_messages()
                {
                    MockLogger.Messages.Keys.ShouldNotContain("Warn");
                }
            }
        }
    }
}
