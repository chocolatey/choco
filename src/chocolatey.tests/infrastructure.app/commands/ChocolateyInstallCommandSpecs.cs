﻿// Copyright © 2017 - 2018 Chocolatey Software, Inc
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
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commandline;
    using Moq;
    using Should;

    public class ChocolateyInstallCommandSpecs
    {
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

        public class when_implementing_command_for : ChocolateyInstallCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void should_implement_install()
            {
                results.ShouldContain(CommandNameType.install.to_string());
            }
        }

        public class when_configurating_the_argument_parser : ChocolateyInstallCommandSpecsBase
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
            public void should_add_version_to_the_option_set()
            {
                optionSet.Contains("version").ShouldBeTrue();
            }

            [Fact]
            public void should_allow_insensitive_case_Version_to_the_option_set()
            {
                optionSet.Contains("Version").ShouldBeTrue();
            }

            [Fact]
            public void should_add_prerelease_to_the_option_set()
            {
                optionSet.Contains("prerelease").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_prerelease_to_the_option_set()
            {
                optionSet.Contains("pre").ShouldBeTrue();
            }

            [Fact]
            public void should_add_installargs_to_the_option_set()
            {
                optionSet.Contains("installarguments").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_installargs_to_the_option_set()
            {
                optionSet.Contains("ia").ShouldBeTrue();
            }

            [Fact]
            public void should_add_overrideargs_to_the_option_set()
            {
                optionSet.Contains("overridearguments").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_overrideargs_to_the_option_set()
            {
                optionSet.Contains("o").ShouldBeTrue();
            }

            [Fact]
            public void should_add_notsilent_to_the_option_set()
            {
                optionSet.Contains("notsilent").ShouldBeTrue();
            }

            [Fact]
            public void should_add_packageparameters_to_the_option_set()
            {
                optionSet.Contains("packageparameters").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_packageparameters_to_the_option_set()
            {
                optionSet.Contains("params").ShouldBeTrue();
            }

            [Fact]
            public void should_add_applyPackageParametersToDependencies_to_the_option_set()
            {
                optionSet.Contains("apply-package-parameters-to-dependencies").ShouldBeTrue();
            }

            [Fact]
            public void should_add_applyInstallArgumentsToDependencies_to_the_option_set()
            {
                optionSet.Contains("apply-install-arguments-to-dependencies").ShouldBeTrue();
            }

            [Fact]
            public void should_add_allowmultipleversions_to_the_option_set()
            {
                optionSet.Contains("allowmultipleversions").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_allowmultipleversions_to_the_option_set()
            {
                optionSet.Contains("m").ShouldBeTrue();
            }

            [Fact]
            public void should_add_ignoredependencies_to_the_option_set()
            {
                optionSet.Contains("ignoredependencies").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_ignoredependencies_to_the_option_set()
            {
                optionSet.Contains("i").ShouldBeTrue();
            }

            [Fact]
            public void should_add_forcedependencies_to_the_option_set()
            {
                optionSet.Contains("forcedependencies").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_forcedependencies_to_the_option_set()
            {
                optionSet.Contains("x").ShouldBeTrue();
            }

            [Fact]
            public void should_add_skippowershell_to_the_option_set()
            {
                optionSet.Contains("skippowershell").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_skippowershell_to_the_option_set()
            {
                optionSet.Contains("n").ShouldBeTrue();
            }

            [Fact]
            public void should_add_user_to_the_option_set()
            {
                optionSet.Contains("user").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_user_to_the_option_set()
            {
                optionSet.Contains("u").ShouldBeTrue();
            }

            [Fact]
            public void should_add_password_to_the_option_set()
            {
                optionSet.Contains("password").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_password_to_the_option_set()
            {
                optionSet.Contains("p").ShouldBeTrue();
            }
        }

        public class when_handling_additional_argument_parsing : ChocolateyInstallCommandSpecsBase
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
                command.handle_additional_argument_parsing(unparsedArgs, configuration);
            }

            [Fact]
            public void should_set_unparsed_arguments_to_the_package_names()
            {
                configuration.PackageNames.ShouldEqual("pkg1;pkg2");
            }
        }

        public class when_handling_validation : ChocolateyInstallCommandSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void should_throw_when_packagenames_is_not_set()
            {
                configuration.PackageNames = "";
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
            public void should_continue_when_packagenames_is_set()
            {
                configuration.PackageNames = "bob";
                command.handle_validation(configuration);
            }
        }

        public class when_noop_is_called : ChocolateyInstallCommandSpecsBase
        {
            public override void Because()
            {
                command.noop(configuration);
            }

            [Fact]
            public void should_call_service_install_noop()
            {
                packageService.Verify(c => c.install_noop(configuration), Times.Once);
            }
        }

        public class when_run_is_called : ChocolateyInstallCommandSpecsBase
        {
            public override void Because()
            {
                command.run(configuration);
            }

            [Fact]
            public void should_call_service_install_run()
            {
                packageService.Verify(c => c.install_run(configuration), Times.Once);
            }
        }
    }
}
