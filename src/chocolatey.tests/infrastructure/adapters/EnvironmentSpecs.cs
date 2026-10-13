using System;
using System.Collections;
using System.Reflection;
using chocolatey.infrastructure.adapters;
using FluentAssertions;
using NUnit.Framework;
using Environment = chocolatey.infrastructure.adapters.Environment;

namespace chocolatey.tests.infrastructure.adapters
{
    public class EnvironmentSpecs
    {
        public abstract class EnvironmentSpecsBase : TinySpec
        {
            protected Environment Environment;

            public override void Context()
            {
                Environment = new Environment();
            }
        }

        public class When_checking_is64bitprocess : EnvironmentSpecsBase
        {
            private bool _is64Bit;

            public override void Because()
            {
                _is64Bit = Environment.Is64BitProcess;
            }

            [Fact]
            public void Should_match_the_process_bitness_unless_running_on_arm64()
            {
                // On Windows on ARM the CLI runs either natively as ARM64 or as emulated x64;
                // either way the host is reported as 64-bit. Everywhere else it matches the
                // pointer size.
                if (Environment.IsArm64OperatingSystem)
                {
                    _is64Bit.Should().BeTrue();
                }
                else
                {
                    _is64Bit.Should().Be(IntPtr.Size == 8);
                }
            }
        }

        public class When_getting_the_native_processor_architecture : EnvironmentSpecsBase
        {
            private ProcessorArchitectureType _architecture;

            public override void Because()
            {
                _architecture = Environment.NativeProcessorArchitecture;
            }

            [Fact]
            public void Should_return_a_known_architecture_on_windows_and_unknown_elsewhere()
            {
                if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    _architecture.Should().BeOneOf(
                        ProcessorArchitectureType.X86,
                        ProcessorArchitectureType.X64,
                        ProcessorArchitectureType.Arm64);
                }
                else
                {
                    _architecture.Should().Be(ProcessorArchitectureType.Unknown);
                }
            }

            [Fact]
            public void Should_report_the_arm64_flag_consistently()
            {
                Environment.IsArm64OperatingSystem.Should().Be(_architecture == ProcessorArchitectureType.Arm64);
            }
        }

        [NonParallelizable]
        public class When_expanding_known_environment_variable : EnvironmentSpecsBase
        {
            private string _expanded;

            public override void Context()
            {
                base.Context();
                System.Environment.SetEnvironmentVariable("TEST_ENV_VAR", "expanded_value");
            }

            public override void Because()
            {
                _expanded = Environment.ExpandEnvironmentVariables("before_%TEST_ENV_VAR%_after");
            }

            [Fact]
            public void Should_expand_the_variable()
            {
                _expanded.Should().Be("before_expanded_value_after");
            }
        }

        public class When_getting_current_directory : EnvironmentSpecsBase
        {
            private string _current;

            public override void Because()
            {
                _current = Environment.CurrentDirectory;
            }

            [Fact]
            public void Should_return_non_empty_directory()
            {
                _current.Should().NotBeNullOrWhiteSpace();
            }
        }

        public class When_getting_newline : EnvironmentSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void Should_match_system_newline()
            {
                Environment.NewLine.Should().Be(System.Environment.NewLine);
            }
        }
    }
}
