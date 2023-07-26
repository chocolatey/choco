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

namespace chocolatey.tests.infrastructure.tokens
{
    using System.Collections.Generic;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.tokens;
    using FluentAssertions;

    public class TokenReplacerSpecs
    {
        public abstract class TokenReplacerSpecsBase : TinySpec
        {
            public override void Context()
            {
            }
        }

        public class When_using_TokenReplacer : TokenReplacerSpecsBase
        {
            public ChocolateyConfiguration Configuration = new ChocolateyConfiguration();
            public string Name = "bob";

            public override void Because()
            {
                Configuration.CommandName = Name;
            }

            [Fact]
            public void When_given_brace_brace_CommandName_brace_brace_should_replace_with_the_Name_from_the_configuration()
            {
                TokenReplacer.ReplaceTokens(Configuration, "Hi! My name is [[CommandName]]").Should().Be("Hi! My name is " + Name);
            }

            [Fact]
            public void When_given_brace_CommandName_brace_should_NOT_replace_the_value()
            {
                TokenReplacer.ReplaceTokens(Configuration, "Hi! My name is [CommandName]").Should().Be("Hi! My name is [CommandName]");
            }

            [Fact]
            public void When_given_a_value_that_is_the_name_of_a_configuration_item_but_is_not_properly_tokenized_it_should_NOT_replace_the_value()
            {
                TokenReplacer.ReplaceTokens(Configuration, "Hi! My name is CommandName").Should().Be("Hi! My name is CommandName");
            }

            [Fact]
            public void When_given_brace_brace_commandname_brace_brace_should_replace_with_the_Name_from_the_configuration()
            {
                TokenReplacer.ReplaceTokens(Configuration, "Hi! My name is [[commandname]]").Should().Be("Hi! My name is " + Name);
            }

            [Fact]
            public void When_given_brace_brace_COMMANDNAME_brace_brace_should_replace_with_the_Name_from_the_configuration()
            {
                TokenReplacer.ReplaceTokens(Configuration, "Hi! My name is [[COMMANDNAME]]").Should().Be("Hi! My name is " + Name);
            }

            [Fact]
            public void When_given_brace_brace_cOMmAnDnAMe_brace_brace_should_replace_with_the_Name_from_the_configuration()
            {
                TokenReplacer.ReplaceTokens(Configuration, "Hi! My name is [[cOMmAnDnAMe]]").Should().Be("Hi! My name is " + Name);
            }

            [Fact]
            public void If_given_brace_brace_Version_brace_brace_should_NOT_replace_with_the_Name_from_the_configuration()
            {
                TokenReplacer.ReplaceTokens(Configuration, "Go to [[Version]]").Should().NotContain(Name);
            }

            [Fact]
            public void If_given_a_value_that_is_not_set_should_return_that_value_as_string_Empty()
            {
                TokenReplacer.ReplaceTokens(Configuration, "Go to [[Version]]").Should().Be("Go to " + string.Empty);
            }

            [Fact]
            public void If_given_a_value_that_does_not_exist_should_return_the_original_value_unchanged()
            {
                TokenReplacer.ReplaceTokens(Configuration, "Hi! My name is [[DataBase]]").Should().Be("Hi! My name is [[DataBase]]");
            }

            [Fact]
            public void If_given_an_empty_value_should_return_the_empty_value()
            {
                TokenReplacer.ReplaceTokens(Configuration, "").Should().Be("");
            }

            [Fact]
            public void If_given_an_null_value_should_return_the_ll_value()
            {
                TokenReplacer.ReplaceTokens(Configuration, null).Should().Be("");
            }
        }

        public class When_using_TokenReplacer_with_a_Dictionary : TokenReplacerSpecsBase
        {
            public Dictionary<string, string> Tokens = new Dictionary<string, string>();
            private readonly string _value = "sweet";

            public override void Because()
            {
                Tokens.Add("dude", _value);
            }

            [Fact]
            public void When_given_a_proper_token_it_should_replace_with_the_dictionary_value()
            {
                TokenReplacer.ReplaceTokens(Tokens, "Hi! My name is [[dude]]").Should().Be("Hi! My name is " + _value);
            }
        }
    }
}
