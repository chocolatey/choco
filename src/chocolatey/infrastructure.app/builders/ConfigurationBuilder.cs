namespace chocolatey.infrastructure.app.builders
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using configuration;
    using domain;
    using extractors;
    using filesystem;
    using information;
    using infrastructure.configuration;
    using infrastructure.services;
    using logging;
    using platforms;

    /// <summary>
    ///   Responsible for gathering all configuration related information and producing the ChocolateyConfig
    /// </summary>
    public static class ConfigurationBuilder
    {
        /// <summary>
        ///   Sets up the configuration based on arguments passed in, config file, and environment
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="xmlService">The XML service.</param>
        public static void set_up_configuration(IList<string> args, ChocolateyConfiguration config, IFileSystem fileSystem, IXmlService xmlService)
        {
            set_file_configuration(config, fileSystem, xmlService);
            set_global_options(args, config);
            set_environment_options(config);
        }

        private static void set_file_configuration(ChocolateyConfiguration config, IFileSystem fileSystem, IXmlService xmlService)
        {
            var globalConfigPath = ApplicationParameters.GlobalConfigFileLocation;
            AssemblyFileExtractor.extract_text_file_from_assembly(fileSystem, Assembly.GetExecutingAssembly(), ApplicationParameters.ChocolateyConfigFileResource, globalConfigPath);

            var configFileSettings = xmlService.deserialize<ConfigFileSettings>(globalConfigPath);
            var sources = new StringBuilder();
            foreach (var source in configFileSettings.Sources)
            {
                sources.AppendFormat("{0};", source.Value);
            }
            config.Source = sources.Remove(sources.Length - 1, 1).ToString();

            config.CheckSumFiles = configFileSettings.ChecksumFiles;
            config.VirusCheckFiles = configFileSettings.VirusCheckFiles;
        }

        private static void set_global_options(IList<string> args, ChocolateyConfiguration config)
        {
            ConfigurationOptions.parse_arguments_and_update_configuration(
                args,
                config,
                (option_set) =>
                {
                    option_set
                        .Add("d|debug",
                             "Debug - Run in Debug Mode.",
                             option => config.Debug = option != null)
                        .Add("v|verbose",
                             "Verbose - See verbose messaging.",
                             option => config.Verbose = option != null)
                        .Add("y|yes|confirm",
                             "Confirm all prompts - Chooses default answer - See verbose messaging.",
                             option => config.PromptForConfirmation = option == null)
                        .Add("f|force",
                             "Force - force the behavior",
                             option => config.Force = option != null)
                        .Add("noop|whatif|what-if",
                             "NoOp - Don't actually do anything.",
                             option => config.Noop = option != null)
                        .Add("r|limitoutput|limit-output",
                             "LimitOuptut - Limit the output to essential information",
                             option => config.RegularOuptut = option == null)
                        ;
                },
                (unparsedArgs) =>
                {
                    if (!string.IsNullOrWhiteSpace(config.CommandName))
                    {
                        // save help for next menu
                        config.HelpRequested = false;
                    }
                },
                () =>
                {
                    var commandsLog = new StringBuilder();
                    foreach (var command in Enum.GetValues(typeof(CommandNameType)).Cast<CommandNameType>())
                    {
                        commandsLog.AppendFormat(" * {0}\n", command.get_description_or_value());
                    }

                    "chocolatey".Log().Info(ChocolateyLoggers.Important, "Commands");
                    "chocolatey".Log().Info(@"{1}

Please run chocolatey with `choco command -help` for specific help on each command.".format_with(config.ChocolateyVersion, commandsLog.ToString()));
                });
        }

        private static void set_environment_options(ChocolateyConfiguration config)
        {
            config.PlatformType = Platform.get_platform();
            config.PlatformVersion = Platform.get_version();
            config.ChocolateyVersion = VersionInformation.get_current_assembly_version();
            config.Is64Bit = Environment.Is64BitOperatingSystem;
            config.IsInteractive = Environment.UserInteractive;
        }
    }
}