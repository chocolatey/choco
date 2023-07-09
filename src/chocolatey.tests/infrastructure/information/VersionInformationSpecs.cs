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

namespace chocolatey.tests.infrastructure.information
{
    using System;
    using chocolatey.infrastructure.information;
    using FluentAssertions;

    public class VersionInformationSpecs
    {
        public abstract class VersionInformationSpecsBase : TinySpec
        {
            public override void Context()
            {
            }
        }

        public class When_calling_VersionInformation_to_get_current_assembly_version : VersionInformationSpecsBase
        {
            public string Result = null;

            public override void Because()
            {
                Result = VersionInformation.GetCurrentAssemblyVersion();
            }

            [Fact]
            public void Should_not_be_null()
            {
                Result.Should().NotBeNull();
            }

            [Fact]
            public void Should_not_be_empty()
            {
                Result.Should().NotBeEmpty();
            }

            [Fact]
            public void Should_be_transferable_to_Version()
            {
                new Version(Result).Should().NotBeNull();
            }

            [Fact]
            public void Should_not_equal_zero_dot_zero_dot_zero_dot_zero()
            {
                Result.Should().NotBe("0.0.0.0");
            }
        }
    }
}
