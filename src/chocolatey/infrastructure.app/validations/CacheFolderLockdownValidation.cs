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

using System;
using System.Collections.Generic;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.filesystem;
using chocolatey.infrastructure.information;
using chocolatey.infrastructure.validations;

namespace chocolatey.infrastructure.app.validations
{
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

            if (!ProcessInformation.IsElevated())
            {
                this.Log().Debug(" - Elevated State = Failed");

                if (string.IsNullOrEmpty(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify)))
                {
                    result.Add(new ValidationResult
                    {
                        ExitCode = 0,
                        Message = "There is no valid user profile directory for the HTTP cache.",
                        Status = ValidationStatus.Warning,
                    });
                }
                else
                {
                    result.Add(new ValidationResult
                    {
                        ExitCode = 0,
                        Message = "User Cache directory is valid.",
                        Status = ValidationStatus.Success,
                    });
                }

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
                        Status = ValidationStatus.Success,
                    });

                    return result;
                }
                else
                {
                    // The cache folder exists and may have contents that were put there by
                    // other users who should not have permissions to the cache folder.
                    // As a result, we need to ensure that we are clearing this directory before
                    // trying to use it.
                    // If we cannot delete the directory, we cannot allow Chocolatey to trust
                    // the files in this cache folder as they may contain corrupted or otherwise
                    // unsafe data.
                    this.Log().Debug(" - Folder lockdown = Failed.");

                    try
                    {
                        _fileSystem.DeleteDirectory(cacheFolderPath, recursive: true);
                        result.Add(new ValidationResult
                        {
                            ExitCode = 0,
                            Message = "The HTTP Cache was not correctly locked down, purging the cache before continuing.".SplitOnSpace(linePrefix: "   "),
                            Status = ValidationStatus.Warning,
                        });
                    }
                    catch (Exception deleteError)
                    {
                        if (config.UseHttpCache)
                        {
                            result.Add(new ValidationResult
                            {
                                ExitCode = 1,
                                Message = $"System Cache directory exists, but could not be locked down to administrators or deleted: {deleteError.Message.TrimEnd('.')}. Remove the '{cacheFolderPath}' directory and try again, or provide the '--ignore-http-cache' option to temporarily ignore this error.".SplitOnSpace(linePrefix: "   "),
                                Status = ValidationStatus.Error
                            });
                        }
                        else
                        {
                            result.Add(new ValidationResult
                            {
                                ExitCode = 0,
                                Message = $"System Cache directory exists, but could not be locked down to administrators or deleted: {deleteError.Message.TrimEnd('.')}.".SplitOnSpace(linePrefix: "   "),
                                Status = ValidationStatus.Warning
                            });
                        }

                        return result;
                    }
                }
            }
            else
            {
                this.Log().Debug(" - Folder Exists = Failed");
            }

            if (_fileSystem.LockDirectory(cacheFolderPath))
            {
                this.Log().Debug(" - Folder lockdown update = Success");

                result.Add(new ValidationResult
                {
                    ExitCode = 0,
                    Message = "System Cache directory successfully created and locked down to administrators.".SplitOnSpace(linePrefix: "   "),
                    Status = ValidationStatus.Success,
                });
            }
            else
            {
                this.Log().Debug(" - Folder lockdown update = Failed");

                result.Add(new ValidationResult
                {
                    ExitCode = 1,
                    Message = "System Cache directory was not created, or could not be locked down to administrators.".SplitOnSpace(linePrefix: "   "),
                    Status = ValidationStatus.Error
                });
            }

            return result;
        }

#pragma warning disable IDE0022, IDE1006

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public ICollection<ValidationResult> validate(ChocolateyConfiguration config)
        {
            return Validate(config);
        }

#pragma warning restore IDE0022, IDE1006
    }
}