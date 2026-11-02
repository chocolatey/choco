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
using Chocolatey.PowerShell.Helpers;
using FluentAssertions;
using SystemVariables = chocolatey.StringResources.EnvironmentVariables.System;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    // ReSharper disable InconsistentNaming

    public class ArchitectureWidthSpecs
    {
        public class When_working_out_the_width_from_the_process_and_environment : TinySpec
        {
            public override void Context()
            {
            }

            public override void Because()
            {
            }

            // A 64-bit process.
            [InlineData(8, null, null, 64)]
            [InlineData(8, "AMD64", null, 64)]
            [InlineData(8, "", "", 64)]
            // A 32-bit process on a 32-bit operating system.
            [InlineData(4, "x86", null, 32)]
            // A 32-bit process on a 64-bit operating system.
            [InlineData(4, "x86", "AMD64", 64)]
            // ARM64 uses 32 bit, whichever way it is reported.
            [InlineData(8, "ARM64", null, 32)]
            [InlineData(4, "x86", "ARM64", 32)]
            [InlineData(4, "ARM64", null, 32)]
            [InlineData(8, "arm64", null, 32)]
            [InlineData(8, null, "Arm64", 32)]
            public void Should_report_the_width(int pointerSize, string processorArchitecture, string processorArchitectureW6432, int expected)
            {
                ArchitectureWidth.Get(pointerSize, processorArchitecture, processorArchitectureW6432).Should().Be(expected);
            }
        }

        public abstract class EnvironmentWidthSpecsBase : EnvironmentSpecsBase
        {
            protected int Width;

            protected override string[] ManagedVariables { get; } =
            {
                SystemVariables.ProcessorArchitecture,
                SystemVariables.ProcessorArchitectureW6432,
            };

            public override void Because()
            {
                Width = ArchitectureWidth.Get();
            }
        }

        public class When_no_architecture_variables_are_set : EnvironmentWidthSpecsBase
        {
            [Fact]
            public void Should_report_the_width_of_the_process()
            {
                Width.Should().Be(IntPtr.Size == 4 ? 32 : 64);
            }
        }

        public class When_the_processor_architecture_is_arm64 : EnvironmentWidthSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(SystemVariables.ProcessorArchitecture, "ARM64");
            }

            [Fact]
            public void Should_report_32()
            {
                Width.Should().Be(32);
            }
        }

        public class When_the_operating_system_architecture_is_arm64 : EnvironmentWidthSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(SystemVariables.ProcessorArchitecture, "x86");
                Environment.SetEnvironmentVariable(SystemVariables.ProcessorArchitectureW6432, "ARM64");
            }

            [Fact]
            public void Should_report_32()
            {
                Width.Should().Be(32);
            }
        }

        public class When_the_environment_is_amd64 : EnvironmentWidthSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(SystemVariables.ProcessorArchitecture, "AMD64");
                Environment.SetEnvironmentVariable(SystemVariables.ProcessorArchitectureW6432, "AMD64");
            }

            [Fact]
            public void Should_report_64()
            {
                Width.Should().Be(64);
            }
        }

        public class When_comparing_to_the_width : TinySpec
        {
            public override void Context()
            {
            }

            public override void Because()
            {
            }

            [InlineData(64, 64, true)]
            [InlineData(32, 64, false)]
            [InlineData(32, 32, true)]
            [InlineData("64", 64, true)]
            [InlineData("32", 64, false)]
            [InlineData(64.0, 64, true)]
            [InlineData(0, 64, false)]
            [InlineData("abc", 64, false)]
            public void Should_match_in_the_way_that_powershell_compares(object compareTo, int width, bool expected)
            {
                ArchitectureWidth.Matches(compareTo, width).Should().Be(expected);
            }

            [Fact]
            public void Should_compare_to_the_current_width_without_one_being_given()
            {
                ArchitectureWidth.Matches(ArchitectureWidth.Get()).Should().BeTrue();
            }
        }
    }

    // ReSharper restore InconsistentNaming
}