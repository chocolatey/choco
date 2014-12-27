namespace chocolatey.tests.infrastructure.information
{
    using System;
    using Moq;
    using Should;
    using chocolatey.infrastructure.information;

    public class VersionInformationSpecs
    {
        public abstract class VersionInformationSpecsBase : TinySpec
        {
            public override void Context()
            {
            }
        }

        public class when_calling_VersionInformation_to_get_current_assembly_version : VersionInformationSpecsBase
        {
            public string result = null; 

            public override void Because()
            {
                result = VersionInformation.get_current_assembly_version();
            }

            [Fact]
            public void should_not_be_null()
            {
                result.ShouldNotBeNull();
            }

            [Fact]
            public void should_not_be_empty()
            {
                result.ShouldNotBeEmpty();
            }

            [Fact]
            public void should_be_transferrable_to_Version()
            {
                new Version(result).ShouldNotBeNull();
            }

            [Fact]
            public void should_not_equal_zero_dot_zero_dot_zero_dot_zero()
            {
                result.ShouldNotEqual("0.0.0.0");
            }
        }
    }
}