// Copyright © 2011 - Present RealDimensions Software, LLC
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
    using System.Collections.Generic;
    using attributes;
    using commandline;
    using configuration;
    using logging;
    using services;

    [CommandFor("info", "retrieves package information. Shorthand for choco search pkgname --exact --verbose")]
    public class ChocolateyInfoCommand : ChocolateyListCommand
    {
        public ChocolateyInfoCommand(IChocolateyPackageService packageService)
            : base(packageService)
        {
        }

        public override void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add(
                    "s=|source=",
                    "Source - Source location for install. Can include special 'webpi'. Defaults to sources.",
                    option => configuration.Sources = option.remove_surrounding_quotes())
                .Add(
                    "l|lo|localonly|local-only",
                    "LocalOnly - Only search against local machine items.",
                    option => configuration.ListCommand.LocalOnly = option != null)
                .Add("version=",
                     "Version - Specific version of a package to return.",
                     option => configuration.Version = option.remove_surrounding_quotes())
                .Add(
                    "pre|prerelease",
                    "Prerelease - Include Prereleases? Defaults to false.",
                    option => configuration.Prerelease = option != null)
                .Add(
                    "u=|user=",
                    "User - used with authenticated feeds. Defaults to empty.",
                    option => configuration.SourceCommand.Username = option.remove_surrounding_quotes())
                .Add(
                    "p=|password=",
                    "Password - the user's password to the source. Defaults to empty.",
                    option => configuration.SourceCommand.Password = option.remove_surrounding_quotes())
                .Add("cert=",
                     "Client certificate - PFX pathname for an x509 authenticated feeds. Defaults to empty. Available in 0.9.10+.",
                     option => configuration.SourceCommand.Certificate = option.remove_surrounding_quotes())
                .Add("cp=|certpassword=",
                     "Certificate Password - the client certificate's password to the source. Defaults to empty. Available in 0.9.10+.",
                     option => configuration.SourceCommand.CertificatePassword = option.remove_surrounding_quotes())
                ;
        }

        public override void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);
            configuration.Verbose = true;
            configuration.ListCommand.Exact = true;
        }

        public override void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Info Command");
            this.Log().Info(@"
Chocolatey will perform a search for a package local or remote and provide 
 detailed information about that package. This is a synonym for 
 `choco search <pkgname> --exact --detailed`.

NOTE: New as of 0.9.10.0.
");
            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }
    }
}
