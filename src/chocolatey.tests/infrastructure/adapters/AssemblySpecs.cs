using System;
using System.IO;
using chocolatey.infrastructure.adapters;
using FluentAssertions;
using NUnit.Framework;

namespace chocolatey.tests.infrastructure.adapters
{
    [NonParallelizable]
    public class AssemblySpecs
    {
        public abstract class AssemblySpecsBase : TinySpec
        {
            protected IAssembly Subject;

            public override void Context()
            {
            }
        }

        public class When_getting_executing_assembly : AssemblySpecsBase
        {
            public override void Because()
            {
                Subject = chocolatey.infrastructure.adapters.Assembly.GetExecutingAssembly();
            }

            [Fact]
            public void Should_return_the_chocolatey_assembly()
            {
                Subject.GetName().Name.Should().Be("chocolatey");
            }

            [Fact]
            public void Should_have_a_valid_location()
            {
                Subject.Location.Should().NotBeNullOrWhiteSpace();
            }

            [Fact]
            public void Should_expose_underlying_type()
            {
                Subject.UnderlyingType.Should().BeSameAs(typeof(chocolatey.infrastructure.adapters.Assembly).Assembly);
            }
        }

        public class When_getting_assembly_from_type : AssemblySpecsBase
        {
            public override void Because()
            {
                Subject = Assembly.GetAssembly(typeof(string));
            }

            [Fact]
            public void Should_match_underlying_dotnet_string_assembly()
            {
                Subject.FullName.Should().Be(typeof(string).Assembly.FullName);
            }
        }

        public class When_getting_types_from_current_assembly : AssemblySpecsBase
        {
            private Type[] _types;

            public override void Because()
            {
                Subject = Assembly.GetAssembly(typeof(AssemblySpecs));
                _types = Subject.GetTypes();
            }

            [Fact]
            public void Should_include_this_test_class()
            {
                _types.Should().Contain(t => t == typeof(AssemblySpecs));
            }
        }

        public class When_getting_type_by_name : AssemblySpecsBase
        {
            private Type _type;

            public override void Because()
            {
                Subject = Assembly.GetAssembly(typeof(string));
                _type = Subject.GetType("System.String");
            }

            [Fact]
            public void Should_return_type_for_system_string()
            {
                _type.Should().Be(typeof(string));
            }
        }

        public class When_setting_assembly_explicitly : AssemblySpecsBase
        {
            public override void Because()
            {
                var native = typeof(AssemblySpecs).Assembly;
                Subject = Assembly.SetAssembly(native);
            }

            [Fact]
            public void Should_wrap_specified_assembly()
            {
                Subject.FullName.Should().Be(typeof(AssemblySpecs).Assembly.FullName);
            }

            [Fact]
            public void Should_expose_underlying_type()
            {
                Subject.UnderlyingType.Should().BeSameAs(typeof(AssemblySpecs).Assembly);

            }
        }

        public class When_using_obsolete_set_assembly_method : AssemblySpecsBase
        {
            public override void Because()
            {
#pragma warning disable 618
                Subject = Assembly.set_assembly(typeof(AssemblySpecs).Assembly);
#pragma warning restore 618
            }

            [Fact]
            public void Should_wrap_specified_assembly()
            {
                Subject.FullName.Should().Be(typeof(AssemblySpecs).Assembly.FullName);
            }
        }

        public class When_listing_resource_names_for_this_assembly : AssemblySpecsBase
        {
            private string[] _resources;

            public override void Because()
            {
                Subject = Assembly.GetExecutingAssembly();
                _resources = Subject.GetManifestResourceNames();
            }

            [Fact]
            public void Should_return_an_array_even_if_empty()
            {
                _resources.Should().NotBeNull();
            }
        }

        public class When_getting_nonexistent_type_by_name : AssemblySpecsBase
        {
            private Type _type;

            public override void Because()
            {
                Subject = Assembly.GetExecutingAssembly();
                _type = Subject.GetType("Nonexistent.TypeName");
            }

            [Fact]
            public void Should_return_null()
            {
                _type.Should().BeNull();
            }
        }

        public class When_getting_type_case_insensitive : AssemblySpecsBase
        {
            private Type _type;

            public override void Because()
            {
                Subject = Assembly.GetAssembly(typeof(string));
                _type = Subject.GetType("system.string", throwOnError: false, ignoreCase: true);
            }

            [Fact]
            public void Should_still_find_the_type()
            {
                _type.Should().Be(typeof(string));
            }
        }
        public class When_loading_embedded_resource_stream : AssemblySpecsBase
        {
            private Stream _stream;

            public override void Because()
            {
                Subject = Assembly.GetAssembly(typeof(When_loading_embedded_resource_stream));
                _stream = Subject.GetManifestResourceStream("chocolatey.tests.Resources.SampleResource.txt");
            }

            [Fact]
            public void Should_list_embedded_resource_name()
            {
                Subject.GetManifestResourceNames()
                    .Should().Contain("chocolatey.tests.Resources.SampleResource.txt");
            }


            [Fact]
            public void Should_return_a_non_null_stream()
            {
                _stream.Should().NotBeNull();
            }

            [Fact]
            public void Should_contain_expected_text()
            {
                using (var reader = new StreamReader(_stream))
                {
                    var content = reader.ReadToEnd();
                    content.Should().Contain("Hello from embedded resource"); // Update to match actual file contents
                }
            }
        }

    }
}
