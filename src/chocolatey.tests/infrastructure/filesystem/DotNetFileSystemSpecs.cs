namespace chocolatey.tests.infrastructure.filesystem
{
    using System;
    using System.IO;
    using Should;
    using chocolatey.infrastructure.filesystem;

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
                FileSystem.get_directory_name("C:\\temp\\test.txt").ShouldEqual("C:\\temp");
            }

            [Fact]
            public void Combine_should_combine_the_file_paths_of_all_the_included_items_together()
            {
                FileSystem.combine_paths("C:\\temp", "yo", "filename.txt").ShouldEqual("C:\\temp\\yo\\filename.txt");
            }
        }

    }
}