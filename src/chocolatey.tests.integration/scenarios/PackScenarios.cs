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

namespace chocolatey.tests.integration.scenarios
{
    using System;
    using System.IO;
    using System.Xml;

    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;

    using NuGet.Packaging;

    using NUnit.Framework;

    using FluentAssertions;
    using FluentAssertions.Execution;

    public class PackScenarios
    {
        [ConcernFor("pack")]
        public abstract class ScenariosInvalidBase : TinySpec
        {
            protected ChocolateyConfiguration Configuration;
            protected INugetService Service;
            protected Action ServiceAct;

            public override sealed void Context()
            {
            }

            public override void Because()
            {
            }

            public override void BeforeEachSpec()
            {
                Configuration = Scenario.Pack();
                Scenario.Reset(Configuration);

                Service = NUnitSetup.Container.GetInstance<INugetService>();
                MockLogger.Reset();
                ServiceAct = () => Service.Pack(Configuration);
            }

            protected void AddFile(string fileName, string fileContent)
            {
                Scenario.AddFiles(new[] { new Tuple<string, string>(fileName, fileContent) });
            }
        }

        [Categories.ExceptionHandling]
        public class When_invalid_data_is_used_in_nuspec_file : ScenariosInvalidBase
        {
            [Fact]
            public void Should_throw_xml_exception_on_empty_nuspec_file()
            {
                AddFile("myPackage.nuspec", string.Empty);

                ServiceAct.Should().Throw<XmlException>();
            }

            [TestCase("")]
            [TestCase("invalid_version")]
            public void Should_throw_invalid_data_exception_on_invalid_version(string version)
            {
                AddFile("myPackage.nuspec", @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>{0}</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
  </metadata>
</package>".FormatWith(version));

                ServiceAct.Should().Throw<InvalidDataException>();
            }
        }

        [ConcernFor("pack")]
        public abstract class ScenariosBase : TinySpec
        {
            protected ChocolateyConfiguration Configuration;
            protected INugetService Service;

            protected virtual string PackagePath
            {
                get
                {
                    if (string.IsNullOrEmpty(ExpectedSubDirectory))
                    {
                        return Path.Combine(Scenario.GetTopLevel(), "test-package." + ExpectedPathVersion + ".nupkg");
                    }

                    return Path.Combine(ExpectedSubDirectory, "test-package." + ExpectedPathVersion + ".nupkg");
                }
            }

            protected abstract string ExpectedNuspecVersion { get; }

            protected virtual string ExpectedPathVersion => ExpectedNuspecVersion;

            protected virtual string ExpectedSubDirectory { get; } = string.Empty;

            public override void Context()
            {
                Configuration = Scenario.Pack();
                Scenario.Reset(Configuration);
                Scenario.AddFiles(new[] { new Tuple<string, string>("myPackage.nuspec", GetNuspecContent()) });

                if (!string.IsNullOrEmpty(ExpectedSubDirectory))
                {
                    Configuration.OutputDirectory = ExpectedSubDirectory;
                    Scenario.CreateDirectory(Configuration.OutputDirectory);
                }

                Service = NUnitSetup.Container.GetInstance<INugetService>();
            }

            public override void AfterObservations()
            {
                if (File.Exists(PackagePath))
                {
                    File.Delete(PackagePath);
                }

                base.AfterObservations();
            }

            [Fact]
            public void Generated_package_should_be_in_current_directory()
            {
                var infos = MockLogger.MessagesFor(LogLevel.Info);

                using (new AssertionScope())
                {
                    infos.Should().HaveCount(2);
                    infos.Should().HaveElementAt(0, "Attempting to build package from 'myPackage.nuspec'.");
                    infos.Should().HaveElementAt(1, string.Concat("Successfully created package '", PackagePath, "'"));
                }

                FileAssert.Exists(PackagePath);
            }

            [Fact]
            public void Generated_package_should_include_expected_version_in_nuspec()
            {
                using (var packageReader = new PackageArchiveReader(PackagePath))
                {
                    var version = packageReader.NuspecReader.GetVersion();

                    version.ToFullString().Should().Be(ExpectedNuspecVersion);
                }
            }

            [Fact]
            public void Sources_should_be_set_to_current_directory()
            {
                if (string.IsNullOrEmpty(ExpectedSubDirectory))
                {
                    Configuration.Sources.Should().Be(Scenario.GetTopLevel());
                }
                else
                {
                    Configuration.Sources.Should().Be(ExpectedSubDirectory);
                }
            }

