namespace chocolatey.tests.infrastructure.tokens
{
    using Should;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.tokens;

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
            public void if_given_brace_brace_ServerName_brace_brace_should_NOT_replace_with_the_Name_from_the_configuration()
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
        }
    }
}