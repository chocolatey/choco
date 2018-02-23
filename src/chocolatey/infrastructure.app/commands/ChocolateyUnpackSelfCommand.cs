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
    using System.ComponentModel;
    using adapters;
    using attributes;
    using commandline;
    using configuration;
    using extractors;
    using filesystem;
    using infrastructure.commands;
    using logging;
#if !NoResources
    using resources;
#endif

    [CommandFor("unpackself", "have chocolatey set itself up")]
    public class ChocolateyUnpackSelfCommand : ICommand
    {
        private readonly IFileSystem _fileSystem;

#if !NoResources
        private Lazy<IAssembly> _assemblyInitializer = new Lazy<IAssembly>(() => Assembly.GetAssembly(typeof (ChocolateyResourcesAssembly)));
#else
        private Lazy<IAssembly> _assemblyInitializer = new Lazy<IAssembly>();
#endif

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void initialize_with(Lazy<IAssembly> assembly_initializer)
        {
            _assemblyInitializer = assembly_initializer;
        }

        private IAssembly assembly
        {
            get { return _assemblyInitializer.Value; }
        }

        public ChocolateyUnpackSelfCommand(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
        }

        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
        }

        public virtual void handle_validation(ChocolateyConfiguration configuration)
        {
        }

        public virtual void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "UnpackSelf Command");
            this.Log().Info(@"
This will unpack files needed by choco. It will overwrite existing
 files only if --force is specified.

NOTE: This command should only be used when installing Chocolatey, not
 during normal operation.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }

        public virtual void noop(ChocolateyConfiguration configuration)
        {
            this.Log().Info("This would have unpacked {0} for use relative where the executable is, based on resources embedded in {0}.".format_with(ApplicationParameters.Name));
        }

        public virtual void run(ChocolateyConfiguration configuration)
        {
            this.Log().Info("{0} is unpacking required files for use. Overwriting? {1}".format_with(ApplicationParameters.Name, configuration.Force));
            // refactor - thank goodness this is temporary, cuz manifest resource streams are dumb

            // unpack the manifest file as well
            AssemblyFileExtractor.extract_all_resources_to_relative_directory(_fileSystem, Assembly.GetAssembly(typeof(ChocolateyUnpackSelfCommand)), _fileSystem.get_directory_name(_fileSystem.get_current_assembly_path()), new List<string>(), "chocolatey.console");

            IList<string> folders = new List<string>
                {
                    "helpers",
                    "functions",
                    "redirects",
                    "tools"
                };

            AssemblyFileExtractor.extract_all_resources_to_relative_directory(
                _fileSystem,
                assembly,
                 _fileSystem.get_directory_name(_fileSystem.get_current_assembly_path()),
                folders,
                ApplicationParameters.ChocolateyFileResources,
                overwriteExisting: configuration.Force,
                logOutput: true);
        }

        public virtual bool may_require_admin_access()
        {
            return true;
        }
    }
}
