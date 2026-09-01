// Copyright © 2017 - 2025 Chocolatey Software, Inc
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

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using chocolatey.infrastructure.app.attributes;
using chocolatey.infrastructure.app.commands;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.domain;
using chocolatey.infrastructure.app.services;
using chocolatey.infrastructure.commandline;
using chocolatey.infrastructure.results;
using Moq;
using FluentAssertions;

namespace chocolatey.tests.infrastructure.app.commands
{
    public class ChocolateyInstallCommandSpecs
    {
        [ConcernFor("install")]
        public abstract class ChocolateyInstallCommandSpecsBase : TinySpec
        {
            protected ChocolateyInstallCommand Command;
            protected Mock<IChocolateyPackageService> PackageService = new Mock<IChocolateyPackageService>();
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                Configuration.Sources = "bob";
                Command = new ChocolateyInstallCommand(PackageService.Object);
            }
        }

        public class When_implementing_command_for : ChocolateyInstallCommandSpecsBase
        {
            private List<string> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_install()
            {
                _results.Should().Contain("install");
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyInstallCommandSpecsBase
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
            public void Should_add_version_to_the_option_set()
            {
                _optionSet.Contains("version").Should().BeTrue();
            }

            [Fact]
            public void Should_allow_insensitive_case_Version_to_the_option_set()
            {
                _optionSet.Contains("Version").Should().BeTrue();
            }

            [Fact]
            public void Should_add_prerelease_to_the_option_set()
            {
                _optionSet.Contains("prerelease").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_prerelease_to_the_option_set()
            {
                _optionSet.Contains("pre").Should().BeTrue();
            }

            [Fact]
            public void Should_add_installargs_to_the_option_set()
            {
                _optionSet.Contains("installarguments").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_installargs_to_the_option_set()
            {
                _optionSet.Contains("ia").Should().BeTrue();
            }

            [Fact]
            public void Should_add_overrideargs_to_the_option_set()
            {
                _optionSet.Contains("overridearguments").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_overrideargs_to_the_option_set()
            {
                _optionSet.Contains("o").Should().BeTrue();
            }

            [Fact]
            public void Should_add_notsilent_to_the_option_set()
            {
                _optionSet.Contains("notsilent").Should().BeTrue();
            }

            [Fact]
            public void Should_add_packageparameters_to_the_option_set()
            {
                _optionSet.Contains("packageparameters").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_packageparameters_to_the_option_set()
            {
                _optionSet.Contains("params").Should().BeTrue();
            }

            [Fact]
            public void Should_add_applyPackageParametersToDependencies_to_the_option_set()
            {
                _optionSet.Contains("apply-package-parameters-to-dependencies").Should().BeTrue();
            }

            [Fact]
            public void Should_add_applyInstallArgumentsToDependencies_to_the_option_set()
            {
                _optionSet.Contains("apply-install-arguments-to-dependencies").Should().BeTrue();
            }

            [Fact]
            public void Should_add_ignoredependencies_to_the_option_set()
            {
                _optionSet.Contains("ignoredependencies").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_ignoredependencies_to_the_option_set()
            {
                _optionSet.Contains("i").Should().BeTrue();
            }

            [Fact]
            public void Should_add_forcedependencies_to_the_option_set()
            {
                _optionSet.Contains("forcedependencies").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_forcedependencies_to_the_option_set()
            {
                _optionSet.Contains("x").Should().BeTrue();
            }

            [Fact]
            public void Should_add_skippowershell_to_the_option_set()
            {
                _optionSet.Contains("skippowershell").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_skippowershell_to_the_option_set()
            {
                _optionSet.Contains("n").Should().BeTrue();
            }

            [Fact]
            public void Should_add_user_to_the_option_set()
            {
                _optionSet.Contains("user").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_user_to_the_option_set()
            {
                _optionSet.Contains("u").Should().BeTrue();
            }

            [Fact]
            public void Should_add_password_to_the_option_set()
            {
                _optionSet.Contains("password").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_password_to_the_option_set()
            {
                _optionSet.Contains("p").Should().BeTrue();
            }

            [Fact]
            public void Should_add_pin_to_the_option_set()
            {
                _optionSet.Contains("pinpackage").Should().BeTrue();
            }

            [Fact]
            public void Should_add_long_version_of_pin_to_the_option_set()
            {
                _optionSet.Contains("pin-package").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_pin_to_the_option_set()
            {
                _optionSet.Contains("pin").Should().BeTrue();
            }

            [Fact]
            public void Should_add_skip_hooks_to_the_option_set()
            {
                _optionSet.Contains("skip-hooks").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_skip_hooks_to_the_option_set()
            {
                _optionSet.Contains("skiphooks").Should().BeTrue();
            }

            [Fact]
            public void Should_add_include_configured_sources_to_the_option_set()
            {
                _optionSet.Contains("include-configured-sources").Should().BeTrue();
            }
        }

        public class When_handling_additional_argument_parsing : ChocolateyInstallCommandSpecsBase
        {
            private readonly IList<string> _unparsedArgs = new List<string>();

            public override void Context()
            {
                base.Context();
                _unparsedArgs.Add("pkg1");
                _unparsedArgs.Add("pkg2");
            }

            public override void Because()
            {
                Command.ParseAdditionalArguments(_unparsedArgs, Configuration);
            }

            [Fact]
            public void Should_set_unparsed_arguments_to_the_package_names()
            {
                Configuration.PackageNames.Should().Be("pkg1;pkg2");
            }
        }

        public class When_validating : ChocolateyInstallCommandSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void Should_throw_when_packagenames_is_not_set()
            {
                Configuration.PackageNames = "";
                var errored = false;
                Exception error = null;

                try
                {
                    Command.Validate(Configuration);
                }
                catch (Exception ex)
                {
                    errored = true;
                    error = ex;
                }

                errored.Should().BeTrue();
                error.Should().NotBeNull();
                error.Should().BeOfType<ApplicationException>();
            }

            [Fact]
            public void Should_continue_when_packagenames_is_set()
            {
                Configuration.PackageNames = "bob";
                Command.Validate(Configuration);
            }
        }

        public class When_noop_is_called : ChocolateyInstallCommandSpecsBase
        {
            public override void Because()
            {
                Command.DryRun(Configuration);
            }

            [Fact]
            public void Should_call_service_install_noop()
            {
                PackageService.Verify(c => c.InstallDryRun(Configuration), Times.Once);
            }
        }

        public class When_run_is_called : ChocolateyInstallCommandSpecsBase
        {
            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_call_service_install_run()
            {
                PackageService.Verify(c => c.Install(Configuration), Times.Once);
            }
        }

        public class When_install_is_called_with_specific_version : ChocolateyInstallCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Version = "1.0.0";
                Configuration.PackageNames = "testpackage";
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_pass_version_to_service()
            {
                PackageService.Verify(c => c.Install(It.Is<ChocolateyConfiguration>(
                    config => config.Version == "1.0.0")), Times.Once);
            }

            [Fact]
            public void Should_pass_package_name_to_service()
            {
                PackageService.Verify(c => c.Install(It.Is<ChocolateyConfiguration>(
                    config => config.PackageNames == "testpackage")), Times.Once);
            }
        }

        public class When_install_detects_version_mismatch : ChocolateyInstallCommandSpecsBase
        {
            private ConcurrentDictionary<string, PackageResult> _installResults;

            public override void Context()
            {
                base.Context();
                Configuration.Version = "2.0.0";
                Configuration.PackageNames = "testpackage";

                // Simulate version mismatch scenario - service returns error result
                _installResults = new ConcurrentDictionary<string, PackageResult>();
                var packageResult = new PackageResult("testpackage", "2.0.0", null);
                packageResult.Messages.Add(new ResultMessage(
                    ResultType.Error,
                    "Upgrading package testpackage to version 2.0.0 is not possible, likely due to package dependency version requirements. The latest package version that meets the dependency requirements is 1.5.0."));
                _installResults.TryAdd("testpackage", packageResult);

                PackageService.Setup(s => s.Install(It.IsAny<ChocolateyConfiguration>()))
                    .Returns(_installResults);
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_receive_version_mismatch_error_from_service()
            {
                PackageService.Verify(c => c.Install(It.Is<ChocolateyConfiguration>(
                    config => config.Version == "2.0.0")), Times.Once);

                _installResults["testpackage"].Messages
                    .Should().Contain(m => m.MessageType == ResultType.Error && 
                                          m.Message.Contains("is not possible"));
            }
        }

        public class When_install_with_StopOnFirstPackageFailure_and_version_mismatch : ChocolateyInstallCommandSpecsBase
        {
            private Exception _caughtException;

            public override void Context()
            {
                base.Context();
                Configuration.Version = "2.0.0";
                Configuration.PackageNames = "testpackage";
                Configuration.Features.StopOnFirstPackageFailure = true;

                // Simulate service throwing when StopOnFirstPackageFailure is enabled
                PackageService.Setup(s => s.Install(It.IsAny<ChocolateyConfiguration>()))
                    .Throws(new ApplicationException("Stopping further execution as testpackage has failed."));
            }

            public override void Because()
            {
                try
                {
                    Command.Run(Configuration);
                }
                catch (Exception ex)
                {
                    _caughtException = ex;
                }
            }

            [Fact]
            public void Should_throw_ApplicationException()
            {
                _caughtException.Should().NotBeNull();
                _caughtException.Should().BeOfType<ApplicationException>();
            }

            [Fact]
            public void Should_indicate_package_failure_in_exception_message()
            {
                _caughtException.Message.Should().Contain("testpackage");
                _caughtException.Message.Should().Contain("Stopping further execution");
            }
        }

        public class When_install_without_version_specified : ChocolateyInstallCommandSpecsBase
        {
            private ConcurrentDictionary<string, PackageResult> _installResults;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = "testpackage";
                // Version is not set - should install latest available

                _installResults = new ConcurrentDictionary<string, PackageResult>();
                var packageResult = new PackageResult("testpackage", "2.0.0", null);
                packageResult.Messages.Add(new ResultMessage(ResultType.Note, "Successfully installed"));
                _installResults.TryAdd("testpackage", packageResult);

                PackageService.Setup(s => s.Install(It.IsAny<ChocolateyConfiguration>()))
                    .Returns(_installResults);
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_not_check_version_mismatch()
            {
                PackageService.Verify(c => c.Install(It.Is<ChocolateyConfiguration>(
                    config => string.IsNullOrEmpty(config.Version))), Times.Once);
            }

            [Fact]
            public void Should_succeed_without_version_mismatch_errors()
            {
                _installResults["testpackage"].Messages
                    .Should().NotContain(m => m.MessageType == ResultType.Error);
            }
        }
    }
}
