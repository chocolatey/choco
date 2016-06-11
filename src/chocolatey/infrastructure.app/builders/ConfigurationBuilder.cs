﻿// Copyright © 2011 - Present RealDimensions Software, LLC
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
    using System.Reflection;
    using System.Text;
    using adapters;
    using attributes;
    using configuration;
    using cryptography;
    using domain;
    using extractors;
    using filesystem;
    using information;
    using infrastructure.commands;
    using infrastructure.services;
    using licensing;
    using logging;
    using nuget;
    using platforms;
    using tolerance;
    using Assembly = adapters.Assembly;
    using Container = SimpleInjector.Container;
    using Environment = adapters.Environment;

    /// <summary>
    ///   Responsible for gathering all configuration related information and producing the ChocolateyConfig
    /// </summary>
    public static class ConfigurationBuilder
    {
        private const string SET_CONFIGURATION_METHOD = "SetConfiguration";
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
        /// <param name="container">The container.</param>
        /// <param name="license">The license.</param>
        /// <param name="notifyWarnLoggingAction">Notify warn logging action</param>
        public static void set_up_configuration(IList<string> args, ChocolateyConfiguration config, Container container, ChocolateyLicense license, Action<string> notifyWarnLoggingAction)
        {
            var fileSystem = container.GetInstance<IFileSystem>();
            var xmlService = container.GetInstance<IXmlService>();
            var configFileSettings = get_config_file_settings(fileSystem, xmlService);
            // must be done prior to setting the file configuration
            add_or_remove_licensed_source(license, configFileSettings);
            set_file_configuration(config, configFileSettings, fileSystem, notifyWarnLoggingAction);
            ConfigurationOptions.reset_options();
            set_global_options(args, config, container);
            set_environment_options(config);
            EnvironmentSettings.set_environment_variables(config);
            // must be done last for overrides
            set_licensed_options(config, license, configFileSettings);
            // save all changes if there are any
            set_config_file_settings(configFileSettings, xmlService);
            set_hash_provider(config, container);
        }
        
        private static ConfigFileSettings get_config_file_settings(IFileSystem fileSystem, IXmlService xmlService)
        {
            var globalConfigPath = ApplicationParameters.GlobalConfigFileLocation;
            AssemblyFileExtractor.extract_text_file_from_assembly(fileSystem, Assembly.GetExecutingAssembly(), ApplicationParameters.ChocolateyConfigFileResource, globalConfigPath);

            return xmlService.deserialize<ConfigFileSettings>(globalConfigPath);
        }

        private static void set_config_file_settings(ConfigFileSettings configFileSettings, IXmlService xmlService)
        {
            var globalConfigPath = ApplicationParameters.GlobalConfigFileLocation;
            // save so all updated configuration items get set to existing config
            FaultTolerance.try_catch_with_logging_exception(
                () => xmlService.serialize(configFileSettings, globalConfigPath),
                "Error updating '{0}'. Please ensure you have permissions to do so".format_with(globalConfigPath),
                logWarningInsteadOfError: true);
        }

        private static void add_or_remove_licensed_source(ChocolateyLicense license, ConfigFileSettings configFileSettings)
        {
            // do not enable or disable the source, in case the user has disabled it
            var addOrUpdate = license.IsValid;
            var sources = configFileSettings.Sources.Where(s => !s.Disabled).or_empty_list_if_null().ToList();

            var configSource = new ConfigFileSourceSetting
            {
                Id = ApplicationParameters.ChocolateyLicensedFeedSourceName,
                Value = ApplicationParameters.ChocolateyLicensedFeedSource,
                UserName = "customer",
                Password = NugetEncryptionUtility.EncryptString(license.Id),
                Priority = 10
            };

            if (addOrUpdate && !sources.Any(s =>
                    s.Id.is_equal_to(ApplicationParameters.ChocolateyLicensedFeedSourceName)
                    && NugetEncryptionUtility.DecryptString(s.Password).is_equal_to(license.Id)
                    )
                )
            {
                configFileSettings.Sources.Add(configSource);
            }

            if (!addOrUpdate)
            {
                configFileSettings.Sources.RemoveWhere(s => s.Id.is_equal_to(configSource.Id));
            }
        }

        private static void set_file_configuration(ChocolateyConfiguration config, ConfigFileSettings configFileSettings, IFileSystem fileSystem, Action<string> notifyWarnLoggingAction)
        {
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
                        Certificate = source.Certificate,
                        EncryptedCertificatePassword = source.CertificatePassword,
                        Priority = source.Priority
                    });
            }
        }

        private static void set_config_items(ChocolateyConfiguration config, ConfigFileSettings configFileSettings, IFileSystem fileSystem)
        {
            var cacheLocation = set_config_item(ApplicationParameters.ConfigSettings.CacheLocation, configFileSettings, string.IsNullOrWhiteSpace(configFileSettings.CacheLocation) ? string.Empty : configFileSettings.CacheLocation, "Cache location if not TEMP folder.");
            config.CacheLocation = !string.IsNullOrWhiteSpace(cacheLocation) ? cacheLocation : fileSystem.combine_paths(fileSystem.get_temp_path(), "chocolatey"); // System.Environment.GetEnvironmentVariable("TEMP");
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

            var webRequestTimeoutSeconds = -1;
            int.TryParse(
                set_config_item(
                    ApplicationParameters.ConfigSettings.WebRequestTimeoutSeconds,
                    configFileSettings,
                    ApplicationParameters.DefaultWebRequestTimeoutInSeconds.to_string(),
                    "Default timeout for web requests. Available in 0.9.10+."),
                    out webRequestTimeoutSeconds);
            if (webRequestTimeoutSeconds <= 0)
            {
                webRequestTimeoutSeconds = ApplicationParameters.DefaultWebRequestTimeoutInSeconds;
                set_config_item(ApplicationParameters.ConfigSettings.WebRequestTimeoutSeconds,configFileSettings, ApplicationParameters.DefaultWebRequestTimeoutInSeconds.to_string(),"Default timeout for web requests. Available in 0.9.10+.", forceSettingValue: true);
            }
            config.WebRequestTimeoutSeconds = webRequestTimeoutSeconds;

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
            config.Features.FailOnStandardError = set_feature_flag(ApplicationParameters.Features.FailOnStandardError, configFileSettings, defaultEnabled: false, description: "Fail if install provider writes to stderr. Available in 0.9.10+.");
            config.Features.UsePowerShellHost = set_feature_flag(ApplicationParameters.Features.UsePowerShellHost, configFileSettings, defaultEnabled: true, description: "Use Chocolatey's built-in PowerShell host. Available in 0.9.10+.");
            config.Features.LogEnvironmentValues = set_feature_flag(ApplicationParameters.Features.LogEnvironmentValues, configFileSettings, defaultEnabled: false, description: "Log Environment Values - will log values of environment before and after install (could disclose sensitive data). Available in 0.9.10+.");
            config.Features.VirusCheck = set_feature_flag(ApplicationParameters.Features.VirusCheck, configFileSettings, defaultEnabled: false, description: "Virus Check - perform virus checking on downloaded files. Available in 0.9.10+. Licensed versions only.");
            config.Features.FailOnInvalidOrMissingLicense = set_feature_flag(ApplicationParameters.Features.FailOnInvalidOrMissingLicense, configFileSettings, defaultEnabled: false, description: "Fail On Invalid Or Missing License - allows knowing when a license is expired or not applied to a machine. Available in 0.9.10+.");
            config.Features.IgnoreInvalidOptionsSwitches = set_feature_flag(ApplicationParameters.Features.IgnoreInvalidOptionsSwitches, configFileSettings, defaultEnabled: true, description: "Ignore Invalid Options/Switches - If a switch or option is passed that is not recognized, should choco fail? Available in 0.9.10+.");
            config.Features.UsePackageExitCodes = set_feature_flag(ApplicationParameters.Features.UsePackageExitCodes, configFileSettings, defaultEnabled: true, description: "Use Package Exit Codes - Package scripts can provide exit codes. With this on, package exit codes will be what choco uses for exit when non-zero (this value can come from a dependency package). Chocolatey defines valid exit codes as 0, 1605, 1614, 1641, 3010. With this feature off, choco will exit with a 0 or a 1 (matching previous behavior). Available in 0.9.10+.");
            config.Features.UseFipsCompliantChecksums = set_feature_flag(ApplicationParameters.Features.UseFipsCompliantChecksums, configFileSettings, defaultEnabled: false, description: "Use FIPS Compliant Checksums - Ensure checksumming done by choco uses FIPS compliant algorithms. Not recommended unless required by FIPS Mode. Enabling on an existing installation could have unintended consequences related to upgrades/uninstalls. Available in 0.9.10+.");
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

        private static void set_global_options(IList<string> args, ChocolateyConfiguration config, Container container)
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
                            .Add("timeout=|execution-timeout=",
                                 "CommandExecutionTimeout (in seconds) - The time to allow a command to finish before timing out. Overrides the default execution timeout in the configuration of {0} seconds.".format_with(config.CommandExecutionTimeoutSeconds.to_string()),
                                option =>
                                {
                                    int timeout = 0;
                                    int.TryParse(option.remove_surrounding_quotes(), out timeout);
                                    if (timeout > 0)
                                    {
                                        config.CommandExecutionTimeoutSeconds = timeout;
                                    }
                                })
                            .Add("c=|cache=|cachelocation=|cache-location=",
                                 "CacheLocation - Location for download cache, defaults to %TEMP% or value in chocolatey.config file.",
                                 option => config.CacheLocation = option.remove_surrounding_quotes())
                            .Add("allowunofficial|allow-unofficial|allowunofficialbuild|allow-unofficial-build",
                                 "AllowUnofficialBuild - When not using the official build you must set this flag for choco to continue.",
                                 option => config.AllowUnofficialBuild = option != null)
                            .Add("failstderr|failonstderr|fail-on-stderr|fail-on-standard-error|fail-on-error-output",
                                 "FailOnStandardError - Fail on standard error output (stderr), typically received when running external commands during install providers. This overrides the feature failOnStandardError.",
                                 option => config.Features.FailOnStandardError = option != null)
                            .Add("use-system-powershell",
                                 "UseSystemPowerShell - Execute PowerShell using an external process instead of the built-in PowerShell host. Available in 0.9.10+.",
                                 option => config.Features.UsePowerShellHost = option == null)
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
                        IEnumerable<ICommand> commands = container.GetAllInstances<ICommand>();
                        foreach (var command in commands.or_empty_list_if_null())
                        {
                            var attributes = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>();
                            foreach (var attribute in attributes.or_empty_list_if_null())
                            {
                                commandsLog.AppendFormat(" * {0} - {1}\n", attribute.CommandName, attribute.Description);
                            }
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

 * Unless stated otherwise, an option/switch should only be passed one
   time. Otherwise you may find weird/non-supported behavior.
 * `-`, `/`, or `--` (one character switches should not use `--`)
 * **Option Bundling / Bundled Options**: One character switches can be
   bundled. e.g. `-d` (debug), `-f` (force), `-v` (verbose), and `-y`
   (confirm yes) can be bundled as `-dfvy`.
 * NOTE: If `debug` or `verbose` are bundled with local options
   (not the global ones above), some logging may not show up until after
   the local options are parsed.
 * **Use Equals**: You can also include or not include an equals sign
   `=` between options and values.
 * **Quote Values**: When you need to quote an entire argument, such as
   when using spaces, please use a combination of double quotes and
   apostrophes (`""'value'""`). In cmd.exe you can just use double quotes
   (`""value""`) but in powershell.exe you should use backticks
   (`` `""value`"" ``) or apostrophes (`'value'`). Using the combination
   allows for both shells to work without issue, except for when the next
   section applies.
 * **Pass quotes in arguments**: When you need to pass quoted values to
   to something like a native installer, you are in for a world of fun. In
   cmd.exe you must pass it like this: `-ia ""/yo=""""Spaces spaces""""""`. In
   PowerShell.exe, you must pass it like this: `-ia '/yo=""""Spaces spaces""""'`.
   No other combination will work. In PowerShell.exe if you are on version
   v3+, you can try `--%` before `-ia` to just pass the args through as is,
   which means it should not require any special workarounds.
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

        private static void set_licensed_options(ChocolateyConfiguration config, ChocolateyLicense license, ConfigFileSettings configFileSettings)
        {
            config.Information.IsLicensedVersion = license.is_licensed_version();

            if (license.AssemblyLoaded)
            {
                Type licensedConfigBuilder = license.Assembly.GetType(ApplicationParameters.LicensedConfigurationBuilder, throwOnError: false, ignoreCase: true);

                if (licensedConfigBuilder == null)
                {
                    "chocolatey".Log().Warn(ChocolateyLoggers.Important,
@"Unable to set licensed configuration. This is likely related to a
 missing or outdated licensed DLL.");
                    return;
                }
                try
                {
                    object componentClass = Activator.CreateInstance(licensedConfigBuilder);

                    licensedConfigBuilder.InvokeMember(
                        SET_CONFIGURATION_METHOD,
                        BindingFlags.InvokeMethod,
                        null,
                        componentClass,
                        new Object[] { config, configFileSettings }
                        );
                }
                catch (Exception ex)
                {
                    "chocolatey".Log().Error(
                        ChocolateyLoggers.Important,
                        @"Error when setting configuration for '{0}':{1} {2}".format_with(
                            licensedConfigBuilder.FullName,
                            Environment.NewLine,
                            ex.Message
                            ));
                }
            }
        }

        private static void set_hash_provider(ChocolateyConfiguration config, Container container)
        {
            if (!config.Features.UseFipsCompliantChecksums)
            {
                var hashprovider = container.GetInstance<IHashProvider>();
                try
                {
                    hashprovider.set_hash_algorithm(CryptoHashProviderType.Md5);
                }
                catch (Exception ex)
                {
                    if (!config.CommandName.is_equal_to("feature"))
                    {
                        if (ex.InnerException != null && ex.InnerException.Message.contains("FIPS"))
                        {
                            "chocolatey".Log().Warn(ChocolateyLoggers.Important, @"
FIPS Mode detected - run 'choco feature enable -n {0}' 
 to use Chocolatey.".format_with(ApplicationParameters.Features.UseFipsCompliantChecksums));
                            throw new ApplicationException("When FIPS Mode is enabled, Chocolatey requires {0} feature also be enabled.".format_with(ApplicationParameters.Features.UseFipsCompliantChecksums));
                        }

                        throw;
                    }
                }
            }
        }
    }
}
