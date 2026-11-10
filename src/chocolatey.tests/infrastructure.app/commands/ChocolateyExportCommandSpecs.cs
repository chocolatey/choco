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
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using chocolatey.infrastructure.app.attributes;
using chocolatey.infrastructure.app.commands;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.services;
using chocolatey.infrastructure.commandline;
using chocolatey.infrastructure.filesystem;
using chocolatey.infrastructure.results;
using Moq;
using NuGet.Packaging;
using NuGet.Versioning;
using FluentAssertions;

namespace chocolatey.tests.infrastructure.app.commands
{
    public class ChocolateyExportCommandSpecs
    {
        [ConcernFor("export")]
        public abstract class ChocolateyExportCommandSpecsBase : TinySpec
        {
            protected ChocolateyExportCommand Command;
            protected Mock<INugetService> NugetService = new Mock<INugetService>();
            protected Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                Command = new ChocolateyExportCommand(NugetService.Object, FileSystem.Object);
            }

            public void Reset()
            {
                NugetService.ResetCalls();
                FileSystem.ResetCalls();
            }
        }

        public class When_implementing_command_for : ChocolateyExportCommandSpecsBase
        {
            private List<string> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_help()
            {
                _results.Should().Contain("export");
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyExportCommandSpecsBase
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
            public void Should_add_output_file_path_to_the_option_set()
            {
                _optionSet.Contains("output-file-path").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_output_file_path_to_the_option_set()
            {
                _optionSet.Contains("o").Should().BeTrue();
            }

            [Fact]
            public void Should_add_include_version_numbers_to_the_option_set()
            {
                _optionSet.Contains("include-version-numbers").Should().BeTrue();
            }

            [Fact]
            public void Should_add_include_version_to_the_option_set()
            {
                _optionSet.Contains("include-version").Should().BeTrue();
            }
        }

        public class When_handling_additional_argument_parsing : ChocolateyExportCommandSpecsBase
        {
            private readonly IList<string> _unparsedArgs = new List<string>();
            private Action _because;

            public override void Because()
            {
                _because = () => Command.ParseAdditionalArguments(_unparsedArgs, Configuration);
            }

            public new void Reset()
            {
                Configuration.ExportCommand.OutputFilePath = string.Empty;
                _unparsedArgs.Clear();
                base.Reset();
            }

            [Fact]
            public void Should_handle_passing_in_an_empty_string_for_output_file_path()
            {
                Reset();
                _unparsedArgs.Add(" ");
                _because();

                Configuration.ExportCommand.OutputFilePath.Should().Be("packages.config");
            }

            [Fact]
            public void Should_handle_passing_in_a_string_for_output_file_path()
            {
                Reset();
                _unparsedArgs.Add("custompackages.config");
                _because();

                Configuration.ExportCommand.OutputFilePath.Should().Be("custompackages.config");
            }
        }

        public class When_noop_is_called : ChocolateyExportCommandSpecsBase
        {
            public override void Because()
            {
                Command.DryRun(Configuration);
            }

            [Fact]
            public void Should_log_a_message()
            {
                MockLogger.Verify(l => l.Info(It.IsAny<string>()), Times.AtLeastOnce);
            }

            [Fact]
            public void Should_log_the_message_we_expect()
            {
                var messages = MockLogger.MessagesFor(LogLevel.Info);
                messages.Should().ContainSingle();
                messages[0].Should().Contain("Export would have been with options");
            }
        }

        public class When_run_is_called : ChocolateyExportCommandSpecsBase
        {
            public new void Reset()
            {
                Context();
                base.Reset();
            }

            public override void AfterEachSpec()
            {
                base.AfterEachSpec();
                MockLogger.Messages.Clear();
            }

            public override void Because()
            {
                // because = () => command.run(configuration);
            }

            [Fact]
            public void Should_call_nuget_service_get_all_installed_packages()
            {
                Reset();
                Command.Run(Configuration);

                NugetService.Verify(n => n.GetInstalledPackages(It.IsAny<ChocolateyConfiguration>()), Times.Once);
            }

            [Fact]
            public void Should_call_replace_file_when_file_already_exists()
            {
                FileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);

                Reset();
                Command.Run(Configuration);

                FileSystem.Verify(n => n.ReplaceFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }

            [Fact]
            public void Should_not_call_replace_file_when_file_doesnt_exist()
            {
                FileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);

                Reset();
                Command.Run(Configuration);

                FileSystem.Verify(n => n.ReplaceFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            }
        }

        // Both overridable members are overridden here by a command that records when it is called.
        public class RecordingExportCommand : ChocolateyExportCommand
        {
            public readonly List<string> Events = new List<string>();
            public IEnumerable<PackageResult> PackagesToExport;
            public Func<PackageResult, string> InstanceOf;

            public RecordingExportCommand(INugetService nugetService, IFileSystem fileSystem)
                : base(nugetService, fileSystem)
            {
            }

            protected override IEnumerable<PackageResult> GetPackagesToExport(ChocolateyConfiguration configuration)
            {
                Events.Add("GetPackagesToExport");
                return PackagesToExport ?? base.GetPackagesToExport(configuration);
            }

            protected override void WriteAdditionalPackageAttributes(XmlWriter writer, PackageResult packageResult, ChocolateyConfiguration configuration)
            {
                Events.Add("WriteAdditionalPackageAttributes:" + packageResult.PackageMetadata.Id);
                if (InstanceOf != null)
                {
                    writer.WriteAttributeString("instance", InstanceOf(packageResult));
                }

                base.WriteAdditionalPackageAttributes(writer, packageResult, configuration);
            }
        }

        public abstract class ExportSeamSpecsBase : TinySpec
        {
            protected RecordingExportCommand Command;
            protected Mock<INugetService> NugetService;
            protected Mock<IFileSystem> FileSystem;
            protected ChocolateyConfiguration Configuration;
            protected string Written;

            protected static PackageResult PackageResultFor(string id, string version)
            {
                var package = new Mock<IPackageMetadata>();
                package.Setup(x => x.Id).Returns(id);
                package.Setup(x => x.Version).Returns(NuGetVersion.Parse(version));

                return new PackageResult(package.Object, @"c:\lib\" + id);
            }

            public override void Context()
            {
                NugetService = new Mock<INugetService>();
                FileSystem = new Mock<IFileSystem>();
                Configuration = new ChocolateyConfiguration();
                Configuration.ExportCommand.OutputFilePath = @"c:\out\packages.config";

                FileSystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns<string>(path => path);
                FileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
                FileSystem.Setup(f => f.WriteFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Encoding>()))
                    .Callback<string, string, Encoding>((path, text, encoding) => Written = text);

                NugetService.Setup(n => n.GetInstalledPackages(It.IsAny<ChocolateyConfiguration>()))
                    .Returns(new[] { PackageResultFor("bob", "1.0.0"), PackageResultFor("dep", "2.0.0") });

                Command = new RecordingExportCommand(NugetService.Object, FileSystem.Object);
            }

            protected IEnumerable<string> Ids()
            {
                return XDocument.Parse(Written).Root.Elements("package").Select(p => (string)p.Attribute("id"));
            }
        }

        public class When_exporting_with_a_command_that_overrides_nothing : ExportSeamSpecsBase
        {
            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_export_the_packages_the_service_has_installed()
            {
                Ids().Should().Equal("bob", "dep");
            }

            [Fact]
            public void Should_write_nothing_but_the_id_when_versions_are_not_included()
            {
                Written.Should().Contain("<package id=\"bob\" />").And.Contain("<package id=\"dep\" />");
            }

            [Fact]
            public void Should_ask_for_the_packages_and_then_for_the_attributes_of_each()
            {
                Command.Events.Should().Equal("GetPackagesToExport", "WriteAdditionalPackageAttributes:bob", "WriteAdditionalPackageAttributes:dep");
            }
        }

        public class When_exporting_with_version_numbers_with_a_command_that_overrides_nothing : ExportSeamSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.ExportCommand.IncludeVersionNumbers = true;
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_write_the_version_after_the_id()
            {
                Written.Should().Contain("<package id=\"bob\" version=\"1.0.0\" />").And.Contain("<package id=\"dep\" version=\"2.0.0\" />");
            }
        }

