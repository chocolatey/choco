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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Chocolatey.PowerShell.Commands;
using FluentAssertions;
using Moq;
using static chocolatey.StringResources.EnvironmentVariables;

namespace Chocolatey.PowerShell.Tests.Commands
{
    // ReSharper disable InconsistentNaming

    public class GetPackageParametersCommandSpecs
    {
        public class TestGetPackageParametersCommand : GetPackageParametersCommand
        {
            public void RunEnd()
            {
                End();
            }
        }

        public abstract class PackageParametersCommandSpecsBase : EnvironmentSpecsBase
        {
            protected Mock<ICommandRuntime> CommandRuntime;
            protected TestGetPackageParametersCommand Command;
            protected List<object> Written;
            protected Exception Error;

            protected override string[] ManagedVariables { get; } =
            {
                Package.ChocolateyPackageParameters,
                Package.ChocolateyPackageParametersSensitive,
            };

            public override void Context()
            {
                base.Context();

                Written = new List<object>();
                CommandRuntime = new Mock<ICommandRuntime>();
                CommandRuntime
                    .Setup(r => r.WriteObject(It.IsAny<object>(), It.IsAny<bool>()))
                    .Callback<object, bool>((output, enumerate) => Written.Add(output));

                Command = new TestGetPackageParametersCommand { CommandRuntime = CommandRuntime.Object };
            }

            public override void Because()
            {
                try
                {
                    Command.RunEnd();
                }
                catch (Exception error)
                {
                    Error = error;
                }
            }
        }

        public class When_parameters_are_given : PackageParametersCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Environment.SetEnvironmentVariable(Package.ChocolateyPackageParameters, "/FromEnvironment:1");
                Command.Parameters = "/A:1 /B";
            }

            [Fact]
            public void Should_not_fail()
            {
                Error.Should().BeNull();
            }

            [Fact]
            public void Should_write_one_hashtable_of_the_parameters()
            {
                Written.Should().ContainSingle().Which.Should().BeOfType<Hashtable>();
                var result = (Hashtable)Written.Single();

                result.Count.Should().Be(2);
                result["A"].Should().Be("1");
                result["B"].Should().Be(true);
            }
        }

        public class When_no_parameters_are_given : PackageParametersCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Environment.SetEnvironmentVariable(Package.ChocolateyPackageParameters, "/A:1");
            }

            [Fact]
            public void Should_write_one_hashtable_of_the_parameters_in_the_environment()
            {
                Written.Should().ContainSingle().Which.Should().BeOfType<Hashtable>();
                var result = (Hashtable)Written.Single();

                result["A"].Should().Be("1");
            }
        }
    }
}
