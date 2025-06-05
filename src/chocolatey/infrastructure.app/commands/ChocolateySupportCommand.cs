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
using chocolatey.infrastructure.app.attributes;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.commandline;
using chocolatey.infrastructure.commands;
using chocolatey.infrastructure.logging;

namespace chocolatey.infrastructure.app.commands
{
    [CommandFor("support", "provides support information", Version = "2.5.0")]
    public class ChocolateySupportCommand : ICommand
    {
        public void ConfigureArgumentParser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            // Intentionally left blank
        }

        public void ParseAdditionalArguments(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            // Intentionally left blank
        }

        public void Validate(ChocolateyConfiguration configuration)
        {
            // Intentionally left blank
        }

        public void HelpMessage(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Support Command");
            this.Log().Info(@"
As a user of Chocolatey CLI open-source, we are unable to
 provide private support. See https://chocolatey.org/support
 for details.
");
        }

        public void DryRun(ChocolateyConfiguration configuration)
        {
            Run(configuration);
        }

        public void Run(ChocolateyConfiguration config)
        {
            var isLicensed = config.Information.IsLicensedVersion;

            if (config.Information.IsLicensedVersion)
            {
                this.Log().Warn(@"
As a licensed customer, you can access our Support Team. However,
 it looks like the Chocolatey Licensed Extension package is not
 currently installed. Please run
 `choco install chocolatey.extension` and run `choco support`
 again.
");
            }
            else
            {
                this.Log().Info(@"
Unfortunately, we are unable to provide private support for
 open-source users. However, there is community assistance
 available. Please visit: 
 https://chocolatey.org/support for support options, or
 https://docs.chocolatey.org for our open-source documentation.
");
            }
        }

        public bool MayRequireAdminAccess()
        {
            return false;
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            ConfigureArgumentParser(optionSet, configuration);
        }

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            ParseAdditionalArguments(unparsedArguments, configuration);
        }

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_validation(ChocolateyConfiguration configuration)
        {
            Validate(configuration);
        }

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void help_message(ChocolateyConfiguration configuration)
        {
            HelpMessage(configuration);
        }

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void noop(ChocolateyConfiguration configuration)
        {
            DryRun(configuration);
        }

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void run(ChocolateyConfiguration configuration)
        {
            Run(configuration);
        }

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual bool may_require_admin_access()
        {
            return MayRequireAdminAccess();
        }
#pragma warning restore IDE1006
    }
}
