namespace chocolatey.tests.infrastructure.commands
{
    using System;
    using System.Collections.Generic;
    using Should;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.builders;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.commands;

    public class ExternalCommandArgsBuilderSpecs
    {
        public class when_using_ExternalCommandArgsBuilder : TinySpec
        {
            private Func<string> buildConfigs;
            protected IDictionary<string, ExternalCommandArgument> argsDictionary = new Dictionary<string, ExternalCommandArgument>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                configuration.Source = "yo";
                configuration.Verbose = true;
                configuration.CommandName = "with a space";
            }

            public override void Because()
            {
                buildConfigs = () => ExternalCommandArgsBuilder.build_arguments(configuration, argsDictionary);
            }

            [Fact]
            public void should_add_a_parameter_if_property_value_is_set()
            {
                argsDictionary.Clear();
                argsDictionary.Add("Source", new ExternalCommandArgument { ArgumentOption = "-source " });
                buildConfigs().ShouldEqual("-source " + configuration.Source);
            }

            [Fact]
            public void should_skip_a_parameter_that_does_not_match_the_case_of_the_property_name_exactly()
            {
                argsDictionary.Clear();
                argsDictionary.Add("source", new ExternalCommandArgument { ArgumentOption = "-source " });
                buildConfigs().ShouldEqual("");
            } 
            
            [Fact]
            public void should_add_a_parameter_that_does_not_match_the_case_of_the_property_name_when_dictionary_ignores_case()
            {
                IDictionary<string, ExternalCommandArgument> ignoreCaseDictionary = new Dictionary<string, ExternalCommandArgument>(StringComparer.InvariantCultureIgnoreCase);
                ignoreCaseDictionary.Add("source", new ExternalCommandArgument { ArgumentOption = "-source " });
                ExternalCommandArgsBuilder.build_arguments(configuration, ignoreCaseDictionary).ShouldEqual("-source yo");
            }

            [Fact]
            public void should_not_override_ArgumentValue_with_the_property_value_for_a_parameter()
            {
                argsDictionary.Clear();
                argsDictionary.Add("Source", new ExternalCommandArgument { ArgumentOption = "-source ", ArgumentValue = "bob" });
                buildConfigs().ShouldEqual("-source bob");
            }

            [Fact]
            public void should_skip_a_parameter_if_property_value_has_no_value()
            {
                argsDictionary.Clear();
                argsDictionary.Add("Version", new ExternalCommandArgument { ArgumentOption = "-version " });
                buildConfigs().ShouldEqual("");
            }

            [Fact]
            public void should_add_a_parameter_when_Required_set_true_even_if_property_has_no_value()
            {
                argsDictionary.Clear();
                argsDictionary.Add("Version", new ExternalCommandArgument { ArgumentOption = "install", Required = true });
                buildConfigs().ShouldEqual("install");
            }

            [Fact]
            public void should_skip_a_parameter_not_found_in_the_properties_object()
            {
                argsDictionary.Clear();
                argsDictionary.Add("_install_", new ExternalCommandArgument { ArgumentOption = "install" });
                buildConfigs().ShouldEqual("");
            }

            [Fact]
            public void should_add_a_parameter_not_found_in_the_properties_object_when_Required_set_to_true()
            {
                argsDictionary.Clear();
                argsDictionary.Add("_install_", new ExternalCommandArgument { ArgumentOption = "install", Required = true });
                buildConfigs().ShouldEqual("install");
            }

            [Fact]
            public void should_add_a_boolean_as_a_switch_when_true()
            {
                argsDictionary.Clear();
                argsDictionary.Add("Verbose", new ExternalCommandArgument { ArgumentOption = "-verbose" });
                buildConfigs().ShouldEqual("-verbose");
            }

            [Fact]
            public void should_skip_a_boolean_as_a_switch_when_false()
            {
                argsDictionary.Clear();
                argsDictionary.Add("Prerelease", new ExternalCommandArgument { ArgumentOption = "-pre" });
                buildConfigs().ShouldEqual("");
            }

            [Fact]
            public void should_quote_a_value_when_QuoteValue_is_set_to_true()
            {
                argsDictionary.Clear();
                argsDictionary.Add("Source", new ExternalCommandArgument { ArgumentOption = "-source ", QuoteValue = true });
                buildConfigs().ShouldEqual("-source \"yo\"");
            }

            [Fact]
            public void should_auto_quote_an_argument_value_with_spaces()
            {
                argsDictionary.Clear();
                argsDictionary.Add("CommandName", new ExternalCommandArgument { ArgumentOption = "-command " });
                buildConfigs().ShouldEqual("-command \"{0}\"".format_with(configuration.CommandName));
            }

            [Fact]
            public void should_not_quote_an_argument_option_with_spaces()
            {
                argsDictionary.Clear();
                argsDictionary.Add("Source", new ExternalCommandArgument { ArgumentOption = "-source you know = ", QuoteValue = true });
                buildConfigs().ShouldEqual("-source you know = \"yo\"");
            }

            [Fact]
            public void should_use_only_the_value_when_UseValueOnly_is_set_to_true()
            {
                argsDictionary.Clear();
                argsDictionary.Add("Source", new ExternalCommandArgument { ArgumentOption = "-source ", UseValueOnly = true });
                buildConfigs().ShouldEqual("yo");
            }   
            
            [Fact]
            public void should_use_only_the_value_when_UseValueOnly_and_Required_is_set_to_true()
            {
                argsDictionary.Clear();
                argsDictionary.Add("_source_", new ExternalCommandArgument { ArgumentOption = "-source ", ArgumentValue = "bob", UseValueOnly = true, Required = true});
                buildConfigs().ShouldEqual("bob");
            }

            [Fact]
            public void should_not_add_a_value_when_UseValueOnly_is_set_to_true_and_no_value_is_set()
            {
                argsDictionary.Clear();
                argsDictionary.Add("Version", new ExternalCommandArgument { ArgumentOption = "-version ", UseValueOnly = true });
                buildConfigs().ShouldEqual("");
            }

            [Fact]
            public void should_separate_arguments_by_one_space()
            {
                argsDictionary.Clear();
                argsDictionary.Add("_install_", new ExternalCommandArgument { ArgumentOption = "install", Required = true });
                argsDictionary.Add("Source", new ExternalCommandArgument { ArgumentOption = "-source " });
                buildConfigs().ShouldEqual("install -source yo");
            }

            [Fact]
            public void should_add_items_in_order_based_on_the_dictionary()
            {
                argsDictionary.Clear();
                argsDictionary.Add("_install_", new ExternalCommandArgument { ArgumentOption = "install", Required = true });
                argsDictionary.Add("_output_directory_", new ExternalCommandArgument
                {
                    ArgumentOption = "-outputdirectory ",
                    ArgumentValue = "bob",
                    QuoteValue = true,
                    Required = true
                });
                argsDictionary.Add("Source", new ExternalCommandArgument { ArgumentOption = "-source ", QuoteValue = true });
                argsDictionary.Add("_non_interactive_", new ExternalCommandArgument { ArgumentOption = "-noninteractive", Required = true });
                argsDictionary.Add("_no_cache_", new ExternalCommandArgument { ArgumentOption = "-nocache", Required = true });

                buildConfigs().ShouldEqual("install -outputdirectory \"bob\" -source \"{0}\" -noninteractive -nocache".format_with(configuration.Source));
            }
        }
    }
}