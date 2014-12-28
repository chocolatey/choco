namespace chocolatey.infrastructure.app.commands
{
    using System.Collections.Generic;
    using System.Reflection;
    using attributes;
    using commandline;
    using configuration;
    using domain;
    using extractors;
    using filesystem;
    using infrastructure.commands;
    using logging;
    using resources;

    [CommandFor(CommandNameType.unpackself)]
    public sealed class ChocolateyUnpackSelfCommand : ICommand
    {
        private readonly IFileSystem _fileSystem;

        public ChocolateyUnpackSelfCommand(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
        }

        public void handle_validation(ChocolateyConfiguration configuration)
        {
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "UnpackSelf Command");
            this.Log().Info(@"
This command should only be used when installing Chocolatey, not during normal operation. This will unpack unconditionally overwriting files that may exist.
");
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            this.Log().Info("This would have unpacked {0} for use relative where the executable is, based on resources embedded in {0}.".format_with(ApplicationParameters.Name));
        }

        public void run(ChocolateyConfiguration configuration)
        {
            this.Log().Info("{0} is setting itself up for use".format_with(ApplicationParameters.Name));
            //refactor - thank goodness this is temporary, cuz manifest resource streams are dumb
            IList<string> folders = new List<string>
                {
                    "helpers",
                    "functions",
                    "redirects",
                    "tools"
                };

            AssemblyFileExtractor.extract_all_resources_to_relative_directory(
                _fileSystem,
                Assembly.GetAssembly(typeof (ChocolateyResourcesAssembly)),
                ApplicationParameters.InstallLocation,
                folders, 
                "chocolatey.resources",
                overwriteExisting: true);
        }
    }
}