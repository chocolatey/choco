namespace chocolatey.tests.infrastructure.app.commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Moq;
    using Should;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commandline;

    public class ChocolateyListCommandSpecs
    {
        public abstract class ChocolateyListCommandSpecsBase : TinySpec
        {
            protected ChocolateyListCommand command;
            protected Mock<IChocolateyPackageService> packageService = new Mock<IChocolateyPackageService>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                configuration.Source = "bob";
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
                results.ShouldContain(CommandNameType.list.to_string());
            }

            [Fact]
            public void should_implement_search()
            {
                results.ShouldContain(CommandNameType.search.to_string());
            }
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
            public void should_add_localonly_to_the_option_set()
            {
                optionSet.Contains("localonly").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_localonly_to_the_option_set()
            {
                optionSet.Contains("l").ShouldBeTrue();
            }

            [Fact]
            public void should_add_includeprograms_to_the_option_set()
            {
                optionSet.Contains("includeprograms").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_includeprograms_to_the_option_set()
            {
                optionSet.Contains("p").ShouldBeTrue();
            }

            [Fact]
            public void should_add_allversions_to_the_option_set()
            {
                optionSet.Contains("allversions").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_allversions_to_the_option_set()
            {
                optionSet.Contains("a").ShouldBeTrue();
            }
        }

        public class when_handling_additional_argument_parsing : ChocolateyListCommandSpecsBase
        {
            private IList<string> unparsedArgs = new List<string>();
            private string source = "https://somewhereoutthere";
            private Action because;

            public override void Context()
            {
                base.Context();
                unparsedArgs.Add("pkg1");
                unparsedArgs.Add("pkg2");
                configuration.Source = source;
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

            [Fact]
            public void should_leave_source_as_set()
            {
                configuration.LocalOnly = false;
                because();
                configuration.Source.ShouldEqual(source);
            }
            
            [Fact]
            public void should_set_source_to_local_location_when_localonly_is_true()
            {
                configuration.LocalOnly = true;
                because();
                configuration.Source.ShouldEqual(ApplicationParameters.PackagesLocation);
            }
        }   

        public class when_noop_is_called : ChocolateyListCommandSpecsBase
        {
            public override void Because()
            {
                command.noop(configuration);
            }

            [Fact]
            public void should_call_service_list_noop()
            {
                packageService.Verify(c => c.list_noop(configuration), Times.Once);
            }
        }

        public class when_run_is_called : ChocolateyListCommandSpecsBase
        {
            public override void Because()
            {
                command.run(configuration);
            }

            [Fact]
            public void should_call_service_list_run()
            {
                packageService.Verify(c => c.list_run(configuration,true), Times.Once);
            }
        }
    }
}