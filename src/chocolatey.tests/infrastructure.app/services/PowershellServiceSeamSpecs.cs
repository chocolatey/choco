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
using System.Collections;
using System.Collections.Generic;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.services;
using Moq;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using FluentAssertions;
using IFileSystem = chocolatey.infrastructure.filesystem.IFileSystem;

namespace chocolatey.tests.infrastructure.app.services
{
    // SetAdditionalPackageEnvironmentVariables is overridden here by a service that records what the
    // environment holds when it is called.
    public class PowershellServiceSeamSpecs
    {
        public class PowershellServiceWithExtraVariables : PowershellService
        {
            public int Calls;
            public ChocolateyConfiguration ConfigurationSeen;
            public string DirectorySeen;
            public string PackageNameWhenCalled;
            public string PackageFolderWhenCalled;

            public PowershellServiceWithExtraVariables(IFileSystem fileSystem)
                : base(fileSystem)
            {
            }

            protected override void SetAdditionalPackageEnvironmentVariables(ChocolateyConfiguration configuration, string packageDirectory)
            {
                Calls++;
                ConfigurationSeen = configuration;
                DirectorySeen = packageDirectory;
                PackageNameWhenCalled = Environment.GetEnvironmentVariable("chocolateyPackageName");
                PackageFolderWhenCalled = Environment.GetEnvironmentVariable("chocolateyPackageFolder");

                Environment.SetEnvironmentVariable("ChocolateyPackageSeamProbe", "set by the service");
                // One that the install clears again after this member, so is not passed on to the next package.
                Environment.SetEnvironmentVariable("packageParameters", "set by the service");
            }
        }

        public abstract class PowershellServiceSeamSpecsBase : TinySpec
        {
            private Dictionary<string, string> _environmentBefore;
            protected const string PackageDirectory = @"c:\lib\bob";
            protected ChocolateyConfiguration Configuration;
            protected Mock<IPackageSearchMetadata> Package;

            public override void Context()
            {
                _environmentBefore = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables())
                {
                    _environmentBefore[(string)variable.Key] = (string)variable.Value;
                }

                Environment.SetEnvironmentVariable("ChocolateyPackageSeamProbe", null);
                Configuration = new ChocolateyConfiguration();

                Package = new Mock<IPackageSearchMetadata>();
                Package.Setup(x => x.Identity).Returns(new PackageIdentity("bob", NuGetVersion.Parse("1.0.0")));
                Package.Setup(x => x.Title).Returns("Bob");
            }

            // The process environment is shared by every spec, so put back what these changed.
            public override void AfterObservations()
            {
                base.AfterObservations();

                foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables())
                {
                    var name = (string)variable.Key;
                    if (!_environmentBefore.ContainsKey(name))
                    {
                        Environment.SetEnvironmentVariable(name, null);
                    }
                }

                foreach (var variable in _environmentBefore)
                {
                    if (Environment.GetEnvironmentVariable(variable.Key) != variable.Value)
                    {
                        Environment.SetEnvironmentVariable(variable.Key, variable.Value);
                    }
                }
            }
        }

        public class When_preparing_the_environment_with_a_service_that_adds_nothing : PowershellServiceSeamSpecsBase
        {
            public override void Because()
            {
                new PowershellService(new Mock<IFileSystem>().Object).PreparePowerShellEnvironment(Package.Object, Configuration, PackageDirectory);
            }

            [Fact]
            public void Should_set_the_name_of_the_package()
            {
                Environment.GetEnvironmentVariable("chocolateyPackageName").Should().Be("bob");
            }

            [Fact]
            public void Should_set_the_folder_of_the_package()
            {
                Environment.GetEnvironmentVariable("chocolateyPackageFolder").Should().Be(PackageDirectory);
            }

            [Fact]
            public void Should_not_set_any_variable_for_the_service_to_add()
            {
                Environment.GetEnvironmentVariable("ChocolateyPackageSeamProbe").Should().BeNull();
            }
        }

        public class When_preparing_the_environment_with_a_service_that_adds_variables : PowershellServiceSeamSpecsBase
        {
            private PowershellServiceWithExtraVariables _service;

            public override void Context()
            {
                base.Context();

                _service = new PowershellServiceWithExtraVariables(new Mock<IFileSystem>().Object);
            }

            public override void Because()
            {
                _service.PreparePowerShellEnvironment(Package.Object, Configuration, PackageDirectory);
            }

            [Fact]
            public void Should_ask_the_service_once()
            {
                _service.Calls.Should().Be(1);
            }

            [Fact]
            public void Should_pass_it_the_configuration_and_the_folder_of_the_package()
            {
                _service.ConfigurationSeen.Should().BeSameAs(Configuration);
                _service.DirectorySeen.Should().Be(PackageDirectory);
            }

            [Fact]
            public void Should_ask_it_after_the_standard_variables_of_the_package_are_set()
            {
                _service.PackageNameWhenCalled.Should().Be("bob");
                _service.PackageFolderWhenCalled.Should().Be(PackageDirectory);
            }

            [Fact]
            public void Should_leave_the_variables_it_set_in_the_environment()
            {
                Environment.GetEnvironmentVariable("ChocolateyPackageSeamProbe").Should().Be("set by the service");
            }

            [Fact]
            public void Should_ask_it_before_the_variables_that_are_not_passed_on_are_cleared()
            {
                Environment.GetEnvironmentVariable("packageParameters").Should().BeNull();
            }
        }

        public class When_preparing_the_environment_for_no_package : PowershellServiceSeamSpecsBase
        {
            private PowershellServiceWithExtraVariables _service;

            public override void Context()
            {
                base.Context();

                _service = new PowershellServiceWithExtraVariables(new Mock<IFileSystem>().Object);
            }

            public override void Because()
            {
                _service.PreparePowerShellEnvironment(null, Configuration, PackageDirectory);
            }

            [Fact]
            public void Should_not_ask_the_service()
            {
                _service.Calls.Should().Be(0);
            }
        }
    }
}
