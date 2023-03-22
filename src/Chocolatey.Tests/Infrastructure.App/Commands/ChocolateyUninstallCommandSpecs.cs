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

    public class ChocolateyUninstallCommandSpecs
    {
        [ConcernFor("uninstall")]
        public abstract class ChocolateyUninstallCommandSpecsBase : TinySpec
        {
            protected ChocolateyUninstallCommand command;
            protected Mock<IChocolateyPackageService> packageService = new Mock<IChocolateyPackageService>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                configuration.Sources = "bob";
                command = new ChocolateyUninstallCommand(packageService.Object);
            }
        }

        public class When_implementing_command_for : ChocolateyUninstallCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_uninstall()
            {
                results.ShouldContain(CommandNameType.Uninstall.ToStringChecked());
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyUninstallCommandSpecsBase
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
            public void Should_add_version_to_the_option_set()
            {
                optionSet.Contains("version").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_allversions_to_the_option_set()
            {
                optionSet.Contains("allversions").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_allversions_to_the_option_set()
            {
                optionSet.Contains("a").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_uninstallargs_to_the_option_set()
            {
                optionSet.Contains("uninstallarguments").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_uninstallargs_to_the_option_set()
            {
                optionSet.Contains("ua").ShouldBeTrue();
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

        public class When_handling_additional_argument_parsing : ChocolateyUninstallCommandSpecsBase
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

        public class When_handling_validation : ChocolateyUninstallCommandSpecsBase
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

        public class When_noop_is_called : ChocolateyUninstallCommandSpecsBase
        {
            public override void Because()
            {
                command.DryRun(configuration);
            }

            [Fact]
            public void Should_call_service_uninstall_noop()
            {
                packageService.Verify(c => c.UninstallDryRun(configuration), Times.Once);
            }
        }

        public class When_run_is_called : ChocolateyUninstallCommandSpecsBase
        {
            public override void Because()
            {
                command.Run(configuration);
            }

            [Fact]
            public void Should_call_service_uninstall_run()
            {
                packageService.Verify(c => c.Uninstall(configuration), Times.Once);
            }
        }
    }
}
