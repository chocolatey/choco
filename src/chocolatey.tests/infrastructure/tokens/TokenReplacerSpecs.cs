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

namespace chocolatey.tests.infrastructure.tokens
{
    using System.Collections.Generic;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.tokens;
    using Should;

    public class TokenReplacerSpecs
    {
        public abstract class TokenReplacerSpecsBase : TinySpec
        {
            public override void Context()
            {
            }
        }

        public class when_using_TokenReplacer : TokenReplacerSpecsBase
        {
            public ChocolateyConfiguration configuration = new ChocolateyConfiguration();
            public string name = "bob";

            public override void Because()
            {
                configuration.CommandName = name;
            }

            [Fact]
            public void when_given_brace_brace_CommandName_brace_brace_should_replace_with_the_Name_from_the_configuration()
            {
                TokenReplacer.replace_tokens(configuration, "Hi! My name is [[CommandName]]").ShouldEqual("Hi! My name is " + name);
            }

            [Fact]
            public void when_given_brace_CommandName_brace_should_NOT_replace_the_value()
            {
                TokenReplacer.replace_tokens(configuration, "Hi! My name is [CommandName]").ShouldEqual("Hi! My name is [CommandName]");
            }

            [Fact]
            public void when_given_a_value_that_is_the_name_of_a_configuration_item_but_is_not_properly_tokenized_it_should_NOT_replace_the_value()
            {
                TokenReplacer.replace_tokens(configuration, "Hi! My name is CommandName").ShouldEqual("Hi! My name is CommandName");
            }

            [Fact]
            public void when_given_brace_brace_commandname_brace_brace_should_replace_with_the_Name_from_the_configuration()
            {
                TokenReplacer.replace_tokens(configuration, "Hi! My name is [[commandname]]").ShouldEqual("Hi! My name is " + name);
            }

            [Fact]
            public void when_given_brace_brace_COMMANDNAME_brace_brace_should_replace_with_the_Name_from_the_configuration()
            {
                TokenReplacer.replace_tokens(configuration, "Hi! My name is [[COMMANDNAME]]").ShouldEqual("Hi! My name is " + name);
            }

            [Fact]
            public void when_given_brace_brace_cOMmAnDnAMe_brace_brace_should_replace_with_the_Name_from_the_configuration()
            {
                TokenReplacer.replace_tokens(configuration, "Hi! My name is [[cOMmAnDnAMe]]").ShouldEqual("Hi! My name is " + name);
            }

            [Fact]
            public void if_given_brace_brace_Version_brace_brace_should_NOT_replace_with_the_Name_from_the_configuration()
            {
                TokenReplacer.replace_tokens(configuration, "Go to [[Version]]").ShouldNotContain(name);
            }

            [Fact]
            public void if_given_a_value_that_is_not_set_should_return_that_value_as_string_Empty()
            {
                TokenReplacer.replace_tokens(configuration, "Go to [[Version]]").ShouldEqual("Go to " + string.Empty);
            }

            [Fact]
            public void if_given_a_value_that_does_not_exist_should_return_the_original_value_unchanged()
            {
                TokenReplacer.replace_tokens(configuration, "Hi! My name is [[DataBase]]").ShouldEqual("Hi! My name is [[DataBase]]");
            }

            [Fact]
            public void if_given_an_empty_value_should_return_the_empty_value()
            {
                TokenReplacer.replace_tokens(configuration, "").ShouldEqual("");
            }

            [Fact]
            public void if_given_an_null_value_should_return_the_ll_value()
            {
                TokenReplacer.replace_tokens(configuration, null).ShouldEqual("");
            }
        }

        public class when_using_TokenReplacer_with_a_Dictionary : TokenReplacerSpecsBase
        {
            public Dictionary<string, string> tokens = new Dictionary<string, string>();
            private readonly string value = "sweet";

            public override void Because()
            {
                tokens.Add("dude", value);
            }

            [Fact]
            public void when_given_a_proper_token_it_should_replace_with_the_dictionary_value()
            {
                TokenReplacer.replace_tokens(tokens, "Hi! My name is [[dude]]").ShouldEqual("Hi! My name is " + value);
            }
        }
    }
}
