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
    using Moq;
    using FluentAssertions;

    public class ChocolateyInfoCommandSpecs
    {
        [ConcernFor("info")]
        public abstract class ChocolateyInfoCommandSpecsBase : TinySpec
        {
            protected ChocolateyInfoCommand Command;
            protected Mock<IChocolateyPackageService> PackageService = new Mock<IChocolateyPackageService>();
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                Configuration.Sources = "bob";
                Command = new ChocolateyInfoCommand(PackageService.Object);
            }
        }

        public class When_implementing_command_for : ChocolateyInfoCommandSpecsBase
        {
            private List<string> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_info()
            {
                _results.Should().Contain("info");
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyInfoCommandSpecsBase
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
            public void Should_add_source_to_the_option_set()
            {
                _optionSet.Contains("source").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_source_to_the_option_set()
            {
                _optionSet.Contains("s").Should().BeTrue();
            }

            [Fact]
            public void Should_add_localonly_to_the_option_set()
            {
                _optionSet.Contains("localonly").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_localonly_to_the_option_set()
            {
                _optionSet.Contains("l").Should().BeTrue();
            }

            [Fact]
            public void Should_add_prerelease_to_the_option_set()
            {
                _optionSet.Contains("prerelease").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_prerelease_to_the_option_set()
            {
                _optionSet.Contains("pre").Should().BeTrue();
            }

            [Fact]
            public void Should_add_user_to_the_option_set()
            {
                _optionSet.Contains("user").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_user_to_the_option_set()
            {
                _optionSet.Contains("u").Should().BeTrue();
            }

            [Fact]
            public void Should_add_password_to_the_option_set()
            {
                _optionSet.Contains("password").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_password_to_the_option_set()
            {
                _optionSet.Contains("p").Should().BeTrue();
            }
        }

        public class When_handling_validation : ChocolateyInfoCommandSpecsBase
        {
            private Exception _error = null;
            private Action _because;

            public override void Context()
            {
                base.Context();
            }

            public override void Because()
            {
                _because = () => Command.Validate(Configuration);
            }

            [Fact]
            public void Show_throw_when_package_id_is_not_set()
            {
                Configuration.Input = "";
                _error = null;

                try
                {
                    _because();
                }
                catch (Exception ex)
                {
                    _error = ex;
                }

                _error.Should().NotBeNull();
                _error.Should().BeOfType<ApplicationException>();
                _error.Message.Should().Contain("A single package name is required to run the choco info command.");
            }

            [Fact]
            public void Should_throw_when_multiple_package_ids_set()
            {
                Configuration.Input = "foo bar";
                _error = null;

                try
                {
                    _because();
                }
                catch (Exception ex)
                {
                    _error = ex;
                }

                _error.Should().NotBeNull();
                _error.Should().BeOfType<ApplicationException>();
                _error.Message.Should().Contain("Only a single package name can be passed to the choco info command.");
            }
        }
        public class When_handling_additional_argument_parsing : ChocolateyInfoCommandSpecsBase
        {
            private readonly IList<string> _unparsedArgs = new List<string>();
            private readonly string _source = "https://somewhereoutthere";
            private Action _because;

            public override void Context()
            {
                base.Context();
                _unparsedArgs.Add("pkg1");
                _unparsedArgs.Add("pkg2");
                Configuration.Sources = _source;
            }

            public override void Because()
            {
                _because = () => Command.ParseAdditionalArguments(_unparsedArgs, Configuration);
            }

            [Fact]
            public void Should_set_unparsed_arguments_to_configuration_input()
            {
                _because();
                Configuration.Input.Should().Be("pkg1 pkg2");
            }

            [Fact]
            public void Should_leave_source_as_set()
            {
                Configuration.ListCommand.LocalOnly = false;
                _because();
                Configuration.Sources.Should().Be(_source);
            }

            [Fact]
            public void Should_set_exact_to_true()
            {
                Configuration.ListCommand.Exact = false;
                _because();
                Configuration.ListCommand.Exact.Should().BeTrue();
            }

            [Fact]
            public void Should_set_verbose_to_true()
            {
                Configuration.Verbose = false;
                _because();
                Configuration.Verbose.Should().BeTrue();
            }
        }

        public class When_noop_is_called : ChocolateyInfoCommandSpecsBase
        {
            public override void Because()
            {
                Command.DryRun(Configuration);
            }

            [Fact]
            public void Should_call_service_list_noop()
            {
                PackageService.Verify(c => c.ListDryRun(Configuration), Times.Once);
            }
        }

        public class When_run_is_called : ChocolateyInfoCommandSpecsBase
        {
            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_call_service_list_run()
            {
                PackageService.Verify(c => c.List(Configuration), Times.Once);
            }
        }
    }
}
