// Copyright © 2017 - 2025 Chocolatey Software, Inc
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

using chocolatey.infrastructure.app.attributes;
using FluentAssertions;

namespace chocolatey.tests.infrastructure.app.attributes
{
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

        public class When_CommandForAttribute_is_set_with_description : CommandForAttributeSpecsBase
        {
            private string _result;

            public override void Context()
            {
                Attribute = new CommandForAttribute("list", "Lists packages");
            }

            public override void Because()
            {
                _result = Attribute.Description;
            }

            [Fact]
            public void Should_be_set_to_the_description()
            {
                _result.Should().Be("Lists packages");
            }
        }

        public class When_CommandForAttribute_has_version_set : CommandForAttributeSpecsBase
        {
            private string _result;

            public override void Context()
            {
                Attribute = new CommandForAttribute("search", "Search packages");
                Attribute.Version = "1.2.3";
            }

            public override void Because()
            {
                _result = Attribute.Version;
            }

            [Fact]
            public void Should_return_the_set_version()
            {
                _result.Should().Be("1.2.3");
            }
        }

        public class When_multiple_CommandForAttributes_are_applied : TinySpec
        {
            private object[] _attributes;

            public override void Context()
            {
                _attributes = typeof(DummyCommand).GetCustomAttributes(typeof(CommandForAttribute), false);
            }

            public override void Because() { }

            [Fact]
            public void Should_have_two_command_attributes()
            {
                _attributes.Should().HaveCount(2);
            }

            [CommandFor("install", "Install packages")]
            [CommandFor("upgrade", "Upgrade packages")]
            private class DummyCommand
            {
            }
        }

    }
}
