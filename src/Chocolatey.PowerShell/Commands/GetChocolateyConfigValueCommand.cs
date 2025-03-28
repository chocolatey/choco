// Copyright © 2017 - 2025 Chocolatey Software, Inc
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

using Chocolatey.PowerShell.Helpers;
using Chocolatey.PowerShell.Shared;
using System;
using System.IO;
using System.Management.Automation;

namespace Chocolatey.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "ChocolateyConfigValue")]
    [OutputType(typeof(string))]
    public class GetChocolateyConfigValueCommand : ChocolateyCmdlet
    {
        [Parameter(Mandatory = true)]
        [Alias("ConfigKey")]
        public string Name { get; set; }

        protected override void End()
        {
            try
            {
                var result = ConfigHelper.GetConfigValue(this, Name);
                if (!(result is null))
                {
                    WriteObject(result);
                }
            }
            catch (InvalidOperationException error)
            {
                ThrowTerminatingError(new ErrorRecord(
                    error,
                    $"{ErrorId}.CannotLoadConfig",
                    ErrorCategory.InvalidOperation,
                    targetObject: null));
            }
            catch (FileNotFoundException error)
            {
                ThrowTerminatingError(new ErrorRecord(
                    error,
                    $"{ErrorId}.ConfigNotFound",
                    ErrorCategory.ObjectNotFound,
                    targetObject: null));
            }
            catch (Exception error)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new RuntimeException(error.Message, error),
                    $"{ErrorId}.Unknown",
                    ErrorCategory.NotSpecified,
                    targetObject: null));
            }
        }
    }
}
