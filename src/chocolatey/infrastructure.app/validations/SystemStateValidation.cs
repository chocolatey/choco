// Copyright © 2017 - 2021 Chocolatey Software, Inc
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
    using configuration;
    using infrastructure.validations;
    using services;

    /// <summary>
    ///   Performs validation against the current System State.  This
    ///   includes things like pending reboot requirement.  Any errors
    ///   that are returned will halt the current operation.
    /// </summary>
    public class SystemStateValidation : IValidation
    {
        private readonly IPendingRebootService _pendingRebootService;

        public SystemStateValidation(IPendingRebootService pendingRebootService)
        {
            _pendingRebootService = pendingRebootService;
        }

        public ICollection<ValidationResult> Validate(ChocolateyConfiguration config)
        {
            this.Log().Debug("System State Validation Checks:");
            var validationResults = new List<ValidationResult>();

            ValidateSystemPendingReboot(config, validationResults);

            if (validationResults.Count == 0)
            {
                validationResults.Add(new ValidationResult
                {
                    Message = "System State is valid",
                    Status = ValidationStatus.Success,
                    ExitCode = 0
                });
            }

            return validationResults;
        }

        private void ValidateSystemPendingReboot(ChocolateyConfiguration config, ICollection<ValidationResult> validationResults)
        {
            var result = _pendingRebootService.IsRebootPending(config);

            if (result)
            {
                var commandsToErrorOn = new List<string> {"install", "uninstall", "upgrade"};

                if (!commandsToErrorOn.Contains(config.CommandName.ToLowerInvariant()))
                {
                    validationResults.Add(new ValidationResult
                    {
                        Message = @"A pending system reboot request has been detected, however, this is
   being ignored due to the current command being used '{0}'.
   It is recommended that you reboot at your earliest convenience.
".FormatWith(config.CommandName),
                        Status = ValidationStatus.Warning,
                        ExitCode = 0
                    });
                }
                else if (!config.Features.ExitOnRebootDetected)
                {
                    validationResults.Add(new ValidationResult
                    {
                        Message = @"A pending system reboot request has been detected, however, this is
   being ignored due to the current Chocolatey configuration.  If you
   want to halt when this occurs, then either set the global feature
   using:
     choco feature enable --name=""{0}""
   or pass the option --exit-when-reboot-detected.
".FormatWith(ApplicationParameters.Features.ExitOnRebootDetected),
                        Status = ValidationStatus.Warning,
                        ExitCode = 0
                    });
                }
                else
                {
                    Environment.ExitCode = ApplicationParameters.ExitCodes.ErrorFailNoActionReboot;

                    validationResults.Add(new ValidationResult
                    {
                        Message = "A pending system reboot has been detected (exit code {0}).".FormatWith(ApplicationParameters.ExitCodes.ErrorFailNoActionReboot),
                        Status = ValidationStatus.Error,
                        ExitCode = ApplicationParameters.ExitCodes.ErrorFailNoActionReboot
                    });
                }
            }
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public ICollection<ValidationResult> validate(ChocolateyConfiguration config)
            => Validate(config);
#pragma warning restore IDE1006
    }
}
