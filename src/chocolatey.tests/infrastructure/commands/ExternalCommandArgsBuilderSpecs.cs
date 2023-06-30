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

namespace chocolatey.tests.infrastructure.commands
{
    using System;
    using System.Collections.Generic;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.commands;
    using FluentAssertions;

    public class ExternalCommandArgsBuilderSpecs
    {
        public class When_using_ExternalCommandArgsBuilder : TinySpec
        {
            private Func<string> _buildConfigs;
            protected IDictionary<string, ExternalCommandArgument> ArgsDictionary = new Dictionary<string, ExternalCommandArgument>();
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                Configuration.Sources = "yo";
                Configuration.Verbose = true;
                Configuration.ApiKeyCommand.Key = "dude";
                Configuration.CommandName = "with a space";
            }

            public override void Because()
            {
                _buildConfigs = () => ExternalCommandArgsBuilder.BuildArguments(Configuration, ArgsDictionary);
            }

            [Fact]
            public void Should_add_a_parameter_if_property_value_is_set()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "Sources",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-source "
                    });
                _buildConfigs().Should().Be("-source " + Configuration.Sources);
            }

            [Fact]
            public void Should_add_a_parameter_if_property_value_is_sub_property()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "ApiKeyCommand.Key",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-apikey "
                    });
                _buildConfigs().Should().Be("-apikey " + Configuration.ApiKeyCommand.Key);
            }

            [Fact]
            public void Should_skip_a_parameter_that_does_not_match_the_case_of_the_property_name_exactly()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "sources",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-source "
                    });
                _buildConfigs().Should().Be("");
            }

            [Fact]
            public void Should_add_a_parameter_that_does_not_match_the_case_of_the_property_name_when_dictionary_ignores_case()
            {
                IDictionary<string, ExternalCommandArgument> ignoreCaseDictionary = new Dictionary<string, ExternalCommandArgument>(StringComparer.InvariantCultureIgnoreCase);
                ignoreCaseDictionary.Add(
                    "sources",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-source "
                    });
                ExternalCommandArgsBuilder.BuildArguments(Configuration, ignoreCaseDictionary).Should().Be("-source yo");
            }

            [Fact]
            public void Should_not_override_ArgumentValue_with_the_property_value_for_a_parameter()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "Sources",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-source ",
                        ArgumentValue = "bob"
                    });
                _buildConfigs().Should().Be("-source bob");
            }

            [Fact]
            public void Should_skip_a_parameter_if_property_value_has_no_value()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "Version",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-version "
                    });
                _buildConfigs().Should().Be("");
            }

            [Fact]
            public void Should_add_a_parameter_when_Required_set_true_even_if_property_has_no_value()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "Version",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "install",
                        Required = true
                    });
                _buildConfigs().Should().Be("install");
            }

            [Fact]
            public void Should_skip_a_parameter_not_found_in_the_properties_object()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "_install_",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "install"
                    });
                _buildConfigs().Should().Be("");
            }

            [Fact]
            public void Should_add_a_parameter_not_found_in_the_properties_object_when_Required_set_to_true()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "_install_",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "install",
                        Required = true
                    });
                _buildConfigs().Should().Be("install");
            }

            [Fact]
            public void Should_add_a_boolean_as_a_switch_when_true()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "Verbose",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-verbose"
                    });
                _buildConfigs().Should().Be("-verbose");
            }

            [Fact]
            public void Should_skip_a_boolean_as_a_switch_when_false()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "Prerelease",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-pre"
                    });
                _buildConfigs().Should().Be("");
            }

            [Fact]
            public void Should_quote_a_value_when_QuoteValue_is_set_to_true()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "Sources",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-source ",
                        QuoteValue = true
                    });
                _buildConfigs().Should().Be("-source \"yo\"");
            }

            [Fact]
            public void Should_auto_quote_an_argument_value_with_spaces()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "CommandName",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-command "
                    });
                _buildConfigs().Should().Be("-command \"{0}\"".FormatWith(Configuration.CommandName));
            }

            [Fact]
            public void Should_not_quote_an_argument_option_with_spaces()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "Sources",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-source you know = ",
                        QuoteValue = true
                    });
                _buildConfigs().Should().Be("-source you know = \"yo\"");
            }

            [Fact]
            public void Should_use_only_the_value_when_UseValueOnly_is_set_to_true()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "Sources",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-source ",
                        UseValueOnly = true
                    });
                _buildConfigs().Should().Be("yo");
            }

            [Fact]
            public void Should_use_only_the_value_when_UseValueOnly_and_Required_is_set_to_true()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "_source_",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-source ",
                        ArgumentValue = "bob",
                        UseValueOnly = true,
                        Required = true
                    });
                _buildConfigs().Should().Be("bob");
            }

            [Fact]
            public void Should_not_add_a_value_when_UseValueOnly_is_set_to_true_and_no_value_is_set()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "Version",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-version ",
                        UseValueOnly = true
                    });
                _buildConfigs().Should().Be("");
            }

            [Fact]
            public void Should_separate_arguments_by_one_space()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "_install_",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "install",
                        Required = true
                    });
                ArgsDictionary.Add(
                    "Sources",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-source "
                    });
                _buildConfigs().Should().Be("install -source yo");
            }

            [Fact]
            public void Should_add_items_in_order_based_on_the_dictionary()
            {
                ArgsDictionary.Clear();
                ArgsDictionary.Add(
                    "_install_",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "install",
                        Required = true
                    });
                ArgsDictionary.Add(
                    "_output_directory_",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-outputdirectory ",
                        ArgumentValue = "bob",
                        QuoteValue = true,
                        Required = true
                    });
                ArgsDictionary.Add(
                    "Sources",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-source ",
                        QuoteValue = true
                    });
                ArgsDictionary.Add(
                    "_non_interactive_",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-noninteractive",
                        Required = true
                    });
                ArgsDictionary.Add(
                    "_no_cache_",
                    new ExternalCommandArgument
                    {
                        ArgumentOption = "-nocache",
                        Required = true
                    });

                _buildConfigs().Should().Be("install -outputdirectory \"bob\" -source \"{0}\" -noninteractive -nocache".FormatWith(Configuration.Sources));
            }
        }
    }
}
