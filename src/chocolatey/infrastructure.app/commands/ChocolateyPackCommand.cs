// Copyright © 2017 - 2018 Chocolatey Software, Inc
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
                     option => configuration.OutputDirectory = option.remove_surrounding_quotes())
                ;
        }

        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            // First non-switch argument that is not a name=value pair will be treated as the nuspec file to pack.
            configuration.Input = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault(arg => !arg.StartsWith("-") && !arg.contains("="));

            foreach (var unparsedArgument in unparsedArguments.or_empty_list_if_null())
            {
                var property = unparsedArgument.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (property.Length == 2)
                {
                    var propName = property[0].trim_safe();
                    var propValue = property[1].trim_safe().remove_surrounding_quotes();

                    if (configuration.PackCommand.Properties.ContainsKey(propName))
                    {
                        this.Log().Warn(() => "A value for '{0}' has already been added with the value '{1}'. Ignoring {0}='{2}'.".format_with(propName, configuration.PackCommand.Properties[propName], propValue));
                    }
                    else
                    {
                        configuration.PackCommand.Properties.Add(propName, propValue);
                    }
                }
            }
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

NOTE: You can pass arbitrary property value pairs through to nuspecs.
 These will replace variables formatted as `$property$` with the value passed.

NOTE: `cpack` has been deprecated as it has a name collision with CMake. Please 
 use `choco pack` instead. The shortcut will be removed in v1.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco pack [<path to nuspec>] [<options/switches>] [<property=value>]
    cpack [<path to nuspec>] [<options/switches>] (DEPRECATED)
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco pack
    choco pack --version 1.2.3 configuration=release
    choco pack path/to/nuspec
    choco pack --outputdirectory build

NOTE: See scripting in the command reference (`choco -?`) for how to 
 write proper scripts and integrations.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Exit Codes");
            "chocolatey".Log().Info(@"
Exit codes that normally result from running this command.

Normal:
 - 0: operation was successful, no issues detected
 - -1 or 1: an error has occurred

If you find other exit codes that we have not yet documented, please 
 file a ticket so we can document it at 
 https://github.com/chocolatey/choco/issues/new/choose.

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
