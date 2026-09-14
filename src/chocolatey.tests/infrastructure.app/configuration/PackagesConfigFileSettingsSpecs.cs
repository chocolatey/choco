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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using chocolatey.infrastructure.app.configuration;
using FluentAssertions;

namespace chocolatey.tests.infrastructure.app.configuration
{
    // Characterization tests for the packages.config XML contract
    // (PackagesConfigFileSettings / PackagesConfigFilePackageSetting). The
    // production XmlService deserializes this with a plain XmlSerializer (see
    // XmlService.Deserialize), so exercising XmlSerializer directly here pins
    // the exact attribute->property mapping that `choco install a.config`
    // relies on.
    //
    // This matters for Package Instances: the existing service-level spec
    // mocks the deserialization, so the attribute mapping itself was
    // previously untested. The `instance` attribute is read here and consumed
    // by the licensed extension.
    public class PackagesConfigFileSettingsSpecs
    {
        public abstract class PackagesConfigFileSettingsSpecsBase : TinySpec
        {
            protected PackagesConfigFileSettings Result;
            protected string Xml;

            public override void Because()
            {
                var serializer = new XmlSerializer(typeof(PackagesConfigFileSettings));
                using (var reader = new StringReader(Xml))
                {
                    Result = (PackagesConfigFileSettings)serializer.Deserialize(reader);
                }
            }
        }

        [ConcernFor("packages.config")]
        public class When_deserializing_a_fully_specified_package : PackagesConfigFileSettingsSpecsBase
        {
            private PackagesConfigFilePackageSetting _package;

            public override void Context()
            {
                Xml =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""bob"" version=""1.2.3"" source=""https://example.com/api/v2/"" installArguments=""/S"" packageParameters=""/Config:Foo"" pinPackage=""true"" force=""true"" prerelease=""true"" ignoreDependencies=""true"" allowDowngrade=""true"" executionTimeout=""123"" />
</packages>";
            }

            public override void Because()
            {
                base.Because();
                _package = Result.Packages.Single();
            }

            [Fact]
            public void Should_read_the_id()
            {
                _package.Id.Should().Be("bob");
            }

            [Fact]
            public void Should_read_the_version()
            {
                _package.Version.Should().Be("1.2.3");
            }

            [Fact]
            public void Should_read_the_source()
            {
                _package.Source.Should().Be("https://example.com/api/v2/");
            }

            [Fact]
            public void Should_read_the_install_arguments()
            {
                _package.InstallArguments.Should().Be("/S");
            }

            [Fact]
            public void Should_read_the_package_parameters()
            {
                _package.PackageParameters.Should().Be("/Config:Foo");
            }

            [Fact]
            public void Should_read_the_pin_package_flag()
            {
                _package.PinPackage.Should().BeTrue();
            }

            [Fact]
            public void Should_read_the_force_flag()
            {
                _package.Force.Should().BeTrue();
            }

            [Fact]
            public void Should_read_the_prerelease_flag()
            {
                _package.Prerelease.Should().BeTrue();
            }

            [Fact]
            public void Should_read_the_ignore_dependencies_flag()
            {
                _package.IgnoreDependencies.Should().BeTrue();
            }

            [Fact]
            public void Should_read_the_allow_downgrade_flag()
            {
                _package.AllowDowngrade.Should().BeTrue();
            }

            [Fact]
            public void Should_read_the_execution_timeout()
            {
                _package.ExecutionTimeout.Should().Be(123);
            }
        }

        [ConcernFor("packages.config")]
        public class When_deserializing_a_package_with_an_instance : PackagesConfigFileSettingsSpecsBase
        {
            private PackagesConfigFilePackageSetting _package;

            public override void Context()
            {
                Xml =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""installpackage"" version=""1.0.0"" instance=""first"" />
</packages>";
            }

            public override void Because()
            {
                base.Because();
                _package = Result.Packages.Single();
            }

            [Fact]
            public void Should_read_the_id()
            {
                _package.Id.Should().Be("installpackage");
            }

            [Fact]
            public void Should_read_the_version()
            {
                _package.Version.Should().Be("1.0.0");
            }

            [Fact]
            public void Should_read_the_instance()
            {
                _package.Instance.Should().Be("first");
            }
        }

        [ConcernFor("packages.config")]
        public class When_deserializing_a_package_with_only_an_id : PackagesConfigFileSettingsSpecsBase
        {
            private PackagesConfigFilePackageSetting _package;

            public override void Context()
            {
                Xml =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""minimal"" />
</packages>";
            }

            public override void Because()
            {
                base.Because();
                _package = Result.Packages.Single();
            }

            [Fact]
            public void Should_read_the_id()
            {
                _package.Id.Should().Be("minimal");
            }

            [Fact]
            public void Should_leave_unspecified_string_attributes_null()
            {
                _package.Version.Should().BeNull();
                _package.Source.Should().BeNull();
                _package.PackageParameters.Should().BeNull();
                _package.Instance.Should().BeNull();
            }

            [Fact]
            public void Should_default_unspecified_boolean_attributes_to_false()
            {
                _package.PinPackage.Should().BeFalse();
                _package.Force.Should().BeFalse();
                _package.Prerelease.Should().BeFalse();
                _package.IgnoreDependencies.Should().BeFalse();
            }
        }

        [ConcernFor("packages.config")]
        public class When_deserializing_multiple_packages : PackagesConfigFileSettingsSpecsBase
        {
            public override void Context()
            {
                Xml =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""first"" version=""1.0.0"" />
  <package id=""second"" version=""2.0.0"" pinPackage=""true"" />
  <package id=""third"" />
</packages>";
            }

            [Fact]
            public void Should_read_all_packages()
            {
                Result.Packages.Should().HaveCount(3);
            }

            [Fact]
            public void Should_read_each_package_with_its_own_attributes()
            {
                var second = Result.Packages.Single(p => p.Id == "second");
                second.Version.Should().Be("2.0.0");
                second.PinPackage.Should().BeTrue();

                var third = Result.Packages.Single(p => p.Id == "third");
                third.Version.Should().BeNull();
                third.PinPackage.Should().BeFalse();
            }
        }

        [ConcernFor("packages.config")]
        public class When_round_tripping_a_package_through_the_serializer : PackagesConfigFileSettingsSpecsBase
        {
            private PackagesConfigFilePackageSetting _package;

            public override void Context()
            {
                var settings = new PackagesConfigFileSettings
                {
                    Packages = new HashSet<PackagesConfigFilePackageSetting>
                    {
                        new PackagesConfigFilePackageSetting
                        {
                            Id = "bob",
                            Version = "3.4.5",
                            PinPackage = true,
                            PackageParameters = "/Config:Bar",
                            Instance = "first"
                        }
                    }
                };

                var serializer = new XmlSerializer(typeof(PackagesConfigFileSettings));
                using (var writer = new StringWriter())
                {
                    serializer.Serialize(writer, settings);
                    Xml = writer.ToString();
                }
            }

            public override void Because()
            {
                base.Because();
                _package = Result.Packages.Single();
            }

            [Fact]
            public void Should_preserve_the_package_values_across_a_serialize_deserialize_round_trip()
            {
                _package.Id.Should().Be("bob");
                _package.Version.Should().Be("3.4.5");
                _package.PinPackage.Should().BeTrue();
                _package.PackageParameters.Should().Be("/Config:Bar");
                _package.Instance.Should().Be("first");
            }
        }
    }
}