        public class When_exporting_with_a_command_that_chooses_the_packages : ExportSeamSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Command.PackagesToExport = new[] { PackageResultFor("chosen", "3.0.0") };
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_export_only_the_packages_it_chose()
            {
                Ids().Should().Equal("chosen");
            }

            [Fact]
            public void Should_not_ask_the_service_for_the_installed_packages()
            {
                NugetService.Verify(n => n.GetInstalledPackages(It.IsAny<ChocolateyConfiguration>()), Times.Never);
            }
        }

        public class When_exporting_with_a_command_that_adds_an_attribute_to_each_package : ExportSeamSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Command.InstanceOf = result => "instance-of-" + result.PackageMetadata.Id;
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_write_the_attribute_after_the_id_for_each_package()
            {
                Written.Should().Contain("<package id=\"bob\" instance=\"instance-of-bob\" />").And.Contain("<package id=\"dep\" instance=\"instance-of-dep\" />");
            }
        }

        public class When_exporting_with_version_numbers_with_a_command_that_adds_an_attribute_to_each_package : ExportSeamSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.ExportCommand.IncludeVersionNumbers = true;
                Command.InstanceOf = result => "first";
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_write_the_attribute_between_the_id_and_the_version()
            {
                Written.Should().Contain("<package id=\"bob\" instance=\"first\" version=\"1.0.0\" />");
            }
        }
    }
}
