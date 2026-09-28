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

using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.commands;
using chocolatey.infrastructure.logging;
using chocolatey.infrastructure.validations;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace chocolatey.infrastructure.app.validations
{
    internal static class PreRunValidationChecks
    {
        /// <summary>
        /// Retrieve the full list of validation results from any secondary validations.
        /// </summary>
        /// <param name="config">The current <see cref="ChocolateyConfiguration"/>.</param>
        /// <param name="container">The currently in-use <see cref="Container"/>.</param>
        /// <returns>A list of validation results that should be inspected for any secondary failures, to be handled by the caller.</returns>
        /// <exception cref="ApplicationException">Thrown if the core command and config validation fails.</exception>
        internal static IList<ValidationResult> GetValidationResults(ChocolateyConfiguration config, Container container)
        {
            "chocolatey".Log().Debug(() => "Performing validation checks...");
            var validationResults = new List<ValidationResult>();
            var validationChecks = container.GetAllInstances<IValidation>();

            foreach (var validationCheck in validationChecks)
            {
                validationResults.AddRange(validationCheck.Validate(config));
            }

            return validationResults;
        }

        /// <summary>
        /// Validate the current state of the application, and its configuration and environment.
        /// Throws if there are validation failures.
        /// </summary>
        /// <param name="command">The command being run.</param>
        /// <param name="config">The current configuration.</param>
        /// <param name="container">The IOC container to retrieve validation rules from.</param>
        /// <exception cref="ApplicationException">Thrown on validation failure.</exception>
        internal static void Validate(
            ICommand command,
            ChocolateyConfiguration config,
            Container container)
        {
            // First validate the command under the current configuration.
            // If this fails, it will throw an exception for the caller.
            command.Validate(config);

            // Perform secondary validations and report the results to the console or log file.
            // Validation errors will generate an exception that is thrown for the caller.
            var validationResults = GetValidationResults(config, container);

            var successes = validationResults.Count(v => v.Status == ValidationStatus.Success);
            var warnings = validationResults.Count(v => v.Status == ValidationStatus.Warning);
            var errors = validationResults.Where(v => v.Status == ValidationStatus.Error);
            var errorCount = errors.Count();

            var logOnWarnings = config.Features.LogValidationResultsOnWarnings;
            if (config.RegularOutput)
            {
                "chocolatey".Log().Info(errorCount + (logOnWarnings ? warnings : 0) == 0 ? ChocolateyLoggers.LogFileOnly : ChocolateyLoggers.Important, () => "{0} validations performed. {1} success(es), {2} warning(s), and {3} error(s).".FormatWith(
                    validationResults.Count,
                    successes,
                    warnings,
                    errorCount));

                if (warnings != 0)
                {
                    var warningLogger = logOnWarnings ? ChocolateyLoggers.Normal : ChocolateyLoggers.LogFileOnly;
                    "chocolatey".Log().Info(warningLogger, "");
                    "chocolatey".Log().Warn(warningLogger, "Validation Warnings:");
                    foreach (var warning in validationResults.Where(p => p.Status == ValidationStatus.Warning).OrEmpty())
                    {
                        "chocolatey".Log().Warn(warningLogger, " - {0}".FormatWith(warning.Message));
                    }
                }
            }

            if (errors.Any())
            {
                "chocolatey".Log().Info("");
                var errorMessage = new StringBuilder("Validation Errors:").AppendLine();

                foreach (var error in errors.OrEmpty())
                {
                    errorMessage.AppendLine(" - {0}".FormatWith(error.Message));
                }

                throw new ApplicationException(errorMessage.ToString());
            }
        }
    }
}
