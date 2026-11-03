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
using System.Management.Automation;
using FluentAssertions;
using Moq;
using SystemVariables = chocolatey.StringResources.EnvironmentVariables.System;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    // ReSharper disable InconsistentNaming

    public class ToolsLocationHelperSpecs
    {
        /// <summary>
        /// Clears the environment variables that the helper reads, with the system drive set to <c>C:</c>, and
        /// gives each spec a cmdlet to create a helper with. Specs derived from this create the helper for the
        /// value they are about.
        /// </summary>
        public abstract class ToolsLocationSpecsBase : EnvironmentSpecsBase
        {
            protected Mock<ICommandRuntime> CommandRuntime;
            protected TestCmdlet Cmdlet;

            protected override string[] ManagedVariables { get; } =
            {
                SystemVariables.ChocolateyToolsLocation,
                SystemVariables.SystemDrive,
            };

            public override void Context()
            {
                base.Context();

                Environment.SetEnvironmentVariable(SystemVariables.SystemDrive, "C:");

                CommandRuntime = new Mock<ICommandRuntime>();
                Cmdlet = new TestCmdlet { CommandRuntime = CommandRuntime.Object };
            }

            public override void Because()
            {
            }

            protected static void SetVariable(string name, string value)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }

        /// <summary>
        /// Base for specs that arrange the environment and the <see cref="Helper"/> in <c>Context()</c>, and look
        /// at the <see cref="Result"/> of getting the location, or the <see cref="Error"/> it fails with.
        /// </summary>
        public abstract class ToolsLocationResultSpecsBase : ToolsLocationSpecsBase
        {
            protected TestToolsLocationHelper Helper;
            protected string Result;
            protected Exception Error;

            public override void Context()
            {
                base.Context();

                Helper = new TestToolsLocationHelper(Cmdlet);
            }

            public override void Because()
            {
                try
                {
                    Result = Helper.Get();
                }
                catch (Exception error)
                {
                    Error = error;
                }
            }
        }

        public class When_nothing_is_set : ToolsLocationResultSpecsBase
        {
            [Fact]
            public void Should_not_fail()
            {
                Error.Should().BeNull();
            }

            [Fact]
            public void Should_be_the_tools_folder_of_the_system_drive()
            {
                Result.Should().Be(@"C:\tools");
            }

            [Fact]
            public void Should_save_it_to_the_user_environment_and_do_nothing_else()
            {
                Helper.Calls.Should().Equal(@"User ChocolateyToolsLocation=C:\tools");
            }
        }

        public class When_the_tools_location_is_set : ToolsLocationResultSpecsBase
        {
            public override void Context()
            {
                base.Context();

                SetVariable(SystemVariables.ChocolateyToolsLocation, @"D:\tools");
            }

            [Fact]
            public void Should_be_that_location()
            {
                Result.Should().Be(@"D:\tools");
            }

            [Fact]
            public void Should_not_change_the_environment()
            {
                Helper.Calls.Should().BeEmpty();
            }
        }

        public class When_the_tools_location_has_a_drive : ToolsLocationSpecsBase
        {
            [InlineData(@"D:\tools")]
            [InlineData(@"d:\tools")]
            [InlineData("D:")]
            [InlineData(@"D:relative")]
            [InlineData(@"1:\tools")]
            [InlineData(@"_:\tools")]
            public void Should_be_used_as_it_is(string toolsLocation)
            {
                SetVariable(SystemVariables.ChocolateyToolsLocation, toolsLocation);
                var helper = new TestToolsLocationHelper(Cmdlet);

                helper.Get().Should().Be(toolsLocation);
                helper.Calls.Should().BeEmpty();
            }
        }

        public class When_the_tools_location_has_no_drive : ToolsLocationSpecsBase
        {
            [InlineData("tools", @"C:\tools")]
            [InlineData(@"\tools", @"C:\tools")]
            [InlineData("/tools", @"C:\tools")]
            [InlineData(@"sub\dir", @"C:\sub\dir")]
            [InlineData(@"ab:\tools", @"C:\ab:\tools")]
            [InlineData(@" C:\tools", @"C:\ C:\tools")]
            public void Should_be_put_on_the_system_drive_and_saved(string toolsLocation, string expected)
            {
                SetVariable(SystemVariables.ChocolateyToolsLocation, toolsLocation);
                var helper = new TestToolsLocationHelper(Cmdlet);

                helper.Get().Should().Be(expected);
                helper.Calls.Should().Equal($"User ChocolateyToolsLocation={expected}");
            }
        }

        public class When_the_system_drive_is_not_c : ToolsLocationResultSpecsBase
        {
            public override void Context()
            {
                base.Context();

                SetVariable(SystemVariables.SystemDrive, "D:");
            }

            [Fact]
            public void Should_use_the_system_drive_for_the_default()
            {
                Result.Should().Be(@"D:\tools");
            }
        }

        public class When_the_user_environment_cannot_be_written_and_the_process_is_not_elevated : ToolsLocationResultSpecsBase
        {
            private readonly Exception _failure = new InvalidOperationException("no user environment");

            public override void Context()
            {
                base.Context();

                Helper.Failure = (name, scope) => scope == EnvironmentVariableTarget.User ? _failure : null;
            }

            [Fact]
            public void Should_fail_with_the_original_exception()
            {
                Error.Should().BeSameAs(_failure);
            }

            [Fact]
            public void Should_not_try_the_machine_environment()
            {
                Helper.Calls.Should().Equal(
                    @"User ChocolateyToolsLocation=C:\tools",
                    "IsElevated");
            }
        }

        public class When_the_user_environment_cannot_be_written_and_the_process_is_elevated : ToolsLocationResultSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Helper.Elevated = true;
                Helper.Failure = (name, scope) => scope == EnvironmentVariableTarget.User ? new InvalidOperationException("no user environment") : null;
            }

            [Fact]
            public void Should_not_fail()
            {
                Error.Should().BeNull();
            }

            [Fact]
            public void Should_be_the_location()
            {
                Result.Should().Be(@"C:\tools");
            }

            [Fact]
            public void Should_save_it_to_the_machine_environment_instead()
            {
                Helper.Calls.Should().Equal(
                    @"User ChocolateyToolsLocation=C:\tools",
                    "IsElevated",
                    @"Machine ChocolateyToolsLocation=C:\tools");
            }
        }

        public class When_neither_environment_can_be_written : ToolsLocationResultSpecsBase
        {
            private readonly Exception _machineFailure = new InvalidOperationException("no machine environment");

            public override void Context()
            {
                base.Context();

                Helper.Elevated = true;
                Helper.Failure = (name, scope) => scope == EnvironmentVariableTarget.User
                    ? new InvalidOperationException("no user environment")
                    : _machineFailure;
            }

            [Fact]
            public void Should_fail_with_the_exception_from_the_machine_environment()
            {
                Error.Should().BeSameAs(_machineFailure);
            }
        }

        public class When_the_pipeline_is_stopped_while_saving_the_location : ToolsLocationResultSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Helper.Elevated = true;
                Helper.Failure = (name, scope) => new PipelineStoppedException();
            }

            [Fact]
            public void Should_stop()
            {
                Error.Should().BeOfType<PipelineStoppedException>();
            }

            [Fact]
            public void Should_not_try_another_environment()
            {
                Helper.Calls.Should().Equal(@"User ChocolateyToolsLocation=C:\tools");
            }
        }
    }
}
