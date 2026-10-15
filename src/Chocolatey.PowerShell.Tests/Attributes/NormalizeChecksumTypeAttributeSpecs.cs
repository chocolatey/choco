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
using System.Management.Automation;
using Chocolatey.PowerShell.Attributes;
using Chocolatey.PowerShell.Shared;
using FluentAssertions;

namespace Chocolatey.PowerShell.Tests.Attributes
{
    // ReSharper disable InconsistentNaming

    public class NormalizeChecksumTypeAttributeSpecs
    {
        public class When_transforming_a_checksum_type : TinySpec
        {
            public override void Context()
            {
            }

            public override void Because()
            {
            }

            [InlineData("", ChecksumType.Md5, ChecksumType.Md5)]
            [InlineData("", ChecksumType.Sha256, ChecksumType.Sha256)]
            [InlineData("sha256", ChecksumType.Md5, ChecksumType.Sha256)]
            [InlineData("SHA1", ChecksumType.Md5, ChecksumType.Sha1)]
            [InlineData("Md5", ChecksumType.Sha512, ChecksumType.Md5)]
            [InlineData(ChecksumType.Sha512, ChecksumType.Md5, ChecksumType.Sha512)]
            public void Should_use_the_default_only_for_an_empty_string(object input, ChecksumType defaultType, ChecksumType expected)
            {
                var attribute = new NormalizeChecksumTypeAttribute { Default = defaultType };

                attribute.Transform(null, input).Should().Be(expected);
            }

            [Fact]
            public void Should_transform_an_empty_string_to_null_when_asked_to()
            {
                var attribute = new NormalizeChecksumTypeAttribute { EmptyAsNull = true };

                attribute.Transform(null, string.Empty).Should().BeNull();
            }

            [Fact]
            public void Should_still_convert_a_type_name_when_transforming_an_empty_string_to_null()
            {
                var attribute = new NormalizeChecksumTypeAttribute { EmptyAsNull = true };

                attribute.Transform(null, "sha256").Should().Be(ChecksumType.Sha256);
            }

            [Fact]
            public void Should_default_to_md5()
            {
                new NormalizeChecksumTypeAttribute().Transform(null, string.Empty).Should().Be(ChecksumType.Md5);
            }

            [Fact]
            public void Should_fail_for_a_type_that_is_not_supported()
            {
                Action transform = () => new NormalizeChecksumTypeAttribute().Transform(null, "sha384");

                transform.Should().Throw<PSInvalidCastException>();
            }
        }
    }

    // ReSharper restore InconsistentNaming
}
