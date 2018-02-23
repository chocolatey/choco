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
    using Should;

    public class ConfigurationOptionsSpec
    {
        public abstract class ConfigurationOptionsSpecBase : TinySpec
        {
            protected ICollection<string> args;
            protected ChocolateyConfiguration config;
            protected Action<OptionSet> setOptions;
            protected Action<IList<string>> afterParse;
            protected Action validateConfiguration;
            protected Action helpMessage;

            protected Mock<IConsole> console = new Mock<IConsole>();
            protected static StringBuilder helpMessageContents = new StringBuilder();
            protected TextWriter errorWriter = new StringWriter(helpMessageContents);
            protected TextWriter outputWriter = new StringWriter(helpMessageContents);

            public override void Context()
            {
                ConfigurationOptions.initialize_with(new Lazy<IConsole>(() => console.Object));
                ConfigurationOptions.reset_options();
                console.Setup((c) => c.Error).Returns(errorWriter);
                console.Setup((c) => c.Out).Returns(outputWriter);
            }

            protected Action because;

            public override void Because()
            {
                because = () => ConfigurationOptions.parse_arguments_and_update_configuration(args, config, setOptions, afterParse, validateConfiguration, helpMessage);
            }

            public override void BeforeEachSpec()
            {
                args = new List<string>();
                config = new ChocolateyConfiguration();
                setOptions = set => { };
                afterParse = list => { };
                validateConfiguration = () => { };
                helpMessage = () => { };
                helpMessageContents.Clear();
            }
        }

        public class when_ConfigurationOptions_parses_arguments_and_updates_configuration_method : ConfigurationOptionsSpecBase
        {
            [Fact]
            public void should_set_help_options_by_default()
            {
                setOptions = set =>
                {
                    set.Contains("h").ShouldBeTrue();
                    set.Contains("help").ShouldBeTrue();
                    set.Contains("?").ShouldBeTrue();
                };
                because();
            }

            [Fact]
            public void should_not_have_set_other_options_by_default()
            {
                setOptions = set => { set.Contains("dude").ShouldBeFalse(); };
                because();
            }

            [Fact, Explicit]
            public void should_show_help_menu_when_help_is_requested()
            {
                args.Add("-h");

                because();

                config.HelpRequested.ShouldBeTrue();
            }

            [Fact, Explicit]
            public void should_have_a_helpMessage_with_contents_when_help_is_requested()
            {
                args.Add("-h");

                because();

                helpMessageContents.ToString().ShouldNotBeEmpty();
            }

            [Fact, Explicit]
            public void should_not_run_validate_configuration_when_help_is_requested()
            {
                args.Add("-h");
                validateConfiguration = () => { "should".ShouldEqual("not be reached"); };

                because();
            }

            [Fact]
            public void should_run_validate_configuration_unless_help_is_requested()
            {
                var wasCalled = false;
                validateConfiguration = () => { wasCalled = true; };

                because();

                wasCalled.ShouldBeTrue();
            }

            [Fact]
            public void should_give_an_empty_unparsed_args_to_after_parse()
            {
                var wasCalled = false;
                afterParse = list =>
                {
                    wasCalled = true;
                    list.ShouldBeEmpty();
                };

                because();

                wasCalled.ShouldBeTrue();
            }

            [Fact]
            public void should_give_an_empty_unparsed_args_to_after_parse_when_all_specified_args_are_parsed()
            {
                args.Add("-h");
                var wasCalled = false;
                afterParse = list =>
                {
                    wasCalled = true;
                    list.ShouldBeEmpty();
                };

                because();

                wasCalled.ShouldBeTrue();
            }

            [Fact]
            public void should_give_unparsed_args_to_after_parse_when_not_picked_up_by_an_option()
            {
                args.Add("--what-is=this");
                var wasCalled = false;
                afterParse = list =>
                {
                    wasCalled = true;
                    list.ShouldContain(args.First());
                };

                because();

                wasCalled.ShouldBeTrue();
            }

            [Fact]
            public void should_find_command_name_in_unparsed_args_if_not_set_otherwise()
            {
                args.Add("dude");
                var wasCalled = false;
                afterParse = list =>
                {
                    wasCalled = true;
                    list.ShouldContain(args.First());
                };

                because();

                config.CommandName.ShouldEqual("dude");
                wasCalled.ShouldBeTrue();
            }

            [Fact]
            public void should_set_help_requested_if_command_name_is_starts_with_a_prefix()
            {
                args.Add("/dude");
                var wasCalled = false;
                afterParse = list =>
                {
                    wasCalled = true;
                    list.ShouldContain(args.First());
                };

                because();

                config.CommandName.ShouldNotEqual("dude");
                config.HelpRequested.ShouldBeTrue();
                wasCalled.ShouldBeTrue();
            }

            [Fact]
            public void should_add_an_option_for_bob_when_specified()
            {
                setOptions = set => { set.Add("bob", "sets the bob switch", option => config.Verbose = option != null); };
                because();

                config.Verbose.ShouldBeFalse();
            }

            [Fact]
            public void should_set_option_for_tim_to_true_when_specified_with_dash()
            {
                setOptions = set => { set.Add("tim", "sets the tim switch", option => config.Verbose = option != null); };
                args.Add("-tim");

                because();

                config.Verbose.ShouldBeTrue();
            }

            [Fact]
            public void should_set_option_for_tina_to_true_when_specified_with_two_dashes()
            {
                setOptions = set => { set.Add("tina", "sets the tina switch", option => config.Verbose = option != null); };
                args.Add("--tina");
                because();

                config.Verbose.ShouldBeTrue();
            }

            [Fact]
            public void should_set_option_for_gena_to_true_when_specified_with_forward_slash()
            {
                setOptions = set => { set.Add("gena", "sets the gena switch", option => config.Verbose = option != null); };
                args.Add("/gena");

                because();

                config.Verbose.ShouldBeTrue();
            }

            [Fact]
            public void should_set_option_when_specified_as_single_dash_for_timmy_and_other_option_short_values_are_passed_the_same_way()
            {
                setOptions = set =>
                {
                    set.Add("timmy", "sets the timmy switch", option => config.Verbose = option != null);
                    set.Add("s|skip", "sets the skip switch", option => config.SkipPackageInstallProvider = option != null);
                    set.Add("d|debug", "sets the debug switch", option => config.Debug = option != null);
                };
                args.Add("-timmy");
                args.Add("-sd");

                because();

                config.SkipPackageInstallProvider.ShouldBeTrue();
                config.Debug.ShouldBeTrue();
                config.Verbose.ShouldBeTrue();
            }

            [Fact]
            public void should_set_option_when_specified_as_single_dash_for_lo_and_other_option_short_values_are_passed_the_same_way()
            {
                setOptions = set =>
                {
                    set.Add("lo|local-only", "sets the lo switch", option => config.ListCommand.LocalOnly = option != null);
                    set.Add("l|lskip", "sets the skip switch", option => config.SkipPackageInstallProvider = option != null);
                    set.Add("m|mdebug", "sets the debug switch", option => config.Debug = option != null);
                };
                args.Add("-lo");
                args.Add("-ml");

                because();

                config.SkipPackageInstallProvider.ShouldBeTrue();
                config.Debug.ShouldBeTrue();
                config.ListCommand.LocalOnly.ShouldBeTrue();
                helpMessageContents.ToString().ShouldBeEmpty();
            }

            [Fact]
            public void should_show_help_menu_when_passing_bundled_options_that_do_not_exist()
            {
                setOptions = set => { set.Add("w|wdebug", "sets the debug switch", option => config.Debug = option != null); };
                args.Add("-wz");

                because();

                config.Debug.ShouldBeFalse();
                helpMessageContents.ToString().ShouldNotBeEmpty();
            }

            [Fact]
            public void should_successfully_parse_help_option()
            {
                args.Add("-h");

                because();

                config.UnsuccessfulParsing.ShouldBeFalse();
            }

            [Fact]
            public void should_not_parse_unknown_option()
            {
                args.Add("-unknown");

                because();

                config.UnsuccessfulParsing.ShouldBeTrue();
            }
        }
    }
}
