// Copyright © 2017 - 2026 Chocolatey Software, Inc
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
using Chocolatey.PowerShell.Helpers;
using FluentAssertions;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    // ReSharper disable InconsistentNaming

    public class RemoteFileNameResolverSpecs
    {
        public abstract class ResolverSpecsBase : TinySpec
        {
            protected string DefaultName = "default.bin";
            protected string RequestUrl = "http://example.com/download";
            protected Uri ResponseUri = new Uri("http://example.com/");
            protected string ContentDisposition;
            protected string Location;
            protected List<string> Steps = new List<string>();
            protected string Name;

            public override void Context()
            {
            }

            public override void Because()
            {
                Name = RemoteFileNameResolver.Resolve(DefaultName, RequestUrl, ResponseUri, ContentDisposition, Location, Steps.Add);
            }
        }

        public class When_the_content_disposition_header_names_the_file : ResolverSpecsBase
        {
            public override void Context()
            {
                ContentDisposition = "attachment; filename=\"installer.msi\"";
            }

            [Fact]
            public void Should_use_that_name_without_the_quotes()
            {
                Name.Should().Be("installer.msi");
            }

            [Fact]
            public void Should_say_that_the_header_was_used()
            {
                Steps.Should().Contain("Using header 'Content-Disposition' to determine file name.");
            }

            [Fact]
            public void Should_say_which_name_was_determined()
            {
                Steps.Should().Contain("File name determined from url is 'installer.msi'");
            }
        }

        public class When_the_content_disposition_filename_is_unquoted_and_in_another_case : ResolverSpecsBase
        {
            public override void Context()
            {
                ContentDisposition = "attachment; FileName=setup.exe";
            }

            [Fact]
            public void Should_use_that_name()
            {
                Name.Should().Be("setup.exe");
            }
        }

        public class When_the_content_disposition_filename_is_followed_by_other_parameters : ResolverSpecsBase
        {
            public override void Context()
            {
                ContentDisposition = "attachment; filename=setup.exe; size=5";
                ResponseUri = new Uri("http://example.com/files/other.exe");
            }

            [Fact]
            public void Should_not_use_the_header_and_move_on_to_the_response_url()
            {
                Name.Should().Be("other.exe");
            }
        }

        public class When_the_content_disposition_filename_has_an_invalid_character : ResolverSpecsBase
        {
            public override void Context()
            {
                ContentDisposition = "attachment; filename=\"a:b.msi\"";
                ResponseUri = new Uri("http://example.com/fallback.msi");
            }

            [Fact]
            public void Should_move_on_to_the_response_url()
            {
                Name.Should().Be("fallback.msi");
            }
        }

        public class When_only_the_location_header_names_the_file : ResolverSpecsBase
        {
            public override void Context()
            {
                Location = "http://cdn.example.com/files/tool.zip";
            }

            [Fact]
            public void Should_use_the_last_segment_of_the_location()
            {
                Name.Should().Be("tool.zip");
            }

            [Fact]
            public void Should_say_that_the_header_was_used()
            {
                Steps.Should().Contain("Using header 'Location' to determine file name.");
            }
        }

        public class When_the_location_header_has_an_invalid_character : ResolverSpecsBase
        {
            public override void Context()
            {
                Location = "http://cdn.example.com/a?b.zip";
                ResponseUri = new Uri("http://example.com/real.zip");
            }

            [Fact]
            public void Should_move_on_to_the_response_url()
            {
                Name.Should().Be("real.zip");
            }
        }

        public class When_the_response_url_names_the_file : ResolverSpecsBase
        {
            public override void Context()
            {
                ResponseUri = new Uri("http://cdn.example.com/final/setup.exe");
            }

            [Fact]
            public void Should_use_the_last_segment_of_the_response_url()
            {
                Name.Should().Be("setup.exe");
            }
        }

        public class When_the_response_url_ends_in_a_segment_without_an_extension : ResolverSpecsBase
        {
            public override void Context()
            {
                ResponseUri = new Uri("http://cdn.example.com/download");
            }

            [Fact]
            public void Should_use_that_segment_as_the_name()
            {
                Name.Should().Be("download");
            }
        }

        public class When_the_response_url_has_a_query : ResolverSpecsBase
        {
            public override void Context()
            {
                ResponseUri = new Uri("http://cdn.example.com/dl?id=5");
                RequestUrl = "http://example.com/original/tool.exe";
            }

            [Fact]
            public void Should_use_the_name_in_the_request_url()
            {
                Name.Should().Be("tool.exe");
            }
        }

        public class When_the_request_url_has_a_query_and_nothing_else_gives_a_name : ResolverSpecsBase
        {
            public override void Context()
            {
                ResponseUri = new Uri("http://cdn.example.com/dl?id=5");
                RequestUrl = "http://example.com/tool.exe?x=1";
            }

            [Fact]
            public void Should_use_the_default_name()
            {
                Name.Should().Be("default.bin");
            }
        }

        public class When_nothing_gives_a_usable_name : ResolverSpecsBase
        {
            [Fact]
            public void Should_use_the_default_name()
            {
                Name.Should().Be("default.bin");
            }

            [Fact]
            public void Should_say_that_the_default_was_used()
            {
                Steps.Should().Contain("File name is null or illegal. Using default.bin instead.");
            }
        }

        public class When_the_name_in_the_request_url_has_an_invalid_character : ResolverSpecsBase
        {
            public override void Context()
            {
                RequestUrl = "http://example.com/we*ird.exe";
            }

            [Fact]
            public void Should_use_the_default_name()
            {
                Name.Should().Be("default.bin");
            }
        }
    }

    // ReSharper restore InconsistentNaming
}
