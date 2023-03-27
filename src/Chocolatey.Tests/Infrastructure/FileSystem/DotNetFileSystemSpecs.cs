﻿// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

namespace Chocolatey.Tests.Infrastructure.Filesystem
{
    using System;
    using System.IO;
    using Chocolatey.Infrastructure.Adapters;
    using Chocolatey.Infrastructure.App;
    using Chocolatey.Infrastructure.Filesystem;
    using Chocolatey.Infrastructure.Platforms;
    using Moq;
    using NUnit.Framework;
    using Should;

    public class DotNetFileSystemSpecs
    {
        public abstract class DotNetFileSystemSpecsBase : TinySpec
        {
            protected DotNetFileSystem FileSystem;

            public override void Context()
            {
                FileSystem = new DotNetFileSystem();
            }
        }

        public class When_doing_file_system_path_operations_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void GetFullPath_should_return_the_full_path_to_an_item()
            {
                FileSystem.GetFullPath("test.txt").ShouldEqual(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.txt"));
            }

            [Fact]
            public void GetFileNameWithoutExtension_should_return_a_file_name_without_an_extension()
            {
                FileSystem.GetFilenameWithoutExtension("test.txt").ShouldEqual("test");
            }

            [Fact]
            public void GetFileNameWithoutExtension_should_return_a_file_name_without_an_extension_even_with_a_full_path()
            {
                FileSystem.GetFilenameWithoutExtension("C:\\temp\\test.txt").ShouldEqual("test");
            }

            [Fact]
            public void GetExtension_should_return_the_extension_of_the_filename()
            {
                FileSystem.GetFileExtension("test.txt").ShouldEqual(".txt");
            }

            [Fact]
            public void GetExtension_should_return_the_extension_of_the_filename_even_with_a_full_path()
            {
                FileSystem.GetFileExtension("C:\\temp\\test.txt").ShouldEqual(".txt");
            }

            [Fact]
            public void GetDirectoryName_should_return_the_directory_of_the_path_to_the_file()
            {
                FileSystem.GetDirectoryName("C:\\temp\\test.txt").ShouldEqual(
                    Platform.GetPlatform() == PlatformType.Windows
                        ? "C:\\temp"
                        : "C:/temp");
            }

            [Fact]
            public void Combine_should_combine_the_file_paths_of_all_the_included_items_together()
            {
                FileSystem.CombinePaths("C:\\temp", "yo", "filename.txt").ShouldEqual(
                    Platform.GetPlatform() == PlatformType.Windows
                        ? "C:\\temp\\yo\\filename.txt"
                        : "C:/temp/yo/filename.txt");
            }

            [Fact]
            public void Combine_should_combine_when_paths_have_backslashes_in_subpaths()
            {
                FileSystem.CombinePaths("C:\\temp", "yo\\timmy", "filename.txt").ShouldEqual(
                    Platform.GetPlatform() == PlatformType.Windows
                        ? "C:\\temp\\yo\\timmy\\filename.txt"
                        : "C:/temp/yo/timmy/filename.txt");
            }

            [Fact]
            public void Combine_should_combine_when_paths_start_with_backslashes_in_subpaths()
            {
                FileSystem.CombinePaths("C:\\temp", "\\yo", "filename.txt").ShouldEqual(
                    Platform.GetPlatform() == PlatformType.Windows
                        ? "C:\\temp\\yo\\filename.txt"
                        : "C:/temp/yo/filename.txt");
            }

            [Fact]
            public void Combine_should_combine_when_paths_start_with_forwardslashes_in_subpaths()
            {
                FileSystem.CombinePaths("C:\\temp", "/yo", "filename.txt").ShouldEqual(
                    Platform.GetPlatform() == PlatformType.Windows
                        ? "C:\\temp\\yo\\filename.txt"
                        : "C:/temp/yo/filename.txt");
            }

            [Fact]
            public void Combine_should_error_if_any_path_but_the_primary_contains_colon()
            {
                Assert.That(() => FileSystem.CombinePaths("C:\\temp", "C:"),
                    Throws.TypeOf<ApplicationException>()
                          .And.Message.StartsWith("Cannot combine a path with"));
            }
        }

        public class When_finding_paths_to_executables_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public Mock<IEnvironment> _environment = new Mock<IEnvironment>();

