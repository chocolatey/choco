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
using Chocolatey.PowerShell.Shared;
using FluentAssertions;
using Microsoft.Win32;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    // ReSharper disable InconsistentNaming

    public class RegistryHelperExplorerMenuItemSpecs
    {
        public class When_getting_the_explorer_menu_item_key_path : TinySpec
        {
            public override void Context()
            {
            }

            public override void Because()
            {
            }

            [InlineData("sublime", ExplorerMenuItemType.File, @"*\shell\sublime")]
            [InlineData("sublime", ExplorerMenuItemType.Directory, @"directory\shell\sublime")]
            [InlineData(@"nested\child", ExplorerMenuItemType.File, @"*\shell\nested\child")]
            public void Should_use_the_shell_key_for_the_item_type(string key, ExplorerMenuItemType type, string expected)
            {
                RegistryHelper.GetExplorerMenuItemKeyPath(key, type).Should().Be(expected);
            }
        }

        /// <summary>
        /// Gives each spec a temporary key under <c>HKCU\Software\ChocolateyTests</c> to stand in for
        /// <c>HKEY_CLASSES_ROOT</c>, removed after the spec. Derived specs create a menu item under it in
        /// <c>Because()</c> and read the values back.
        /// </summary>
        public abstract class ExplorerMenuItemSpecsBase : TinySpec
        {
            private string _rootPath;
            protected RegistryKey Root;

            public override void Context()
            {
                _rootPath = $@"Software\ChocolateyTests\{Guid.NewGuid():N}";
                Root = Registry.CurrentUser.CreateSubKey(_rootPath);
            }

            public override void AfterObservations()
            {
                Root?.Dispose();
                Registry.CurrentUser.DeleteSubKeyTree(_rootPath, throwOnMissingSubKey: false);
            }

            protected void SetExplorerMenuItem(string key, string label, string command, ExplorerMenuItemType type = ExplorerMenuItemType.File)
            {
                RegistryHelper.SetExplorerMenuItem(Root, key, label, command, type);
            }

            protected bool KeyExists(string path)
            {
                using (var key = Root.OpenSubKey(path))
                {
                    return key != null;
                }
            }

            protected object GetValue(string path, string name = "")
            {
                using (var key = Root.OpenSubKey(path))
                {
                    return key?.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                }
            }

            protected RegistryValueKind GetValueKind(string path)
            {
                using (var key = Root.OpenSubKey(path))
                {
                    return key.GetValueKind(string.Empty);
                }
            }
        }

        public class When_creating_a_menu_item_for_files : ExplorerMenuItemSpecsBase
        {
            public override void Because()
            {
                SetExplorerMenuItem("tool", "Open with Tool", @"C:\Program Files\Tool\tool.exe");
            }

            [Fact]
            public void Should_set_the_label_as_the_default_value_of_the_menu_key()
            {
                GetValue(@"*\shell\tool").Should().Be("Open with Tool");
            }

            [Fact]
            public void Should_set_the_command_with_the_quoted_item_path_as_the_default_value_of_the_command_key()
            {
                GetValue(@"*\shell\tool\command").Should().Be(@"C:\Program Files\Tool\tool.exe ""%1""");
            }

            [Fact]
            public void Should_write_both_values_as_strings()
            {
                GetValueKind(@"*\shell\tool").Should().Be(RegistryValueKind.String);
                GetValueKind(@"*\shell\tool\command").Should().Be(RegistryValueKind.String);
            }

            [Fact]
            public void Should_not_create_a_directory_menu_item()
            {
                KeyExists("directory").Should().BeFalse();
            }
        }

        public class When_creating_a_menu_item_for_directories : ExplorerMenuItemSpecsBase
        {
            public override void Because()
            {
                SetExplorerMenuItem("tool", "Open folder with Tool", @"C:\tool.exe", ExplorerMenuItemType.Directory);
            }

            [Fact]
            public void Should_set_the_label_under_the_directory_shell_key()
            {
                GetValue(@"directory\shell\tool").Should().Be("Open folder with Tool");
            }

            [Fact]
            public void Should_set_the_command_under_the_directory_shell_key()
            {
                GetValue(@"directory\shell\tool\command").Should().Be(@"C:\tool.exe ""%1""");
            }

            [Fact]
            public void Should_not_create_a_file_menu_item()
            {
                KeyExists("*").Should().BeFalse();
            }
        }

        public class When_updating_an_existing_menu_item : ExplorerMenuItemSpecsBase
        {
            public override void Context()
            {
                base.Context();

                using (var menuKey = Root.CreateSubKey(@"*\shell\tool"))
                {
                    menuKey.SetValue(string.Empty, "Old label");
                    menuKey.SetValue("Icon", @"C:\tool.exe,0");
                }

                using (var commandKey = Root.CreateSubKey(@"*\shell\tool\command"))
                {
                    commandKey.SetValue(string.Empty, @"C:\old.exe ""%1""");
                }
            }

            public override void Because()
            {
                SetExplorerMenuItem("tool", "New label", @"C:\new.exe");
            }

            [Fact]
            public void Should_replace_the_label()
            {
                GetValue(@"*\shell\tool").Should().Be("New label");
            }

            [Fact]
            public void Should_replace_the_command()
            {
                GetValue(@"*\shell\tool\command").Should().Be(@"C:\new.exe ""%1""");
            }

            [Fact]
            public void Should_keep_other_values_on_the_menu_key()
            {
                GetValue(@"*\shell\tool", "Icon").Should().Be(@"C:\tool.exe,0");
            }
        }

        public class When_creating_a_menu_item_with_quotes_and_variables : ExplorerMenuItemSpecsBase
        {
            public override void Because()
            {
                SetExplorerMenuItem("it's", "Tool's $env:USERNAME menu", @"'C:\it's\tool.exe' %TEMP%");
            }

            [Fact]
            public void Should_write_the_label_as_given()
            {
                GetValue(@"*\shell\it's").Should().Be("Tool's $env:USERNAME menu");
            }

            [Fact]
            public void Should_write_the_command_as_given()
            {
                GetValue(@"*\shell\it's\command").Should().Be(@"'C:\it's\tool.exe' %TEMP% ""%1""");
            }
        }

        public class When_creating_a_menu_item_without_a_label_or_command : ExplorerMenuItemSpecsBase
        {
            public override void Because()
            {
                SetExplorerMenuItem("empty", null, null);
            }

            [Fact]
            public void Should_set_an_empty_label()
            {
                GetValue(@"*\shell\empty").Should().Be(string.Empty);
            }

            [Fact]
            public void Should_set_only_the_item_path_as_the_command()
            {
                GetValue(@"*\shell\empty\command").Should().Be(@" ""%1""");
            }
        }

        public class When_creating_a_menu_item_with_a_nested_key : ExplorerMenuItemSpecsBase
        {
            public override void Because()
            {
                SetExplorerMenuItem(@"nested\child", "Nested", @"C:\tool.exe");
            }

            [Fact]
            public void Should_create_the_intermediate_keys()
            {
                GetValue(@"*\shell\nested\child").Should().Be("Nested");
                GetValue(@"*\shell\nested\child\command").Should().Be(@"C:\tool.exe ""%1""");
            }
        }
    }

    // ReSharper restore InconsistentNaming
}
