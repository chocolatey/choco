﻿// Copyright © 2017 - 2025 Chocolatey Software, Inc
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
using chocolatey.infrastructure.commandline;

namespace chocolatey.infrastructure.commands
{
    /// <summary>
    ///   Commands that can be configured and run
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        ///   Configure the argument parser.
        /// </summary>
        /// <param name="optionSet">The option set.</param>
        /// <param name="configuration">The configuration.</param>
        void ConfigureArgumentParser(OptionSet optionSet, ChocolateyConfiguration configuration);

        /// <summary>
        ///   Handle the arguments that were not parsed by the argument parser and/or do additional parsing work
        /// </summary>
        /// <param name="unparsedArguments">The unparsed arguments.</param>
        /// <param name="configuration">The configuration.</param>
        void ParseAdditionalArguments(IList<string> unparsedArguments, ChocolateyConfiguration configuration);

        void Validate(ChocolateyConfiguration configuration);

        /// <summary>
        ///   The specific help message for a particular command.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        void HelpMessage(ChocolateyConfiguration configuration);

        /// <summary>
        ///   Runs in no op mode, which means it doesn't actually make any changes.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        void DryRun(ChocolateyConfiguration configuration);

        /// <summary>
        ///   Runs the command.
        /// </summary>
        /// <param name="config">The configuration.</param>
        void Run(ChocolateyConfiguration config);

        /// <summary>
        ///   This command may require admin rights
        /// </summary>
        /// <returns></returns>
        bool MayRequireAdminAccess();

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void handle_validation(ChocolateyConfiguration configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void help_message(ChocolateyConfiguration configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void noop(ChocolateyConfiguration configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void run(ChocolateyConfiguration config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        bool may_require_admin_access();
#pragma warning restore IDE0022, IDE1006
    }
}
