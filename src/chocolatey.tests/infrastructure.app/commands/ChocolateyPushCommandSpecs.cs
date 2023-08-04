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

namespace chocolatey.tests.infrastructure.app.commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commandline;
    using Moq;
    using NUnit.Framework;
    using FluentAssertions;

    public class ChocolateyPushCommandSpecs
    {
        [ConcernFor("push")]
        public abstract class ChocolateyPushCommandSpecsBase : TinySpec
        {
            protected ChocolateyPushCommand Command;
            protected Mock<IChocolateyPackageService> PackageService = new Mock<IChocolateyPackageService>();
            protected Mock<IChocolateyConfigSettingsService> ConfigSettingsService = new Mock<IChocolateyConfigSettingsService>();
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                Configuration.Sources = "https://localhost/somewhere/out/there";
                Command = new ChocolateyPushCommand(PackageService.Object, ConfigSettingsService.Object);
            }
        }

        public class When_implementing_command_for : ChocolateyPushCommandSpecsBase
        {
            private List<string> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_push()
            {
                _results.Should().Contain("push");
            }
        }

        //Yes, configurating [sic]
        public class When_configurating_the_argument_parser : ChocolateyPushCommandSpecsBase
        {
            private OptionSet _optionSet;

            public override void Context()
            {
                base.Context();
                _optionSet = new OptionSet();
            }

            public override void Because()
            {
                Command.ConfigureArgumentParser(_optionSet, Configuration);
            }

            [Fact]
            public void Should_clear_previously_set_Source()
            {
                Configuration.Sources.Should().BeNull();
            }

            [Fact]
            public void Should_add_source_to_the_option_set()
            {
                _optionSet.Contains("source").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_source_to_the_option_set()
            {
                _optionSet.Contains("s").Should().BeTrue();
            }

            [Fact]
            public void Should_add_apikey_to_the_option_set()
            {
                _optionSet.Contains("apikey").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_apikey_to_the_option_set()
            {
                _optionSet.Contains("k").Should().BeTrue();
            }

            [Fact]
            public void Should_not_add_short_version_of_timeout_to_the_option_set()
            {
                _optionSet.Contains("t").Should().BeFalse();
            }
        }

        public class When_handling_additional_argument_parsing : ChocolateyPushCommandSpecsBase
        {
            private readonly IList<string> _unparsedArgs = new List<string>();
            private Action _because;

            public override void Because()
            {
                _because = () => Command.ParseAdditionalArguments(_unparsedArgs, Configuration);
            }

            public void Reset()
            {
                _unparsedArgs.Clear();
                ConfigSettingsService.ResetCalls();
            }

            [Fact]
            public void Should_allow_a_path_to_the_nupkg_to_be_passed_in()
            {
                Reset();
                string nupkgPath = "./some/path/to.nupkg";
                _unparsedArgs.Add(nupkgPath);
                _because();
                Configuration.Input.Should().Be(nupkgPath);
            }
        }

        public class When_validating : ChocolateyPushCommandSpecsBase
        {
            private Action _because;
            private string _apiKey = "abcdef";

            public override void Because()
            {
                _because = () => Command.Validate(Configuration);
            }

            [Fact]
            public void Should_not_override_explicit_source_if_defaultpushsource_is_set()
            {
                Configuration.Sources = "https://localhost/somewhere/out/there";
                Configuration.PushCommand.Key = _apiKey;
                Configuration.PushCommand.DefaultSource = "https://localhost/default/source";
                _because();

                Configuration.Sources.Should().Be("https://localhost/somewhere/out/there");
            }


            [Fact]
            public void Should_set_the_source_to_defaultpushsource_if_set_and_no_explicit_source()
            {
                Configuration.Sources = "";
                Configuration.PushCommand.DefaultSource = "https://localhost/default/source";
                ConfigSettingsService.Setup(s => s.GetApiKey(It.Is<ChocolateyConfiguration>(c => c.Sources == "https://localhost/default/source"), null))
                    .Returns(() => _apiKey);

                _because();

                Configuration.Sources.Should().Be("https://localhost/default/source");
            }

            [Fact]
            public void Should_throw_when_defaultpushsource_is_not_set_and_no_explicit_sources()
            {
                Configuration.PushCommand.DefaultSource = "";
                Configuration.Sources = "";

                var errorred = false;
                Exception error = null;

                try
                {
                    _because();
                }
                catch (Exception ex)
                {
                    errorred = true;
                    error = ex;
                }

                errorred.Should().BeTrue();
                error.Should().NotBeNull();
                error.Should().BeOfType<ApplicationException>();
                error.Message.Should().Contain("The default push source configuration is not set.");
            }

            [Fact]
            public void Should_continue_when_defaultpushsource_is_not_set_and_explicit_sources_passed()
            {
                Configuration.Sources = "https://somewhere/out/there";
                Configuration.PushCommand.Key = _apiKey;
                Configuration.PushCommand.DefaultSource = "";
                _because();
            }

            [Fact]
            public void Should_throw_if_apikey_is_not_found_for_source()
            {
                ConfigSettingsService.Setup(c => c.GetApiKey(Configuration, null)).Returns("");
                Configuration.PushCommand.Key = "";
                Configuration.Sources = "https://localhost/somewhere/out/there";

                var errorred = false;
                Exception error = null;

                try
                {
                    _because();
                }
                catch (Exception ex)
                {
                    errorred = true;
                    error = ex;
                }

                errorred.Should().BeTrue();
                error.Should().NotBeNull();
                error.Should().BeOfType<ApplicationException>();
                error.Message.Should().Contain($"An API key was not found for '{Configuration.Sources}'");
            }

            [Fact]
            public void Should_not_try_to_determine_the_key_if_passed_in_as_an_argument()
            {
                ConfigSettingsService.Setup(c => c.GetApiKey(Configuration, null)).Returns("");
                Configuration.PushCommand.Key = _apiKey;
                Configuration.Sources = "https://localhost/somewhere/out/there";
                _because();

                Configuration.PushCommand.Key.Should().Be(_apiKey);
                ConfigSettingsService.Verify(c => c.GetApiKey(It.IsAny<ChocolateyConfiguration>(), It.IsAny<Action<ConfigFileApiKeySetting>>()), Times.Never);
            }

            [Fact]
            public void Should_not_try_to_determine_the_key_if_source_is_set_for_a_local_source()
            {
                Configuration.Sources = "c:\\packages";
                Configuration.PushCommand.Key = "";
                _because();

                ConfigSettingsService.Verify(c => c.GetApiKey(It.IsAny<ChocolateyConfiguration>(), It.IsAny<Action<ConfigFileApiKeySetting>>()), Times.Never);
            }

            [Fact]
            public void Should_not_try_to_determine_the_key_if_source_is_set_for_an_unc_source()
            {
                Configuration.Sources = "\\\\someserver\\packages";
                Configuration.PushCommand.Key = "";
                _because();

                ConfigSettingsService.Verify(c => c.GetApiKey(It.IsAny<ChocolateyConfiguration>(), It.IsAny<Action<ConfigFileApiKeySetting>>()), Times.Never);
            }

            [Fact]
            public void Should_throw_if_multiple_sources_are_passed()
            {
                Configuration.Sources = "https://localhost/somewhere/out/there;https://localhost/somewhere/out/there";

                Assert.Throws<ApplicationException>(() => _because(), "Multiple sources are not supported by push command.");
            }

            [Fact]
            [Pending("This functionality is not yet implemented. See https://github.com/chocolatey/choco/issues/63")]
            public void Should_update_source_if_alias_is_passed()
            {
                Configuration.Sources = "chocolatey";
                Configuration.MachineSources = new List<MachineSourceConfiguration>
                {
                    new MachineSourceConfiguration
                    {
                        Name = "chocolatey",
                        Key = "https://localhost/somewhere/out/there"
                    }
                 };
                _because();

                Configuration.Sources.Should().Be("https://localhost/somewhere/out/there");
            }

            [Fact]
            [Pending("This functionality is not yet implemented. See https://github.com/chocolatey/choco/issues/63")]
            public void Should_update_source_if_alias_is_passed_via_defaultpushsource()
            {
                Configuration.Sources = "";
                Configuration.PushCommand.DefaultSource = "myrepo";
                Configuration.MachineSources = new List<MachineSourceConfiguration>
                {
                    new MachineSourceConfiguration
                    {
                        Name = "myrepo",
                        Key = "https://localhost/somewhere/out/there"
                    }
                };
                _because();

                Configuration.Sources.Should().Be("https://localhost/somewhere/out/there");
            }

            [Fact]
            public void Should_throw_when_apiKey_has_not_been_set_or_determined_for_a_https_source()
            {
                Configuration.Sources = "https://somewhere/out/there";
                Configuration.PushCommand.Key = "";
                var errored = false;
                Exception error = null;

                try
                {
                    _because();
                }
                catch (Exception ex)
                {
                    errored = true;
                    error = ex;
                }

                errored.Should().BeTrue();
                error.Should().NotBeNull();
                error.Should().BeOfType<ApplicationException>();
                error.Message.Should().Contain("API key was not found");
            }

            [Fact]
            public void Should_continue_when_source_and_apikey_is_set_for_a_https_source()
            {
                Configuration.Sources = "https://somewhere/out/there";
                Configuration.PushCommand.Key = _apiKey;
                _because();
            }

            [Fact]
            public void Should_continue_when_source_is_set_for_a_local_source()
            {
                Configuration.Sources = "c:\\packages";
                Configuration.PushCommand.Key = "";
                _because();
            }

            [Fact]
            public void Should_continue_when_source_is_set_for_an_unc_source()
            {
                Configuration.Sources = "\\\\someserver\\packages";
                Configuration.PushCommand.Key = "";
                _because();
            }

            [Fact]
            public void Should_throw_when_source_is_http_and_not_secure()
            {
                Configuration.Sources = "http://somewhere/out/there";
                Configuration.PushCommand.Key = _apiKey;
                Configuration.Force = false;
                var errored = false;
                Exception error = null;

                try
                {
                    _because();
                }
                catch (Exception ex)
                {
                    errored = true;
                    error = ex;
                }

                errored.Should().BeTrue();
                error.Should().NotBeNull();
                error.Should().BeOfType<ApplicationException>();
                error.Message.Should().Contain("WARNING! The specified source '{0}' is not secure".FormatWith(Configuration.Sources));
            }

            [Fact]
            public void Should_continue_when_source_is_http_and_not_secure_if_force_is_passed()
            {
                Configuration.Sources = "http://somewhere/out/there";
                Configuration.PushCommand.Key = _apiKey;
                Configuration.Force = true;

                _because();
            }
        }

        public class When_noop_is_called : ChocolateyPushCommandSpecsBase
        {
            public override void Because()
            {
                Command.DryRun(Configuration);
            }

            [Fact]
            public void Should_call_service_push_noop()
            {
                PackageService.Verify(c => c.PushDryRun(Configuration), Times.Once);
            }
        }

        public class When_run_is_called : ChocolateyPushCommandSpecsBase
        {
            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_call_service_push_run()
            {
                PackageService.Verify(c => c.Push(Configuration), Times.Once);
            }
        }
    }
}
