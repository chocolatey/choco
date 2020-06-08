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

namespace chocolatey.tests.infrastructure.configuration
{
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.configuration;
    using Should;

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
            {
            }

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
