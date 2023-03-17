// Copyright © 2023-Present Chocolatey Software, Inc
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
    using System.Linq;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commandline;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.logging;
    using chocolatey.infrastructure.results;

    [CommandFor("list", "lists local packages")]
    public class ChocolateyListCommand : IListCommand<PackageResult>
    {
        private readonly IChocolateyPackageService _packageService;

        [Obsolete("Remove unsupported argument in V3!")]
        private readonly string[] _unsupportedArguments = new[]
        {
            "-l",
            "-lo",
            "--local",
            "--localonly",
            "--local-only",
            "-a",
            "--all",
            "--allversions",
            "--all-versions",
            "--order-by-popularity"
        };

        public ChocolateyListCommand(IChocolateyPackageService packageService)
        {
            _packageService = packageService ?? throw new ArgumentNullException(nameof(packageService));
        }

        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("idonly|id-only",
                     "Id Only - Only return Package Ids in the list results. Available in 0.10.6+.",
                     option => configuration.ListCommand.IdOnly = option != null)
                .Add("pre|prerelease",
                     "Prerelease - Include Prereleases? Defaults to false.",
                     option => configuration.Prerelease = option != null)
                .Add("i|includeprograms|include-programs",
                     "IncludePrograms - Used in conjunction with LocalOnly, filters out apps chocolatey has listed as packages and includes those in the list. Defaults to false.",
                     option => configuration.ListCommand.IncludeRegistryPrograms = option != null)
                .Add("version=",
                     "Version - Specific version of a package to return.",
                     option => configuration.Version = option.remove_surrounding_quotes())
                .Add("page=",
                     "Page - the 'page' of results to return. Defaults to return all results. Available in 0.9.10+.",
                     option =>
                     {
                         if (int.TryParse(option, out var page))
                         {
                             configuration.ListCommand.Page = page;
                         }
                         else
                         {
                             configuration.ListCommand.Page = null;
                         }
                     })
                .Add("page-size=", // Does it make sense to have paging on local packages?
                     "Page Size - the amount of package results to return per page. Defaults to 25. Available in 0.9.10+.",
                     option =>
                     {
                         configuration.ListCommand.PageSize = int.Parse(option);
                         configuration.ListCommand.ExplicitPageSize = true;
                     })
                .Add("e|exact",
                     "Exact - Only return packages with this exact name. Available in 0.9.10+.",
                     option => configuration.ListCommand.Exact = option != null)
                .Add("by-id-only",
                     "ByIdOnly - Only return packages where the id contains the search filter. Available in 0.9.10+.",
                     option => configuration.ListCommand.ByIdOnly = option != null)
                 .Add("by-tag-only|by-tags-only",
                     "ByTagOnly - Only return packages where the search filter matches on the tags. Available in 0.10.6+.",
                     option => configuration.ListCommand.ByTagOnly = option != null)
                 .Add("id-starts-with",
                     "IdStartsWith - Only return packages where the id starts with the search filter. Available in 0.9.10+.",
                     option => configuration.ListCommand.IdStartsWith = option != null)
                 .Add("detail|detailed",
                     "Detailed - Alias for verbose. Available in 0.9.10+.",
                     option => configuration.Verbose = option != null);
        }

        public virtual int count(ChocolateyConfiguration config)
        {
            config.ListCommand.LocalOnly = true;
            config.QuietOutput = true;

            return _packageService.count_run(config);
        }

        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            var argumentsWithoutLocalOnly = new List<string>(unparsedArguments.Count);

            foreach (var argument in unparsedArguments)
            {
                if (_unsupportedArguments.Contains(argument, StringComparer.OrdinalIgnoreCase))
                {
                    this.Log().Warn(ChocolateyLoggers.Important, @"
UNSUPPORTED ARGUMENT: Ignoring the argument {0}. This argument is unsupported for locally installed packages, and will be treated as a package name in Chocolatey CLI v3!", argument);
                }
                else
                {
                    argumentsWithoutLocalOnly.Add(argument);
                }
            }

            configuration.Input = string.Join(" ", argumentsWithoutLocalOnly);
        }

        public virtual void handle_validation(ChocolateyConfiguration configuration)
        {
            // There is nothing to validate.
        }

        public virtual void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "List Command");
            this.Log().Info(string.Empty);

            this.Log().Info(ChocolateyLoggers.Important, "Usage");
            this.Log().Info(@"
    choco {0} <filter> [<options/switches>]
".format_with(configuration.CommandName));

            this.Log().Info(ChocolateyLoggers.Important, "Examples");
            this.Log().Info(@"
    choco {0} --local-only
    choco {0} --local-only --include-programs

NOTE: See scripting in the command reference (`choco -?`) for how to
 write proper scripts and integrations.

".format_with(configuration.CommandName));

            this.Log().Info(ChocolateyLoggers.Important, "Exit Codes");
            this.Log().Info(@"
Exit codes that normally result from running this command.

Normal:
 - 0: operation was successful, no issues detected
 - -1 or 1: an error has occurred

Enhanced:
 - 0: operation was successful, no issues detected
 - -1 or 1: an error has occurred
 - 2: no results (enhanced)

NOTE: Starting in v0.10.12, if you have the feature '{0}'
 turned on, then choco will provide enhanced exit codes that allow
 better integration and scripting.

If you find other exit codes that we have not yet documented, please
 file a ticket so we can document it at
 https://github.com/chocolatey/choco/issues/new/choose.

".format_with(ApplicationParameters.Features.UseEnhancedExitCodes));

            this.Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }

        public virtual IEnumerable<PackageResult> list(ChocolateyConfiguration config)
        {
            config.ListCommand.LocalOnly = true;
            config.QuietOutput = true;

            return _packageService.list_run(config);
        }

        public virtual bool may_require_admin_access()
        {
            return false;
        }

        public virtual void noop(ChocolateyConfiguration configuration)
        {
            configuration.ListCommand.LocalOnly = true;

            _packageService.list_noop(configuration);
        }

        public virtual void run(ChocolateyConfiguration config)
        {
            config.ListCommand.LocalOnly = true;

            // note: you must leave the .ToList() here or else the method won't be evaluated!
            var packageResults = _packageService.list_run(config).ToList();

            // if there are no results, exit with a 2 if enhanced exit codes is enabled.
            if (config.Features.UseEnhancedExitCodes && packageResults.Count == 0 && Environment.ExitCode == 0)
            {
                Environment.ExitCode = 2;
            }
        }
    }
}
