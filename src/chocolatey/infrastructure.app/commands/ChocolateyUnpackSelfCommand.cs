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

    [CommandFor("unpackself", "re-installs Chocolatey base files")]
    public class ChocolateyUnpackSelfCommand : ICommand
    {
        private readonly IFileSystem _fileSystem;

#if !NoResources
        private Lazy<IAssembly> _assemblyInitializer = new Lazy<IAssembly>(() => adapters.Assembly.GetAssembly(typeof (ChocolateyResourcesAssembly)));
#else
        private Lazy<IAssembly> _assemblyInitializer = new Lazy<IAssembly>();
#endif

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void InitializeWith(Lazy<IAssembly> assembly_initializer)
        {
            _assemblyInitializer = assembly_initializer;
        }

        private IAssembly Assembly
        {
            get { return _assemblyInitializer.Value; }
        }

        public ChocolateyUnpackSelfCommand(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public virtual void ConfigureArgumentParser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
        }

        public virtual void ParseAdditionalArguments(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
        }

        public virtual void Validate(ChocolateyConfiguration configuration)
        {
        }

        public virtual void HelpMessage(ChocolateyConfiguration configuration)
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

        public virtual void DryRun(ChocolateyConfiguration configuration)
        {
            this.Log().Info("This would have unpacked {0} for use relative where the executable is, based on resources embedded in {0}.".FormatWith(ApplicationParameters.Name));
        }

        public virtual void Run(ChocolateyConfiguration configuration)
        {
            this.Log().Info("{0} is unpacking required files for use. Overwriting? {1}".FormatWith(ApplicationParameters.Name, configuration.Force));
            // refactor - thank goodness this is temporary, cuz manifest resource streams are dumb

            // unpack the manifest file as well
            AssemblyFileExtractor.ExtractAssemblyResourcesToRelativeDirectory(_fileSystem, adapters.Assembly.GetAssembly(typeof(ChocolateyUnpackSelfCommand)), _fileSystem.GetDirectoryName(_fileSystem.GetCurrentAssemblyPath()), new List<string>(), "chocolatey.console");

            IList<string> folders = new List<string>
                {
                    "helpers",
                    "functions",
                    "redirects",
                    "tools"
                };

            AssemblyFileExtractor.ExtractAssemblyResourcesToRelativeDirectory(
                _fileSystem,
                Assembly,
                 _fileSystem.GetDirectoryName(_fileSystem.GetCurrentAssemblyPath()),
                folders,
                ApplicationParameters.ChocolateyFileResources,
                overwriteExisting: configuration.Force,
                logOutput: true);
        }

        public virtual bool MayRequireAdminAccess()
        {
            return true;
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
