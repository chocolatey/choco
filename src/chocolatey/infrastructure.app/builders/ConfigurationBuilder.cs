namespace chocolatey.infrastructure.app.builders
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using commands;
    using configuration;
    using extractors;
    using filesystem;
    using infrastructure.configuration;
    using infrastructure.services;
    using logging;
    using platforms;

    public class ConfigurationBuilder
    {
        public static void set_up_configuration(IList<string> args, ChocolateyConfiguration config, IFileSystem fileSystem, IXmlService xmlService)
        {
            set_file_configuration(config, fileSystem, xmlService);
            set_global_options(args, config);
            set_platform(config);
        }

        private static void set_file_configuration(ChocolateyConfiguration config, IFileSystem fileSystem, IXmlService xmlService)
        {
            var globalConfigPath = ApplicationParameters.GlobalConfigFileLocation;
            AssemblyFileExtractor.extract_text_file_from_assembly(fileSystem, Assembly.GetExecutingAssembly(), ApplicationParameters.ChocolateyConfigFileResource, globalConfigPath);

            var configFileSettings = xmlService.deserialize<ConfigFileSettings>(globalConfigPath);

            config.UseNugetForSources = configFileSettings.UseNugetForSources;
            if (!config.UseNugetForSources)
            {
                var sources = new StringBuilder();
                foreach (var source in configFileSettings.Sources)
                {
                    sources.AppendFormat("{0};", source.Value);
                }
                config.Source = sources.Remove(sources.Length - 1, 1).ToString();
            }
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
                            .Add("f|force",
                                 "Force - force the behavior",
                                 option => config.Force = option != null)
                            .Add("noop",
                                 "NoOp - Don't actually do anything.",
                                 option => config.Noop = option != null)
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
                        foreach (var command in Enum.GetValues(typeof (CommandNameType)).Cast<CommandNameType>())
                        {
                            commandsLog.AppendFormat(" * {0}\n", command.GetDescriptionOrValue());
                        }

                        "chocolatey".Log().Warn(ChocolateyLoggers.Important, "Commands");
                        "chocolatey".Log().Info(@"{1}

Please run chocolatey with `choco command -help` for specific help on each command.".format_with(config.ChocolateyVersion, commandsLog.ToString()));
                    });
        }

        private static void set_platform(ChocolateyConfiguration config)
        {
            config.PlatformType = Platform.get_platform();
            config.PlatformVersion = Platform.get_version();
        }
    }
}