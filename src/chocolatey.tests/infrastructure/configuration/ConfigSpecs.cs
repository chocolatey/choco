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

namespace chocolatey.tests.infrastructure.configuration
{
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.configuration;
    using FluentAssertions;

    public class ConfigSpecs
    {
        public abstract class ConfigSpecsBase : TinySpec
        {
            public override void Context()
            {
                Config.InitializeWith(new ChocolateyConfiguration());
            }
        }

        public class When_Config_is_set_normally : ConfigSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void Should_be_of_type_ChocolateyConfiguration()
            {
                Config.GetConfigurationSettings().Should().BeOfType<ChocolateyConfiguration>();
            }
        }

        public class When_Config_is_overridden : ConfigSpecsBase
        {
            private class LocalConfig : ChocolateyConfiguration
            {
            }

            public override void Because()
            {
                Config.InitializeWith(new LocalConfig());
            }

            [Fact]
            public void Should_use_the_overridden_type()
            {
                Config.GetConfigurationSettings().Should().BeOfType<LocalConfig>();
            }
        }
    }
}
