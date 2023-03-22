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

namespace Chocolatey.Tests.Infrastructure.App.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Chocolatey.Infrastructure.App.Attributes;
    using Chocolatey.Infrastructure.App.Commands;
    using Chocolatey.Infrastructure.App.Configuration;
    using Chocolatey.Infrastructure.App.Domain;
    using Chocolatey.Infrastructure.App.Services;
    using Chocolatey.Infrastructure.CommandLine;
    using Moq;
    using Should;

    public class ChocolateyInstallCommandSpecs
    {
        [ConcernFor("install")]
        public abstract class ChocolateyInstallCommandSpecsBase : TinySpec
        {
            protected ChocolateyInstallCommand command;
            protected Mock<IChocolateyPackageService> packageService = new Mock<IChocolateyPackageService>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                configuration.Sources = "bob";
                command = new ChocolateyInstallCommand(packageService.Object);
            }
        }

        public class When_implementing_command_for : ChocolateyInstallCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_install()
            {
                results.ShouldContain(CommandNameType.Install.ToStringChecked());
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyInstallCommandSpecsBase
        {
            private OptionSet optionSet;

            public override void Context()
            {
                base.Context();
                optionSet = new OptionSet();
            }

            public override void Because()
            {
                command.ConfigureArgumentParser(optionSet, configuration);
            }

            [Fact]
            public void Should_add_source_to_the_option_set()
            {
                optionSet.Contains("source").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_source_to_the_option_set()
            {
                optionSet.Contains("s").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_version_to_the_option_set()
            {
                optionSet.Contains("version").ShouldBeTrue();
            }

            [Fact]
            public void Should_allow_insensitive_case_Version_to_the_option_set()
            {
                optionSet.Contains("Version").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_prerelease_to_the_option_set()
            {
                optionSet.Contains("prerelease").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_prerelease_to_the_option_set()
            {
                optionSet.Contains("pre").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_installargs_to_the_option_set()
            {
                optionSet.Contains("installarguments").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_installargs_to_the_option_set()
            {
                optionSet.Contains("ia").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_overrideargs_to_the_option_set()
            {
                optionSet.Contains("overridearguments").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_overrideargs_to_the_option_set()
            {
                optionSet.Contains("o").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_notsilent_to_the_option_set()
            {
                optionSet.Contains("notsilent").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_packageparameters_to_the_option_set()
            {
                optionSet.Contains("packageparameters").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_packageparameters_to_the_option_set()
            {
                optionSet.Contains("params").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_applyPackageParametersToDependencies_to_the_option_set()
            {
                optionSet.Contains("apply-package-parameters-to-dependencies").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_applyInstallArgumentsToDependencies_to_the_option_set()
            {
                optionSet.Contains("apply-install-arguments-to-dependencies").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_ignoredependencies_to_the_option_set()
            {
                optionSet.Contains("ignoredependencies").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_ignoredependencies_to_the_option_set()
            {
                optionSet.Contains("i").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_forcedependencies_to_the_option_set()
            {
                optionSet.Contains("forcedependencies").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_forcedependencies_to_the_option_set()
            {
                optionSet.Contains("x").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_skippowershell_to_the_option_set()
            {
                optionSet.Contains("skippowershell").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_skippowershell_to_the_option_set()
            {
                optionSet.Contains("n").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_user_to_the_option_set()
            {
                optionSet.Contains("user").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_user_to_the_option_set()
            {
                optionSet.Contains("u").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_password_to_the_option_set()
            {
                optionSet.Contains("password").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_password_to_the_option_set()
            {
                optionSet.Contains("p").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_pin_to_the_option_set()
            {
                optionSet.Contains("pinpackage").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_long_version_of_pin_to_the_option_set()
            {
                optionSet.Contains("pin-package").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_pin_to_the_option_set()
            {
                optionSet.Contains("pin").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_skip_hooks_to_the_option_set()
            {
                optionSet.Contains("skip-hooks").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_skip_hooks_to_the_option_set()
            {
                optionSet.Contains("skiphooks").ShouldBeTrue();
            }
        }

        public class When_handling_additional_argument_parsing : ChocolateyInstallCommandSpecsBase
        {
            private readonly IList<string> unparsedArgs = new List<string>();

            public override void Context()
            {
                base.Context();
                unparsedArgs.Add("pkg1");
                unparsedArgs.Add("pkg2");
            }

            public override void Because()
            {
                command.ParseAdditionalArguments(unparsedArgs, configuration);
            }

            [Fact]
            public void Should_set_unparsed_arguments_to_the_package_names()
            {
                configuration.PackageNames.ShouldEqual("pkg1;pkg2");
            }
        }

        public class When_handling_validation : ChocolateyInstallCommandSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void Should_throw_when_packagenames_is_not_set()
            {
                configuration.PackageNames = "";
                var errored = false;
                Exception error = null;

                try
                {
                    command.Validate(configuration);
                }
                catch (Exception ex)
                {
                    errored = true;
                    error = ex;
                }

                errored.ShouldBeTrue();
                error.ShouldNotBeNull();
                error.ShouldBeType<ApplicationException>();
            }

            [Fact]
            public void Should_continue_when_packagenames_is_set()
            {
                configuration.PackageNames = "bob";
                command.Validate(configuration);
            }
        }

        public class When_noop_is_called : ChocolateyInstallCommandSpecsBase
        {
            public override void Because()
            {
                command.DryRun(configuration);
            }

            [Fact]
            public void Should_call_service_install_noop()
            {
                packageService.Verify(c => c.InstallDryRun(configuration), Times.Once);
            }
        }

        public class When_run_is_called : ChocolateyInstallCommandSpecsBase
        {
            public override void Because()
            {
                command.Run(configuration);
            }

            [Fact]
            public void Should_call_service_install_run()
            {
                packageService.Verify(c => c.Install(configuration), Times.Once);
            }
        }
    }
}
