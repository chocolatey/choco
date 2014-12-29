namespace chocolatey.tests.infrastructure.app.attributes
{
    using Should;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.domain;

    public class CommandForAttributeSpecs
    {
        public abstract class CommandForAttributeSpecsBase : TinySpec
        {
            protected CommandForAttribute attribute;
        }

        public class when_CommandForAttribute_is_set_with_string : CommandForAttributeSpecsBase
        {
            private string result;

            public override void Context()
            {
                attribute = new CommandForAttribute("bob");
            }

            public override void Because()
            {
                result = attribute.CommandName;
            }

            [Fact]
            public void should_be_set_to_the_string()
            {
                result.ShouldEqual("bob");
            }
        }

        public class when_CommandForAttribute_is_set_with_CommandNameType : CommandForAttributeSpecsBase
        {
            private string result;

            public override void Context()
            {
                attribute = new CommandForAttribute(CommandNameType.@new);
            }

            public override void Because()
            {
                result = attribute.CommandName;
            }

            [Fact]
            public void should_be_set_to_a_string_representation_of_the_CommandNameType()
            {
                result.ShouldEqual("new");
            }
        }
    }
}