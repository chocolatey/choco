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

namespace chocolatey.tests.infrastructure.filesystem
{
    using System;
    using System.IO;
    using Should;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.platforms;

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

        public class when_doing_file_system_path_operations_with_dotNetFileSystem : DotNetFileSystemSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void GetFullPath_should_return_the_full_path_to_an_item()
            {
                FileSystem.get_full_path("test.txt").ShouldEqual(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.txt"));
            }

            [Fact]
            public void GetFileNameWithoutExtension_should_return_a_file_name_without_an_extension()
            {
                FileSystem.get_file_name_without_extension("test.txt").ShouldEqual("test");
            }

            [Fact]
            public void GetFileNameWithoutExtension_should_return_a_file_name_without_an_extension_even_with_a_full_path()
            {
                FileSystem.get_file_name_without_extension("C:\\temp\\test.txt").ShouldEqual("test");
            }

            [Fact]
            public void GetExtension_should_return_the_extension_of_the_filename()
            {
                FileSystem.get_file_extension("test.txt").ShouldEqual(".txt");
            }

            [Fact]
            public void GetExtension_should_return_the_extension_of_the_filename_even_with_a_full_path()
            {
                FileSystem.get_file_extension("C:\\temp\\test.txt").ShouldEqual(".txt");
            }

            [Fact]
            public void GetDirectoryName_should_return_the_directory_of_the_path_to_the_file()
            {
                FileSystem.get_directory_name("C:\\temp\\test.txt").ShouldEqual(
                    Platform.get_platform() == PlatformType.Windows ? 
                        "C:\\temp" 
                        : "C:/temp");
            }

            [Fact]
            public void Combine_should_combine_the_file_paths_of_all_the_included_items_together()
            {
                FileSystem.combine_paths("C:\\temp", "yo", "filename.txt").ShouldEqual(
                    Platform.get_platform() == PlatformType.Windows ? 
                        "C:\\temp\\yo\\filename.txt" 
                        : "C:\\temp/yo/filename.txt");
            }

            [Fact]
            public void Combine_should_combine_when_paths_have_backslashes_in_subpaths()
            {
                FileSystem.combine_paths("C:\\temp", "yo\\timmy", "filename.txt").ShouldEqual(
                    Platform.get_platform() == PlatformType.Windows ? 
                        "C:\\temp\\yo\\timmy\\filename.txt" 
                        : "C:\\temp/yo/timmy/filename.txt");
            }

            [Fact]
            public void Combine_should_combine_when_paths_start_with_backslashes_in_subpaths()
            {
                FileSystem.combine_paths("C:\\temp", "\\yo", "filename.txt").ShouldEqual(
                    Platform.get_platform() == PlatformType.Windows ? 
                        "C:\\temp\\yo\\filename.txt" 
                        : "C:\\temp/yo/filename.txt");
            }
            
            [Fact]
            public void Combine_should_combine_when_paths_start_with_forwardslashes_in_subpaths()
            {
                FileSystem.combine_paths("C:\\temp", "/yo", "filename.txt").ShouldEqual(
                    Platform.get_platform() == PlatformType.Windows ? 
                        "C:\\temp\\yo\\filename.txt" 
                        : "C:\\temp/yo/filename.txt");
            }
        }
    }
}