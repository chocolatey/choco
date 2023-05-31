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
    using attributes;
    using commandline;
    using configuration;
    using infrastructure.commands;
    using logging;
    using services;

    [CommandFor("outdated", "retrieves information about packages that are outdated. Similar to upgrade all --noop")]
    public class ChocolateyOutdatedCommand : ICommand
    {
        private readonly IChocolateyPackageService _packageService;

        public ChocolateyOutdatedCommand(IChocolateyPackageService packageService)
        {
            _packageService = packageService;
        }

        public virtual void ConfigureArgumentParser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("s=|source=",
                     "Source - The source to find the package(s) to install. Special sources include: ruby, cygwin, windowsfeatures, and python. To specify more than one source, pass it with a semi-colon separating the values (e.g. \"'source1;source2'\"). Defaults to default feeds.",
                     option => configuration.Sources = option.UnquoteSafe())
                .Add("u=|user=",
                     "User - used with authenticated feeds. Defaults to empty.",
                     option => configuration.SourceCommand.Username = option.UnquoteSafe())
                .Add("p=|password=",
                     "Password - the user's password to the source. Defaults to empty.",
                     option => configuration.SourceCommand.Password = option.UnquoteSafe())
                .Add("cert=",
                     "Client certificate - PFX pathname for an x509 authenticated feeds. Defaults to empty.",
                     option => configuration.SourceCommand.Certificate = option.UnquoteSafe())
                .Add("cp=|certpassword=",
                     "Certificate Password - the client certificate's password to the source. Defaults to empty.",
                     option => configuration.SourceCommand.CertificatePassword = option.UnquoteSafe())
                .Add("pre|prerelease",
                    "Prerelease - Include Prereleases? Defaults to false.",
                    option => configuration.Prerelease = option != null)
                .Add("ignore-pinned",
                     "Ignore Pinned - Ignore pinned packages. Defaults to false.",
                     option => configuration.OutdatedCommand.IgnorePinned = option != null)
                .Add("ignore-unfound",
                    "Ignore Unfound Packages - Ignore packages that are not found on the sources used (or the defaults). Overrides the default feature '{0}' set to '{1}'.".FormatWith(ApplicationParameters.Features.IgnoreUnfoundPackagesOnUpgradeOutdated, configuration.Features.IgnoreUnfoundPackagesOnUpgradeOutdated.ToStringSafe()),
                    option => configuration.Features.IgnoreUnfoundPackagesOnUpgradeOutdated = option != null)
                .Add("disable-repository-optimizations|disable-package-repository-optimizations",
                    "Disable Package Repository Optimizations - Do not use optimizations for reducing bandwidth with repository queries during package install/upgrade/outdated operations. Should not generally be used, unless a repository needs to support older methods of query. When disabled, this makes queries similar to the way they were done in earlier versions of Chocolatey. Overrides the default feature '{0}' set to '{1}'.".FormatWith
                        (ApplicationParameters.Features.UsePackageRepositoryOptimizations, configuration.Features.UsePackageRepositoryOptimizations.ToStringSafe()),
                    option =>
                    {
                        if (option != null)
                        {
                            configuration.Features.UsePackageRepositoryOptimizations = false;
                        }
                    })
                ;
        }

        public virtual void ParseAdditionalArguments(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);
            configuration.PackageNames = string.Join(ApplicationParameters.PackageNamesSeparator.ToStringSafe(), unparsedArguments);
        }

        public virtual void Validate(ChocolateyConfiguration configuration)
        {
            if (!string.IsNullOrWhiteSpace(configuration.SourceCommand.Username) && string.IsNullOrWhiteSpace(configuration.SourceCommand.Password))
            {
                this.Log().Debug(ChocolateyLoggers.LogFileOnly, "Username '{0}' provided. Asking for password.".FormatWith(configuration.SourceCommand.Username));
                System.Console.Write("User name '{0}' provided. Password: ".FormatWith(configuration.SourceCommand.Username));
                configuration.SourceCommand.Password = InteractivePrompt.GetPassword(configuration.PromptForConfirmation);
            }
        }

        public virtual void HelpMessage(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Outdated Command");
            this.Log().Info(@"
Returns a list of outdated packages.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco outdated [<options/switches>]
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco outdated
    choco outdated -s https://somewhere/out/there
    choco outdated -s ""'https://somewhere/protected'"" -u user -p pass

If you use `--source=https://somewhere/out/there`, it is
 going to look for outdated packages only based on that source, so
 you may want to add `--ignore-unfound` to your options.

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
 - 0: no outdated packages
 - -1 or 1: an error has occurred
 - 2: outdated packages have been found

NOTE: If you have the feature '{0}' turned on,
 then choco will provide enhanced exit codes that allow
 better integration and scripting.

If you find other exit codes that we have not yet documented, please
 file a ticket so we can document it at
 https://github.com/chocolatey/choco/issues/new/choose.

".FormatWith(ApplicationParameters.Features.UseEnhancedExitCodes));

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "See It In Action");
            "chocolatey".Log().Info(@"
choco outdated: https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/choco_outdated.gif

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }

        public virtual void DryRun(ChocolateyConfiguration configuration)
        {
            _packageService.OutdatedDryRun(configuration);
        }

        public virtual void Run(ChocolateyConfiguration configuration)
        {
            _packageService.Outdated(configuration);
        }

        public virtual bool MayRequireAdminAccess()
        {
            return false;
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
            => ConfigureArgumentParser(optionSet, configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
            => ParseAdditionalArguments(unparsedArguments, configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_validation(ChocolateyConfiguration configuration)
            => Validate(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void help_message(ChocolateyConfiguration configuration)
            => HelpMessage(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void noop(ChocolateyConfiguration configuration)
            => DryRun(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void run(ChocolateyConfiguration configuration)
            => Run(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual bool may_require_admin_access()
            => MayRequireAdminAccess();
#pragma warning restore IDE1006
    }
}
