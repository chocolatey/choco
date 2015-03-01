// Copyright © 2011 - Present RealDimensions Software, LLC
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

namespace chocolatey.tests
{
    using log4net;
    using Should;

    public class GetChocolateySpecs
    {
        public abstract class GetChocolateySpecsBase : TinySpec
        {
            public override void Context()
            {
            }
        }

        public class when_getting_chocolatey : GetChocolateySpecsBase
        {
            private GetChocolatey _chocolatey;

            public override void Because()
            {
                _chocolatey = Lets.GetChocolatey();
            }

            [Fact]
            public void should_get_chocolotey()
            {
                _chocolatey.ShouldNotBeNull();
            }

            [Fact]
            public void should_conigure_log4net()
            {
                LogManager.GetRepository().Configured.ShouldBeTrue();
            }
        }

        public class when_getting_chocolatey_more_than_once : GetChocolateySpecsBase
        {
            private GetChocolatey _chocolatey1;
            private GetChocolatey _chocolatey2;

            public override void Because()
            {
                _chocolatey1 = Lets.GetChocolatey();
                _chocolatey2 = Lets.GetChocolatey();
            }

            [Fact]
            public void should_get_instantiated_chocolotey1()
            {
                _chocolatey1.ShouldNotBeNull();
            }

            [Fact]
            public void should_get_instantiated_chocolotey2()
            {
                _chocolatey2.ShouldNotBeNull();
            }
        }
    }
}