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
    using System;
    using System.Collections.Generic;
    using commandline;
    using configuration;
    using services;

    public class ChocolateyUpdateCommand : ChocolateyUpgradeCommand
    {
        public ChocolateyUpdateCommand(IChocolateyPackageService packageService) : base(packageService)
        {
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            throw_exception();
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            throw_exception();
        }

        public void handle_validation(ChocolateyConfiguration configuration)
        {
            throw_exception();
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            throw_exception();
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            throw_exception();
        }

        public void run(ChocolateyConfiguration config)
        {
            throw_exception();
        }

        public bool may_require_admin_access()
        {
            return false;
        }

        private void throw_exception()
        {
            throw new Exception(@"Could not find a command registered that meets 'update'.
 Try choco -? for command reference/help.");
        }
    }
}