            protected virtual string GetNuspecContent()
            {
                return NuspecContent;
            }
        }

        public class When_packing_without_specifying_an_output_directory : ScenariosBase
        {
            protected override string ExpectedNuspecVersion => "0.1.0";

            public override void Because()
            {
                MockLogger.Reset();
                Service.Pack(Configuration);
            }
        }

        public class When_packing_with_an_output_directory : ScenariosBase
        {
            protected override string ExpectedNuspecVersion => "0.1.0";
            protected override string ExpectedSubDirectory => "PackageOutput";

            public override void Because()
            {
                MockLogger.Reset();
                Service.Pack(Configuration);
            }
        }

        [Categories.LegacySemVer]
        public class When_packing_with_only_major_minor_version : ScenariosBase
        {
            protected override string PackagePath => Path.Combine("PackageOutput", "test-package.0.3.0.nupkg");
            protected override string ExpectedNuspecVersion => "0.3.0";
            protected override string ExpectedSubDirectory => "PackageOutput";

            public override void Because()
            {
                MockLogger.Reset();
                Service.Pack(Configuration);
            }

            protected override string GetNuspecContent()
            {
                return NuspecContentWithOnlyMajorMinorVersion;
            }
        }

        [Categories.LegacySemVer]
        public class When_packing_with_full_4_part_versioning_scheme : ScenariosBase
        {
            protected override string ExpectedNuspecVersion => "0.5.0.5";
            protected override string ExpectedSubDirectory => "PackageOutput";

            public override void Because()
            {
                MockLogger.Reset();
                Service.Pack(Configuration);
            }

            protected override string GetNuspecContent()
            {
                return NuspecContentWithFull4PartVersioning;
            }
        }

        [Categories.SemVer20]
        public class When_packaging_with_build_metadata : ScenariosBase
        {
            protected override string ExpectedNuspecVersion => "0.1.0+build.543";
            protected override string ExpectedPathVersion => "0.1.0";
            protected override string ExpectedSubDirectory => "PackageOutput";

            public override void Because()
            {
                MockLogger.Reset();
                Service.Pack(Configuration);
            }

            protected override string GetNuspecContent()
            {
                return NuspecContentWithBuildMetadata;
            }
        }

        [Categories.SemVer20]
        public class When_packaging_with_semver_20_pre_release_tag : ScenariosBase
        {
            protected override string ExpectedNuspecVersion => "0.1.0-rc.5+build.999";
            protected override string ExpectedPathVersion => "0.1.0-rc.5";
            protected override string ExpectedSubDirectory => "PackageOutput";

            public override void Because()
            {
                MockLogger.Reset();
                Service.Pack(Configuration);
            }

            protected override string GetNuspecContent()
            {
                return NuspecContentWithSemVer20PreReleaseVersioning;
            }
        }

        [Categories.LegacySemVer]
        public class When_packaging_with_legacy_pre_release_tag : ScenariosBase
        {
            protected override string ExpectedNuspecVersion => "0.1.0-rc-5";
            protected override string ExpectedSubDirectory => "PackageOutput";

            public override void Because()
            {
                MockLogger.Reset();
                Service.Pack(Configuration);
            }

            protected override string GetNuspecContent()
            {
                return NuspecContentWithLegacySemVerPreReleaseVersioning;
            }
        }

        [Categories.SemVer20]
        public class When_packaging_with_four_part_version_with_trailing_zero : ScenariosBase
        {
            private string _originalVersion = "0.1.0.0";
            protected override string ExpectedNuspecVersion => "0.1.0";
            protected override string ExpectedSubDirectory => "PackageOutput";

            public override void Because()
            {
                MockLogger.Reset();
                Service.Pack(Configuration);
            }

            protected override string GetNuspecContent()
            {
                return NuspecContentWithFormatableVersion.FormatWith(_originalVersion);
            }
        }

        [Categories.SemVer20]
        public class When_packaging_with_leading_zeros_four_part : ScenariosBase
        {
            private string _originalVersion = "01.02.03.04";
            protected override string ExpectedNuspecVersion => "1.2.3.4";
            protected override string ExpectedSubDirectory => "PackageOutput";

            public override void Because()
            {
                MockLogger.Reset();
                Service.Pack(Configuration);
            }

            protected override string GetNuspecContent()
            {
                return NuspecContentWithFormatableVersion.FormatWith(_originalVersion);
            }
        }

