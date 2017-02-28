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
    using infrastructure.commands;
    using logging;
    using services;

    [CommandFor("pack", "packages up a nuspec to a compiled nupkg")]
    public class ChocolateyPackCommand : ICommand
    {
        private readonly IChocolateyPackageService _packageService;

        public ChocolateyPackCommand(IChocolateyPackageService packageService)
        {
            _packageService = packageService;
        }

        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("version=",
                     "Version - The version you would like to insert into the package.",
                     option => configuration.Version = option.remove_surrounding_quotes())
                .Add("out=|outdir=|outputdirectory=|output-directory=",
                     "OutputDirectory - Specifies the directory for the created Chocolatey package file. If not specified, uses the current directory.",
                     option => configuration.OutputDirectory = option)
                ;
        }

        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);
        }

        public virtual void handle_validation(ChocolateyConfiguration configuration)
        {
        }

        public virtual void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Pack Command");
            this.Log().Info(@"
Chocolatey will attempt to package a nuspec into a compiled nupkg. Some
 may prefer to use `cpack` as a shortcut for `choco pack`.

NOTE: 100% compatible with older chocolatey client (0.9.8.32 and below)
 with options and switches. In most cases you can still pass options 
 and switches with one dash (`-`). For more details, see 
 the command reference (`choco -?`).

NOTE: `cpack` has been deprecated as it has a name collision with CMake. Please 
 use `choco pack` instead. The shortcut will be removed in v1.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco pack [<path to nuspec>] [<options/switches>]
    cpack [<path to nuspec>] [<options/switches>] (DEPRECATED)
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco pack
    choco pack --version 1.2.3
    choco pack path/to/nuspec
    choco pack --outputdirectory build

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }

        public virtual void noop(ChocolateyConfiguration configuration)
        {
            _packageService.pack_noop(configuration);
        }

        public virtual void run(ChocolateyConfiguration configuration)
        {
            _packageService.pack_run(configuration);
        }

        public virtual bool may_require_admin_access()
        {
            return false;
        }
    }
}
