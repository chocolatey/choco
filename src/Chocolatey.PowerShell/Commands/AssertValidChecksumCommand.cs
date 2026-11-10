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

using System;
using System.IO;
using System.Management.Automation;
using Chocolatey.PowerShell.Helpers;
using Chocolatey.PowerShell.Shared;

namespace Chocolatey.PowerShell.Commands
{
    [Cmdlet(VerbsLifecycle.Assert, "ValidChecksum")]
    [OutputType(typeof(void))]
    public class AssertValidChecksumCommand : ChocolateyCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        [Alias("File", "FilePath")]
        public string Path { get; set; }

        [Parameter(Position = 1)]
        public string Checksum { get; set; } = string.Empty;

        [Parameter(Position = 2)]
        public ChecksumType ChecksumType { get; set; } = ChecksumType.Md5;

        [Parameter(Position = 3)]
        [Alias("OriginalUrl")]
        public string Url { get; set; } = string.Empty;

        protected override void End()
        {
            try
            {
                ChecksumValidator.AssertChecksumValid(this, Path, Checksum, ChecksumType, Url);
            }
            catch (ChecksumMissingException error)
            {
                ThrowTerminatingError(new ErrorRecord(error, $"{ErrorId}.ChecksumMissing", ErrorCategory.MetadataError, Checksum));
            }
            catch (ChecksumVerificationFailedException error)
            {
                ThrowTerminatingError(new ErrorRecord(error, $"{ErrorId}.BadChecksum", ErrorCategory.InvalidResult, Checksum));
            }
            catch (FileNotFoundException error)
            {
                ThrowTerminatingError(new ErrorRecord(error, $"{ErrorId}.FileNotFound", ErrorCategory.ObjectNotFound, Path));
            }
            catch (ChecksumExeNotFoundException error)
            {
                ThrowTerminatingError(new ErrorRecord(error, $"{ErrorId}.ChecksumExeNotFound", ErrorCategory.ObjectNotFound, targetObject: null));
            }
            catch (Exception error)
            {
                ThrowTerminatingError(new ErrorRecord(error, $"{ErrorId}.Unknown", ErrorCategory.NotSpecified, Path));
            }
        }

    }
}
