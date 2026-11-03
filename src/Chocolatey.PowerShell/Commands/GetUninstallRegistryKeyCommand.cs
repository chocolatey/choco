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
    [Cmdlet(VerbsCommon.Get, "UninstallRegistryKey")]
    [OutputType(typeof(PSCustomObject))]
    public class GetUninstallRegistryKeyCommand : ChocolateyCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        // We should use this once we update our System.Management.Automation reference dll
        // to a version that ships it (Definitely v5.1, possibly v3 or v4, needs to be checked.)
        // [SupportsWildcards]
        public string SoftwareName { get; set; }

        protected override void Process()
        {
            try
            {
                WriteObject(RegistryHelper.FindUninstallKeys(this, SoftwareName));
            }
            catch (WildcardPatternException error)
            {
                ThrowTerminatingError(new ErrorRecord(error, $"{ErrorId}.InvalidPattern", ErrorCategory.InvalidArgument, SoftwareName));
            }
            catch (Exception error) when (!(error is PipelineStoppedException))
            {
                // Most other errors are swallowed, but on the off chance we come across an unexpected
                // error, we should err on the side of caution and make it non-terminating in case
                // subsequent inputs can yield useful results.
                WriteError(new ErrorRecord(error, $"{ErrorId}.UnknownError", ErrorCategory.NotSpecified, SoftwareName));
            }
        }
    }
}
