﻿// Copyright © 2017 - 2021 Chocolatey Software, Inc
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Chocolatey.Infrastructure.App.Attributes;
    using Chocolatey.Infrastructure.App.Commands;
    using Chocolatey.Infrastructure.App.Configuration;
    using Chocolatey.Infrastructure.App.Services;
    using Chocolatey.Infrastructure.CommandLine;
    using Moq;
    using Should;

    public class ChocolateyInfoCommandSpecs
    {
        [ConcernFor("info")]
        public abstract class ChocolateyInfoCommandSpecsBase : TinySpec
        {
            protected ChocolateyInfoCommand command;
            protected Mock<IChocolateyPackageService> packageService = new Mock<IChocolateyPackageService>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                configuration.Sources = "bob";
                command = new ChocolateyInfoCommand(packageService.Object);
            }
        }

        public class When_implementing_command_for : ChocolateyInfoCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_info()
            {
                results.ShouldContain("info");
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyInfoCommandSpecsBase
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
            public void Should_add_source_to_the_option_set()
            {
                optionSet.Contains("source").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_source_to_the_option_set()
            {
                optionSet.Contains("s").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_localonly_to_the_option_set()
            {
                optionSet.Contains("localonly").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_localonly_to_the_option_set()
            {
                optionSet.Contains("l").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_prerelease_to_the_option_set()
            {
                optionSet.Contains("prerelease").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_prerelease_to_the_option_set()
            {
                optionSet.Contains("pre").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_user_to_the_option_set()
            {
                optionSet.Contains("user").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_user_to_the_option_set()
            {
                optionSet.Contains("u").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_password_to_the_option_set()
            {
                optionSet.Contains("password").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_password_to_the_option_set()
            {
                optionSet.Contains("p").ShouldBeTrue();
            }
        }

        public class When_handling_additional_argument_parsing : ChocolateyInfoCommandSpecsBase
        {
            private readonly IList<string> unparsedArgs = new List<string>();
            private readonly string source = "https://somewhereoutthere";
            private Action because;

            public override void Context()
            {
                base.Context();
                unparsedArgs.Add("pkg1");
                unparsedArgs.Add("pkg2");
                configuration.Sources = source;
            }

            public override void Because()
            {
                because = () => command.ParseAdditionalArguments(unparsedArgs, configuration);
            }

            [Fact]
            public void Should_set_unparsed_arguments_to_configuration_input()
            {
                because();
                configuration.Input.ShouldEqual("pkg1 pkg2");
            }

            [Fact]
            public void Should_leave_source_as_set()
            {
                configuration.ListCommand.LocalOnly = false;
                because();
                configuration.Sources.ShouldEqual(source);
            }

            [Fact]
            public void Should_set_exact_to_true()
            {
                configuration.ListCommand.Exact = false;
                because();
                configuration.ListCommand.Exact.ShouldBeTrue();
            }

            [Fact]
            public void Should_set_verbose_to_true()
            {
                configuration.Verbose = false;
                because();
                configuration.Verbose.ShouldBeTrue();
            }
        }

        public class When_noop_is_called : ChocolateyInfoCommandSpecsBase
        {
            public override void Because()
            {
                command.DryRun(configuration);
            }

            [Fact]
            public void Should_call_service_list_noop()
            {
                packageService.Verify(c => c.ListDryRun(configuration), Times.Once);
            }
        }

        public class When_run_is_called : ChocolateyInfoCommandSpecsBase
        {
            public override void Because()
            {
                command.Run(configuration);
            }

            [Fact]
            public void Should_call_service_list_run()
            {
                packageService.Verify(c => c.List(configuration), Times.Once);
            }
        }
    }
}
