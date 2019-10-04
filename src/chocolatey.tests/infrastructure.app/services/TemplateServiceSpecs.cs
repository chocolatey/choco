// Copyright © 2017 - 2018 Chocolatey Software, Inc
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
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.app.templates;
    using chocolatey.infrastructure.filesystem;
    using Moq;
    using Should;

    public class TemplateServiceSpecs
    {
        public abstract class TemplateServiceSpecsBase : TinySpec
        {
            protected TemplateService service;
            protected Mock<IFileSystem> fileSystem = new Mock<IFileSystem>();

            public override void Context()
            {
                fileSystem.ResetCalls();

                service = new TemplateService(fileSystem.Object);
            }
        }

        public class when_noop_is_called : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();
                fileSystem.Setup(x => x.get_current_directory()).Returns("c:\\chocolatey");
                fileSystem.Setup(x => x.combine_paths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string a, string[] b) => { return a + "\\" + b[0]; });
                config.NewCommand.Name = "Bob";
            }

            public override void Because()
            {
                because = () => service.noop(config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.reset();
            }

            [Fact]
            public void should_log_current_directory_if_no_outputdirectory()
            {
                because();

                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Count.ShouldEqual(1);
                infos[0].ShouldEqual("Would have generated a new package specification at c:\\chocolatey\\Bob");
            }

            [Fact]
            public void should_log_output_directory_if_outputdirectory_is_specified()
            {
                config.OutputDirectory = "c:\\packages";

                because();

                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Count.ShouldEqual(1);
                infos[0].ShouldEqual("Would have generated a new package specification at c:\\packages\\Bob");
            }
        }

        public class when_generate_file_from_template_is_called : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();
            private readonly TemplateValues templateValues = new TemplateValues();
            private readonly string template = "[[PackageName]]";
            private const string fileLocation = "c:\\packages\\bob.nuspec";
            private string fileContent;

            public override void Context()
            {
                base.Context();

                fileSystem.Setup(x => x.write_file(It.Is((string fl) => fl == fileLocation), It.IsAny<string>(), Encoding.UTF8))
                    .Callback((string filePath, string fileContent, Encoding encoding) => this.fileContent = fileContent);

                templateValues.PackageName = "Bob";
            }

            public override void Because()
            {
                because = () => service.generate_file_from_template(config, templateValues, template, fileLocation, Encoding.UTF8);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.reset();
            }

            [Fact]
            public void should_write_file_withe_replaced_tokens()
            {
                because();

                var debugs = MockLogger.MessagesFor(LogLevel.Debug);
                debugs.Count.ShouldEqual(1);
                debugs[0].ShouldEqual("Bob");
            }

            [Fact]
            public void should_log_info_if_regular_output()
            {
                config.RegularOutput = true;

                because();

                var debugs = MockLogger.MessagesFor(LogLevel.Debug);
                debugs.Count.ShouldEqual(1);
                debugs[0].ShouldEqual("Bob");

                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Count.ShouldEqual(1);
                infos[0].ShouldEqual(string.Format(@"Generating template to a file{0} at 'c:\packages\bob.nuspec'", Environment.NewLine));
            }
        }

        public class when_generate_is_called_with_existing_directory : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();
            private string verifiedDirectoryPath;

            public override void Context()
            {
                base.Context();

                fileSystem.Setup(x => x.get_current_directory()).Returns("c:\\chocolatey");
                fileSystem.Setup(x => x.combine_paths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string a, string[] b) => { return a + "\\" + b[0]; });
                fileSystem.Setup(x => x.directory_exists(It.IsAny<string>())).Returns<string>(
                    x =>
                    {
                        verifiedDirectoryPath = x;
                        return true;
                    });

                config.NewCommand.Name = "Bob";
            }

            public override void Because()
            {
                because = () => service.generate(config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.reset();
            }

            [Fact]
            public void should_throw_exception()
            {
                bool errored = false;
                string errorMessage = string.Empty;

                try
                {
                    because();
                }
                catch (ApplicationException ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                errored.ShouldBeTrue();
                errorMessage.ShouldEqual(string.Format("The location for the template already exists. You can:{0} 1. Remove 'c:\\chocolatey\\Bob'{0} 2. Use --force{0} 3. Specify a different name", Environment.NewLine));
                verifiedDirectoryPath.ShouldEqual("c:\\chocolatey\\Bob");
            }

            [Fact]
            public void should_throw_exception_even_with_outputdirectory()
            {
                config.OutputDirectory = "c:\\packages";

                bool errored = false;
                string errorMessage = string.Empty;

                try
                {
                    because();
                }
                catch (ApplicationException ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                errored.ShouldBeTrue();
                errorMessage.ShouldEqual(string.Format("The location for the template already exists. You can:{0} 1. Remove 'c:\\packages\\Bob'{0} 2. Use --force{0} 3. Specify a different name", Environment.NewLine));
                verifiedDirectoryPath.ShouldEqual("c:\\packages\\Bob");
            }
        }

        public class when_generate_is_called : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();
            private readonly List<string> files = new List<string>();
            private readonly HashSet<string> directoryCreated = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            public override void Context()
            {
                base.Context();

                fileSystem.Setup(x => x.get_current_directory()).Returns("c:\\chocolatey");
                fileSystem.Setup(x => x.combine_paths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(
                        (string a, string[] b) =>
                        {
                            if (a.EndsWith("templates") && b[0] == "default")
                            {
                                return "templates\\default";
                            }
                            return a + "\\" + b[0];
                        });
                fileSystem.Setup(x => x.directory_exists(It.IsAny<string>())).Returns<string>(dirPath => dirPath.EndsWith("templates\\default"));
                fileSystem.Setup(x => x.write_file(It.IsAny<string>(), It.IsAny<string>(), Encoding.UTF8))
                    .Callback((string filePath, string fileContent, Encoding encoding) => files.Add(filePath));
                fileSystem.Setup(x => x.delete_directory_if_exists(It.IsAny<string>(), true));
                fileSystem.Setup(x => x.create_directory_if_not_exists(It.IsAny<string>())).Callback(
                    (string directory) =>
                    {
                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            directoryCreated.Add(directory);
                        }
                    });
                fileSystem.Setup(x => x.get_files(It.IsAny<string>(), "*.*", SearchOption.AllDirectories)).Returns(new[] { "templates\\default\\template.nuspec", "templates\\default\\random.txt" });
                fileSystem.Setup(x => x.get_directory_name(It.IsAny<string>())).Returns<string>(file => Path.GetDirectoryName(file));
                fileSystem.Setup(x => x.get_file_extension(It.IsAny<string>())).Returns<string>(file => Path.GetExtension(file));
                fileSystem.Setup(x => x.read_file(It.IsAny<string>())).Returns(string.Empty);

                config.NewCommand.Name = "Bob";
            }

            public override void Because()
            {
                because = () => service.generate(config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.reset();
                files.Clear();
                directoryCreated.Clear();
            }

            [Fact]
            public void should_generate_all_files_and_directories()
            {
                because();

                var directories = directoryCreated.ToList();
                directories.Count.ShouldEqual(2, "There should be 2 directories, but there was: " + string.Join(", ", directories));
                directories[0].ShouldEqual("c:\\chocolatey\\Bob");
                directories[1].ShouldEqual("c:\\chocolatey\\Bob\\tools");

                files.Count.ShouldEqual(2, "There should be 2 files, but there was: " + string.Join(", ", files));
                files[0].ShouldEqual("c:\\chocolatey\\Bob\\__name_replace__.nuspec");
                files[1].ShouldEqual("c:\\chocolatey\\Bob\\random.txt");

                MockLogger.MessagesFor(LogLevel.Info).Last().ShouldEqual(string.Format(@"Successfully generated Bob package specification files{0} at 'c:\chocolatey\Bob'", Environment.NewLine));
            }

            [Fact]
            public void should_generate_all_files_and_directories_even_with_outputdirectory()
            {
                config.OutputDirectory = "c:\\packages";

                because();

                var directories = directoryCreated.ToList();
                directories.Count.ShouldEqual(2, "There should be 2 directories, but there was: " + string.Join(", ", directories));
                directories[0].ShouldEqual("c:\\packages\\Bob");
                directories[1].ShouldEqual("c:\\packages\\Bob\\tools");

                files.Count.ShouldEqual(2, "There should be 2 files, but there was: " + string.Join(", ", files));
                files[0].ShouldEqual("c:\\packages\\Bob\\__name_replace__.nuspec");
                files[1].ShouldEqual("c:\\packages\\Bob\\random.txt");

                MockLogger.MessagesFor(LogLevel.Info).Last().ShouldEqual(string.Format(@"Successfully generated Bob package specification files{0} at 'c:\packages\Bob'", Environment.NewLine));
            }
        }

        public class when_generate_is_called_with_nested_folders : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();
            private readonly List<string> files = new List<string>();
            private readonly HashSet<string> directoryCreated = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            public override void Context()
            {
                base.Context();

                fileSystem.Setup(x => x.get_current_directory()).Returns("c:\\chocolatey");
                fileSystem.Setup(x => x.combine_paths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(
                        (string a, string[] b) =>
                        {
                            if (a.EndsWith("templates") && b[0] == "test")
                            {
                                return "templates\\test";
                            }
                            return a + "\\" + b[0];
                        });
                fileSystem.Setup(x => x.directory_exists(It.IsAny<string>())).Returns<string>(dirPath => dirPath.EndsWith("templates\\test"));
                fileSystem.Setup(x => x.write_file(It.IsAny<string>(), It.IsAny<string>(), Encoding.UTF8))
                    .Callback((string filePath, string fileContent, Encoding encoding) => files.Add(filePath));
                fileSystem.Setup(x => x.delete_directory_if_exists(It.IsAny<string>(), true));
                fileSystem.Setup(x => x.get_files(It.IsAny<string>(), "*.*", SearchOption.AllDirectories))
                    .Returns(new[] { "templates\\test\\template.nuspec", "templates\\test\\random.txt", "templates\\test\\tools\\chocolateyInstall.ps1", "templates\\test\\tools\\lower\\another.ps1" });
                fileSystem.Setup(x => x.create_directory_if_not_exists(It.IsAny<string>())).Callback(
                    (string directory) =>
                    {
                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            directoryCreated.Add(directory);
                        }
                    });
                fileSystem.Setup(x => x.get_directory_name(It.IsAny<string>())).Returns<string>(file => Path.GetDirectoryName(file));
                fileSystem.Setup(x => x.get_file_extension(It.IsAny<string>())).Returns<string>(file => Path.GetExtension(file));
                fileSystem.Setup(x => x.read_file(It.IsAny<string>())).Returns(string.Empty);

                config.NewCommand.Name = "Bob";
                config.NewCommand.TemplateName = "test";
            }

            public override void Because()
            {
                because = () => service.generate(config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.reset();
                files.Clear();
                directoryCreated.Clear();
            }

            [Fact]
            [WindowsOnly]
            public void should_generate_all_files_and_directories()
            {
                because();

                var directories = directoryCreated.ToList();
                directories.Count.ShouldEqual(3, "There should be 3 directories, but there was: " + string.Join(", ", directories));
                directories[0].ShouldEqual("c:\\chocolatey\\Bob");
                directories[1].ShouldEqual("c:\\chocolatey\\Bob\\tools");
                directories[2].ShouldEqual("c:\\chocolatey\\Bob\\tools\\lower");

                files.Count.ShouldEqual(4, "There should be 4 files, but there was: " + string.Join(", ", files));
                files[0].ShouldEqual("c:\\chocolatey\\Bob\\__name_replace__.nuspec");
                files[1].ShouldEqual("c:\\chocolatey\\Bob\\random.txt");
                files[2].ShouldEqual("c:\\chocolatey\\Bob\\tools\\chocolateyInstall.ps1");
                files[3].ShouldEqual("c:\\chocolatey\\Bob\\tools\\lower\\another.ps1");

                MockLogger.MessagesFor(LogLevel.Info).Last().ShouldEqual(string.Format(@"Successfully generated Bob package specification files{0} at 'c:\chocolatey\Bob'", Environment.NewLine));
            }

            [Fact]
            [WindowsOnly]
            public void should_generate_all_files_and_directories_even_with_outputdirectory()
            {
                config.OutputDirectory = "c:\\packages";

                because();

                var directories = directoryCreated.ToList();
                directories.Count.ShouldEqual(3, "There should be 3 directories, but there was: " + string.Join(", ", directories));
                directories[0].ShouldEqual("c:\\packages\\Bob");
                directories[1].ShouldEqual("c:\\packages\\Bob\\tools");
                directories[2].ShouldEqual("c:\\packages\\Bob\\tools\\lower");

                files.Count.ShouldEqual(4, "There should be 4 files, but there was: " + string.Join(", ", files));
                files[0].ShouldEqual("c:\\packages\\Bob\\__name_replace__.nuspec");
                files[1].ShouldEqual("c:\\packages\\Bob\\random.txt");
                files[2].ShouldEqual("c:\\packages\\Bob\\tools\\chocolateyInstall.ps1");
                files[3].ShouldEqual("c:\\packages\\Bob\\tools\\lower\\another.ps1");

                MockLogger.MessagesFor(LogLevel.Info).Last().ShouldEqual(string.Format(@"Successfully generated Bob package specification files{0} at 'c:\packages\Bob'", Environment.NewLine));
            }
        }
    }
}
