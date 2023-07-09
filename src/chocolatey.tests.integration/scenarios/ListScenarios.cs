// Copyright © 2023-Present Chocolatey Software, Inc
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

namespace chocolatey.tests.integration.scenarios
{
    using System.Collections.Generic;
    using System.Linq;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.results;
    using NuGet.Configuration;
    using FluentAssertions;
    using FluentAssertions.Execution;

    public class ListScenarios
    {
        [ConcernFor("list")]
        public abstract class ScenariosBase : TinySpec
        {
            protected IList<PackageResult> Results;
            protected ChocolateyConfiguration Configuration;
            protected IChocolateyPackageService Service;

            public override void Context()
            {
                Configuration = Scenario.List();
                Scenario.Reset(Configuration);
                Scenario.AddPackagesToSourceLocation(Configuration, Configuration.Input + "*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "installpackage*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "installpackage", "1.0.0");
                Scenario.InstallPackage(Configuration, "upgradepackage", "1.0.0");

                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();
                Configuration.ListCommand.LocalOnly = true;
            }
        }

        public class When_listing_local_packages : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage 1.0.0").Should().BeTrue("Warnings: " + string.Join("\n", MockLogger.Messages["Info"]));
            }

            [Fact]
            public void Should_not_contain_packages_and_versions_with_a_pipe_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage|1.0.0").Should().BeFalse();
            }

            [Fact]
            public void Should_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages installed").Should().BeTrue();
            }

            [Fact]
            public void Should_contain_debugging_messages()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Searching for package information"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Running list with the following filter"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Start of List"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("End of List"));
            }
        }

        public class When_listing_local_packages_with_id_only : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.ListCommand.IdOnly = true;
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_contain_package_name()
            {
                MockLogger.ContainsMessage("upgradepackage").Should().BeTrue();
            }

            [Fact]
            public void Should_not_contain_any_version_number()
            {
                MockLogger.ContainsMessage(".0").Should().BeFalse();
            }
        }

        public class When_listing_local_packages_limiting_output : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.RegularOutput = false;
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_contain_packages_and_versions_with_a_pipe_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage|1.0.0").Should().BeTrue();
            }

            [Fact]
            public void Should_only_have_messages_related_to_package_information()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().HaveCount(2);
            }

            [Fact]
            public void Should_not_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage 1.0.0").Should().BeFalse();
            }

            [Fact]
            public void Should_not_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages installed").Should().BeFalse();
            }

            [Fact]
            public void Should_not_contain_debugging_messages()
            {
                MockLogger.Messages.Should().NotContainKey(LogLevel.Debug.ToStringSafe());
            }
        }

        public class When_listing_local_packages_limiting_output_with_id_only : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.ListCommand.IdOnly = true;
                Configuration.RegularOutput = false;
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_contain_packages_id()
            {
                MockLogger.ContainsMessage("upgradepackage").Should().BeTrue();
            }

            [Fact]
            public void Should_not_contain_any_version_number()
            {
                MockLogger.ContainsMessage(".0").Should().BeFalse();
            }

            [Fact]
            public void Should_not_contain_pipe()
            {
                MockLogger.ContainsMessage("|").Should().BeFalse();
            }
        }

        public class When_listing_local_packages_with_uppercase_id_package_installed : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Scenario.AddPackagesToSourceLocation(Configuration, "UpperCase" + "*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "UpperCase", "1.1.0");
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage 1.0.0").Should().BeTrue();
            }

            [Fact]
            public void Should_contain_uppercase_id_package()
            {
                MockLogger.ContainsMessage("UpperCase 1.1.0").Should().BeTrue();
            }

            [Fact]
            public void Should_not_contain_packages_and_versions_with_a_pipe_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage|1.0.0").Should().BeFalse();
            }

            [Fact]
            public void Should_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages installed").Should().BeTrue();
            }

            [Fact]
            public void Should_contain_debugging_messages()
            {
                using (new AssertionScope())
                {
                    MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                        .WhoseValue.Should().Contain(m => m.Contains("Searching for package information"));
                    MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                        .WhoseValue.Should().Contain(m => m.Contains("Running list with the following filter"));
                    MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                        .WhoseValue.Should().Contain(m => m.Contains("Start of List"));
                    MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                        .WhoseValue.Should().Contain(m => m.Contains("End of List"));
                }
            }
        }
    }
}
