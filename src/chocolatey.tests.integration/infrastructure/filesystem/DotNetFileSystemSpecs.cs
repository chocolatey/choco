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

namespace chocolatey.tests.integration.infrastructure.filesystem
{
    using System;
    using System.IO;
    using System.Linq;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.platforms;
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

        [Category("Integration")]
        public class when_finding_paths_to_executables_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void GetExecutablePath_should_find_existing_executable()
            {
                FileSystem.get_executable_path("cmd").ShouldEqual(
                    Platform.get_platform() == PlatformType.Windows
                        ? "C:\\Windows\\system32\\cmd.exe"
                        : "cmd",
                    StringComparer.OrdinalIgnoreCase
                );
            }

            [Fact]
            public void GetExecutablePath_should_find_existing_executable_with_extension()
            {
                FileSystem.get_executable_path("cmd.exe").ShouldEqual(
                    Platform.get_platform() == PlatformType.Windows
                        ? "c:\\windows\\system32\\cmd.exe"
                        : "cmd",
                    StringComparer.OrdinalIgnoreCase
                );
            }

            [Fact]
            public void GetExecutablePath_should_return_same_value_when_executable_is_not_found()
            {
                FileSystem.get_executable_path("daslakjsfdasdfwea").ShouldEqual("daslakjsfdasdfwea");
            }

            [Fact]
            public void GetExecutablePath_should_return_empty_string_when_value_is_null()
            {
                FileSystem.get_executable_path(null).ShouldEqual(string.Empty);
            }