        [Categories.SemVer20]
        public class When_packaging_with_leading_zeros_three_part : ScenariosBase
        {
            private string _originalVersion = "01.02.04";
            protected override string ExpectedNuspecVersion => "1.2.4";
            protected override string ExpectedSubDirectory => "PackageOutput";

            public override void Because()
            {
                MockLogger.Reset();
                Service.Pack(Configuration);
            }

            protected override string GetNuspecContent()
            {
                return NuspecContentWithFormatableVersion.FormatWith(_originalVersion);
            }
        }

        [Categories.SemVer20]
        public class When_packaging_with_leading_zeros_two_part : ScenariosBase
        {
            private string _originalVersion = "01.02";
            protected override string ExpectedNuspecVersion => "1.2.0";
            protected override string ExpectedSubDirectory => "PackageOutput";

            public override void Because()
            {
                MockLogger.Reset();
                Service.Pack(Configuration);
            }

            protected override string GetNuspecContent()
            {
                return NuspecContentWithFormatableVersion.FormatWith(_originalVersion);
            }
        }

        [Categories.SemVer20]
        public class When_packaging_with_multiple_leading_zeros : ScenariosBase
        {
            private string _originalVersion = "0001.0002.0003";
            protected override string ExpectedNuspecVersion => "1.2.3";
            protected override string ExpectedSubDirectory => "PackageOutput";

            public override void Because()
            {
                MockLogger.Reset();
                Service.Pack(Configuration);
            }

            protected override string GetNuspecContent()
            {
                return NuspecContentWithFormatableVersion.FormatWith(_originalVersion);
            }
        }

        public class When_packing_with_properties : ScenariosBase
        {
            protected override string ExpectedNuspecVersion => "0.1.0";

            public override void Context()
            {
                base.Context();

                Scenario.Reset(Configuration);
                Configuration.Version = "0.1.0";
                Configuration.PackCommand.Properties.Add("commitId", "1234abcd");
                Scenario.AddFiles(new[] { new Tuple<string, string>("myPackage.nuspec", NuspecContentWithVariables) });
            }

            public override void Because()
            {
                MockLogger.Reset();
                Service.Pack(Configuration);
            }

            [Fact]
            public void Property_settings_should_be_logged_as_debug_messages()
            {
                var messages = MockLogger.MessagesFor(LogLevel.Debug);

                using (new AssertionScope())
                {
                    messages.Should().HaveCount(2);
                    messages.Should().ContainEquivalentOf("Setting property 'commitId': 1234abcd");
                    messages.Should().ContainEquivalentOf("Setting property 'version': 0.1.0");
                }
            }
        }

        public class When_packing_with_unsupported_elements : ScenariosInvalidBase
        {
            [Fact]
            public void Should_throw_exception_on_all_unsupported_elements()
            {
                AddFile("myPackage.nuspec", NuspecContentWithAllUnsupportedElements);

                ServiceAct.Should().Throw<System.IO.InvalidDataException>();
            }

            [Fact]
            public void Should_throw_exception_on_serviceable_element()
            {
                AddFile("myPackage.nuspec", NuspecContentWithServiceableElement);

                ServiceAct.Should().Throw<System.IO.InvalidDataException>();
            }

            [Fact]
            public void Should_throw_exception_on_license_element()
            {
                AddFile("myPackage.nuspec", NuspecContentWithLicenseElement);

                ServiceAct.Should().Throw<System.IO.InvalidDataException>();
            }

            [Fact]
            public void Should_throw_exception_on_repository_element()
            {
                AddFile("myPackage.nuspec", NuspecContentWithRepositoryElement);

                ServiceAct.Should().Throw<System.IO.InvalidDataException>();
            }

            [Fact]
            public void Should_throw_exception_on_package_types_element()
            {
                AddFile("myPackage.nuspec", NuspecContentWithPackageTypesElement);

                ServiceAct.Should().Throw<System.IO.InvalidDataException>();
            }

            [Fact]
            public void Should_throw_exception_on_framework_references_element()
            {
                AddFile("myPackage.nuspec", NuspecContentWithFrameWorkReferencesElement);

                ServiceAct.Should().Throw<System.IO.InvalidDataException>();
            }

            [Fact]
            public void Should_throw_exception_on_readme_element()
            {
                AddFile("myPackage.nuspec", NuspecContentWithReadmeElement);

                ServiceAct.Should().Throw<System.IO.InvalidDataException>();
            }

