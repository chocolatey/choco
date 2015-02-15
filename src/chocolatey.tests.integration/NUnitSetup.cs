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

namespace chocolatey.tests.integration
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using SimpleInjector;
    using chocolatey.infrastructure.app.builders;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.registration;
    using chocolatey.infrastructure.services;

    // ReSharper disable InconsistentNaming

    [SetUpFixture]
    public class NUnitSetup : tests.NUnitSetup
    {
        public override void BeforeEverything()
        {
            base.BeforeEverything();

            Container = SimpleInjectorContainer.initialize();
            var config = Container.GetInstance<ChocolateyConfiguration>();

            ConfigurationBuilder.set_up_configuration(new List<string>(), config, Container.GetInstance<IFileSystem>(), Container.GetInstance<IXmlService>(), null);
        }

        public static Container Container { get; set; }
    }


    // ReSharper restore InconsistentNaming
}