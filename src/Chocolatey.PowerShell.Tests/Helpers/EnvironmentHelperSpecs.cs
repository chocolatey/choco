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
using Chocolatey.PowerShell.Helpers;
using FluentAssertions;
using Microsoft.Win32;
using Moq;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    public class EnvironmentHelperSpecs
    {
        /// <summary>
        /// Points <see cref="EnvironmentHelper"/> at User and Machine environment keys created under a
        /// temporary key in HKCU, and gives it a cmdlet whose ShouldProcess calls answer false unless a
        /// spec allows them, so the system notification and session refresh never run.
        /// </summary>
        public abstract class EnvironmentHelperSpecsBase : EnvironmentSpecsBase
        {
            private const string MachineEnvironmentKeyName = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";
            private const string UserEnvironmentKeyName = "Environment";

            private RegistryKey _originalUserRoot;
            private RegistryKey _originalMachineRoot;
            private string _testKeyPath;

            protected RegistryKey UserRoot;
            protected RegistryKey MachineRoot;
            protected RegistryKey UserEnvironmentKey;
            protected RegistryKey MachineEnvironmentKey;
            protected Mock<ICommandRuntime> CommandRuntime;
            protected TestCmdlet Cmdlet;

            // Unique, so that the helper's fallback to the real environment can never find a value for it.
            protected string VariableName { get; } = "ChocolateyTest" + Guid.NewGuid().ToString("N");

            protected override string[] ManagedVariables => new[] { VariableName };

            public override void Context()
            {
                base.Context();

                _testKeyPath = $@"Software\ChocolateyTests\{Guid.NewGuid():N}";
                UserRoot = Registry.CurrentUser.CreateSubKey($@"{_testKeyPath}\User");
                MachineRoot = Registry.CurrentUser.CreateSubKey($@"{_testKeyPath}\Machine");
                UserEnvironmentKey = UserRoot.CreateSubKey(UserEnvironmentKeyName);
                MachineEnvironmentKey = MachineRoot.CreateSubKey(MachineEnvironmentKeyName);

                _originalUserRoot = EnvironmentHelper.UserEnvironmentRoot;
                _originalMachineRoot = EnvironmentHelper.MachineEnvironmentRoot;
                EnvironmentHelper.UserEnvironmentRoot = UserRoot;
                EnvironmentHelper.MachineEnvironmentRoot = MachineRoot;

                CommandRuntime = new Mock<ICommandRuntime>();
                Cmdlet = new TestCmdlet { CommandRuntime = CommandRuntime.Object };
            }

            public override void AfterObservations()
            {
                EnvironmentHelper.UserEnvironmentRoot = _originalUserRoot;
                EnvironmentHelper.MachineEnvironmentRoot = _originalMachineRoot;

                UserEnvironmentKey?.Dispose();
                MachineEnvironmentKey?.Dispose();
                UserRoot?.Dispose();
                MachineRoot?.Dispose();
                Registry.CurrentUser.DeleteSubKeyTree(_testKeyPath, throwOnMissingSubKey: false);

                base.AfterObservations();
            }

            protected RegistryKey GetEnvironmentKey(EnvironmentVariableTarget scope)
            {
                return scope == EnvironmentVariableTarget.User ? UserEnvironmentKey : MachineEnvironmentKey;
            }

            protected void DeleteUserEnvironmentKey()
            {
                UserEnvironmentKey.Dispose();
                UserEnvironmentKey = null;
                UserRoot.DeleteSubKeyTree(UserEnvironmentKeyName);
            }

            protected void AllowShouldProcess(string target, string action)
            {
                CommandRuntime.Setup(r => r.ShouldProcess(target, action)).Returns(true);
            }
        }

        public class When_the_environment_keys_exist : EnvironmentHelperSpecsBase
        {
            private bool _userKeyExists;
            private bool _machineKeyExists;

            public override void Because()
            {
                _userKeyExists = EnvironmentHelper.EnvironmentKeyExists(EnvironmentVariableTarget.User);
                _machineKeyExists = EnvironmentHelper.EnvironmentKeyExists(EnvironmentVariableTarget.Machine);
            }

            [Fact]
            public void Should_find_the_user_key()
            {
                _userKeyExists.Should().BeTrue();
            }

            [Fact]
            public void Should_find_the_machine_key()
            {
                _machineKeyExists.Should().BeTrue();
            }
        }

        public class When_the_user_environment_key_is_missing : EnvironmentHelperSpecsBase
        {
            private bool _keyExists;
            private string[] _names;
            private string _value;
            private Exception _setError;

            public override void Context()
            {
                base.Context();
                DeleteUserEnvironmentKey();
                AllowShouldProcess(VariableName, "Set User environment variable");
            }

            public override void Because()
            {
                _keyExists = EnvironmentHelper.EnvironmentKeyExists(EnvironmentVariableTarget.User);
                _names = EnvironmentHelper.GetVariableNames(EnvironmentVariableTarget.User);
                _value = EnvironmentHelper.GetVariable(Cmdlet, VariableName, EnvironmentVariableTarget.User);

                try
                {
                    EnvironmentHelper.SetVariable(Cmdlet, VariableName, EnvironmentVariableTarget.User, "value");
                }
                catch (Exception error)
                {
                    _setError = error;
                }
            }

            [Fact]
            public void Should_not_find_the_key()
            {
                _keyExists.Should().BeFalse();
            }

            [Fact]
            public void Should_list_no_names()
            {
                _names.Should().BeEmpty();
            }

            [Fact]
            public void Should_get_an_empty_value()
            {
                _value.Should().BeEmpty();
            }

            [Fact]
            public void Should_fail_to_set_a_value()
            {
                // Characterization: callers are expected to check EnvironmentKeyExists first.
                _setError.Should().BeOfType<NullReferenceException>();
            }
        }

        public class When_getting_a_string_variable : EnvironmentHelperSpecsBase
        {
            private string _userValue;
            private string _machineValue;

            public override void Context()
            {
                base.Context();
                UserEnvironmentKey.SetValue(VariableName, "user value", RegistryValueKind.String);
                MachineEnvironmentKey.SetValue(VariableName, "machine value", RegistryValueKind.String);
            }

            public override void Because()
            {
                _userValue = EnvironmentHelper.GetVariable(Cmdlet, VariableName, EnvironmentVariableTarget.User);
                _machineValue = EnvironmentHelper.GetVariable(Cmdlet, VariableName, EnvironmentVariableTarget.Machine);
            }

            [Fact]
            public void Should_get_the_user_value()
            {
                _userValue.Should().Be("user value");
            }

            [Fact]
            public void Should_get_the_machine_value()
            {
                _machineValue.Should().Be("machine value");
            }
        }

        public class When_getting_an_expandable_variable : EnvironmentHelperSpecsBase
        {
            private const string RawValue = @"%SystemRoot%\test";

            private string _expandedValue;
            private string _preservedValue;

            public override void Context()
            {
                base.Context();
                UserEnvironmentKey.SetValue(VariableName, RawValue, RegistryValueKind.ExpandString);
            }

            public override void Because()
            {
                _expandedValue = EnvironmentHelper.GetVariable(Cmdlet, VariableName, EnvironmentVariableTarget.User);
                _preservedValue = EnvironmentHelper.GetVariable(Cmdlet, VariableName, EnvironmentVariableTarget.User, preserveVariables: true);
            }

            [Fact]
            public void Should_expand_the_value_by_default()
            {
                _expandedValue.Should().Be(Environment.ExpandEnvironmentVariables(RawValue));
            }

            [Fact]
            public void Should_not_expand_the_value_when_preserving_variables()
            {
                _preservedValue.Should().Be(RawValue);
            }
        }

        public class When_getting_a_variable_that_is_not_a_string : EnvironmentHelperSpecsBase
        {
            private string _value;

            public override void Context()
            {
                base.Context();
                UserEnvironmentKey.SetValue(VariableName, 1, RegistryValueKind.DWord);
            }

            public override void Because()
            {
                _value = EnvironmentHelper.GetVariable(Cmdlet, VariableName, EnvironmentVariableTarget.User);
            }

            [Fact]
            public void Should_get_an_empty_value()
            {
                _value.Should().BeEmpty();
            }

            [Fact]
            public void Should_write_why_it_could_not_get_the_value()
            {
                CommandRuntime.Verify(
                    r => r.WriteDebug(It.Is<string>(s => s.StartsWith($"Unable to retrieve the {VariableName} environment variable. Details: "))),
                    Times.Once);
            }
        }

        public class When_getting_a_process_variable : EnvironmentHelperSpecsBase
        {
            private string _value;

            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(VariableName, "process value");
            }

            public override void Because()
            {
                _value = EnvironmentHelper.GetVariable(Cmdlet, VariableName, EnvironmentVariableTarget.Process);
            }

            [Fact]
            public void Should_get_the_process_value()
            {
                _value.Should().Be("process value");
            }
        }

        public class When_getting_variable_names : EnvironmentHelperSpecsBase
        {
            private string[] _userNames;
            private string[] _machineNames;

            public override void Context()
            {
                base.Context();
                UserEnvironmentKey.SetValue("First", "1");
                UserEnvironmentKey.SetValue("Second", "2");
                MachineEnvironmentKey.SetValue("Third", "3");
            }

            public override void Because()
            {
                _userNames = EnvironmentHelper.GetVariableNames(EnvironmentVariableTarget.User);
                _machineNames = EnvironmentHelper.GetVariableNames(EnvironmentVariableTarget.Machine);
            }

            [Fact]
            public void Should_list_the_user_names()
            {
                _userNames.Should().BeEquivalentTo("First", "Second");
            }

            [Fact]
            public void Should_list_the_machine_names()
            {
                _machineNames.Should().BeEquivalentTo("Third");
            }
        }

        public class When_setting_a_new_variable : EnvironmentHelperSpecsBase
        {
            public override void Context()
            {
                base.Context();
                AllowShouldProcess(VariableName, "Set User environment variable");
                AllowShouldProcess(VariableName, "Set Machine environment variable");
            }

            public override void Because()
            {
                EnvironmentHelper.SetVariable(Cmdlet, VariableName, EnvironmentVariableTarget.User, "user value");
                EnvironmentHelper.SetVariable(Cmdlet, VariableName, EnvironmentVariableTarget.Machine, "machine value");
            }

            [InlineData(EnvironmentVariableTarget.User, "user value")]
            [InlineData(EnvironmentVariableTarget.Machine, "machine value")]
            public void Should_write_the_value(EnvironmentVariableTarget scope, string expected)
            {
                GetEnvironmentKey(scope).GetValue(VariableName).Should().Be(expected);
            }

            [InlineData(EnvironmentVariableTarget.User)]
            [InlineData(EnvironmentVariableTarget.Machine)]
            public void Should_write_a_string_value(EnvironmentVariableTarget scope)
            {
                GetEnvironmentKey(scope).GetValueKind(VariableName).Should().Be(RegistryValueKind.String);
            }

            [Fact]
            public void Should_write_the_registry_type()
            {
                CommandRuntime.Verify(r => r.WriteDebug($"Registry type for {VariableName} is/will be String"), Times.Exactly(2));
            }
        }

        public class When_setting_an_expandable_variable : EnvironmentHelperSpecsBase
        {
            private const string NewValue = @"%SystemRoot%\new";

            public override void Context()
            {
                base.Context();
                UserEnvironmentKey.SetValue(VariableName, @"%SystemRoot%\old", RegistryValueKind.ExpandString);
                AllowShouldProcess(VariableName, "Set User environment variable");
            }

            public override void Because()
            {
                EnvironmentHelper.SetVariable(Cmdlet, VariableName, EnvironmentVariableTarget.User, NewValue);
            }

            [Fact]
            public void Should_keep_the_existing_value_kind()
            {
                UserEnvironmentKey.GetValueKind(VariableName).Should().Be(RegistryValueKind.ExpandString);
            }

            [Fact]
            public void Should_write_the_value_unexpanded()
            {
                UserEnvironmentKey.GetValue(VariableName, null, RegistryValueOptions.DoNotExpandEnvironmentNames).Should().Be(NewValue);
            }
        }

        public class When_setting_the_path_variable : EnvironmentHelperSpecsBase
        {
            private const string PathName = "Path";

            public override void Context()
            {
                base.Context();
                AllowShouldProcess(PathName, "Set User environment variable");
            }

            public override void Because()
            {
                EnvironmentHelper.SetVariable(Cmdlet, PathName, EnvironmentVariableTarget.User, @"C:\test");
            }

            [Fact]
            public void Should_write_an_expandable_value()
            {
                UserEnvironmentKey.GetValueKind(PathName).Should().Be(RegistryValueKind.ExpandString);
            }

            [Fact]
            public void Should_write_the_registry_type()
            {
                CommandRuntime.Verify(r => r.WriteDebug($"Registry type for {PathName} is/will be ExpandString"), Times.Once);
            }
        }

        public class When_setting_a_variable_to_an_empty_value : EnvironmentHelperSpecsBase
        {
            private const string NullName = "SetToNull";
            private const string EmptyName = "SetToEmpty";
            private const string MissingName = "NeverSet";

            private Exception _missingError;

            public override void Context()
            {
                base.Context();
                UserEnvironmentKey.SetValue(NullName, "value");
                UserEnvironmentKey.SetValue(EmptyName, "value");

                foreach (var name in new[] { NullName, EmptyName, MissingName })
                {
                    AllowShouldProcess(name, "Set User environment variable");
                }
            }

            public override void Because()
            {
                EnvironmentHelper.SetVariable(Cmdlet, NullName, EnvironmentVariableTarget.User, null);
                EnvironmentHelper.SetVariable(Cmdlet, EmptyName, EnvironmentVariableTarget.User, string.Empty);

                try
                {
                    EnvironmentHelper.SetVariable(Cmdlet, MissingName, EnvironmentVariableTarget.User, null);
                }
                catch (Exception error)
                {
                    _missingError = error;
                }
            }

            [InlineData(NullName)]
            [InlineData(EmptyName)]
            public void Should_delete_the_value(string name)
            {
                UserEnvironmentKey.GetValueNames().Should().NotContain(name);
            }

            [Fact]
            public void Should_not_fail_when_the_value_does_not_exist()
            {
                _missingError.Should().BeNull();
            }
        }

        public class When_should_process_declines : EnvironmentHelperSpecsBase
        {
            public override void Context()
            {
                base.Context();
                UserEnvironmentKey.SetValue(VariableName, "old value");
                Environment.SetEnvironmentVariable(VariableName, "old value");
            }

            public override void Because()
            {
                EnvironmentHelper.SetVariable(Cmdlet, VariableName, EnvironmentVariableTarget.User, "new value");
                EnvironmentHelper.SetVariable(Cmdlet, VariableName, EnvironmentVariableTarget.Process, "new value");
            }

            [Fact]
            public void Should_not_change_the_registry_value()
            {
                UserEnvironmentKey.GetValue(VariableName).Should().Be("old value");
            }

            [Fact]
            public void Should_not_change_the_process_value()
            {
                Environment.GetEnvironmentVariable(VariableName).Should().Be("old value");
            }
        }

        public class When_setting_a_process_variable : EnvironmentHelperSpecsBase
        {
            public override void Context()
            {
                base.Context();
                AllowShouldProcess(VariableName, "Set Process environment variable");
            }

            public override void Because()
            {
                EnvironmentHelper.SetVariable(Cmdlet, VariableName, EnvironmentVariableTarget.Process, "process value");
            }

            [Fact]
            public void Should_set_the_process_value()
            {
                Environment.GetEnvironmentVariable(VariableName).Should().Be("process value");
            }

            [Fact]
            public void Should_not_write_to_the_registry()
            {
                UserEnvironmentKey.GetValueNames().Should().NotContain(VariableName);
                MachineEnvironmentKey.GetValueNames().Should().NotContain(VariableName);
            }
        }
    }
}
