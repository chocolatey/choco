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

namespace chocolatey.infrastructure.app.builders
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using adapters;
    using configuration;
    using domain;
    using extractors;
    using filesystem;
    using information;
    using infrastructure.services;
    using logging;
    using platforms;
    using Environment = adapters.Environment;

    /// <summary>
    ///   Responsible for gathering all configuration related information and producing the ChocolateyConfig
    /// </summary>
    public static class ConfigurationBuilder
    {
        private static Lazy<IEnvironment> environment_initializer = new Lazy<IEnvironment>(() => new Environment());

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void initialize_with(Lazy<IEnvironment> environment)
        {
            environment_initializer = environment;
        }

        private static IEnvironment Environment
        {
            get { return environment_initializer.Value; }
        }

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
            foreach (var source in configFileSettings.Sources.or_empty_list_if_null())
            {
                sources.AppendFormat("{0};", source.Value);
            }
            if (sources.Length != 0)
            {
                config.Sources = sources.Remove(sources.Length - 1, 1).ToString();
            }

            config.CheckSumFiles = configFileSettings.ChecksumFiles;
            config.VirusCheckFiles = configFileSettings.VirusCheckFiles;
            config.CacheLocation = configFileSettings.CacheLocation;
            config.ContainsLegacyPackageInstalls = configFileSettings.ContainsLegacyPackageInstalls;
            if (configFileSettings.CommandExecutionTimeoutSeconds <= 0)
            {
                configFileSettings.CommandExecutionTimeoutSeconds = ApplicationParameters.DefaultWaitForExitInSeconds;
            }
            config.CommandExecutionTimeoutSeconds = configFileSettings.CommandExecutionTimeoutSeconds;

            try
            {
                xmlService.serialize(configFileSettings, globalConfigPath);
            }
            catch (Exception ex)
            {
                "chocolatey".Log().Warn(() => "Error updating '{0}'. Please ensure you have permissions to do so.{1} {2}".format_with(globalConfigPath, Environment.NewLine, ex.Message));
            }
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
                            .Add("acceptlicense|accept-license",
                                 "AcceptLicense - Accept license dialogs automatically.",
                                 option => config.AcceptLicense = option != null)
                            .Add("y|yes|confirm",
                                 "Confirm all prompts - Chooses default answer instead of prompting. Implies --accept-license",
                                 option =>
                                     {
                                         config.PromptForConfirmation = option == null;
                                         config.AcceptLicense = option != null;
                                     })
                            .Add("f|force",
                                 "Force - force the behavior",
                                 option => config.Force = option != null)
                            .Add("noop|whatif|what-if",
                                 "NoOp - Don't actually do anything.",
                                 option => config.Noop = option != null)
                            .Add("r|limitoutput|limit-output",
                                 "LimitOuptut - Limit the output to essential information",
                                 option => config.RegularOuptut = option == null)
                            .Add("execution-timeout=",
                                 "CommandExecutionTimeoutSeconds - Override the default execution timeout in the configuration of {0} seconds.".format_with(config.CommandExecutionTimeoutSeconds.to_string()),
                                 option => config.CommandExecutionTimeoutSeconds = int.Parse(option))
                            .Add("c=|cache=|cachelocation=|cache-location=",
                                 "CacheLocation - Location for download cache, defaults to %TEMP% or value in chocolatey.config file.",
                                 option => config.CacheLocation = option)
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
                () => { },
                () =>
                    {
                        var commandsLog = new StringBuilder();
                        foreach (var command in Enum.GetValues(typeof (CommandNameType)).Cast<CommandNameType>())
                        {
                            commandsLog.AppendFormat(" * {0}\n", command.get_description_or_value());
                        }

                        "chocolatey".Log().Info(ChocolateyLoggers.Important, "Commands");
                        "chocolatey".Log().Info(@"{1}

Please run chocolatey with `choco command -help` for specific help on each command.".format_with(config.Information.ChocolateyVersion, commandsLog.ToString()));
                    });
        }

        private static void set_environment_options(ChocolateyConfiguration config)
        {
            config.Information.PlatformType = Platform.get_platform();
            config.Information.PlatformVersion = Platform.get_version();
            config.Information.ChocolateyVersion = VersionInformation.get_current_assembly_version();
            config.Information.Is64Bit = Environment.Is64BitOperatingSystem;
            config.Information.IsInteractive = Environment.UserInteractive;
        }
    }
}