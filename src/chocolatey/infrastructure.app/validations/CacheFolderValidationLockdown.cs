// Copyright © 2017 - 2023 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.app.validations
{
    using System;
    using System.Collections.Generic;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.information;
    using chocolatey.infrastructure.validations;

    public sealed class CacheFolderLockdownValidation : IValidation
    {
        private readonly IFileSystem _fileSystem;

        public CacheFolderLockdownValidation(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public ICollection<ValidationResult> Validate(ChocolateyConfiguration config)
        {
            this.Log().Debug("Cache Folder Lockdown Checks:");

            var result = new List<ValidationResult>();

            if (!ProcessInformation.IsElevated() && !string.IsNullOrEmpty(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify)))
            {
                this.Log().Debug(" - Elevated State = Failed");

                result.Add(new ValidationResult
                {
                    ExitCode = 0,
                    Message = "User Cache directory is valid.",
                    Status = ValidationStatus.Success
                });

                return result;
            }

            this.Log().Debug(" - Elevated State = Checked");

            var cacheFolderPath = ApplicationParameters.HttpCacheLocation;

            if (_fileSystem.DirectoryExists(cacheFolderPath))
            {
                this.Log().Debug(" - Folder Exists = Checked");

                if (_fileSystem.IsLockedDirectory(cacheFolderPath))
                {
                    this.Log().Debug(" - Folder lockdown = Checked");

                    result.Add(new ValidationResult
                    {
                        ExitCode = 0,
                        Message = "System Cache directory is locked down to administrators.",
                        Status = ValidationStatus.Success
                    });
                }
                else
                {
                    this.Log().Debug(" - Folder lockdown = Failed");

                    result.Add(new ValidationResult
                    {
                        ExitCode = 0,
                        Message = "System Cache directory is not locked down to administrators.\nRemove the directory '{0}' to have Chocolatey CLI create it with the proper permissions.".FormatWith(cacheFolderPath).SplitOnSpace(linePrefix: "   "),
                        Status = ValidationStatus.Warning
                    });
                }

                return result;
            }

            this.Log().Debug(" - Folder Exists = Failed");

            if (_fileSystem.LockDirectory(cacheFolderPath))
            {
                this.Log().Debug(" - Folder lockdown update = Success");

                result.Add(new ValidationResult
                {
                    ExitCode = 0,
                    Message = "System Cache directory successfullly created and locked down to administrators.",
                    Status = ValidationStatus.Success,
                });
            }
            else
            {
                this.Log().Debug(" - Folder lockdown update = Failed");

                result.Add(new ValidationResult
                {
                    ExitCode = 1, // Should we error?
                    Message = "System Cache directory was not created, or could not be locked down to administrators.".SplitOnSpace(linePrefix: "   "),
                    Status = ValidationStatus.Error
                });
            }

            return result;
        }

#pragma warning disable IDE1006

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public ICollection<ValidationResult> validate(ChocolateyConfiguration config)
        {
            return Validate(config);
        }

#pragma warning restore IDE1006
    }
}