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

namespace chocolatey.infrastructure.app.commands
{
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.commandline;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.licensing;
    using chocolatey.infrastructure.logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    [CommandFor("license", "Retrieve or modify the Chocolatey License")]
    public class ChocolateyLicenseCommand : ICommand
    {
        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            // We don't currently expect to have any arguments
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            // We don't currently expect to have any additional arguments
        }

        public void handle_validation(ChocolateyConfiguration configuration)
        {
            // We don't currently accept any arguments, so there is no validation
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "License Command");
            this.Log().Info(@"
Chocolatey will do license things.
");
        }

        public bool may_require_admin_access()
        {
            return false;
        }

        public void noop(ChocolateyConfiguration configuration)
        {
        }

        public void run(ChocolateyConfiguration config)
        {
            var ourLicense = LicenseValidation.validate();
            var nodeCount = parse_node_count(ourLicense.Name);
            var logger = config.RegularOutput ? ChocolateyLoggers.Normal : ChocolateyLoggers.LogFileOnly;

            if (ourLicense.LicenseType == ChocolateyLicenseType.Foss)
            {
                this.Log().Warn(logger, "No Commercial License Found, running with Open Source License.");
                return;
            }

            if (!ourLicense.IsValid)
            {
                this.Log().Warn(logger, "We have found an invalid license for Chocolatey {0}: {1}", ourLicense.LicenseType, ourLicense.InvalidReason);
                Environment.ExitCode = 1;
            }

            if (config.RegularOutput)
            {
                this.Log().Info("Registered to:   {0}".format_with(ourLicense.Name));
                this.Log().Info("Expiration Date: {0} UTC".format_with(ourLicense.ExpirationDate));
                this.Log().Info("License type:    {0}".format_with(ourLicense.LicenseType));
                this.Log().Info("Node Count:      {0}".format_with(nodeCount));
            }
            else
            {
                // Headers: Name, LicenseType, ExpirationDate, NodeCount
                this.Log().Info("{0}|{1}|{2}|{3}".format_with(ourLicense.Name, ourLicense.LicenseType, ourLicense.ExpirationDate, nodeCount));
            }
        }

        private int? parse_node_count(string licenseName)
        {
            if (string.IsNullOrWhiteSpace(licenseName))
            {
                return null;
            }

            // Starting from the beginning of the license name
            var startIndex = 0;
            // while the start index is less than the length of the string
            // and there is a '[' in the name
            while (startIndex < licenseName.Length && (startIndex = licenseName.IndexOf('[', startIndex)) > -1)
            {
                var endIndex = 1;
                // increment endIndex (which is actually a relative position) while the next digit is still a digit
                while (startIndex + endIndex < licenseName.Length && char.IsDigit(licenseName[startIndex + endIndex]))
                {
                    endIndex++;
                }

                // If the length of the string is greater or equal than the length of the name, stop!
                if (licenseName.Length <= startIndex + endIndex)
                {
                    break;
                }

                // If next character is ']', return the content within the square braces
                if (licenseName[startIndex + endIndex] == ']')
                {
                    return int.Parse(licenseName.Substring(startIndex + 1, endIndex - 1));
                }

                // If we get this far, start testing at the end of the current range
                startIndex = startIndex + endIndex;
            }

            return null;

        }
    }
}
