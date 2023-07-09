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

namespace chocolatey.tests.infrastructure.app.attributes
{
    using chocolatey.infrastructure.app.attributes;
    using FluentAssertions;

    public class CommandForAttributeSpecs
    {
        public abstract class CommandForAttributeSpecsBase : TinySpec
        {
            protected CommandForAttribute Attribute;
        }

        public class When_CommandForAttribute_is_set_with_string : CommandForAttributeSpecsBase
        {
            private string _result;

            public override void Context()
            {
                Attribute = new CommandForAttribute("bob", "");
            }

            public override void Because()
            {
                _result = Attribute.CommandName;
            }

            [Fact]
            public void Should_be_set_to_the_string()
            {
                _result.Should().Be("bob");
            }
        }
    }
}
