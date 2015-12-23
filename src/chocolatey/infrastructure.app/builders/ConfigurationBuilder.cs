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
    using nuget;
    using platforms;
    using tolerance;
    using Environment = adapters.Environment;

    /// <summary>
    ///   Responsible for gathering all configuration related information and producing the ChocolateyConfig
    /// </summary>
    public static class ConfigurationBuilder
    {
        private static Lazy<IEnvironment> _environmentInitializer = new Lazy<IEnvironment>(() => new Environment());

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void initialize_with(Lazy<IEnvironment> environment)
        {
            _environmentInitializer = environment;
        }

        private static IEnvironment Environment
        {
            get { return _environmentInitializer.Value; }
        }

        /// <summary>
        ///   Sets up the configuration based on arguments passed in, config file, and environment
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="xmlService">The XML service.</param>
        /// <param name="notifyWarnLoggingAction">Notify warn logging action</param>
        public static void set_up_configuration(IList<string> args, ChocolateyConfiguration config, IFileSystem fileSystem, IXmlService xmlService, Action<string> notifyWarnLoggingAction)
        {
            set_file_configuration(config, fileSystem, xmlService, notifyWarnLoggingAction);
            ConfigurationOptions.reset_options();
            set_global_options(args, config);
            set_environment_options(config);
            set_environment_variables(config);
        }

        private static void set_file_configuration(ChocolateyConfiguration config, IFileSystem fileSystem, IXmlService xmlService, Action<string> notifyWarnLoggingAction)
        {
            var globalConfigPath = ApplicationParameters.GlobalConfigFileLocation;
            AssemblyFileExtractor.extract_text_file_from_assembly(fileSystem, Assembly.GetExecutingAssembly(), ApplicationParameters.ChocolateyConfigFileResource, globalConfigPath);

            var configFileSettings = xmlService.deserialize<ConfigFileSettings>(globalConfigPath);
            var sources = new StringBuilder();

            var defaultSourcesInOrder = configFileSettings.Sources.Where(s => !s.Disabled).or_empty_list_if_null().ToList();
            if (configFileSettings.Sources.Any(s => s.Priority > 0))
            {
                defaultSourcesInOrder = configFileSettings.Sources.Where(s => !s.Disabled && s.Priority != 0).OrderBy(s => s.Priority).or_empty_list_if_null().ToList();
                defaultSourcesInOrder.AddRange(configFileSettings.Sources.Where(s => !s.Disabled && s.Priority == 0 ).or_empty_list_if_null().ToList());
            }

            foreach (var source in defaultSourcesInOrder)
            {
                sources.AppendFormat("{0};", source.Value);
            }
            if (sources.Length != 0)
            {
                config.Sources = sources.Remove(sources.Length - 1, 1).ToString();
            }

            set_machine_sources(config, configFileSettings);

            set_config_items(config, configFileSettings, fileSystem);

            FaultTolerance.try_catch_with_logging_exception(
                () => fileSystem.create_directory_if_not_exists(config.CacheLocation),
                "Could not create temp directory at '{0}'".format_with(config.CacheLocation),
                logWarningInsteadOfError: true);

            set_feature_flags(config, configFileSettings);

            // save so all updated configuration items get set to existing config
            FaultTolerance.try_catch_with_logging_exception(
                () => xmlService.serialize(configFileSettings, globalConfigPath),
                "Error updating '{0}'. Please ensure you have permissions to do so".format_with(globalConfigPath),
                logWarningInsteadOfError: true);
        }

        private static void set_machine_sources(ChocolateyConfiguration config, ConfigFileSettings configFileSettings)
        {
            foreach (var source in configFileSettings.Sources.Where(s => !s.Disabled).or_empty_list_if_null())
            {
                config.MachineSources.Add(new MachineSourceConfiguration
                    {
                        Key = source.Value,
                        Name = source.Id,
                        Username = source.UserName,
                        EncryptedPassword = source.Password,
                        Priority = source.Priority
                    });
            }
        }

        private static void set_config_items(ChocolateyConfiguration config, ConfigFileSettings configFileSettings, IFileSystem fileSystem)
        {
            var cacheLocation = set_config_item(ApplicationParameters.ConfigSettings.CacheLocation, configFileSettings, string.IsNullOrWhiteSpace(configFileSettings.CacheLocation) ? string.Empty : configFileSettings.CacheLocation, "Cache location if not TEMP folder.");
            config.CacheLocation = !string.IsNullOrWhiteSpace(cacheLocation) ? cacheLocation : System.Environment.GetEnvironmentVariable("TEMP");
            if (string.IsNullOrWhiteSpace(config.CacheLocation))
            {
                config.CacheLocation = fileSystem.combine_paths(ApplicationParameters.InstallLocation, "temp");
            }

            var originalCommandTimeout = configFileSettings.CommandExecutionTimeoutSeconds;
            var commandExecutionTimeoutSeconds = -1;
            int.TryParse(
                set_config_item(
                    ApplicationParameters.ConfigSettings.CommandExecutionTimeoutSeconds,
                    configFileSettings,
                    originalCommandTimeout == 0 ?
                        ApplicationParameters.DefaultWaitForExitInSeconds.to_string()
                        : originalCommandTimeout.to_string(),
                    "Default timeout for command execution."),
                out commandExecutionTimeoutSeconds);
            config.CommandExecutionTimeoutSeconds = commandExecutionTimeoutSeconds;
            if (configFileSettings.CommandExecutionTimeoutSeconds <= 0)
            {
                set_config_item(ApplicationParameters.ConfigSettings.CommandExecutionTimeoutSeconds, configFileSettings, ApplicationParameters.DefaultWaitForExitInSeconds.to_string(), "Default timeout for command execution.", forceSettingValue: true);
                config.CommandExecutionTimeoutSeconds = ApplicationParameters.DefaultWaitForExitInSeconds;
            }

            config.ContainsLegacyPackageInstalls = set_config_item(ApplicationParameters.ConfigSettings.ContainsLegacyPackageInstalls, configFileSettings, "true", "Install has packages installed prior to 0.9.9 series.").is_equal_to(bool.TrueString);
            config.Proxy.Location = set_config_item(ApplicationParameters.ConfigSettings.Proxy, configFileSettings, string.Empty, "Explicit proxy location.");
            config.Proxy.User = set_config_item(ApplicationParameters.ConfigSettings.ProxyUser, configFileSettings, string.Empty, "Optional proxy user.");
            config.Proxy.EncryptedPassword = set_config_item(ApplicationParameters.ConfigSettings.ProxyPassword, configFileSettings, string.Empty, "Optional proxy password. Encrypted.");
        }

        private static string set_config_item(string configName, ConfigFileSettings configFileSettings, string defaultValue, string description, bool forceSettingValue = false)
        {
            var config = configFileSettings.ConfigSettings.FirstOrDefault(f => f.Key.is_equal_to(configName));
            if (config == null)
            {
                config = new ConfigFileConfigSetting
                {
                    Key = configName,
                    Value = defaultValue,
                    Description = description
                };

                configFileSettings.ConfigSettings.Add(config);
            }
            if (forceSettingValue)
            {
                config.Value = defaultValue;
            }

            config.Description = description;

            return config.Value;
        }

        private static void set_feature_flags(ChocolateyConfiguration config, ConfigFileSettings configFileSettings)
        {
            config.Features.CheckSumFiles = set_feature_flag(ApplicationParameters.Features.CheckSumFiles, configFileSettings, defaultEnabled: true, description: "Checksum files when pulled in from internet (based on package).");
            config.Features.AutoUninstaller = set_feature_flag(ApplicationParameters.Features.AutoUninstaller, configFileSettings, defaultEnabled: true, description: "Uninstall from programs and features without requiring an explicit uninstall script.");
            config.Features.FailOnAutoUninstaller = set_feature_flag(ApplicationParameters.Features.FailOnAutoUninstaller, configFileSettings, defaultEnabled: false, description: "Fail if automatic uninstaller fails.");
            config.Features.FailOnStandardError = set_feature_flag(ApplicationParameters.Features.FailOnStandardError, configFileSettings, defaultEnabled: false, description: "Fail if install provider writes to stderr.");
            config.PromptForConfirmation = !set_feature_flag(ApplicationParameters.Features.AllowGlobalConfirmation, configFileSettings, defaultEnabled: false, description: "Prompt for confirmation in scripts or bypass.");
        }

        private static bool set_feature_flag(string featureName, ConfigFileSettings configFileSettings, bool defaultEnabled, string description)
        {
            var feature = configFileSettings.Features.FirstOrDefault(f => f.Name.is_equal_to(featureName));

            if (feature == null)
            {
                feature = new ConfigFileFeatureSetting
                {
                    Name = featureName,
                    Enabled = defaultEnabled,
                    Description = description
                };

                configFileSettings.Features.Add(feature);
            }
            else
            {
                if (!feature.SetExplicitly && feature.Enabled != defaultEnabled)
                {
                    feature.Enabled = defaultEnabled;
                }
            }

            feature.Description = description;

            return feature != null ? feature.Enabled : defaultEnabled;
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
                                 "Confirm all prompts - Chooses affirmative answer instead of prompting. Implies --accept-license",
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
                                 "LimitOutput - Limit the output to essential information",
                                 option => config.RegularOutput = option == null)
                            .Add("execution-timeout=",
                                 "CommandExecutionTimeoutSeconds - Override the default execution timeout in the configuration of {0} seconds.".format_with(config.CommandExecutionTimeoutSeconds.to_string()),
                                 option => config.CommandExecutionTimeoutSeconds = int.Parse(option.remove_surrounding_quotes()))
                            .Add("c=|cache=|cachelocation=|cache-location=",
                                 "CacheLocation - Location for download cache, defaults to %TEMP% or value in chocolatey.config file.",
                                 option => config.CacheLocation = option.remove_surrounding_quotes())
                            .Add("allowunofficial|allow-unofficial|allowunofficialbuild|allow-unofficial-build",
                                 "AllowUnofficialBuild - When not using the official build you must set this flag for choco to continue.",
                                 option => config.AllowUnofficialBuild = option != null) 
                            .Add("failstderr|failonstderr|fail-on-stderr|fail-on-standard-error|fail-on-error-output",
                                 "FailOnStandardError - Fail on standard error output (stderr), typically received when running external commands during install providers. This overrides the feature failOnStandardError.",
                                 option => config.Features.FailOnStandardError = option != null)
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

                        "chocolatey".Log().Info(@"This is a listing of all of the different things you can pass to choco.
");
                        "chocolatey".Log().Info(ChocolateyLoggers.Important, "Commands");
                        "chocolatey".Log().Info(@"
{0}

Please run chocolatey with `choco command -help` for specific help on
 each command.
".format_with(commandsLog.ToString()));
                        "chocolatey".Log().Info(ChocolateyLoggers.Important, @"How To Pass Options / Switches");
                        "chocolatey".Log().Info(@"
You can pass options and switches in the following ways:

 * `-`, `/`, or `--` (one character switches should not use `--`)
 * **Option Bundling / Bundled Options**: One character switches can be
   bundled. e.g. `-d` (debug), `-f` (force), `-v` (verbose), and `-y`
   (confirm yes) can be bundled as `-dfvy`.
 * ***Note:*** If `debug` or `verbose` are bundled with local options
   (not the global ones above), some logging may not show up until after
   the local options are parsed.
 * **Use Equals**: You can also include or not include an equals sign
   `=` between options and values.
 * **Quote Values**: When you need to quote things, such as when using
   spaces, please use apostrophes (`'value'`). In cmd.exe you may be
   able to use just double quotes (`""value""`) but in powershell.exe
   you may need to either escape the quotes with backticks
   (`` `""value`"" ``) or use a combination of double quotes and
   apostrophes (`""'value'""`). This is due to the hand off to
   PowerShell - it seems to strip off the outer set of quotes.
 * Options and switches apply to all items passed, so if you are
   installing multiple packages, and you use `--version=1.0.0`, choco
   is going to look for and try to install version 1.0.0 of every
   package passed. So please split out multiple package calls when
   wanting to pass specific options.
");
                        "chocolatey".Log().Info(ChocolateyLoggers.Important, "Default Options and Switches");
                    });
        }

        private static void set_environment_options(ChocolateyConfiguration config)
        {
            config.Information.PlatformType = Platform.get_platform();
            config.Information.PlatformVersion = Platform.get_version();
            config.Information.PlatformName = Platform.get_name();
            config.Information.ChocolateyVersion = VersionInformation.get_current_assembly_version();
            config.Information.ChocolateyProductVersion = VersionInformation.get_current_informational_version();
            config.Information.FullName = Assembly.GetExecutingAssembly().FullName;
            config.Information.Is64Bit = Environment.Is64BitOperatingSystem;
            config.Information.IsInteractive = Environment.UserInteractive;
            config.Information.IsUserAdministrator = ProcessInformation.user_is_administrator();
            config.Information.IsProcessElevated = ProcessInformation.process_is_elevated();
        }

        public static void set_environment_variables(ChocolateyConfiguration config)
        {
            Environment.SetEnvironmentVariable(ApplicationParameters.ChocolateyInstallEnvironmentVariableName, ApplicationParameters.InstallLocation);
            Environment.SetEnvironmentVariable("CHOCOLATEY_VERSION", config.Information.ChocolateyVersion);
            Environment.SetEnvironmentVariable("CHOCOLATEY_VERSION_PRODUCT", config.Information.ChocolateyProductVersion);
            Environment.SetEnvironmentVariable("OS_PLATFORM", config.Information.PlatformType.get_description_or_value());
            Environment.SetEnvironmentVariable("OS_VERSION", config.Information.PlatformVersion.to_string());
            Environment.SetEnvironmentVariable("OS_NAME", config.Information.PlatformName.to_string());
            // experimental until we know if this value returns correctly based on the OS and not the current process.
            Environment.SetEnvironmentVariable("OS_IS64BIT", config.Information.Is64Bit ? "true" : "false");
            Environment.SetEnvironmentVariable("IS_ADMIN", config.Information.IsUserAdministrator ? "true" : "false");
            Environment.SetEnvironmentVariable("IS_PROCESSELEVATED", config.Information.IsProcessElevated ? "true" : "false");

            Environment.SetEnvironmentVariable("TEMP", config.CacheLocation);
            if (config.Debug)
            {
                Environment.SetEnvironmentVariable("ChocolateyEnvironmentDebug", "true");
            }
            if (config.Verbose)
            {
                Environment.SetEnvironmentVariable("ChocolateyEnvironmentVerbose", "true");
            }
            if (!config.Features.CheckSumFiles)
            {
                Environment.SetEnvironmentVariable("ChocolateyIgnoreChecksums", "true");
            }
            if (!string.IsNullOrWhiteSpace(config.Proxy.Location))
            {
                var proxyCreds = string.Empty;
                if (!string.IsNullOrWhiteSpace(config.Proxy.User) &&
                    !string.IsNullOrWhiteSpace(config.Proxy.EncryptedPassword)
                   )
                {
                    proxyCreds = "{0}:{1}@".format_with(config.Proxy.User, NugetEncryptionUtility.DecryptString(config.Proxy.EncryptedPassword));

                    Environment.SetEnvironmentVariable("chocolateyProxyUser", config.Proxy.User);
                    Environment.SetEnvironmentVariable("chocolateyProxyPassword", NugetEncryptionUtility.DecryptString(config.Proxy.EncryptedPassword));
                }

                Environment.SetEnvironmentVariable("http_proxy", "{0}{1}".format_with(proxyCreds, config.Proxy.Location));
                Environment.SetEnvironmentVariable("https_proxy", "{0}{1}".format_with(proxyCreds, config.Proxy.Location));
                Environment.SetEnvironmentVariable("chocolateyProxyLocation", config.Proxy.Location);
            }
        }
    }
}
