// Copyright © 2017 - 2026 Chocolatey Software, Inc
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Chocolatey.PowerShell.Commands;
using FluentAssertions;
using Moq;
using SystemVariables = chocolatey.StringResources.EnvironmentVariables.System;

namespace Chocolatey.PowerShell.Tests.Commands
{
    // ReSharper disable InconsistentNaming

    public class GetToolsLocationCommandSpecs
    {
        public class TestGetToolsLocationCommand : GetToolsLocationCommand
        {
            public void RunEnd()
            {
                End();
            }
        }

        public class When_the_tools_location_is_set : EnvironmentSpecsBase
        {
            private Mock<ICommandRuntime> _commandRuntime;
            private TestGetToolsLocationCommand _command;
            private List<object> _written;
            private Exception _error;

            protected override string[] ManagedVariables { get; } =
            {
                SystemVariables.ChocolateyToolsLocation,
                SystemVariables.ChocolateyBinRoot,
                SystemVariables.ChocolateyBinRootLegacy,
            };

            public override void Context()
            {
                base.Context();

                // A location that has a drive is used as it is, so the command makes no change to the environment.
                Environment.SetEnvironmentVariable(SystemVariables.ChocolateyToolsLocation, @"D:\tools");

                _written = new List<object>();
                _commandRuntime = new Mock<ICommandRuntime>();
                _commandRuntime
                    .Setup(r => r.WriteObject(It.IsAny<object>(), It.IsAny<bool>()))
                    .Callback<object, bool>((output, enumerate) => _written.Add(output));

                _command = new TestGetToolsLocationCommand { CommandRuntime = _commandRuntime.Object };
            }

            public override void Because()
            {
                try
                {
                    _command.RunEnd();
                }
                catch (Exception error)
                {
                    _error = error;
                }
            }

            [Fact]
            public void Should_not_fail()
            {
                _error.Should().BeNull();
            }

            [Fact]
            public void Should_write_the_location_as_one_string()
            {
                _written.Should().ContainSingle().Which.Should().Be(@"D:\tools");
                _written.Single().Should().BeOfType<string>();
            }
        }
    }
}
