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
    using System.Linq;
    using attributes;
    using commandline;
    using configuration;
    using infrastructure.commands;
    using logging;
    using results;
    using services;

    [CommandFor("list", "lists remote or local packages")]
    [CommandFor("search", "searches remote or local packages")]
    [CommandFor("find", "searches remote or local packages (alias for search)")]
    public class ChocolateyListCommand : IListCommand<PackageResult>
    {
        private readonly IChocolateyPackageService _packageService;

        public ChocolateyListCommand(IChocolateyPackageService packageService)
        {
            _packageService = packageService;
        }

        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            var localOnlyDescription = configuration.CommandName.is_equal_to("list")
                ? "LocalOnly - Only search against local machine items. Ignores --source if provided.. (DEPRECATION NOTICE: Will be removed and enabled by default in v2.0.0 and removed.)"
                : "LocalOnly - Only search against local machine items. Ignores --source if provided..";
            var deprecationNotice = configuration.CommandName.is_equal_to("list")
                ? " (DEPRECATION NOTICE: Will be removed for list command in v2.0.0)"
                : string.Empty;

            optionSet
                .Add("s=|source=",
                     "Source - Source location for install. Can use special 'webpi' or 'windowsfeatures' sources. Defaults to sources." + deprecationNotice,
                     option => configuration.Sources = option.remove_surrounding_quotes())
                .Add("l|lo|local|localonly|local-only",
                     localOnlyDescription,
                     option => configuration.ListCommand.LocalOnly = option != null)
                .Add("idonly|id-only",
                     "Id Only - Only return Package Ids in the list results. Available in 0.10.6+.",
                     option => configuration.ListCommand.IdOnly = option != null)
                .Add("pre|prerelease",
                     "Prerelease - Include Prereleases? Defaults to false.",
                     option => configuration.Prerelease = option != null)
                .Add("i|includeprograms|include-programs",
                     "IncludePrograms - Used in conjunction with LocalOnly, filters out apps chocolatey has listed as packages and includes those in the list. Defaults to false.",
                     option => configuration.ListCommand.IncludeRegistryPrograms = option != null)
                .Add("a|all|allversions|all-versions",
                     "AllVersions - include results from all versions.",
                     option => configuration.AllVersions = option != null)
                .Add("version=",
                     "Version - Specific version of a package to return.",
                     option => configuration.Version = option.remove_surrounding_quotes())
                .Add("u=|user=",
                     "User - used with authenticated feeds. Defaults to empty." + deprecationNotice,
                     option => configuration.SourceCommand.Username = option.remove_surrounding_quotes())
                .Add("p=|password=",
                     "Password - the user's password to the source. Defaults to empty." + deprecationNotice,
                     option => configuration.SourceCommand.Password = option.remove_surrounding_quotes())
                .Add("cert=",
                     "Client certificate - PFX pathname for an x509 authenticated feeds. Defaults to empty. Available in 0.9.10+." + deprecationNotice,
                     option => configuration.SourceCommand.Certificate = option.remove_surrounding_quotes())
                .Add("cp=|certpassword=",
                     "Certificate Password - the client certificate's password to the source. Defaults to empty. Available in 0.9.10+." + deprecationNotice,
                     option => configuration.SourceCommand.CertificatePassword = option.remove_surrounding_quotes())
                .Add("page=",
                     "Page - the 'page' of results to return. Defaults to return all results. Available in 0.9.10+.",
                     option =>
                         {
                             int page;
                             if (int.TryParse(option, out page))
                             {
                                 configuration.ListCommand.Page = page;
                             }
                             else
                             {
                                 configuration.ListCommand.Page = null;
                             }
                         })
                .Add("page-size=",
                     "Page Size - the amount of package results to return per page. Defaults to 25. Available in 0.9.10+.",
                     option => configuration.ListCommand.PageSize = int.Parse(option))
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
                 .Add("order-by-popularity",
                     "OrderByPopularity - Sort by package results by popularity. Available in 0.9.10+.",
                     option => configuration.ListCommand.OrderByPopularity = option != null)
                 .Add("approved-only",
                    "ApprovedOnly - Only return approved packages - this option will filter out results not from the community repository. Available in 0.9.10+." + deprecationNotice,
                     option => configuration.ListCommand.ApprovedOnly = option != null)
                 .Add("download-cache|download-cache-only",
                     "DownloadCacheAvailable - Only return packages that have a download cache available - this option will filter out results not from the community repository. Available in 0.9.10+." + deprecationNotice,
                     option => configuration.ListCommand.DownloadCacheAvailable = option != null)
                 .Add("not-broken",
                     "NotBroken - Only return packages that are not failing testing - this option only filters out failing results from the community feed. It will not filter against other sources. Available in 0.9.10+." + deprecationNotice,
                     option => configuration.ListCommand.NotBroken = option != null)
                  .Add("detail|detailed",
                     "Detailed - Alias for verbose. Available in 0.9.10+.",
                     option => configuration.Verbose = option != null)
                  .Add("disable-repository-optimizations|disable-package-repository-optimizations",
                    "Disable Package Repository Optimizations - Do not use optimizations for reducing bandwidth with repository queries during package install/upgrade/outdated operations. Should not generally be used, unless a repository needs to support older methods of query. When disabled, this makes queries similar to the way they were done in Chocolatey v0.10.11 and before. Overrides the default feature '{0}' set to '{1}'. Available in 0.10.14+.{2}".format_with
                        (ApplicationParameters.Features.UsePackageRepositoryOptimizations, configuration.Features.UsePackageRepositoryOptimizations.to_string(), deprecationNotice),
                    option =>
                    {
                        if (option != null)
                        {
                            configuration.Features.UsePackageRepositoryOptimizations = false;
                        }
                    })
                ;
        }

        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);
        }

        public virtual void handle_validation(ChocolateyConfiguration configuration)
        {
            if (!string.IsNullOrWhiteSpace(configuration.SourceCommand.Username) && string.IsNullOrWhiteSpace(configuration.SourceCommand.Password))
            {
                this.Log().Debug(ChocolateyLoggers.LogFileOnly, "Username '{0}' provided. Asking for password.".format_with(configuration.SourceCommand.Username));
                System.Console.Write("User name '{0}' provided. Password: ".format_with(configuration.SourceCommand.Username));
                configuration.SourceCommand.Password = InteractivePrompt.get_password(configuration.PromptForConfirmation);
            }
        }

        public virtual void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "List/Search Command");
            this.Log().Info(@"
Chocolatey will perform a search for a package local or remote.

NOTE: 100% compatible with older Chocolatey client (0.9.8.x and below)
 with options and switches. In most cases you can still pass options
 and switches  with one dash (`-`). For more details, see
 the command reference (`choco -?`).
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            // TODO: use command name in usage and examples, instead of hard
            // coding the names?
            "chocolatey".Log().Info(@"
    choco find <filter> [<options/switches>]
    choco list <filter> [<options/switches>]
    choco search <filter> [<options/switches>]
    clist <filter> [<options/switches>] (DEPRECATED, will be removed in v2.0.0)
");

            if (configuration.CommandName.is_equal_to("list"))
            {
                "chocolatey".Log().Warn(ChocolateyLoggers.Important, "DEPRECATION NOTICE");
                "chocolatey".Log().Warn(@"
Starting in v2.0.0 the list command will be made local only and will only
 work with the installed packages. All options available for connecting
 to sources will be removed and can only be used when using `search` or
 `find`.

To avoid breakage, change any calls made to remote sources to use `choco search`
 or `choco find` instead. These will continue to work as usual.

Starting in v2.0.0 the shortcut `clist` will be removed and can not be used
 to list package anymore. We recommend you make sure that you always
 use the full command going forward (`choco list`).
");
            }

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco list --local-only (DEPRECATED: will be default for list in v2.0.0)
    choco list -li
    choco list -lai
    choco list --page=0 --page-size=25
    choco search git
    choco search git --source=""'https://somewhere/out/there'""
    choco search bob -s ""'https://somewhere/protected'"" -u user -p pass

NOTE: See scripting in the command reference (`choco -?`) for how to
 write proper scripts and integrations.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Exit Codes");
            "chocolatey".Log().Info(@"
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

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "See It In Action");
            "chocolatey".Log().Info(@"
choco {0}: https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/choco_search.gif

".format_with(configuration.CommandName));
            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Alternative Sources");

            if (configuration.CommandName.is_equal_to("list"))
            {
                "chocolatey".Log().Warn(@"
Will be removed for the list command in v2.0.0.");
            }

            "chocolatey".Log().Info(@"
Available in 0.9.10+.

WebPI
This specifies the source is Web PI (Web Platform Installer) and that
 we are searching for a WebPI product, such as IISExpress. If you do
 not have the Web PI command line installed, it will install that first
 and then perform the search requested.
 e.g. `choco {0} --source webpi`

Windows Features
This specifies that the source is a Windows Feature and we should
 install via the Deployment Image Servicing and Management tool (DISM)
 on the local machine.
 e.g. `choco {0} --source windowsfeatures`
".format_with(configuration.CommandName));

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }

        public virtual void noop(ChocolateyConfiguration configuration)
        {
            log_deprecation_warning(configuration);

            _packageService.list_noop(configuration);
        }

        public virtual void run(ChocolateyConfiguration configuration)
        {
            log_deprecation_warning(configuration);

            _packageService.ensure_source_app_installed(configuration);
            // note: you must leave the .ToList() here or else the method won't be evaluated!
            var packageResults = _packageService.list_run(configuration).ToList();

            // if there are no results, exit with a 2.
            if (configuration.Features.UseEnhancedExitCodes && packageResults.Count == 0 && Environment.ExitCode == 0)
            {
                Environment.ExitCode = 2;
            }
        }

        public virtual IEnumerable<PackageResult> list(ChocolateyConfiguration configuration)
        {
            configuration.QuietOutput = true;
            // here it's up to the caller to enumerate the results
            return _packageService.list_run(configuration);
        }

        public virtual int count(ChocolateyConfiguration config)
        {
            config.QuietOutput = true;
            return _packageService.count_run(config);
        }

        public virtual bool may_require_admin_access()
        {
            return false;
        }

        // Marked as obsolete on purpose so we remember to remove
        // this method when we make list local only, currently this
        // is planned for v2.0.0, with #158.
        [Obsolete("Remove once list is made local only!")]
        private void log_deprecation_warning(ChocolateyConfiguration configuration)
        {
            if (configuration.CommandName.is_equal_to("list") && !configuration.ListCommand.LocalOnly)
            {
                var logger = ChocolateyLoggers.LogFileOnly;

                if (configuration.RegularOutput)
                {
                    logger = ChocolateyLoggers.Normal;
                }

                this.Log().Warn(logger, @"Using the list command with remote sources is deprecated and will be made
to only list locally installed packages in v2.0.0. Use the search, or find,
command to find packages on remote sources (such as the Chocolatey Community
Repository).");
            }
        }
    }
}
