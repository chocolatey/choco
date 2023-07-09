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

namespace chocolatey.tests.infrastructure.app.services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.app.templates;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.services;
    using Moq;
    using NuGet.Common;
    using NUnit.Framework;
    using FluentAssertions;
    using LogLevel = tests.LogLevel;

    public class TemplateServiceSpecs
    {
        public abstract class TemplateServiceSpecsBase : TinySpec
        {
            protected TemplateService Service;
            protected Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();
            protected Mock<IXmlService> XmlService = new Mock<IXmlService>();
            protected Mock<ILogger> Logger = new Mock<ILogger>();

            public override void Context()
            {
                FileSystem.ResetCalls();
                XmlService.ResetCalls();
                Service = new TemplateService(FileSystem.Object, XmlService.Object, Logger.Object);
            }
        }

        public class When_generate_noop_is_called : TemplateServiceSpecsBase
        {
            private Action _because;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();
                FileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                FileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string a, string[] b) => { return a + "\\" + b[0]; });
                _config.NewCommand.Name = "Bob";
            }

            public override void Because()
            {
                _because = () => Service.GenerateDryRun(_config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            public void Should_log_current_directory_if_no_outputdirectory()
            {
                _because();

                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Should().ContainSingle();
                infos.Should().HaveElementAt(0,"Would have generated a new package specification at c:\\chocolatey\\Bob");
            }

            [Fact]
            public void Should_log_output_directory_if_outputdirectory_is_specified()
            {
                _config.OutputDirectory = "c:\\packages";

                _because();

                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Should().ContainSingle();
                infos.Should().HaveElementAt(0,"Would have generated a new package specification at c:\\packages\\Bob");
            }
        }

        public class When_generate_file_from_template_is_called : TemplateServiceSpecsBase
        {
            private Action _because;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();
            private readonly TemplateValues _templateValues = new TemplateValues();
            private readonly string _template = "[[PackageName]]";
            private const string FileLocation = "c:\\packages\\bob.nuspec";
            private string _fileContent;

            public override void Context()
            {
                base.Context();

                FileSystem.Setup(x => x.WriteFile(It.Is((string fl) => fl == FileLocation), It.IsAny<string>(), Encoding.UTF8))
                    .Callback((string filePath, string fileContent, Encoding encoding) => _fileContent = fileContent);

                _templateValues.PackageName = "Bob";
            }

            public override void Because()
            {
                _because = () => Service.GenerateFileFromTemplate(_config, _templateValues, _template, FileLocation, Encoding.UTF8);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            public void Should_write_file_withe_replaced_tokens()
            {
                _because();

                var debugs = MockLogger.MessagesFor(LogLevel.Debug);
                debugs.Should().ContainSingle();
                debugs.Should().HaveElementAt(0,"Bob");
            }

            [Fact]
            public void Should_log_info_if_regular_output()
            {
                _config.RegularOutput = true;

                _because();

                var debugs = MockLogger.MessagesFor(LogLevel.Debug);
                debugs.Should().ContainSingle();
                debugs.Should().HaveElementAt(0,"Bob");

                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Should().ContainSingle();
                infos.Should().HaveElementAt(0,string.Format(@"Generating template to a file{0} at 'c:\packages\bob.nuspec'", Environment.NewLine));
            }
        }

        public class When_generate_is_called_with_existing_directory : TemplateServiceSpecsBase
        {
            private Action _because;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();
            private string _verifiedDirectoryPath;

            public override void Context()
            {
                base.Context();

                FileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                FileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string a, string[] b) => { return a + "\\" + b[0]; });
                FileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns<string>(
                    x =>
                    {
                        _verifiedDirectoryPath = x;
                        return true;
                    });

                _config.NewCommand.Name = "Bob";
            }

            public override void Because()
            {
                _because = () => Service.Generate(_config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            public void Should_throw_exception()
            {
                bool errored = false;
                string errorMessage = string.Empty;

                try
                {
                    _because();
                }
                catch (ApplicationException ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                errored.Should().BeTrue();
                errorMessage.Should().Be(string.Format("The location for the template already exists. You can:{0} 1. Remove 'c:\\chocolatey\\Bob'{0} 2. Use --force{0} 3. Specify a different name", Environment.NewLine));
                _verifiedDirectoryPath.Should().Be("c:\\chocolatey\\Bob");
            }

            [Fact]
            public void Should_throw_exception_even_with_outputdirectory()
            {
                _config.OutputDirectory = "c:\\packages";

                bool errored = false;
                string errorMessage = string.Empty;

                try
                {
                    _because();
                }
                catch (ApplicationException ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                errored.Should().BeTrue();
                errorMessage.Should().Be(string.Format("The location for the template already exists. You can:{0} 1. Remove 'c:\\packages\\Bob'{0} 2. Use --force{0} 3. Specify a different name", Environment.NewLine));
                _verifiedDirectoryPath.Should().Be("c:\\packages\\Bob");
            }
        }

        public class When_generate_is_called : TemplateServiceSpecsBase
        {
            private Action _because;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();
            private readonly List<string> _files = new List<string>();
            private readonly HashSet<string> _directoryCreated = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            private readonly UTF8Encoding _utf8WithoutBOM = new UTF8Encoding(false);

            public override void Context()
            {
                base.Context();

                FileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                FileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(
                        (string a, string[] b) =>
                        {
                            if (a.EndsWith("templates") && b[0] == "default")
                            {
                                return "templates\\default";
                            }
                            return a + "\\" + b[0];
                        });
                FileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns<string>(dirPath => dirPath.EndsWith("templates\\default"));
                FileSystem.Setup(x => x.WriteFile(It.IsAny<string>(), It.IsAny<string>(), Encoding.UTF8))
                    .Callback((string filePath, string fileContent, Encoding encoding) => _files.Add(filePath));
                FileSystem.Setup(x => x.WriteFile(It.IsAny<string>(), It.IsAny<string>(), _utf8WithoutBOM))
                    .Callback((string filePath, string fileContent, Encoding encoding) => _files.Add(filePath));
                FileSystem.Setup(x => x.DeleteDirectoryChecked(It.IsAny<string>(), true));
                FileSystem.Setup(x => x.EnsureDirectoryExists(It.IsAny<string>())).Callback(
                    (string directory) =>
                    {
                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            _directoryCreated.Add(directory);
                        }
                    });
                FileSystem.Setup(x => x.GetFiles(It.IsAny<string>(), "*.*", SearchOption.AllDirectories)).Returns(new[] { "templates\\default\\template.nuspec", "templates\\default\\random.txt" });
                FileSystem.Setup(x => x.GetDirectoryName(It.IsAny<string>())).Returns<string>(file => Path.GetDirectoryName(file));
                FileSystem.Setup(x => x.GetFileExtension(It.IsAny<string>())).Returns<string>(file => Path.GetExtension(file));
                FileSystem.Setup(x => x.ReadFile(It.IsAny<string>())).Returns(string.Empty);

                _config.NewCommand.Name = "Bob";
            }

            public override void Because()
            {
                _because = () => Service.Generate(_config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
                _files.Clear();
                _directoryCreated.Clear();
            }

            [Fact]
            public void Should_generate_all_files_and_directories()
            {
                _because();

                var directories = _directoryCreated.ToList();
                directories.Should().HaveCount(2, "There should be 2 directories, but there was: " + string.Join(", ", directories));
                directories.Should().HaveElementAt(0,"c:\\chocolatey\\Bob");
                directories.Should().HaveElementAt(1,"c:\\chocolatey\\Bob\\tools");

                _files.Should().HaveCount(2, "There should be 2 files, but there was: " + string.Join(", ", _files));
                _files.Should().HaveElementAt(0, "c:\\chocolatey\\Bob\\__name_replace__.nuspec");
                _files.Should().HaveElementAt(1, "c:\\chocolatey\\Bob\\random.txt");

                MockLogger.MessagesFor(LogLevel.Info).Last().Should().Be(string.Format(@"Successfully generated Bob package specification files{0} at 'c:\chocolatey\Bob'", Environment.NewLine));
            }

            [Fact]
            public void Should_generate_all_files_and_directories_even_with_outputdirectory()
            {
                _config.OutputDirectory = "c:\\packages";

                _because();

                var directories = _directoryCreated.ToList();
                directories.Should().HaveCount(2, "There should be 2 directories, but there was: " + string.Join(", ", directories));
                directories.Should().HaveElementAt(0,"c:\\packages\\Bob");
                directories.Should().HaveElementAt(1,"c:\\packages\\Bob\\tools");

                _files.Should().HaveCount(2, "There should be 2 files, but there was: " + string.Join(", ", _files));
                _files.Should().HaveElementAt(0, "c:\\packages\\Bob\\__name_replace__.nuspec");
                _files.Should().HaveElementAt(1, "c:\\packages\\Bob\\random.txt");

                MockLogger.MessagesFor(LogLevel.Info).Last().Should().Be(string.Format(@"Successfully generated Bob package specification files{0} at 'c:\packages\Bob'", Environment.NewLine));
            }
        }

        public class When_generate_is_called_with_nested_folders : TemplateServiceSpecsBase
        {
            private Action _because;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();
            private readonly List<string> _files = new List<string>();
            private readonly HashSet<string> _directoryCreated = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            private readonly UTF8Encoding _utf8WithoutBOM = new UTF8Encoding(false);

            public override void Context()
            {
                base.Context();

                FileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                FileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(
                        (string a, string[] b) =>
                        {
                            if (a.EndsWith("templates") && b[0] == "test")
                            {
                                return "templates\\test";
                            }
                            return a + "\\" + b[0];
                        });
                FileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns<string>(dirPath => dirPath.EndsWith("templates\\test"));
                FileSystem.Setup(x => x.WriteFile(It.IsAny<string>(), It.IsAny<string>(), Encoding.UTF8))
                    .Callback((string filePath, string fileContent, Encoding encoding) => _files.Add(filePath));
                FileSystem.Setup(x => x.WriteFile(It.IsAny<string>(), It.IsAny<string>(), _utf8WithoutBOM))
                    .Callback((string filePath, string fileContent, Encoding encoding) => _files.Add(filePath));
                FileSystem.Setup(x => x.DeleteDirectoryChecked(It.IsAny<string>(), true));
                FileSystem.Setup(x => x.GetFiles(It.IsAny<string>(), "*.*", SearchOption.AllDirectories))
                    .Returns(new[] { "templates\\test\\template.nuspec", "templates\\test\\random.txt", "templates\\test\\tools\\chocolateyInstall.ps1", "templates\\test\\tools\\lower\\another.ps1" });
                FileSystem.Setup(x => x.GetDirectories(It.IsAny<string>(), "*.*", SearchOption.AllDirectories))
                    .Returns(new[] { "templates\\test", "templates\\test\\tools", "templates\\test\\tools\\lower" });
                FileSystem.Setup(x => x.EnsureDirectoryExists(It.IsAny<string>())).Callback(
                    (string directory) =>
                    {
                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            _directoryCreated.Add(directory);
                        }
                    });
                FileSystem.Setup(x => x.GetDirectoryName(It.IsAny<string>())).Returns<string>(file => Path.GetDirectoryName(file));
                FileSystem.Setup(x => x.GetFileExtension(It.IsAny<string>())).Returns<string>(file => Path.GetExtension(file));
                FileSystem.Setup(x => x.ReadFile(It.IsAny<string>())).Returns(string.Empty);

                _config.NewCommand.Name = "Bob";
                _config.NewCommand.TemplateName = "test";
            }

            public override void Because()
            {
                _because = () => Service.Generate(_config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
                _files.Clear();
                _directoryCreated.Clear();
            }

            [Fact]
            public void Should_generate_all_files_and_directories()
            {
                _because();

                var directories = _directoryCreated.ToList();
                directories.Should().HaveCount(3, "There should be 3 directories, but there was: " + string.Join(", ", directories));
                directories.Should().HaveElementAt(0,"c:\\chocolatey\\Bob");
                directories.Should().HaveElementAt(1,"c:\\chocolatey\\Bob\\tools");
                directories.Should().HaveElementAt(2,"c:\\chocolatey\\Bob\\tools\\lower");

                _files.Should().HaveCount(4, "There should be 4 files, but there was: " + string.Join(", ", _files));
                _files.Should().HaveElementAt(0, "c:\\chocolatey\\Bob\\__name_replace__.nuspec");
                _files.Should().HaveElementAt(1, "c:\\chocolatey\\Bob\\random.txt");
                _files.Should().HaveElementAt(2, "c:\\chocolatey\\Bob\\tools\\chocolateyInstall.ps1");
                _files.Should().HaveElementAt(3, "c:\\chocolatey\\Bob\\tools\\lower\\another.ps1");

                MockLogger.MessagesFor(LogLevel.Info).Last().Should().Be(string.Format(@"Successfully generated Bob package specification files{0} at 'c:\chocolatey\Bob'", Environment.NewLine));
            }

            [Fact]
            public void Should_generate_all_files_and_directories_even_with_outputdirectory()
            {
                _config.OutputDirectory = "c:\\packages";

                _because();

                var directories = _directoryCreated.ToList();
                directories.Should().HaveCount(3, "There should be 3 directories, but there was: " + string.Join(", ", directories));
                directories.Should().HaveElementAt(0,"c:\\packages\\Bob");
                directories.Should().HaveElementAt(1,"c:\\packages\\Bob\\tools");
                directories.Should().HaveElementAt(2,"c:\\packages\\Bob\\tools\\lower");

                _files.Should().HaveCount(4, "There should be 4 files, but there was: " + string.Join(", ", _files));
                _files.Should().HaveElementAt(0, "c:\\packages\\Bob\\__name_replace__.nuspec");
                _files.Should().HaveElementAt(1, "c:\\packages\\Bob\\random.txt");
                _files.Should().HaveElementAt(2, "c:\\packages\\Bob\\tools\\chocolateyInstall.ps1");
                _files.Should().HaveElementAt(3, "c:\\packages\\Bob\\tools\\lower\\another.ps1");

                MockLogger.MessagesFor(LogLevel.Info).Last().Should().Be(string.Format(@"Successfully generated Bob package specification files{0} at 'c:\packages\Bob'", Environment.NewLine));
            }
        }

        public class When_generate_is_called_with_empty_nested_folders : TemplateServiceSpecsBase
        {
            private Action _because;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();
            private readonly List<string> _files = new List<string>();
            private readonly HashSet<string> _directoryCreated = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            private readonly UTF8Encoding _utf8WithoutBOM = new UTF8Encoding(false);

            public override void Context()
            {
                base.Context();

                FileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                FileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(
                        (string a, string[] b) =>
                        {
                            if (a.EndsWith("templates") && b[0] == "test")
                            {
                                return "templates\\test";
                            }
                            return a + "\\" + b[0];
                        });
                FileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns<string>(dirPath => dirPath.EndsWith("templates\\test"));
                FileSystem.Setup(x => x.WriteFile(It.IsAny<string>(), It.IsAny<string>(), Encoding.UTF8))
                    .Callback((string filePath, string fileContent, Encoding encoding) => _files.Add(filePath));
                FileSystem.Setup(x => x.WriteFile(It.IsAny<string>(), It.IsAny<string>(), _utf8WithoutBOM))
                    .Callback((string filePath, string fileContent, Encoding encoding) => _files.Add(filePath));
                FileSystem.Setup(x => x.DeleteDirectoryChecked(It.IsAny<string>(), true));
                FileSystem.Setup(x => x.GetFiles(It.IsAny<string>(), "*.*", SearchOption.AllDirectories))
                    .Returns(new[] { "templates\\test\\template.nuspec", "templates\\test\\random.txt", "templates\\test\\tools\\chocolateyInstall.ps1", "templates\\test\\tools\\lower\\another.ps1" });
                FileSystem.Setup(x => x.GetDirectories(It.IsAny<string>(), "*.*", SearchOption.AllDirectories))
                    .Returns(new[] { "templates\\test", "templates\\test\\tools", "templates\\test\\tools\\lower", "templates\\test\\empty", "templates\\test\\empty\\nested" });
                FileSystem.Setup(x => x.EnsureDirectoryExists(It.IsAny<string>())).Callback(
                    (string directory) =>
                    {
                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            _directoryCreated.Add(directory);
                        }
                    });
                FileSystem.Setup(x => x.GetDirectoryName(It.IsAny<string>())).Returns<string>(file => Path.GetDirectoryName(file));
                FileSystem.Setup(x => x.GetFileExtension(It.IsAny<string>())).Returns<string>(file => Path.GetExtension(file));
                FileSystem.Setup(x => x.ReadFile(It.IsAny<string>())).Returns(string.Empty);

                _config.NewCommand.Name = "Bob";
                _config.NewCommand.TemplateName = "test";
            }

            public override void Because()
            {
                _because = () => Service.Generate(_config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
                _files.Clear();
                _directoryCreated.Clear();
            }

            [Fact]
            public void Should_generate_all_files_and_directories()
            {
                _because();

                var directories = _directoryCreated.ToList();
                directories.Should().HaveCount(5, "There should be 5 directories, but there was: " + string.Join(", ", directories));
                directories.Should().HaveElementAt(0,"c:\\chocolatey\\Bob");
                directories.Should().HaveElementAt(1,"c:\\chocolatey\\Bob\\tools");
                directories.Should().HaveElementAt(2,"c:\\chocolatey\\Bob\\tools\\lower");
                directories.Should().HaveElementAt(3,"c:\\chocolatey\\Bob\\empty");
                directories.Should().HaveElementAt(4,"c:\\chocolatey\\Bob\\empty\\nested");

                _files.Should().HaveCount(4, "There should be 4 files, but there was: " + string.Join(", ", _files));
                _files.Should().HaveElementAt(0, "c:\\chocolatey\\Bob\\__name_replace__.nuspec");
                _files.Should().HaveElementAt(1, "c:\\chocolatey\\Bob\\random.txt");
                _files.Should().HaveElementAt(2, "c:\\chocolatey\\Bob\\tools\\chocolateyInstall.ps1");
                _files.Should().HaveElementAt(3, "c:\\chocolatey\\Bob\\tools\\lower\\another.ps1");

                MockLogger.MessagesFor(LogLevel.Info).Last().Should().Be(string.Format(@"Successfully generated Bob package specification files{0} at 'c:\chocolatey\Bob'", Environment.NewLine));
            }

            [Fact]
            public void Should_generate_all_files_and_directories_even_with_outputdirectory()
            {
                _config.OutputDirectory = "c:\\packages";

                _because();

                var directories = _directoryCreated.ToList();
                directories.Should().HaveCount(5, "There should be 5 directories, but there was: " + string.Join(", ", directories));
                directories.Should().HaveElementAt(0,"c:\\packages\\Bob");
                directories.Should().HaveElementAt(1,"c:\\packages\\Bob\\tools");
                directories.Should().HaveElementAt(2,"c:\\packages\\Bob\\tools\\lower");
                directories.Should().HaveElementAt(3,"c:\\packages\\Bob\\empty");
                directories.Should().HaveElementAt(4,"c:\\packages\\Bob\\empty\\nested");

                _files.Should().HaveCount(4, "There should be 4 files, but there was: " + string.Join(", ", _files));
                _files.Should().HaveElementAt(0, "c:\\packages\\Bob\\__name_replace__.nuspec");
                _files.Should().HaveElementAt(1, "c:\\packages\\Bob\\random.txt");
                _files.Should().HaveElementAt(2, "c:\\packages\\Bob\\tools\\chocolateyInstall.ps1");
                _files.Should().HaveElementAt(3, "c:\\packages\\Bob\\tools\\lower\\another.ps1");

                MockLogger.MessagesFor(LogLevel.Info).Last().Should().Be(string.Format(@"Successfully generated Bob package specification files{0} at 'c:\packages\Bob'", Environment.NewLine));
            }
        }

        public class When_generate_is_called_with_defaulttemplatename_in_configuration_but_template_folder_doesnt_exist : TemplateServiceSpecsBase
        {
            private Action _because;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();

                FileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                FileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string a, string[] b) => { return a + "\\" + b[0]; });

                _config.NewCommand.Name = "Bob";
                _config.DefaultTemplateName = "msi";
            }

            public override void Because()
            {
                _because = () => Service.Generate(_config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_use_null_value_for_template()
            {
                _because();

                _config.NewCommand.TemplateName.Should().BeNull();
            }
        }

        public class When_generate_is_called_with_defaulttemplatename_in_configuration_and_template_folder_exists : TemplateServiceSpecsBase
        {
            private Action _because;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();
            private string _verifiedDirectoryPath;

            public override void Context()
            {
                base.Context();

                FileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                FileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string a, string[] b) => { return a + "\\" + b[0]; });
                FileSystem.Setup(x => x.DirectoryExists(Path.Combine(ApplicationParameters.TemplatesLocation, "msi"))).Returns<string>(
                    x =>
                    {
                        _verifiedDirectoryPath = x;
                        return true;
                    });

                _config.NewCommand.Name = "Bob";
                _config.DefaultTemplateName = "msi";
            }

            public override void Because()
            {
                _because = () => Service.Generate(_config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_use_template_name_from_configuration()
            {
                _because();

                _config.NewCommand.TemplateName.Should().Be("msi");
            }
        }

        public class When_generate_is_called_with_defaulttemplatename_in_configuration_and_template_name_option_set : TemplateServiceSpecsBase
        {
            private Action _because;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();
            private string _verifiedDirectoryPath;

            public override void Context()
            {
                base.Context();

                FileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                FileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string a, string[] b) => { return a + "\\" + b[0]; });
                FileSystem.Setup(x => x.DirectoryExists(Path.Combine(ApplicationParameters.TemplatesLocation, "zip"))).Returns<string>(
                    x =>
                    {
                        _verifiedDirectoryPath = x;
                        return true;
                    });

                _config.NewCommand.Name = "Bob";
                _config.NewCommand.TemplateName = "zip";
                _config.DefaultTemplateName = "msi";
            }

            public override void Because()
            {
                _because = () => Service.Generate(_config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_use_template_name_from_command_line_option()
            {
                _because();

                _config.NewCommand.TemplateName.Should().Be("zip");
            }
        }

        public class When_generate_is_called_with_built_in_option_set : TemplateServiceSpecsBase
        {
            private Action _because;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();

                _config.NewCommand.Name = "Bob";
                _config.NewCommand.UseOriginalTemplate = true;
            }

            public override void Because()
            {
                _because = () => Service.Generate(_config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_use_null_value_for_template()
            {
                _because();

                _config.NewCommand.TemplateName.Should().BeNull();
            }
        }

        public class When_generate_is_called_with_built_in_option_set_and_defaulttemplate_in_configuration : TemplateServiceSpecsBase
        {
            private Action _because;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();

                _config.NewCommand.Name = "Bob";
                _config.NewCommand.UseOriginalTemplate = true;
                _config.DefaultTemplateName = "msi";
            }

            public override void Because()
            {
                _because = () => Service.Generate(_config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_use_null_value_for_template()
            {
                _because();

                _config.NewCommand.TemplateName.Should().BeNull();
            }
        }

        public class When_generate_is_called_with_built_in_option_set_and_template_name_option_set_and_template_folder_exists : TemplateServiceSpecsBase
        {
            private Action _because;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();
            private string _verifiedDirectoryPath;

            public override void Context()
            {
                base.Context();

                FileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                FileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string a, string[] b) => { return a + "\\" + b[0]; });
                FileSystem.Setup(x => x.DirectoryExists(Path.Combine(ApplicationParameters.TemplatesLocation, "zip"))).Returns<string>(
                    x =>
                    {
                        _verifiedDirectoryPath = x;
                        return true;
                    });

                _config.NewCommand.Name = "Bob";
                _config.NewCommand.TemplateName = "zip";
                _config.NewCommand.UseOriginalTemplate = true;
            }

            public override void Because()
            {
                _because = () => Service.Generate(_config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_use_template_name_from_command_line_option()
            {
                _because();

                _config.NewCommand.TemplateName.Should().Be("zip");
            }
        }

        public class When_generate_is_called_with_built_in_option_set_and_template_name_option_set_and_defaulttemplatename_set_and_template_folder_exists : TemplateServiceSpecsBase
        {
            private Action _because;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();
            private string _verifiedDirectoryPath;

            public override void Context()
            {
                base.Context();

                FileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                FileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string a, string[] b) => { return a + "\\" + b[0]; });
                FileSystem.Setup(x => x.DirectoryExists(Path.Combine(ApplicationParameters.TemplatesLocation, "zip"))).Returns<string>(
                    x =>
                    {
                        _verifiedDirectoryPath = x;
                        return true;
                    });

                _config.NewCommand.Name = "Bob";
                _config.NewCommand.TemplateName = "zip";
                _config.DefaultTemplateName = "msi";
                _config.NewCommand.UseOriginalTemplate = true;
            }

            public override void Because()
            {
                _because = () => Service.Generate(_config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_use_template_name_from_command_line_option()
            {
                _because();

                _config.NewCommand.TemplateName.Should().Be("zip");
            }
        }

        public class When_list_noop_is_called : TemplateServiceSpecsBase
        {
            private Action _because;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();

            public override void Because()
            {
                _because = () => Service.ListDryRun(_config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            public void Should_log_template_location_if_no_template_name()
            {
                _because();

                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Should().ContainSingle();
                infos.Should().HaveElementAt(0,"Would have listed templates in {0}".FormatWith(ApplicationParameters.TemplatesLocation));
            }

            [Fact]
            public void Should_log_template_name_if_template_name()
            {
                _config.TemplateCommand.Name = "msi";
                _because();

                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Should().ContainSingle();
                infos.Should().HaveElementAt(0, "Would have listed information about {0}".FormatWith(_config.TemplateCommand.Name));
            }
        }
    }
}
