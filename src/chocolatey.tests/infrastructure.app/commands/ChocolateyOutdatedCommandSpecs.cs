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

    public class ChocolateyOutdatedCommandSpecs
    {
        [ConcernFor("outdated")]
        public abstract class ChocolateyOutdatedCommandSpecsBase : TinySpec
        {
            protected ChocolateyOutdatedCommand Command;
            protected Mock<IChocolateyPackageService> PackageService = new Mock<IChocolateyPackageService>();
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                Configuration.Sources = "bob";
                Command = new ChocolateyOutdatedCommand(PackageService.Object);
            }
        }

        public class When_implementing_command_for : ChocolateyOutdatedCommandSpecsBase
        {
            private List<string> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_outdated()
            {
                _results.Should().Contain("outdated");
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyOutdatedCommandSpecsBase
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

            [Fact]
            public void Should_add_ignore_pinned_to_the_option_set()
            {
                _optionSet.Contains("ignore-pinned").Should().BeTrue();
            }
        }

        public class When_noop_is_called : ChocolateyOutdatedCommandSpecsBase
        {
            public override void Because()
            {
                Command.DryRun(Configuration);
            }

            [Fact]
            public void Should_call_service_outdated_noop()
            {
                PackageService.Verify(c => c.OutdatedDryRun(Configuration), Times.Once);
            }
        }

        public class When_run_is_called : ChocolateyOutdatedCommandSpecsBase
        {
            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_call_service_outdated_run()
            {
                PackageService.Verify(c => c.Outdated(Configuration), Times.Once);
            }
        }
    }
}
