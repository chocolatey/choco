namespace chocolatey.tests.infrastructure.app.commands
{
    using System;
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

    public class ChocolateyPushCommandSpecs
    {
        public abstract class ChocolateyPushCommandSpecsBase : TinySpec
        {
            protected ChocolateyPushCommand command;
            protected Mock<IChocolateyPackageService> packageService = new Mock<IChocolateyPackageService>();
            protected Mock<IChocolateyConfigSettingsService> configSettingsService = new Mock<IChocolateyConfigSettingsService>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                configuration.Source = "https://localhost/somewhere/out/there";
                command = new ChocolateyPushCommand(packageService.Object, configSettingsService.Object);
            }
        }

        public class when_implementing_command_for : ChocolateyPushCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof (CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void should_implement_push()
            {
                results.ShouldContain(CommandNameType.push.to_string());
            }
        }

        public class when_configurating_the_argument_parser : ChocolateyPushCommandSpecsBase
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
            public void should_clear_previously_set_Source()
            {
                configuration.Source.ShouldBeNull();
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
            public void should_add_apikey_to_the_option_set()
            {
                optionSet.Contains("apikey").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_apikey_to_the_option_set()
            {
                optionSet.Contains("k").ShouldBeTrue();
            }

            [Fact]
            public void should_add_timeout_to_the_option_set()
            {
                optionSet.Contains("timeout").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_timeout_to_the_option_set()
            {
                optionSet.Contains("t").ShouldBeTrue();
            }
        }

        public class when_handling_additional_argument_parsing : ChocolateyPushCommandSpecsBase
        {
            private readonly IList<string> unparsedArgs = new List<string>();
            private const string apiKey = "bobdaf";
            private Action because;

            public override void Because()
            {
                because = () => command.handle_additional_argument_parsing(unparsedArgs, configuration);
            }

            public void reset()
            {
                unparsedArgs.Clear();
                configSettingsService.ResetCalls();
            }

            [Fact]
            public void should_allow_a_path_to_the_nupkg_to_be_passed_in()
            {
                reset();
                string nupkgPath = "./some/path/to.nupkg";
                unparsedArgs.Add(nupkgPath);
                because();
                configuration.Input.ShouldEqual(nupkgPath);
            }

            [Fact]
            public void should_not_set_the_apiKey_if_source_is_unset()
            {
                reset();
                configSettingsService.Setup(c => c.get_api_key(configuration, null)).Returns(apiKey);
                configuration.PushCommand.Key = "";
                configuration.Source = "";
                because();

                configuration.PushCommand.Key.ShouldEqual("");
                configSettingsService.Verify(c => c.get_api_key(It.IsAny<ChocolateyConfiguration>(), It.IsAny<Action<ConfigFileApiKeySetting>>()), Times.Never);
            }

            [Fact]
            public void should_not_set_the_apiKey_if_source_is_not_found()
            {
                reset();
                configSettingsService.Setup(c => c.get_api_key(configuration, null)).Returns("");
                configuration.PushCommand.Key = "";
                configuration.Source = "https://localhost/somewhere/out/there";
                because();

                configuration.PushCommand.Key.ShouldEqual("");
            }

            [Fact]
            public void should_not_try_to_determine_the_key_if_passed_in_as_an_argument()
            {
                reset();
                configSettingsService.Setup(c => c.get_api_key(configuration, null)).Returns("");
                configuration.PushCommand.Key = "bob";
                configuration.Source = "https://localhost/somewhere/out/there";
                because();

                configuration.PushCommand.Key.ShouldEqual("bob");
                configSettingsService.Verify(c => c.get_api_key(It.IsAny<ChocolateyConfiguration>(), It.IsAny<Action<ConfigFileApiKeySetting>>()), Times.Never);
            }

            [Fact]
            public void should_not_try_to_determine_the_key_if_source_is_set_for_a_local_source()
            {
                reset();
                configuration.Source = "c:\\packages";
                configuration.PushCommand.Key = "";
                because();

                configSettingsService.Verify(c => c.get_api_key(It.IsAny<ChocolateyConfiguration>(), It.IsAny<Action<ConfigFileApiKeySetting>>()), Times.Never);
            }

            [Fact]
            public void should_not_try_to_determine_the_key_if_source_is_set_for_an_unc_source()
            {
                reset();
                configuration.Source = "\\\\someserver\\packages";
                configuration.PushCommand.Key = "";
                because();

                configSettingsService.Verify(c => c.get_api_key(It.IsAny<ChocolateyConfiguration>(), It.IsAny<Action<ConfigFileApiKeySetting>>()), Times.Never);
            }
        }

        public class when_handling_validation : ChocolateyPushCommandSpecsBase
        {
            private Action because;

            public override void Because()
            {
                because = () => command.handle_validation(configuration);
            }

            [Fact]
            public void should_throw_when_source_is_not_set()
            {
                configuration.Source = "";
                var errorred = false;
                Exception error = null;

                try
                {
                    because();
                }
                catch (Exception ex)
                {
                    errorred = true;
                    error = ex;
                }

                errorred.ShouldBeTrue();
                error.ShouldNotBeNull();
                error.ShouldBeType<ApplicationException>();
                error.Message.ShouldContain("Source is required.");
            }

            [Fact]
            public void should_throw_when_apiKey_has_not_been_set_or_determined_for_a_https_source()
            {
                configuration.Source = "https://somewhere/out/there";
                configuration.PushCommand.Key = "";
                var errorred = false;
                Exception error = null;

                try
                {
                    because();
                }
                catch (Exception ex)
                {
                    errorred = true;
                    error = ex;
                }

                errorred.ShouldBeTrue();
                error.ShouldNotBeNull();
                error.ShouldBeType<ApplicationException>();
                error.Message.ShouldContain("ApiKey was not found");
            }

            [Fact]
            public void should_continue_when_source_and_apikey_is_set_for_a_https_source()
            {
                configuration.Source = "https://somewhere/out/there";
                configuration.PushCommand.Key = "bob";
                because();
            }

            [Fact]
            public void should_continue_when_source_is_set_for_a_local_source()
            {
                configuration.Source = "c:\\packages";
                configuration.PushCommand.Key = "";
                because();
            }

            [Fact]
            public void should_continue_when_source_is_set_for_an_unc_source()
            {
                configuration.Source = "\\\\someserver\\packages";
                configuration.PushCommand.Key = "";
                because();
            }

            [Fact]
            public void should_throw_when_source_is_http_and_not_secure()
            {
                configuration.Source = "http://somewhere/out/there";
                configuration.PushCommand.Key = "bob";
                configuration.Force = false;
                var errorred = false;
                Exception error = null;

                try
                {
                    because();
                }
                catch (Exception ex)
                {
                    errorred = true;
                    error = ex;
                }

                errorred.ShouldBeTrue();
                error.ShouldNotBeNull();
                error.ShouldBeType<ApplicationException>();
                error.Message.ShouldContain("WARNING! The specified source '{0}' is not secure".format_with(configuration.Source));
            }

            [Fact]
            public void should_continue_when_source_is_http_and_not_secure_if_force_is_passed()
            {
                configuration.Source = "http://somewhere/out/there";
                configuration.PushCommand.Key = "bob";
                configuration.Force = true;

                because();
            }
        }

        public class when_noop_is_called : ChocolateyPushCommandSpecsBase
        {
            public override void Because()
            {
                command.noop(configuration);
            }

            [Fact]
            public void should_call_service_push_noop()
            {
                packageService.Verify(c => c.push_noop(configuration), Times.Once);
            }
        }

        public class when_run_is_called : ChocolateyPushCommandSpecsBase
        {
            public override void Because()
            {
                command.run(configuration);
            }

            [Fact]
            public void should_call_service_push_run()
            {
                packageService.Verify(c => c.push_run(configuration), Times.Once);
            }
        }
    }
}