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

namespace chocolatey.tests.infrastructure.app.services
{
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.services;
    using Microsoft.Win32;
    using Moq;
    using Should;
    using Registry = chocolatey.infrastructure.app.domain.Registry;

    public class RegistryServiceSpecs
    {
        public abstract class RegistryServiceSpecsBase : TinySpec
        {
            protected RegistryService Service;
            protected Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();
            protected Mock<IXmlService> XmlService = new Mock<IXmlService>();

            public override void Context()
            {
                reset();
                Service = new RegistryService(XmlService.Object, FileSystem.Object);
            }

            protected void reset()
            {
                FileSystem.ResetCalls();
                XmlService.ResetCalls();
                MockLogger.reset();
            }
        }

        [WindowsOnly]
        public class when_RegistryService_get_installer_keys_is_called : RegistryServiceSpecsBase
        {
            private Registry _result;

            public override void Context()
            {
                base.Context();
            }

            public override void Because()
            {
                _result = Service.get_installer_keys();
            }

            [Fact]
            public void should_not_be_null()
            {
                _result.ShouldNotBeNull();
            }
        }

        [WindowsOnly]
        public class when_RegistryService_get_key_is_called_for_a_value_that_exists : RegistryServiceSpecsBase
        {
            private RegistryKey _result;
            private readonly RegistryHive _hive = RegistryHive.CurrentUser;
            private readonly string _subkeyPath = "Software";

            public override void Context()
            {
                base.Context();
            }

            public override void Because()
            {
                _result = Service.get_key(_hive, _subkeyPath);
            }

            [Fact]
            public void should_return_a_non_null_value()
            {
                _result.ShouldNotBeNull();
            }

            [Fact]
            public void should_return_a_value_of_type_RegistryKey()
            {
                _result.ShouldBeType<RegistryKey>();
            }

            [Fact]
            public void should_contain_keys()
            {
                _result.GetSubKeyNames().ShouldNotBeEmpty();
            }

            [Fact]
            public void should_contain_values()
            {
                Service.get_key(_hive, "Environment").GetValueNames().ShouldNotBeEmpty();
            }
        }

        [WindowsOnly]
        public class when_RegistryService_get_key_is_called_for_a_value_that_does_not_exist : RegistryServiceSpecsBase
        {
            private RegistryKey _result;
            private readonly RegistryHive _hive = RegistryHive.CurrentUser;
            private readonly string _subkeyPath = "Software\\alsdjfalskjfaasdfasdf";

            public override void Context()
            {
                base.Context();
            }

            public override void Because()
            {
                _result = Service.get_key(_hive, _subkeyPath);
            }

            [Fact]
            public void should_not_error()
            {
                //nothing to see here
            }

            [Fact]
            public void should_return_null_key()
            {
                _result.ShouldBeNull();
            }
        }

        //todo a subkey that exists only in 32 bit mode (must create it to test it though)
    }
}