            [Fact]
            public void Should_throw_exception_on_icon_element()
            {
                AddFile("myPackage.nuspec", NuspecContentWithIconElement);

                ServiceAct.Should().Throw<System.IO.InvalidDataException>();
            }
        }

        public class When_packing_with_min_client_version : ScenariosBase
        {
            protected override string ExpectedNuspecVersion => "0.1.0";
            protected override string ExpectedSubDirectory => "PackageOutput";

            // This high version is to ensure that pack does not throw, even if the min client version is well
            // above both the Chocolatey and NuGet assembly versions.
            private string _minClientVersion = "100.0.0";

            public override void Because()
            {
                MockLogger.Reset();
                Service.Pack(Configuration);
            }

            protected override string GetNuspecContent()
            {
                return NuspecContentWithFormatableMinClientVersion.FormatWith(_minClientVersion);
            }
        }

        private const string NuspecContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>0.1.0</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";

        private const string NuspecContentWithBuildMetadata = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>0.1.0+build.543</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";

        private const string NuspecContentWithChocolateyData = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>0.1.0</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";

        private const string NuspecContentWithVariables = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>$version$</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>https://github.com/chocolatey/choco/tree/$commitId$/LICENSE</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";

        private const string NuspecContentWithSemVer20PreReleaseVersioning = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>0.1.0-rc.5+build.999</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";

        private const string NuspecContentWithLegacySemVerPreReleaseVersioning = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>0.1.0-rc-5</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";

        private const string NuspecContentWithOnlyMajorMinorVersion = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>0.3</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";

        private const string NuspecContentWithFull4PartVersioning = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>0.5.0.5</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";

        private const string NuspecContentWithAllUnsupportedElements = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>1.0.0</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <serviceable>true</serviceable>
    <license type=""expression"">MIT</license>
    <repository type=""git"" url=""https://github.com/NuGet/NuGet.Client.git"" branch=""dev"" commit=""e1c65e4524cd70ee6e22abe33e6cb6ec73938cb3"" />
    <packageTypes>
        <packageType name=""ContosoExtension"" />
    </packageTypes>
    <frameworkReferences>
      <group targetFramework="".NETCoreApp3.1"">
        <frameworkReference name=""Chocolatey.Cake.Recipe"" />
      </group>
    </frameworkReferences>
    <readme>readme.md</readme>
    <icon>icon.png</icon>
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";

        private const string NuspecContentWithServiceableElement = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>1.0.0</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <serviceable>true</serviceable>
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";


        private const string NuspecContentWithLicenseElement = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>1.0.0</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <license type=""expression"">MIT</license>
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";

        private const string NuspecContentWithRepositoryElement = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>1.0.0</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <repository type=""git"" url=""https://github.com/NuGet/NuGet.Client.git"" branch=""dev"" commit=""e1c65e4524cd70ee6e22abe33e6cb6ec73938cb3"" />
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";

        private const string NuspecContentWithPackageTypesElement = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>1.0.0</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <packageTypes>
        <packageType name=""ContosoExtension"" />
    </packageTypes>
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";

        private const string NuspecContentWithFrameWorkReferencesElement = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>1.0.0</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <frameworkReferences>
      <group targetFramework="".NETCoreApp3.1"">
        <frameworkReference name=""Chocolatey.Cake.Recipe"" />
      </group>
    </frameworkReferences>
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";

        private const string NuspecContentWithReadmeElement = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>1.0.0</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <readme>readme.md</readme>
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";

        private const string NuspecContentWithIconElement = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>1.0.0</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <icon>icon.png</icon>
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";

        private const string NuspecContentWithFormatableVersion = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata>
    <id>test-package</id>
    <title>Test Package</title>
    <version>{0}</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";

    private const string NuspecContentWithFormatableMinClientVersion = @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
  <metadata minClientVersion=""{0}"">
    <id>test-package</id>
    <title>Test Package</title>
    <version>0.1.0</version>
    <authors>package author</authors>
    <owners>package owner</owners>
    <summary>A brief summary</summary>
    <description>A big description</description>
    <tags>test admin</tags>
    <copyright></copyright>
    <licenseUrl>http://apache.org/2</licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <releaseNotes></releaseNotes>
    <dependencies>
      <dependency id=""chocolatey-core.extension"" />
    </dependencies>
  </metadata>
  <files>
  </files>
</package>";
    }
}
