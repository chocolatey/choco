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

namespace Chocolatey.Tests.Integration.Infrastructure.Filesystem
{
    using System;
    using System.IO;
    using System.Linq;
    using Chocolatey.Infrastructure.Filesystem;
    using Chocolatey.Infrastructure.Platforms;
    using NUnit.Framework;
    using Should;

    public class DotNetFileSystemSpecs
    {
        public abstract class DotNetFileSystemSpecsBase : TinySpec
        {
            protected DotNetFileSystem FileSystem;
            protected string[] FileArray;
            protected string ContextPath;
            protected string DestinationPath;
            protected string TheTestFile;
            protected string FileToManipulate;
            protected string SourceFile;
            protected string DestFile;
            protected string DeleteFile;
            protected string TestDirectory;
            protected string[] DirectoryArray;

            public override void Context()
            {
                FileSystem = new DotNetFileSystem();
                ContextPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "infrastructure", "filesystem");
                DestinationPath = Path.Combine(ContextPath, "context");
                TheTestFile = Path.Combine(ContextPath, "Slipsum.txt");
                TestDirectory = Path.Combine(DestinationPath, "TestDirectory");
            }
        }

        public class When_finding_paths_to_executables_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void GetExecutablePath_should_find_existing_executable()
            {
                FileSystem.GetExecutablePath("cmd").ShouldEqual(
                    Platform.GetPlatform() == PlatformType.Windows
                        ? "C:\\Windows\\system32\\cmd.exe"
                        : "cmd",
                    StringComparer.OrdinalIgnoreCase
                );
            }

