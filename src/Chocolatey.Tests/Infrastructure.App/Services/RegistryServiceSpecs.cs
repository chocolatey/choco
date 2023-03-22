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

namespace Chocolatey.Tests.Infrastructure.App.Services
{
    using Chocolatey.Infrastructure.App.Services;
    using Chocolatey.Infrastructure.Filesystem;
    using Chocolatey.Infrastructure.Services;
    using Microsoft.Win32;
    using Moq;
    using NUnit.Framework;
    using Should;
    using Registry = Chocolatey.Infrastructure.App.Domain.Registry;

    public class RegistryServiceSpecs
    {
        public abstract class RegistryServiceSpecsBase : TinySpec
        {
            protected RegistryService Service;
            protected Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();
            protected Mock<IXmlService> XmlService = new Mock<IXmlService>();

            public override void Context()
            {
                Reset();
                Service = new RegistryService(XmlService.Object, FileSystem.Object);
            }

            protected void Reset()
            {
                FileSystem.ResetCalls();
                XmlService.ResetCalls();
                MockLogger.Reset();
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_RegistryService_get_installer_keys_is_called : RegistryServiceSpecsBase
        {
            private Registry _result;

            public override void Context()
            {
                base.Context();
            }

            public override void Because()
            {
                _result = Service.GetInstallerKeys();
            }

            [Fact]
            public void Should_not_be_null()
            {
                _result.ShouldNotBeNull();
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_RegistryService_get_key_is_called_for_a_value_that_exists : RegistryServiceSpecsBase
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
                _result = Service.GetKey(_hive, _subkeyPath);
            }

            [Fact]
            public void Should_return_a_non_null_value()
            {
                _result.ShouldNotBeNull();
            }

            [Fact]
            public void Should_return_a_value_of_type_RegistryKey()
            {
                _result.ShouldBeType<RegistryKey>();
            }

            [Fact]
            public void Should_contain_keys()
            {
                _result.GetSubKeyNames().ShouldNotBeEmpty();
            }

            [Fact]
            public void Should_contain_values()
            {
                Service.GetKey(_hive, "Environment").GetValueNames().ShouldNotBeEmpty();
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_RegistryService_get_key_is_called_for_a_value_that_does_not_exist : RegistryServiceSpecsBase
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
                _result = Service.GetKey(_hive, _subkeyPath);
            }

            [Fact]
            public void Should_not_error()
            {
                //nothing to see here
            }

            [Fact]
            public void Should_return_null_key()
            {
                _result.ShouldBeNull();
            }
        }

        //todo: #2580 a subkey that exists only in 32 bit mode (must create it to test it though)
    }
}
