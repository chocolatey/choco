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
    using System.Collections.Generic;
    using System.Linq;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commandline;
    using Moq;
    using Should;

    public class ChocolateyOutdatedCommandSpecs
    {
        public abstract class ChocolateyOutdatedCommandSpecsBase : TinySpec
        {
            protected ChocolateyOutdatedCommand command;
            protected Mock<IChocolateyPackageService> packageService = new Mock<IChocolateyPackageService>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                configuration.Sources = "bob";
                command = new ChocolateyOutdatedCommand(packageService.Object);
            }
        }

        public class when_implementing_command_for : ChocolateyOutdatedCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void should_implement_outdated()
            {
                results.ShouldContain("outdated");
            }
        }

        public class when_configurating_the_argument_parser : ChocolateyOutdatedCommandSpecsBase
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
            public void should_add_source_to_the_option_set()
            {
                optionSet.Contains("source").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_source_to_the_option_set()
            {
                optionSet.Contains("s").ShouldBeTrue();
            }

            [Fact]
            public void should_add_user_to_the_option_set()
            {
                optionSet.Contains("user").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_user_to_the_option_set()
            {
                optionSet.Contains("u").ShouldBeTrue();
            }

            [Fact]
            public void should_add_password_to_the_option_set()
            {
                optionSet.Contains("password").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_password_to_the_option_set()
            {
                optionSet.Contains("p").ShouldBeTrue();
            }

            [Fact]
            public void should_add_ignore_pinned_to_the_option_set()
            {
                optionSet.Contains("ignore-pinned").ShouldBeTrue();
            }
        }

        public class when_noop_is_called : ChocolateyOutdatedCommandSpecsBase
        {
            public override void Because()
            {
                command.noop(configuration);
            }

            [Fact]
            public void should_call_service_outdated_noop()
            {
                packageService.Verify(c => c.outdated_noop(configuration), Times.Once);
            }
        }

        public class when_run_is_called : ChocolateyOutdatedCommandSpecsBase
        {
            public override void Because()
            {
                command.run(configuration);
            }

            [Fact]
            public void should_call_service_oudated_run()
            {
                packageService.Verify(c => c.outdated_run(configuration), Times.Once);
            }
        }
    }
}
