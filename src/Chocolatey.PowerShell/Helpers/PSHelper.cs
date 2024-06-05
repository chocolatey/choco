// Copyright © 2017 - 2024 Chocolatey Software, Inc
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

using System.Management.Automation;

namespace Chocolatey.PowerShell.Helpers
{
    /// <summary>
    /// Helper methods for cmdlets to make interfacing with PowerShell easier.
    /// </summary>
    public static class PSHelper
    {
        /// <summary>
        /// Writes objects to the output pipeline of the <paramref name="cmdlet"/>, enumerating collections.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="output"></param>
        public static void WriteObject(PSCmdlet cmdlet, object output)
        {
            cmdlet.WriteObject(output, enumerateCollection: true);
        }

        /// <summary>
        /// Helper method to mimic Write-Host from C#, falls back to Write-Verbose when a host is not available.
        /// </summary>
        /// <param name="cmdlet">The cmdlet calling the method.</param>
        /// <param name="message">The message to write to the host.</param>
        public static void WriteHost(PSCmdlet cmdlet, string message)
        {
            if (!(cmdlet.Host is null))
            {
                cmdlet.Host.UI.WriteLine(message);
            }
            else
            {
                cmdlet.WriteVerbose(message);
            }
        }
    }
}