            [Fact]
            public void GetExecutablePath_should_return_empty_string_when_value_is_empty_string()
            {
                FileSystem.get_executable_path(string.Empty).ShouldEqual(string.Empty);
            }
        }

        [Category("Integration")]
        public class when_doing_file_system_operations_with_dotNetFileSystem : DotNetFileSystemSpecsBase
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
                FileSystem.get_files(ContextPath, "*lipsum*", SearchOption.AllDirectories).ShouldEqual(FileArray);
            }

            [Fact]
            public void GetFiles_should_return_files_that_meet_the_pattern()
            {
                string filePath = FileSystem.combine_paths(ContextPath, "chocolateyInstall.ps1");

                FileSystem.write_file(filePath, "yo");
                var actual = FileSystem.get_files(ContextPath, "chocolateyInstall.ps1", SearchOption.AllDirectories).ToList();
                FileSystem.delete_file(filePath);

                actual.ShouldNotBeEmpty();
                actual.Count().ShouldEqual(1);
            }

            [Fact]
            public void GetFiles_should_return_files_that_meet_the_pattern_regardless_of_case()
            {
                string filePath = FileSystem.combine_paths(ContextPath, "chocolateyInstall.ps1");

                FileSystem.write_file(filePath, "yo");
                var actual = FileSystem.get_files(ContextPath, "chocolateyinstall.ps1", SearchOption.AllDirectories).ToList();
                FileSystem.delete_file(filePath);

                actual.ShouldNotBeEmpty();
                actual.Count().ShouldEqual(1);
            }

            [Fact]
            public void FileExists_should_return_true_if_file_exists()
            {
                FileSystem.file_exists(TheTestFile).ShouldBeTrue();
            }

            [Fact]
            public void FileExists_should_return_false_if_file_does_not_exists()
            {
                FileSystem.file_exists(Path.Combine(ContextPath, "IDontExist.txt")).ShouldBeFalse();
            }

            [Fact]
            public void DirectoryExists_should_return_true_if_directory_exists()
            {
                FileSystem.directory_exists(ContextPath).ShouldBeTrue();
            }

            [Fact]
            public void DirectoryExists_should_return_false_if_directory_does_not_exist()
            {
                FileSystem.directory_exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IDontExist")).ShouldBeFalse();
            }

            [Fact]
            public void GetFileSize_should_return_correct_file_size()
            {
                FileSystem.get_file_size(TheTestFile).ShouldEqual(5377);
            }

            [Fact]
            public void GetDirectories_should_return_a_string_array_with_directories()
            {
                FileSystem.get_directories(ContextPath).ShouldEqual(DirectoryArray);
            }
        }

        [Category("Integration")]
        public class when_setting_file_attributes_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Context()
            {
                base.Context();
                SourceFile = Path.Combine(DestinationPath, "attributes.txt");
                File.SetAttributes(SourceFile, (FileSystem.get_file_info_for(SourceFile).Attributes & ~FileAttributes.Hidden));
            }

            public override void Because()
            {
                FileSystem.ensure_file_attribute_set(SourceFile, FileAttributes.Hidden);
            }

            [Fact]
            public void visible_file_should_now_be_hidden()
            {
                ((FileAttributes)FileSystem.get_file_info_for(SourceFile).Attributes & FileAttributes.Hidden).ShouldEqual(FileAttributes.Hidden);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                File.SetAttributes(SourceFile, (FileSystem.get_file_info_for(SourceFile).Attributes & ~FileAttributes.Hidden));
            }
        }

        [Category("Integration")]
        public class when_removing_readonly_attributes_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Context()
            {
                base.Context();
                SourceFile = Path.Combine(DestinationPath, "attributes.txt");
                File.SetAttributes(SourceFile, (FileSystem.get_file_info_for(SourceFile).Attributes | FileAttributes.ReadOnly));
            }

            public override void Because()
            {
                FileSystem.ensure_file_attribute_removed(SourceFile, FileAttributes.ReadOnly);
            }

            [Fact]
            public void readonly_file_should_no_longer_be_readonly()
            {
                ((FileAttributes)FileSystem.get_file_info_for(SourceFile).Attributes & FileAttributes.ReadOnly).ShouldNotEqual(FileAttributes.ReadOnly);
            }
        }

        [Category("Integration")]
        public class when_running_fileMove_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Because()
            {
                SourceFile = Path.Combine(ContextPath, "MoveMe.txt");
                DestFile = Path.Combine(DestinationPath, "MoveMe.txt");
                if (!FileSystem.file_exists(SourceFile))
                {
                    File.Create(SourceFile);
                }
                if (FileSystem.file_exists(DestFile))
                {
                    File.Delete(DestFile);
                }
                FileSystem.move_file(SourceFile, DestFile);
            }

            [Fact]
            public void Move_me_text_file_should_not_exist_in_the_source_path()
            {
                FileSystem.file_exists(SourceFile).ShouldBeFalse();
            }

            [Fact]
            public void Move_me_text_file_should_exist_in_destination_path()
            {
                FileSystem.file_exists(DestFile).ShouldBeTrue();
            }
        }

        [Category("Integration")]
        public class when_running_fileCopy_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Because()
            {
                SourceFile = Path.Combine(ContextPath, "CopyMe.txt");
                DestFile = Path.Combine(DestinationPath, "CopyMe.txt");
                if (!FileSystem.file_exists(SourceFile))
                {
                    File.Create(SourceFile);
                }
                if (FileSystem.file_exists(DestFile))
                {
                    File.Delete(DestFile);
                }
                //Copy File
                FileSystem.copy_file(SourceFile, DestFile, true);
                //Overwrite File
                FileSystem.copy_file(SourceFile, DestFile, true);
            }

            [Fact]
            public void Copy_me_text_file_should_exist_in_context_path()
            {
                FileSystem.file_exists(SourceFile).ShouldBeTrue();
            }

            [Fact]
            public void Move_me_text_file_should_exist_in_destination_path()
            {
                FileSystem.file_exists(DestFile).ShouldBeTrue();
            }
        }

        [Category("Integration")]
        public class when_running_fileDelete_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Because()
            {
                DeleteFile = Path.Combine(DestinationPath, "DeleteMe.txt");
                if (!FileSystem.file_exists(DeleteFile))
                {
                    using (File.Create(DeleteFile))
                    {
                    }
                }

                FileSystem.delete_file(DeleteFile);
            }

            [Fact]
            public void delete_me_text_file_should_not_exist()
            {
                FileSystem.file_exists(DeleteFile).ShouldBeFalse();
            }
        }

        [Category("Integration")]
        public class when_running_createDirectory_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Because()
            {
                if (FileSystem.directory_exists(TestDirectory))
                {
                    Directory.Delete(TestDirectory, recursive: true);
                }

                FileSystem.create_directory(TestDirectory);
            }

            [Fact]
            public void test_directory_should_exist()
            {
                FileSystem.directory_exists(TestDirectory).ShouldBeTrue();
            }
        }

        [Category("Integration")]
        public class when_running_getFileModDate_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Because()
            {
                File.SetCreationTime(TheTestFile, DateTime.Now.AddDays(-1));
                File.SetLastWriteTime(TheTestFile, DateTime.Now.AddDays(-1));
            }

            [Fact]
            public void should_have_correct_modified_date()
            {
                FileSystem.get_file_modified_date(TheTestFile).ToShortDateString().ShouldEqual(DateTime.Now.AddDays(-1).ToShortDateString());
            }
        }
    }
}
