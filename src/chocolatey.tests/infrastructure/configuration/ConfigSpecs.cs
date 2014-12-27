namespace chocolatey.tests.infrastructure.configuration
{
    using Moq;
    using Should;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.configuration;

    public class ConfigSpecs
    {
        public abstract class ConfigSpecsBase : TinySpec
        {
            public override void Context()
            {
                Config.initialize_with(new ChocolateyConfiguration()); 
            }
        }

        public class when_Config_is_set_normally : ConfigSpecsBase
        {   
            public override void Because()
            {}

            [Fact]
            public void should_be_of_type_ChocolateyConfiguration()
            {
                Config.get_configuration_settings().ShouldBeType<ChocolateyConfiguration>();
            }
        }

        public class when_Config_is_overridden : ConfigSpecsBase
        {
            private class LocalConfig : ChocolateyConfiguration
            {
            }
 
            public override void Because()
            {
                Config.initialize_with(new LocalConfig());    
            }

            [Fact]
            public void should_use_the_overridden_type()
            {
                Config.get_configuration_settings().ShouldBeType<LocalConfig>();
            }


        }
    }
}