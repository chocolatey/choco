
// Copyright © 2011 - Present RealDimensions Software, LLC
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
    using Should;
    using System;
    using System.IO;
    using bdddoc.core;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;

    public class PackScenarios
    {
        public abstract class ScenariosBase : TinySpec
        {
            protected ChocolateyConfiguration Configuration;
            protected INugetService Service;

            public override void Context()
            {
                Configuration = Scenario.pack();
                Scenario.reset(Configuration);
                Scenario.add_files(new[] { new Tuple<string, string>("myPackage.nuspec", NuspecContent) });

                Service = NUnitSetup.Container.GetInstance<INugetService>();
            }
        }

        [Concern(typeof(ChocolateyPackCommand))]
        public class when_packing_without_specifying_an_output_directory : ScenariosBase
        {
            public override void Because()
            {
                MockLogger.reset();
                Service.pack_run(Configuration);
            }

            [Fact]
            public void generated_package_should_be_in_current_directory()
            {
                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Count.ShouldEqual(2);
                infos[0].ShouldEqual("Attempting to build package from 'myPackage.nuspec'.");
                infos[1].ShouldStartWith("Successfully created package '");
                infos[1].ShouldContain("test-package.0.1.0.nupkg'");

                File.Exists("test-package.0.1.0.nupkg").ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyPackCommand))]
        public class when_packing_with_an_output_directory : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.OutputDirectory = "PackageOutput";
                Scenario.create_directory(Configuration.OutputDirectory);
            }

            public override void Because()
            {
                MockLogger.reset();
                Service.pack_run(Configuration);
            }

            [Fact]
            public void generated_package_should_be_in_specified_output_directory()
            {
                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Count.ShouldEqual(2);
                infos[0].ShouldEqual("Attempting to build package from 'myPackage.nuspec'.");
                infos[1].ShouldStartWith("Successfully created package '");
                infos[1].ShouldContain(Path.Combine("PackageOutput", "test-package.0.1.0.nupkg'"));

                File.Exists(Path.Combine("PackageOutput", "test-package.0.1.0.nupkg")).ShouldBeTrue();
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
    }
}
