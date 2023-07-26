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

namespace chocolatey.tests.infrastructure.app.configuration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using chocolatey.infrastructure.adapters;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.commandline;
    using Moq;
    using FluentAssertions;

    public class ConfigurationOptionsSpec
    {
        public abstract class ConfigurationOptionsSpecBase : TinySpec
        {
            protected ICollection<string> Args;
            protected ChocolateyConfiguration Config;
            protected Action<OptionSet> SetOptions;
            protected Action<IList<string>> AfterParse;
            protected Action ValidateConfiguration;
            protected Action HelpMessage;

            protected Mock<IConsole> Console = new Mock<IConsole>();
            protected static StringBuilder HelpMessageContents = new StringBuilder();
            protected TextWriter ErrorWriter = new StringWriter(HelpMessageContents);
            protected TextWriter OutputWriter = new StringWriter(HelpMessageContents);

            public override void Context()
            {
                ConfigurationOptions.InitializeWith(new Lazy<IConsole>(() => Console.Object));
                ConfigurationOptions.ClearOptions();
                Console.Setup((c) => c.Error).Returns(ErrorWriter);
                Console.Setup((c) => c.Out).Returns(OutputWriter);
            }

            protected Action BecauseAction;

            public override void Because()
            {
                BecauseAction = () => ConfigurationOptions.ParseArgumentsAndUpdateConfiguration(Args, Config, SetOptions, AfterParse, ValidateConfiguration, HelpMessage);
            }

            public override void BeforeEachSpec()
            {
                Args = new List<string>();
                Config = new ChocolateyConfiguration();
                SetOptions = set => { };
                AfterParse = list => { };
                ValidateConfiguration = () => { };
                HelpMessage = () => { };
                HelpMessageContents.Clear();
                ConfigurationOptions.ClearOptions();
            }
        }

        public class When_ConfigurationOptions_parses_arguments_and_updates_configuration_method : ConfigurationOptionsSpecBase
        {
            [Fact]
            public void Should_set_help_options_by_default()
            {
                SetOptions = set =>
                {
                    set.Contains("h").Should().BeTrue();
                    set.Contains("help").Should().BeTrue();
                    set.Contains("?").Should().BeTrue();
                };
                BecauseAction();
            }

            [Fact]
            public void Should_not_have_set_other_options_by_default()
            {
                SetOptions = set => { set.Contains("dude").Should().BeFalse(); };
                BecauseAction();
            }

            [Fact]
            public void Should_show_help_menu_when_help_is_requested()
            {
                Args.Add("-h");

                BecauseAction();

                Config.HelpRequested.Should().BeTrue();
                Config.ShowOnlineHelp.Should().BeFalse();
            }

            [Fact]
            public void Should_show_online_help_menu_when_help_is_requested()
            {
                Args.Add("-h");
                Args.Add("--online");

                BecauseAction();

                Config.HelpRequested.Should().BeTrue();
                Config.ShowOnlineHelp.Should().BeTrue();
            }

            [Fact]
            public void Should_have_a_helpMessage_with_contents_when_help_is_requested()
            {
                Args.Add("-h");

                BecauseAction();

                HelpMessageContents.ToString().Should().NotBeEmpty();
            }

            [Fact]
            public void Should_not_run_validate_configuration_when_help_is_requested()
            {
                Args.Add("-h");
                ValidateConfiguration = () => { "should".Should().Be("not be reached"); };

                BecauseAction();
            }

            [Fact]
            public void Should_run_validate_configuration_unless_help_is_requested()
            {
                var wasCalled = false;
                ValidateConfiguration = () => { wasCalled = true; };

                BecauseAction();

                wasCalled.Should().BeTrue();
            }

            [Fact]
            public void Should_give_an_empty_unparsed_args_to_after_parse()
            {
                var wasCalled = false;
                AfterParse = list =>
                {
                    wasCalled = true;
                    list.Should().BeEmpty();
                };

                BecauseAction();

                wasCalled.Should().BeTrue();
            }

            [Fact]
            public void Should_give_an_empty_unparsed_args_to_after_parse_when_all_specified_args_are_parsed()
            {
                Args.Add("-h");
                var wasCalled = false;
                AfterParse = list =>
                {
                    wasCalled = true;
                    list.Should().BeEmpty();
                };

                BecauseAction();

                wasCalled.Should().BeTrue();
            }

            [Fact]
            public void Should_give_unparsed_args_to_after_parse_when_not_picked_up_by_an_option()
            {
                Args.Add("--what-is=this");
                var wasCalled = false;
                AfterParse = list =>
                {
                    wasCalled = true;
                    list.Should().Contain(Args.First());
                };

                BecauseAction();

                wasCalled.Should().BeTrue();
            }

            [Fact]
            public void Should_find_command_name_in_unparsed_args_if_not_set_otherwise()
            {
                Args.Add("dude");
                var wasCalled = false;
                AfterParse = list =>
                {
                    wasCalled = true;
                    list.Should().Contain(Args.First());
                };

                BecauseAction();

                Config.CommandName.Should().Be("dude");
                wasCalled.Should().BeTrue();
            }

            [Fact]
            public void Should_set_help_requested_if_command_name_is_starts_with_a_prefix()
            {
                Args.Add("/dude");
                var wasCalled = false;
                AfterParse = list =>
                {
                    wasCalled = true;
                    list.Should().Contain(Args.First());
                };

                BecauseAction();

                Config.CommandName.Should().NotBe("dude");
                Config.HelpRequested.Should().BeTrue();
                wasCalled.Should().BeTrue();
            }

            [Fact]
            public void Should_add_an_option_for_bob_when_specified()
            {
                SetOptions = set => { set.Add("bob", "sets the bob switch", option => Config.Verbose = option != null); };
                BecauseAction();

                Config.Verbose.Should().BeFalse();
            }

            [Fact]
            public void Should_set_option_for_tim_to_true_when_specified_with_dash()
            {
                SetOptions = set => { set.Add("tim", "sets the tim switch", option => Config.Verbose = option != null); };
                Args.Add("-tim");

                BecauseAction();

                Config.Verbose.Should().BeTrue();
            }

            [Fact]
            public void Should_set_option_for_tina_to_true_when_specified_with_two_dashes()
            {
                SetOptions = set => { set.Add("tina", "sets the tina switch", option => Config.Verbose = option != null); };
                Args.Add("--tina");
                BecauseAction();

                Config.Verbose.Should().BeTrue();
            }

            [Fact]
            public void Should_set_option_for_gena_to_true_when_specified_with_forward_slash()
            {
                SetOptions = set => { set.Add("gena", "sets the gena switch", option => Config.Verbose = option != null); };
                Args.Add("/gena");

                BecauseAction();

                Config.Verbose.Should().BeTrue();
            }

            [Fact]
            public void Should_set_option_when_specified_as_single_dash_for_timmy_and_other_option_short_values_are_passed_the_same_way()
            {
                SetOptions = set =>
                {
                    set.Add("timmy", "sets the timmy switch", option => Config.Verbose = option != null);
                    set.Add("s|skip", "sets the skip switch", option => Config.SkipPackageInstallProvider = option != null);
                    set.Add("d|debug", "sets the debug switch", option => Config.Debug = option != null);
                };
                Args.Add("-timmy");
                Args.Add("-sd");

                BecauseAction();

                Config.SkipPackageInstallProvider.Should().BeTrue();
                Config.Debug.Should().BeTrue();
                Config.Verbose.Should().BeTrue();
            }

            [Fact]
            public void Should_set_option_when_specified_as_single_dash_for_lo_and_other_option_short_values_are_passed_the_same_way()
            {
                SetOptions = set =>
                {
                    set.Add("lo|local-only", "sets the lo switch", option => Config.ListCommand.LocalOnly = option != null);
                    set.Add("l|lskip", "sets the skip switch", option => Config.SkipPackageInstallProvider = option != null);
                    set.Add("m|mdebug", "sets the debug switch", option => Config.Debug = option != null);
                };
                Args.Add("-lo");
                Args.Add("-ml");

                BecauseAction();

                Config.SkipPackageInstallProvider.Should().BeTrue();
                Config.Debug.Should().BeTrue();
                Config.ListCommand.LocalOnly.Should().BeTrue();
                HelpMessageContents.ToString().Should().BeEmpty();
            }

            [Fact]
            public void Should_show_help_menu_when_passing_bundled_options_that_do_not_exist()
            {
                SetOptions = set => { set.Add("w|wdebug", "sets the debug switch", option => Config.Debug = option != null); };
                Args.Add("-wz");

                BecauseAction();

                Config.Debug.Should().BeFalse();
                HelpMessageContents.ToString().Should().NotBeEmpty();
            }

            [Fact]
            public void Should_successfully_parse_help_option()
            {
                Args.Add("-h");

                BecauseAction();

                Config.UnsuccessfulParsing.Should().BeFalse();
            }

            [Fact]
            public void Should_not_parse_unknown_option()
            {
                Args.Add("-unknown");

                BecauseAction();

                Config.UnsuccessfulParsing.Should().BeTrue();
            }
        }
    }
}