            public override void Context()
            {
                base.Context();
                _environment.Setup(x => x.GetEnvironmentVariable(ApplicationParameters.Environment.PathExtensions)).Returns(".COM;.EXE;.BAT;.CMD;.VBS;.VBE;.JS;.JSE;.WSF;.WSH;.MSC;.CPL");
                _environment.Setup(x => x.GetEnvironmentVariable(ApplicationParameters.Environment.Path)).Returns(
                    @"C:\ProgramData\Chocolatey\bin{0}C:\Program Files\Microsoft\Web Platform Installer\{0}C:\Users\yes\AppData\Roaming\Boxstarter{0}C:\tools\ChocolateyPackageUpdater{0}C:\Windows\system32{0}C:\Windows{0}C:\Windows\System32\Wbem{0}C:\Windows\System32\WindowsPowerShell\v1.0\{0}"
                        .FormatWith(Path.PathSeparator)
                );
                FileSystem.InitializeWith(new Lazy<IEnvironment>(() => _environment.Object));
            }

            public override void Because()
            {
            }

            private void reset()
            {
                _environment.ResetCalls();
            }

            [Fact]
            public void GetExecutablePath_should_find_existing_executable()
            {
                FileSystem.GetExecutablePath("cmd").ToLowerChecked().ShouldEqual(
                    Platform.GetPlatform() == PlatformType.Windows
                        ? "c:\\windows\\system32\\cmd.exe"
                        : "cmd",
                    StringComparer.OrdinalIgnoreCase
                );
            }

            [Fact]
            public void GetExecutablePath_should_find_existing_executable_with_extension()
            {
                FileSystem.GetExecutablePath("cmd.exe").ToLowerChecked().ShouldEqual(
                    Platform.GetPlatform() == PlatformType.Windows
                        ? "c:\\windows\\system32\\cmd.exe"
                        : "cmd.exe",
                    StringComparer.OrdinalIgnoreCase
                );
            }

            [Fact]
            public void GetExecutablePath_should_return_same_value_when_executable_is_not_found()
            {
                FileSystem.GetExecutablePath("daslakjsfdasdfwea").ShouldEqual("daslakjsfdasdfwea");
            }

            [Fact]
            public void GetExecutablePath_should_return_empty_string_when_value_is_null()
            {
                FileSystem.GetExecutablePath(null).ShouldEqual(string.Empty);
            }

            [Fact]
            public void GetExecutablePath_should_return_empty_string_when_value_is_empty_string()
            {
                FileSystem.GetExecutablePath(string.Empty).ShouldEqual(string.Empty);
            }
        }

        public class When_finding_paths_to_executables_with_dotNetFileSystem_with_empty_path_extensions : DotNetFileSystemSpecsBase
        {
            public Mock<IEnvironment> _environment = new Mock<IEnvironment>();

            public override void Context()
            {
                base.Context();
                _environment.Setup(x => x.GetEnvironmentVariable(ApplicationParameters.Environment.PathExtensions)).Returns(string.Empty);
                _environment.Setup(x => x.GetEnvironmentVariable(ApplicationParameters.Environment.Path)).Returns(
                    "/usr/local/bin{0}/usr/bin/{0}/bin{0}/usr/sbin{0}/sbin"
                        .FormatWith(Path.PathSeparator)
                );
                FileSystem.InitializeWith(new Lazy<IEnvironment>(() => _environment.Object));
            }

            public override void Because()
            {
            }

            [Fact]
            public void GetExecutablePath_should_find_existing_executable()
            {
                if (Platform.GetPlatform() == PlatformType.Windows)
                {
                    FileSystem.GetExecutablePath("ls").ShouldEqual("ls");
                }
                else
                {
                    new string[]
                    {
                        "/bin/ls", "/usr/bin/ls"
                    }.ShouldContain(FileSystem.GetExecutablePath("ls"));
                }
            }

            [Fact]
            public void GetExecutablePath_should_return_same_value_when_executable_is_not_found()
            {
                FileSystem.GetExecutablePath("daslakjsfdasdfwea").ShouldEqual("daslakjsfdasdfwea");
            }

            [Fact]
            public void GetExecutablePath_should_return_empty_string_when_value_is_null()
            {
                FileSystem.GetExecutablePath(null).ShouldEqual(string.Empty);
            }

            [Fact]
            public void GetExecutablePath_should_return_empty_string_when_value_is_empty_string()
            {
                FileSystem.GetExecutablePath(string.Empty).ShouldEqual(string.Empty);
            }
        }
    }
}
