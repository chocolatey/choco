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
    using logging;
    using services;

    [CommandFor("info", "retrieves package information. Shorthand for choco search pkgname --exact --verbose")]
    public class ChocolateyInfoCommand : ChocolateySearchCommand
    {
        public ChocolateyInfoCommand(IChocolateyPackageService packageService)
            : base(packageService)
        {
        }

        public override void ConfigureArgumentParser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add(
                    "s=|source=",
                    "Source - Source location for install. Can use special 'windowsfeatures', 'ruby', 'cygwin', or 'python' sources. Defaults to configured sources.",
                    option => configuration.Sources = option.UnquoteSafe())
                .Add(
                    "l|lo|localonly|local-only",
                    "LocalOnly - Only search against local machine items.",
                    option => configuration.ListCommand.LocalOnly = option != null)
                .Add("version=",
                     "Version - Specific version of a package to return.",
                     option => configuration.Version = option.UnquoteSafe())
                .Add(
                    "pre|prerelease",
                    "Prerelease - Include Prereleases? Defaults to false.",
                    option => configuration.Prerelease = option != null)
                .Add(
                    "u=|user=",
                    "User - used with authenticated feeds. Defaults to empty.",
                    option => configuration.SourceCommand.Username = option.UnquoteSafe())
                .Add(
                    "p=|password=",
                    "Password - the user's password to the source. Defaults to empty.",
                    option => configuration.SourceCommand.Password = option.UnquoteSafe())
                .Add("cert=",
                     "Client certificate - PFX pathname for an x509 authenticated feeds. Defaults to empty.",
                     option => configuration.SourceCommand.Certificate = option.UnquoteSafe())
                .Add("cp=|certpassword=",
                     "Certificate Password - the client certificate's password to the source. Defaults to empty.",
                     option => configuration.SourceCommand.CertificatePassword = option.UnquoteSafe())
                .Add("disable-repository-optimizations|disable-package-repository-optimizations",
                    "Disable Package Repository Optimizations - Do not use optimizations for reducing bandwidth with repository queries during package install/upgrade/outdated operations. Should not generally be used, unless a repository needs to support older methods of query. When used, this makes queries similar to the way they were done in earlier versions of Chocolatey. Overrides the default feature '{0}' set to '{1}'.".FormatWith
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

        public override void ParseAdditionalArguments(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);
            configuration.Verbose = true;
            configuration.ListCommand.Exact = true;
        }

        public override void Validate(ChocolateyConfiguration configuration)
        {
            base.Validate(configuration);

            if (string.IsNullOrWhiteSpace(configuration.Input))
            {
                throw new ApplicationException("A single package name is required to run the choco info command.");
            }

            // Validate only a single package has been passed to info
            // TODO: Provide ability to get info on a list of packages. See https://github.com/chocolatey/choco/issues/2905
            var input = configuration.Input.Split(' ');
            if (input.Length > 1)
            {
                throw new ApplicationException("Only a single package name can be passed to the choco info command.");
            }
        }

        public override void HelpMessage(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Info Command");
            this.Log().Info(@"
Chocolatey will perform a search for a package local or remote and provide
 detailed information about that package. This is a synonym for
 `choco search <pkgname> --exact --detailed`.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco info [<options/switches>]
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco info chocolatey
    choco info googlechrome
    choco info powershell

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

NOTE: If you have the feature '{0}' turned on,
 then choco will provide enhanced exit codes that allow
 better integration and scripting.

If you find other exit codes that we have not yet documented, please
 file a ticket so we can document it at
 https://github.com/chocolatey/choco/issues/new/choose.

".FormatWith(ApplicationParameters.Features.UseEnhancedExitCodes));
            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }
    }
}
