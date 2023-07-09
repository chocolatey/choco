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

namespace chocolatey.tests.infrastructure.commands
{
    using System;
    using System.IO;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.filesystem;
    using Moq;
    using NUnit.Framework;
    using FluentAssertions;

    public class PowershellExecutorSpecs
    {
        public abstract class PowerShellExecutorSpecsBase : TinySpec
        {
            protected Mock<IFileSystem> FileSystem;

            public override void Context()
            {
                FileSystem = new Mock<IFileSystem>();
            }
        }

        public class When_powershellExecutor_is_searching_for_powershell_locations_and_all_locations_exist : PowerShellExecutorSpecsBase
        {
            private string _result = string.Empty;
            private readonly string _expected = Environment.ExpandEnvironmentVariables("%systemroot%\\SysNative\\WindowsPowerShell\\v1.0\\powershell.exe");

            public override void Context()
            {
                base.Context();
                FileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
            }

            public override void Because()
            {
                _result = PowershellExecutor.GetPowerShellLocation(FileSystem.Object);
            }

            [Fact]
            public void Should_not_return_null()
            {
                _result.Should().NotBeNull();
            }

            [Fact]
            public void Should_find_powershell()
            {
                _result.Should().NotBeEmpty();
            }

            [Fact]
            public void Should_return_the_sysnative_path()
            {
                _result.Should().Be(_expected);
            }
        }

        public class When_powershellExecutor_is_searching_for_powershell_locations_there_is_no_sysnative : PowerShellExecutorSpecsBase
        {
            private string _result = string.Empty;
            private readonly string _expected = Environment.ExpandEnvironmentVariables("%systemroot%\\System32\\WindowsPowerShell\\v1.0\\powershell.exe");

            public override void Context()
            {
                base.Context();

                FileSystem.Setup(fs => fs.FileExists(_expected)).Returns(true);
                FileSystem.Setup(fs => fs.FileExists(It.Is<string>(v => v != _expected))).Returns(false);
            }

            public override void Because()
            {
                _result = PowershellExecutor.GetPowerShellLocation(FileSystem.Object);
            }

            [Fact]
            public void Should_return_system32_path()
            {
                _result.Should().Be(_expected);
            }
        }

        public class When_powershellExecutor_is_searching_for_powershell_locations_and_powershell_is_not_found : PowerShellExecutorSpecsBase
        {
            public override void Context()
            {
                base.Context();
                FileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
            }

            public override void Because()
            {
                //nothing
            }

            [Fact]
            public void Should_throw_an_error()
            {
                Assert.Throws<FileNotFoundException>(() => PowershellExecutor.GetPowerShellLocation(FileSystem.Object));
            }
        }
    }
}
