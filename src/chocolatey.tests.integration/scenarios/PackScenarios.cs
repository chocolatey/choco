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

    using Should;

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
                Configuration = Scenario.pack();
                Scenario.reset(Configuration);

                Service = NUnitSetup.Container.GetInstance<INugetService>();
                MockLogger.reset();
                ServiceAct = () => Service.pack_run(Configuration);
            }

            protected void AddFile(string fileName, string fileContent)
            {
                Scenario.add_files(new[] { new Tuple<string, string>(fileName, fileContent) });
            }
        }

        [Categories.ExceptionHandling]
        public class when_invalid_data_is_used_in_nuspec_file : ScenariosInvalidBase
        {
            [Fact]
            public void should_throw_xml_exception_on_empty_nuspec_file()
            {
                AddFile("myPackage.nuspec", string.Empty);

                ServiceAct.ShouldThrow<XmlException>();
            }

            [TestCase("")]
            [TestCase("invalid_version")]
            public void should_throw_invalid_data_exception_on_invalid_version(string version)
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
</package>".format_with(version));

                ServiceAct.ShouldThrow<InvalidDataException>();
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
                        return Path.Combine(Scenario.get_top_level(), "test-package." + ExpectedPathVersion + ".nupkg");
                    }

                    return Path.Combine(ExpectedSubDirectory, "test-package." + ExpectedPathVersion + ".nupkg");
                }
            }

            protected abstract string ExpectedNuspecVersion { get; }

            protected virtual string ExpectedPathVersion => ExpectedNuspecVersion;

            protected virtual string ExpectedSubDirectory { get; } = string.Empty;

            public override void Context()
            {
                Configuration = Scenario.pack();
                Scenario.reset(Configuration);
                Scenario.add_files(new[] { new Tuple<string, string>("myPackage.nuspec", GetNuspecContent()) });

                if (!string.IsNullOrEmpty(ExpectedSubDirectory))
                {
                    Configuration.OutputDirectory = ExpectedSubDirectory;
                    Scenario.create_directory(Configuration.OutputDirectory);
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
            public void generated_package_should_be_in_current_directory()
            {
                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Count.ShouldEqual(2);
                infos[0].ShouldEqual("Attempting to build package from 'myPackage.nuspec'.");
                infos[1].ShouldEqual(string.Concat("Successfully created package '", PackagePath, "'"));

                FileAssert.Exists(PackagePath);
            }

            [Fact]
            public void generated_package_should_include_expected_version_in_nuspec()
            {
                using (var packageReader = new PackageArchiveReader(PackagePath))
                {
                    var version = packageReader.NuspecReader.GetVersion();

                    version.ToFullString().ShouldEqual(ExpectedNuspecVersion);
                }
            }

            [Fact]
            public void sources_should_be_set_to_current_directory()
            {
                if (string.IsNullOrEmpty(ExpectedSubDirectory))
                {
                    Configuration.Sources.ShouldEqual(Scenario.get_top_level());
                }
                else
                {
                    Configuration.Sources.ShouldEqual(ExpectedSubDirectory);
                }
            }

            protected virtual string GetNuspecContent()
            {
                return NuspecContent;
            }
        }

        public class when_packing_without_specifying_an_output_directory : ScenariosBase
        {
            protected override string ExpectedNuspecVersion => "0.1.0";

            public override void Because()
            {
                MockLogger.reset();
                Service.pack_run(Configuration);
            }
        }

        public class when_packing_with_an_output_directory : ScenariosBase
        {
            protected override string ExpectedNuspecVersion => "0.1.0";
            protected override string ExpectedSubDirectory => "PackageOutput";

            public override void Because()
            {
                MockLogger.reset();
                Service.pack_run(Configuration);
            }
        }

        [Categories.LegacySemVer]
        public class when_packing_with_only_major_minor_version : ScenariosBase
        {
            protected override string PackagePath => Path.Combine("PackageOutput", "test-package.0.3.0.nupkg");
            protected override string ExpectedNuspecVersion => "0.3.0";
            protected override string ExpectedSubDirectory => "PackageOutput";

            public override void Because()
            {
                MockLogger.reset();
                Service.pack_run(Configuration);
            }

            protected override string GetNuspecContent()
            {
                return NuspecContentWithOnlyMajorMinorVersion;
            }
        }

        [Categories.LegacySemVer]
        public class when_packing_with_full_4_part_versioning_scheme : ScenariosBase
        {
            protected override string ExpectedNuspecVersion => "0.5.0.5";
            protected override string ExpectedSubDirectory => "PackageOutput";

            public override void Because()
            {
                MockLogger.reset();
                Service.pack_run(Configuration);
            }

            protected override string GetNuspecContent()
            {
                return NuspecContentWithFull4PartVersioning;
            }
        }

        [Categories.SemVer20]
        public class when_packaging_with_build_metadata : ScenariosBase
        {
            protected override string ExpectedNuspecVersion => "0.1.0+build.543";
            protected override string ExpectedPathVersion => "0.1.0";
            protected override string ExpectedSubDirectory => "PackageOutput";

            public override void Because()
            {
                MockLogger.reset();
                Service.pack_run(Configuration);
            }

            protected override string GetNuspecContent()
            {
                return NuspecContentWithBuildMetadata;
            }
        }

        [Categories.SemVer20]
        public class when_packaging_with_semver_20_pre_release_tag : ScenariosBase
        {
            protected override string ExpectedNuspecVersion => "0.1.0-rc.5+build.999";
            protected override string ExpectedPathVersion => "0.1.0-rc.5";
            protected override string ExpectedSubDirectory => "PackageOutput";

            public override void Because()
            {
                MockLogger.reset();
                Service.pack_run(Configuration);
            }

            protected override string GetNuspecContent()
            {
                return NuspecContentWithSemVer20PreReleaseVersioning;
            }
        }

        [Categories.LegacySemVer]
        public class when_packaging_with_legacy_pre_release_tag : ScenariosBase
        {
            protected override string ExpectedNuspecVersion => "0.1.0-rc-5";
            protected override string ExpectedSubDirectory => "PackageOutput";

            public override void Because()
            {
                MockLogger.reset();
                Service.pack_run(Configuration);
            }

            protected override string GetNuspecContent()
            {
                return NuspecContentWithLegacySemVerPreReleaseVersioning;
            }
        }

        public class when_packing_with_properties : ScenariosBase
        {
            protected override string ExpectedNuspecVersion => "0.1.0";

            public override void Context()
            {
                base.Context();

                Scenario.reset(Configuration);
                Configuration.Version = "0.1.0";
                Configuration.PackCommand.Properties.Add("commitId", "1234abcd");
                Scenario.add_files(new[] { new Tuple<string, string>("myPackage.nuspec", NuspecContentWithVariables) });
            }

            public override void Because()
            {
                MockLogger.reset();
                Service.pack_run(Configuration);
            }

            [Fact]
            public void property_settings_should_be_logged_as_debug_messages()
            {
                var messages = MockLogger.MessagesFor(LogLevel.Debug);
                messages.Count.ShouldEqual(2);
                messages.ShouldContain("Setting property 'commitId': 1234abcd");
                messages.ShouldContain("Setting property 'version': 0.1.0");
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
  </metadata>
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
  </metadata>
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
  </metadata>
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
  </metadata>
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
  </metadata>
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
  </metadata>
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
  </metadata>
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
  </metadata>
</package>";
    }
}
