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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.app.templates;
    using chocolatey.infrastructure.commandline;
    using Moq;
    using Should;

    public class ChocolateyNewCommandSpecs
    {
        public abstract class ChocolateyNewCommandSpecsBase : TinySpec
        {
            protected ChocolateyNewCommand command;
            protected Mock<ITemplateService> templateService = new Mock<ITemplateService>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                command = new ChocolateyNewCommand(templateService.Object);
            }
        }

        public class when_implementing_command_for : ChocolateyNewCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void should_implement_new()
            {
                results.ShouldContain("new");
            }
        }

        public class when_configurating_the_argument_parser : ChocolateyNewCommandSpecsBase
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
            public void should_add_automaticpackage_to_the_option_set()
            {
                optionSet.Contains("automaticpackage").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_automaticpackage_to_the_option_set()
            {
                optionSet.Contains("a").ShouldBeTrue();
            }

            [Fact]
            public void should_add_name_to_the_option_set()
            {
                optionSet.Contains("name").ShouldBeTrue();
            }

            [Fact]
            public void should_add_version_to_the_option_set()
            {
                optionSet.Contains("version").ShouldBeTrue();
            }

            [Fact]
            public void should_add_maintainer_to_the_option_set()
            {
                optionSet.Contains("maintainer").ShouldBeTrue();
            }

            [Fact]
            public void should_add_outputdirectory_to_the_option_set()
            {
                optionSet.Contains("outputdirectory").ShouldBeTrue();
            }
        }

        public class when_handling_additional_argument_parsing : ChocolateyNewCommandSpecsBase
        {
            private readonly IList<string> unparsedArgs = new List<string>();
            private Action because;

            public override void Because()
            {
                because = () => command.handle_additional_argument_parsing(unparsedArgs, configuration);
            }

            private void reset()
            {
                configuration.NewCommand.Name = "";
                unparsedArgs.Clear();
                configuration.NewCommand.TemplateProperties.Clear();
            }

            [Fact]
            public void should_not_set_template_properties_if_none_have_been_defined()
            {
                reset();
                because();
                configuration.NewCommand.TemplateProperties.Count.ShouldEqual(0);
            }

            [Fact]
            public void should_set_template_properties_when_args_are_separated_by_equals()
            {
                reset();
                unparsedArgs.Add("bob=new");
                because();

                var properties = configuration.NewCommand.TemplateProperties;
                properties.Count.ShouldEqual(1);
                var templateProperty = properties.FirstOrDefault();
                templateProperty.Key.ShouldEqual("bob");
                templateProperty.Value.ShouldEqual("new");
            }

            [Fact]
            public void should_set_template_properties_only_once()
            {
                reset();
                unparsedArgs.Add("bob=one");
                unparsedArgs.Add("bob=two");
                because();

                var properties = configuration.NewCommand.TemplateProperties;
                properties.Count.ShouldEqual(1);
                var templateProperty = properties.FirstOrDefault();
                templateProperty.Key.ShouldEqual("bob");
                templateProperty.Value.ShouldEqual("one");
            }

            [Fact]
            public void should_ignore_casing_differences_when_setting_template_properties()
            {
                reset();
                unparsedArgs.Add("bob=one");
                unparsedArgs.Add("Bob=two");
                because();

                var properties = configuration.NewCommand.TemplateProperties;
                properties.Count.ShouldEqual(1);
                var templateProperty = properties.FirstOrDefault();
                templateProperty.Key.ShouldEqual("bob");
                templateProperty.Value.ShouldEqual("one");
            }

            [Fact]
            public void should_not_set_template_properties_when_args_are_not_separated_by_equals()
            {
                reset();
                configuration.NewCommand.Name = "bill";
                configuration.NewCommand.TemplateProperties.Add(TemplateValues.NamePropertyName, "bill");
                unparsedArgs.Add("bob new");
                because();

                var properties = configuration.NewCommand.TemplateProperties;
                properties.Count.ShouldEqual(1);
                var templateProperty = properties.FirstOrDefault();
                templateProperty.Key.ShouldEqual("PackageName");
                templateProperty.Value.ShouldEqual("bill");
            }

            [Fact]
            public void should_not_set_override_configuration_Name_when_unparsed_without_equals()
            {
                reset();
                configuration.NewCommand.Name = "bill";
                configuration.NewCommand.TemplateProperties.Add(TemplateValues.NamePropertyName, "bill");
                unparsedArgs.Add("bob");
                because();

                var properties = configuration.NewCommand.TemplateProperties;
                properties.Count.ShouldEqual(1);
                var templateProperty = properties.FirstOrDefault();
                templateProperty.Key.ShouldEqual("PackageName");
                templateProperty.Value.ShouldEqual("bill");
            }

            [Fact]
            public void should_not_set_override_configuration_Name_when_package_name_is_also_passed()
            {
                reset();
                configuration.NewCommand.Name = "bill";
                configuration.NewCommand.TemplateProperties.Add(TemplateValues.NamePropertyName, "bill");
                unparsedArgs.Add(TemplateValues.NamePropertyName + "=bob");
                because();

                var properties = configuration.NewCommand.TemplateProperties;
                var templateProperty = properties.FirstOrDefault();
                templateProperty.Key.ShouldEqual("PackageName");
                templateProperty.Value.ShouldEqual("bill");
            }

            [Fact]
            public void should_set_template_properties_when_args_are_separated_by_equals_with_spaces()
            {
                reset();
                unparsedArgs.Add("bob = new");
                because();

                var properties = configuration.NewCommand.TemplateProperties;
                properties.Count.ShouldEqual(1);
                var templateProperty = properties.FirstOrDefault();
                templateProperty.Key.ShouldEqual("bob");
                templateProperty.Value.ShouldEqual("new");
            }

            [Fact]
            public void should_set_template_properties_without_surrounding_quotes()
            {
                reset();
                unparsedArgs.Add("bob = \"new this\"");
                because();

                var properties = configuration.NewCommand.TemplateProperties;
                properties.Count.ShouldEqual(1);
                var templateProperty = properties.FirstOrDefault();
                templateProperty.Key.ShouldEqual("bob");
                templateProperty.Value.ShouldEqual("new this");
            }

            [Fact]
            public void should_set_template_properties_without_removing_quote()
            {
                reset();
                unparsedArgs.Add("bob = 'new \"this'");
                because();
                var properties = configuration.NewCommand.TemplateProperties;

                properties.Count.ShouldEqual(1);
                var templateProperty = properties.FirstOrDefault();
                templateProperty.Key.ShouldEqual("bob");
                templateProperty.Value.ShouldEqual("new \"this");
            }

            [Fact]
            public void should_set_template_properties_without_surrounding_apostrophes()
            {
                reset();
                unparsedArgs.Add("bob = 'new this'");
                because();
                var properties = configuration.NewCommand.TemplateProperties;

                properties.Count.ShouldEqual(1);
                var templateProperty = properties.FirstOrDefault();
                templateProperty.Key.ShouldEqual("bob");
                templateProperty.Value.ShouldEqual("new this");
            }

            [Fact]
            public void should_set_template_properties_without_removing_apostrophe()
            {
                reset();
                unparsedArgs.Add("bob = \"new 'this\"");
                because();
                var properties = configuration.NewCommand.TemplateProperties;

                properties.Count.ShouldEqual(1);
                var templateProperty = properties.FirstOrDefault();
                templateProperty.Key.ShouldEqual("bob");
                templateProperty.Value.ShouldEqual("new 'this");
            }
        }

        public class when_handling_validation : ChocolateyNewCommandSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void should_throw_when_Name_is_not_set()
            {
                configuration.NewCommand.Name = "";
                var errorred = false;
                Exception error = null;

                try
                {
                    command.handle_validation(configuration);
                }
                catch (Exception ex)
                {
                    errorred = true;
                    error = ex;
                }

                errorred.ShouldBeTrue();
                error.ShouldNotBeNull();
                error.ShouldBeType<ApplicationException>();
            }

            [Fact]
            public void should_continue_when_Name_is_set()
            {
                configuration.NewCommand.Name = "bob";
                command.handle_validation(configuration);
            }
        }

        public class when_noop_is_called : ChocolateyNewCommandSpecsBase
        {
            public override void Because()
            {
                command.noop(configuration);
            }

            [Fact]
            public void should_call_service_noop()
            {
                templateService.Verify(c => c.noop(configuration), Times.Once);
            }
        }

        public class when_run_is_called : ChocolateyNewCommandSpecsBase
        {
            public override void Because()
            {
                command.run(configuration);
            }

            [Fact]
            public void should_call_service_generate()
            {
                templateService.Verify(c => c.generate(configuration), Times.Once);
            }
        }

        public class when_handling_arguments_parsing : ChocolateyNewCommandSpecsBase
        {
            private OptionSet optionSet;

            public override void Context()
            {
                base.Context();
                optionSet = new OptionSet();
                command.configure_argument_parser(optionSet, configuration);
            }

            public override void Because()
            {
                optionSet.Parse(new[] { "--name", "Bob", "--automaticpackage", "--template-name", "custom", "--version", "0.42.0", "--maintainer", "Loyd", "--outputdirectory", "c:\\packages" });
            }

            [Fact]
            public void should_name_equal_to_Bob()
            {
                configuration.NewCommand.Name.ShouldEqual("Bob");
                configuration.NewCommand.TemplateProperties[TemplateValues.NamePropertyName].ShouldEqual("Bob");
            }

            [Fact]
            public void should_automaticpackage_equal_to_true()
            {
                configuration.NewCommand.AutomaticPackage.ShouldBeTrue();
            }

            [Fact]
            public void should_templatename_equal_to_custom()
            {
                configuration.NewCommand.TemplateName.ShouldEqual("custom");
            }

            [Fact]
            public void should_version_equal_to_42()
            {
                configuration.NewCommand.TemplateProperties[TemplateValues.VersionPropertyName].ShouldEqual("0.42.0");
            }

            [Fact]
            public void should_maintainer_equal_to_Loyd()
            {
                configuration.NewCommand.TemplateProperties[TemplateValues.MaintainerPropertyName].ShouldEqual("Loyd");
            }

            [Fact]
            public void should_outputdirectory_equal_packages()
            {
                configuration.OutputDirectory.ShouldEqual("c:\\packages");
            }
        }
    }
}
