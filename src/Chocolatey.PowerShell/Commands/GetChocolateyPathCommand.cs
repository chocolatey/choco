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

using Chocolatey.PowerShell;
using Chocolatey.PowerShell.Helpers;
using Chocolatey.PowerShell.Shared;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

using static Chocolatey.PowerShell.Helpers.PSHelper;

namespace Chocolatey.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "ChocolateyPath")]
    [OutputType(typeof(string))]
    public class GetChocolateyPathCommand : ChocolateyCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        [Alias("Type")]
        public ChocolateyPathType PathType { get; set; }

        protected override void End()
        {
            try
            {
                var path = Paths.GetChocolateyPathType(this, PathType);

                if (ContainerExists(this, path))
                {
                    WriteObject(path);
                }
            }
            catch (NotImplementedException error)
            {
                ThrowTerminatingError(new ErrorRecord(error, $"{ErrorId}.NotImplemented", ErrorCategory.NotImplemented, PathType));
            }
            catch (Exception error)
            {
                ThrowTerminatingError(new ErrorRecord(error, $"{ErrorId}.Unknown", ErrorCategory.NotSpecified, PathType));
            }
        }
    }
}
