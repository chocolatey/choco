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
using Chocolatey.PowerShell.Shared;

namespace Chocolatey.PowerShell.Commands
{
    [Cmdlet(VerbsLifecycle.Install, "ChocolateyExplorerMenuItem", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    [OutputType(typeof(void))]
    public class InstallChocolateyExplorerMenuItemCommand : ChocolateyCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        [Alias("MenuKey")]
        public string Key { get; set; }

        [Parameter(Position = 1)]
        [Alias("MenuLabel")]
        public string Label { get; set; } = string.Empty;

        [Parameter(Position = 2)]
        public string Command { get; set; } = string.Empty;

        [Parameter(Position = 3)]
        public ExplorerMenuItemType Type { get; set; } = ExplorerMenuItemType.File;

        protected override void End()
        {
            var target = $@"HKEY_CLASSES_ROOT\{RegistryHelper.GetExplorerMenuItemKeyPath(Key, Type)}";
            if (!ShouldProcess(target, "Create explorer menu item"))
            {
                return;
            }

            try
            {
                Elevation.RunElevated(
                    this,
                    () => RegistryHelper.SetExplorerMenuItem(Key, Label, Command, Type),
                    $"Install-ChocolateyExplorerMenuItem -Key '{Escape(Key)}' -Label '{Escape(Label)}' -Command '{Escape(Command)}' -Type {Type}");

                PSHelper.WriteHost(this, $"'{Key}' explorer menu item has been created");
            }
            catch (Exception error) when (!(error is PipelineStoppedException))
            {
                WriteWarning($"'{Key}' explorer menu item was not created - {error.Message}");
            }
        }

        private static string Escape(string value)
        {
            return PSHelper.EscapeSingleQuotedStringContent(value);
        }
    }
}