            [Fact]
            public void GetExecutablePath_should_find_existing_executable_with_extension()
            {
                FileSystem.GetExecutablePath("cmd.exe").ShouldEqual(
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

        public class When_doing_file_system_operations_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Context()
            {
                base.Context();
                FileArray = new[]
                {
                    Path.Combine(ContextPath, TheTestFile)
                };

                DirectoryArray = new[]
                {
                    DestinationPath
                };
            }

            public override void Because()
            {
            }

            [Fact]
            public void GetFiles_should_return_string_array_of_files()
            {
                FileSystem.GetFiles(ContextPath, "*lipsum*", SearchOption.AllDirectories).ShouldEqual(FileArray);
            }

            [Fact]
            public void GetFiles_should_return_files_that_meet_the_pattern()
            {
                string filePath = FileSystem.CombinePaths(ContextPath, "chocolateyInstall.ps1");

                FileSystem.WriteFile(filePath, "yo");
                var actual = FileSystem.GetFiles(ContextPath, "chocolateyInstall.ps1", SearchOption.AllDirectories).ToList();
                FileSystem.DeleteFile(filePath);

                actual.ShouldNotBeEmpty();
                actual.Count().ShouldEqual(1);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void GetFiles_should_return_files_that_meet_the_pattern_regardless_of_case()
            {
                string filePath = FileSystem.CombinePaths(ContextPath, "chocolateyInstall.ps1");

                FileSystem.WriteFile(filePath, "yo");
                var actual = FileSystem.GetFiles(ContextPath, "chocolateyinstall.ps1", SearchOption.AllDirectories).ToList();
                FileSystem.DeleteFile(filePath);

                actual.ShouldNotBeEmpty();
                actual.Count().ShouldEqual(1);
            }

            [Fact]
            public void FileExists_should_return_true_if_file_exists()
            {
                FileSystem.FileExists(TheTestFile).ShouldBeTrue();
            }

            [Fact]
            public void FileExists_should_return_false_if_file_does_not_exists()
            {
                FileSystem.FileExists(Path.Combine(ContextPath, "IDontExist.txt")).ShouldBeFalse();
            }

            [Fact]
            public void DirectoryExists_should_return_true_if_directory_exists()
            {
                FileSystem.DirectoryExists(ContextPath).ShouldBeTrue();
            }

            [Fact]
            public void DirectoryExists_should_return_false_if_directory_does_not_exist()
            {
                FileSystem.DirectoryExists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IDontExist")).ShouldBeFalse();
            }

            [Fact]
            public void GetFileSize_should_return_correct_file_size()
            {
                FileSystem.GetFileSize(TheTestFile).ShouldEqual(5377);
            }

            [Fact]
            public void GetDirectories_should_return_a_string_array_with_directories()
            {
                FileSystem.GetDirectories(ContextPath).ShouldEqual(DirectoryArray);
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_setting_file_attributes_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Context()
            {
                base.Context();
                SourceFile = Path.Combine(DestinationPath, "attributes.txt");
                File.SetAttributes(SourceFile, (FileSystem.GetFileInfoFor(SourceFile).Attributes & ~FileAttributes.Hidden));
            }

            public override void Because()
            {
                FileSystem.EnsureFileAttributeSet(SourceFile, FileAttributes.Hidden);
            }

            [Fact]
            public void Visible_file_should_now_be_hidden()
            {
                ((FileAttributes)FileSystem.GetFileInfoFor(SourceFile).Attributes & FileAttributes.Hidden).ShouldEqual(FileAttributes.Hidden);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                File.SetAttributes(SourceFile, (FileSystem.GetFileInfoFor(SourceFile).Attributes & ~FileAttributes.Hidden));
            }
        }

        public class When_removing_readonly_attributes_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Context()
            {
                base.Context();
                SourceFile = Path.Combine(DestinationPath, "attributes.txt");
                File.SetAttributes(SourceFile, (FileSystem.GetFileInfoFor(SourceFile).Attributes | FileAttributes.ReadOnly));
            }

            public override void Because()
            {
                FileSystem.EnsureFileAttributeRemoved(SourceFile, FileAttributes.ReadOnly);
            }

            [Fact]
            public void Readonly_file_should_no_longer_be_readonly()
            {
                ((FileAttributes)FileSystem.GetFileInfoFor(SourceFile).Attributes & FileAttributes.ReadOnly).ShouldNotEqual(FileAttributes.ReadOnly);
            }
        }

        public class When_running_fileMove_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Because()
            {
                SourceFile = Path.Combine(ContextPath, "MoveMe.txt");
                DestFile = Path.Combine(DestinationPath, "MoveMe.txt");
                if (!FileSystem.FileExists(SourceFile))
                {
                    File.Create(SourceFile);
                }
                if (FileSystem.FileExists(DestFile))
                {
                    File.Delete(DestFile);
                }
                FileSystem.MoveFile(SourceFile, DestFile);
            }

            [Fact]
            public void Move_me_text_file_should_not_exist_in_the_source_path()
            {
                FileSystem.FileExists(SourceFile).ShouldBeFalse();
            }

            [Fact]
            public void Move_me_text_file_should_exist_in_destination_path()
            {
                FileSystem.FileExists(DestFile).ShouldBeTrue();
            }
        }

        public class When_running_fileCopy_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Because()
            {
                SourceFile = Path.Combine(ContextPath, "CopyMe.txt");
                DestFile = Path.Combine(DestinationPath, "CopyMe.txt");
                if (!FileSystem.FileExists(SourceFile))
                {
                    File.Create(SourceFile);
                }
                if (FileSystem.FileExists(DestFile))
                {
                    File.Delete(DestFile);
                }
                //Copy File
                FileSystem.CopyFile(SourceFile, DestFile, true);
                //Overwrite File
                FileSystem.CopyFile(SourceFile, DestFile, true);
            }

            [Fact]
            public void Copy_me_text_file_should_exist_in_context_path()
            {
                FileSystem.FileExists(SourceFile).ShouldBeTrue();
            }

            [Fact]
            public void Move_me_text_file_should_exist_in_destination_path()
            {
                FileSystem.FileExists(DestFile).ShouldBeTrue();
            }
        }

        public class When_running_fileDelete_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Because()
            {
                DeleteFile = Path.Combine(DestinationPath, "DeleteMe.txt");
                if (!FileSystem.FileExists(DeleteFile))
                {
                    using (File.Create(DeleteFile))
                    {
                    }
                }

                FileSystem.DeleteFile(DeleteFile);
            }

            [Fact]
            public void Delete_me_text_file_should_not_exist()
            {
                FileSystem.FileExists(DeleteFile).ShouldBeFalse();
            }
        }

        public class When_running_createDirectory_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Because()
            {
                if (FileSystem.DirectoryExists(TestDirectory))
                {
                    Directory.Delete(TestDirectory, recursive: true);
                }

                FileSystem.CreateDirectory(TestDirectory);
            }

            [Fact]
            public void Test_directory_should_exist()
            {
                FileSystem.DirectoryExists(TestDirectory).ShouldBeTrue();
            }
        }

        public class When_running_getFileModDate_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Because()
            {
                File.SetCreationTime(TheTestFile, DateTime.Now.AddDays(-1));
                File.SetLastWriteTime(TheTestFile, DateTime.Now.AddDays(-1));
            }

            [Fact]
            public void Should_have_correct_modified_date()
            {
                FileSystem.GetFileModifiedDate(TheTestFile).ToShortDateString().ShouldEqual(DateTime.Now.AddDays(-1).ToShortDateString());
            }
        }
    }
}
