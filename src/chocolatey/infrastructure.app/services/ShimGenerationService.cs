namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using builders;
    using configuration;
    using filesystem;
    using infrastructure.commands;
    using results;

    public class ShimGenerationService : IShimGenerationService
    {
        private readonly IFileSystem _fileSystem;
        private const string PATH_TOKEN = "{{path}}";
        private const string OUTPUT_TOKEN = "{{output}}";
        private readonly string _shimGenExePath = ApplicationParameters.Tools.ShimGenExe;
        private readonly IDictionary<string, ExternalCommandArgument> _shimGenArguments = new Dictionary<string, ExternalCommandArgument>();
        

        public ShimGenerationService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            set_shimgen_args_dictionary();
        }

        /// <summary>
        /// Sets webpicmd install dictionary
        /// </summary>
        private void set_shimgen_args_dictionary()
        {
            _shimGenArguments.Add("_file_path_", new ExternalCommandArgument
            {
                ArgumentOption = "--path=",
                ArgumentValue = PATH_TOKEN,
                QuoteValue = true,
                Required = true
            });
            
            _shimGenArguments.Add("_output_directory_", new ExternalCommandArgument
            {
                ArgumentOption = "--output=",
                ArgumentValue = OUTPUT_TOKEN,
                QuoteValue = true,
                Required = true
            });

            //_shimGenArguments.Add("_gui_", new ExternalCommandArgument { ArgumentOption = "--gui", Required = false });
        }


        public void install(ChocolateyConfiguration configuration, PackageResult packageResult)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.ShimsLocation);

            //gather all .exes in the folder 
            var exeFiles = _fileSystem.get_files(packageResult.InstallLocation, pattern: "*.exe",option:SearchOption.AllDirectories);
            foreach (string file in exeFiles)
            {
                if (_fileSystem.file_exists(file + ".ignore")) continue;
                bool isGui = _fileSystem.file_exists(file + ".gui");
                //todo: v2 be able to determine gui automatically

                var args = ExternalCommandArgsBuilder.build_arguments(configuration, _shimGenArguments);
                var shimLocation = _fileSystem.combine_paths(ApplicationParameters.ShimsLocation, _fileSystem.get_file_name(file));
                var argsForPackage = args.Replace(PATH_TOKEN, file.Replace(ApplicationParameters.InstallLocation, "..\\")).Replace(OUTPUT_TOKEN,shimLocation);
                if (isGui)
                {
                    argsForPackage += " --gui";
                }

                var exitCode = CommandExecutor.execute(
                    _shimGenExePath, argsForPackage,true,
                    (s, e) =>
                    {
                        if (string.IsNullOrWhiteSpace(e.Data)) return;
                        this.Log().Debug(() => " [ShimGen] {0}".format_with(e.Data));
                    },
                    (s, e) =>
                    {
                        if (string.IsNullOrWhiteSpace(e.Data)) return;
                        this.Log().Error(() => " {0}".format_with(e.Data));
                    }
                    );

                if (exitCode != 0)
                {
                    Environment.ExitCode = exitCode;
                }
                else
                {
                    this.Log().Info(() => " ShimGen has successfully created a {0}shim for {1}".format_with(isGui ? "gui ":string.Empty,_fileSystem.get_file_name(file)));
                    this.Log().Debug(() => "  Created: {0}{1}  Targeting: {2}{1}  IsGui:{3}{1}".format_with(shimLocation,Environment.NewLine,file,isGui));
                }

            }
            

            
              
             
        }
    }
}