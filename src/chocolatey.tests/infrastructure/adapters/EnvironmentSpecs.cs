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

        [NonParallelizable]
        public class When_checking_is64bitprocess_on_arm64 : EnvironmentSpecsBase
        {
            private bool _is64Bit;
            private string _originalValue;

            public override void Context()
            {
                base.Context();
                _originalValue = System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                System.Environment.SetEnvironmentVariable("PROCESSOR_ARCHITECTURE", "ARM64");
            }

            public override void Because()
            {
                _is64Bit = Environment.Is64BitProcess;
            }

            public override void AfterObservations()
            {
                System.Environment.SetEnvironmentVariable("PROCESSOR_ARCHITECTURE", _originalValue);
            }

            [Fact]
            public void Should_return_false_due_to_arm64_logic()
            {
                _is64Bit.Should().BeFalse();
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
