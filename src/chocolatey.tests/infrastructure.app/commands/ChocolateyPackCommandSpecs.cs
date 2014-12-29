namespace chocolatey.tests.infrastructure.app.commands
{
    using System.Collections.Generic;
    using System.Linq;
    using Moq;
    using Should;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commandline;

    public class ChocolateyPackCommandSpecs
    {
        public abstract class ChocolateyPackCommandSpecsBase : TinySpec
        {
            protected ChocolateyPackCommand command;
            protected Mock<IChocolateyPackageService> packageService = new Mock<IChocolateyPackageService>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                command = new ChocolateyPackCommand(packageService.Object);
            }
        }

        public class when_implementing_command_for : ChocolateyPackCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof (CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void should_implement_pack()
            {
                results.ShouldContain(CommandNameType.pack.to_string());
            }
        }

        public class when_configurating_the_argument_parser : ChocolateyPackCommandSpecsBase
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
            public void should_add_version_to_the_option_set()
            {
                optionSet.Contains("version").ShouldBeTrue();
            }
        }

        public class when_handling_additional_argument_parsing : ChocolateyPackCommandSpecsBase
        {
            private readonly IList<string> unparsedArgs = new List<string>();
            private const string nuspecPath = "./some/path/to.nuspec";

            public override void Context()
            {
                base.Context();
                unparsedArgs.Add(nuspecPath);
            }

            public override void Because()
            {
                command.handle_additional_argument_parsing(unparsedArgs, configuration);
            }

            [Fact]
            public void should_allow_a_path_to_the_nuspec_to_be_passed_in()
            {
                configuration.Input.ShouldEqual(nuspecPath);
            }
        }

        public class when_noop_is_called : ChocolateyPackCommandSpecsBase
        {
            public override void Because()
            {
                command.noop(configuration);
            }

            [Fact]
            public void should_call_service_package_noop()
            {
                packageService.Verify(c => c.pack_noop(configuration), Times.Once);
            }
        }

        public class when_run_is_called : ChocolateyPackCommandSpecsBase
        {
            public override void Because()
            {
                command.run(configuration);
            }

            [Fact]
            public void should_call_service_pack_run()
            {
                packageService.Verify(c => c.pack_run(configuration), Times.Once);
            }
        }
    }
}