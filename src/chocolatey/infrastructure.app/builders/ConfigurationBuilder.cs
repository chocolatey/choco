// Copyright © 2017 Chocolatey Software, Inc
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
            set_config_file_settings(configFileSettings, xmlService, config);
            set_hash_provider(config, container);
        }

        private static ConfigFileSettings get_config_file_settings(IFileSystem fileSystem, IXmlService xmlService)
        {
            var globalConfigPath = ApplicationParameters.GlobalConfigFileLocation;
            AssemblyFileExtractor.extract_text_file_from_assembly(fileSystem, Assembly.GetExecutingAssembly(), ApplicationParameters.ChocolateyConfigFileResource, globalConfigPath);

            return xmlService.deserialize<ConfigFileSettings>(globalConfigPath);
        }

        private static void set_config_file_settings(ConfigFileSettings configFileSettings, IXmlService xmlService, ChocolateyConfiguration config)
        {
            var shouldLogSilently = (!config.Information.IsProcessElevated || !config.Information.IsUserAdministrator);

            var globalConfigPath = ApplicationParameters.GlobalConfigFileLocation;
            // save so all updated configuration items get set to existing config
            FaultTolerance.try_catch_with_logging_exception(
                () => xmlService.serialize(configFileSettings, globalConfigPath, isSilent: shouldLogSilently),
                "Error updating '{0}'. Please ensure you have permissions to do so".format_with(globalConfigPath),
                logDebugInsteadOfError: true);
        }

        private static void add_or_remove_licensed_source(ChocolateyLicense license, ConfigFileSettings configFileSettings)
        {
            // do not enable or disable the source, in case the user has disabled it
            var addOrUpdate = license.IsValid;
            var sources = configFileSettings.Sources.or_empty_list_if_null().ToList();

            var configSource = new ConfigFileSourceSetting
            {
                Id = ApplicationParameters.ChocolateyLicensedFeedSourceName,
                Value = ApplicationParameters.ChocolateyLicensedFeedSource,
                UserName = "customer",
                Password = NugetEncryptionUtility.EncryptString(license.Id),
                Priority = 10,
                BypassProxy = false,
                AllowSelfService = false,
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

            // ensure only one licensed source - helpful when moving between licenses
            configFileSettings.Sources.RemoveWhere(s => s.Id.is_equal_to(configSource.Id) && !NugetEncryptionUtility.DecryptString(s.Password).is_equal_to(license.Id));
        }

        private static void set_file_configuration(ChocolateyConfiguration config, ConfigFileSettings configFileSettings, IFileSystem fileSystem, Action<string> notifyWarnLoggingAction)
        {
            var sources = new StringBuilder();

            var defaultSourcesInOrder = configFileSettings.Sources.Where(s => !s.Disabled).or_empty_list_if_null().ToList();
            if (configFileSettings.Sources.Any(s => s.Priority > 0))
            {
                defaultSourcesInOrder = configFileSettings.Sources.Where(s => !s.Disabled && s.Priority != 0).OrderBy(s => s.Priority).or_empty_list_if_null().ToList();
                defaultSourcesInOrder.AddRange(configFileSettings.Sources.Where(s => !s.Disabled && s.Priority == 0).or_empty_list_if_null().ToList());
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
                        Priority = source.Priority,
                        BypassProxy = source.BypassProxy,
                        AllowSelfService = source.AllowSelfService,
                    });
            }
        }

        private static void set_config_items(ChocolateyConfiguration config, ConfigFileSettings configFileSettings, IFileSystem fileSystem)
        {
            config.CacheLocation = Environment.ExpandEnvironmentVariables(set_config_item(ApplicationParameters.ConfigSettings.CacheLocation, configFileSettings, string.IsNullOrWhiteSpace(configFileSettings.CacheLocation) ? string.Empty : configFileSettings.CacheLocation, "Cache location if not TEMP folder."));
            if (string.IsNullOrWhiteSpace(config.CacheLocation)) {
                config.CacheLocation = fileSystem.get_temp_path(); // System.Environment.GetEnvironmentVariable("TEMP");
                // TEMP gets set in EnvironmentSettings, so it may already have 
                // chocolatey in the path when it installs the next package from
                // the API. 
                if(!config.CacheLocation.EndsWith("chocolatey")) {
                    config.CacheLocation = fileSystem.combine_paths(fileSystem.get_temp_path(), "chocolatey");
                }
            }

            // if it is still empty, use temp in the Chocolatey install directory.
            if (string.IsNullOrWhiteSpace(config.CacheLocation)) config.CacheLocation = fileSystem.combine_paths(ApplicationParameters.InstallLocation, "temp");
            
            var commandExecutionTimeoutSeconds = 0;
            var commandExecutionTimeout = set_config_item(ApplicationParameters.ConfigSettings.CommandExecutionTimeoutSeconds, configFileSettings, string.IsNullOrWhiteSpace(configFileSettings.CommandExecutionTimeoutSeconds.to_string()) ? ApplicationParameters.DefaultWaitForExitInSeconds.to_string() : configFileSettings.CommandExecutionTimeoutSeconds.to_string(), "Default timeout for command execution. '0' for infinite (starting in 0.10.4).");
            int.TryParse(commandExecutionTimeout, out commandExecutionTimeoutSeconds);
            config.CommandExecutionTimeoutSeconds = commandExecutionTimeoutSeconds;
            if (commandExecutionTimeout != "0" && commandExecutionTimeoutSeconds <= 0)
            {
                set_config_item(ApplicationParameters.ConfigSettings.CommandExecutionTimeoutSeconds, configFileSettings, ApplicationParameters.DefaultWaitForExitInSeconds.to_string(), "Default timeout for command execution. '0' for infinite (starting in 0.10.4).", forceSettingValue: true);
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
                set_config_item(ApplicationParameters.ConfigSettings.WebRequestTimeoutSeconds, configFileSettings, ApplicationParameters.DefaultWebRequestTimeoutInSeconds.to_string(), "Default timeout for web requests. Available in 0.9.10+.", forceSettingValue: true);
            }
            config.WebRequestTimeoutSeconds = webRequestTimeoutSeconds;

            config.ContainsLegacyPackageInstalls = set_config_item(ApplicationParameters.ConfigSettings.ContainsLegacyPackageInstalls, configFileSettings, "true", "Install has packages installed prior to 0.9.9 series.").is_equal_to(bool.TrueString);
            config.Proxy.Location = set_config_item(ApplicationParameters.ConfigSettings.Proxy, configFileSettings, string.Empty, "Explicit proxy location. Available in 0.9.9.9+.");
            config.Proxy.User = set_config_item(ApplicationParameters.ConfigSettings.ProxyUser, configFileSettings, string.Empty, "Optional proxy user. Available in 0.9.9.9+.");
            config.Proxy.EncryptedPassword = set_config_item(ApplicationParameters.ConfigSettings.ProxyPassword, configFileSettings, string.Empty, "Optional proxy password. Encrypted. Available in 0.9.9.9+.");
            config.Proxy.BypassList = set_config_item(ApplicationParameters.ConfigSettings.ProxyBypassList, configFileSettings, string.Empty, "Optional proxy bypass list. Comma separated. Available in 0.10.4+.");
            config.Proxy.BypassOnLocal = set_config_item(ApplicationParameters.ConfigSettings.ProxyBypassOnLocal, configFileSettings, "true", "Bypass proxy for local connections. Available in 0.10.4+.").is_equal_to(bool.TrueString);
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
            config.Features.ChecksumFiles = set_feature_flag(ApplicationParameters.Features.ChecksumFiles, configFileSettings, defaultEnabled: true, description: "Checksum files when pulled in from internet (based on package).");
            config.Features.AllowEmptyChecksums = set_feature_flag(ApplicationParameters.Features.AllowEmptyChecksums, configFileSettings, defaultEnabled: false, description: "Allow packages to have empty/missing checksums for downloaded resources from non-secure locations (HTTP, FTP). Enabling is not recommended if using sources that download resources from the internet. Available in 0.10.0+.");
            config.Features.AllowEmptyChecksumsSecure = set_feature_flag(ApplicationParameters.Features.AllowEmptyChecksumsSecure, configFileSettings, defaultEnabled: true, description: "Allow packages to have empty/missing checksums for downloaded resources from secure locations (HTTPS). Available in 0.10.0+.");
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
            config.Features.ShowNonElevatedWarnings = set_feature_flag(ApplicationParameters.Features.ShowNonElevatedWarnings, configFileSettings, defaultEnabled: true, description: "Show Non-Elevated Warnings - Display non-elevated warnings. Available in 0.10.4+.");
            config.Features.ShowDownloadProgress = set_feature_flag(ApplicationParameters.Features.ShowDownloadProgress, configFileSettings, defaultEnabled: true, description: "Show Download Progress - Show download progress percentages in the CLI. Available in 0.10.4+.");
            config.Features.StopOnFirstPackageFailure = set_feature_flag(ApplicationParameters.Features.StopOnFirstPackageFailure, configFileSettings, defaultEnabled: false, description: "Stop On First Package Failure - stop running install, upgrade or uninstall on first package failure instead of continuing with others. As this will affect upgrade all, it is normally recommended to leave this off. Available in 0.10.4+.");
            config.Features.UseRememberedArgumentsForUpgrades = set_feature_flag(ApplicationParameters.Features.UseRememberedArgumentsForUpgrades, configFileSettings, defaultEnabled: false, description: "Use Remembered Arguments For Upgrades - when running upgrades, use arguments for upgrade that were used for installation ('remembered'). This is helpful when running upgrade for all packages. Available in 0.10.4+. This is considered in preview for 0.10.4 and will be flipped to on by default in a future release.");
            config.Features.ScriptsCheckLastExitCode = set_feature_flag(ApplicationParameters.Features.ScriptsCheckLastExitCode, configFileSettings, defaultEnabled: false, description: "Scripts Check $LastExitCode (external commands) - Leave this off unless you absolutely need it while you fix your package scripts  to use `throw 'error message'` or `Set-PowerShellExitCode #` instead of `exit #`. This behavior started in 0.9.10 and produced hard to find bugs. If the last external process exits successfully but with an exit code of not zero, this could cause hard to detect package failures. Available in 0.10.3+. Will be removed in 0.11.0.");
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
                             "Debug - Show debug messaging.",
                             option => config.Debug = option != null)
                        .Add("v|verbose",
                             "Verbose - Show verbose messaging. Very verbose messaging, avoid using under normal circumstances.",
                             option => config.Verbose = option != null)
                        .Add("trace",
                             "Trace - Show trace messaging. Very, very verbose trace messaging. Avoid except when needing super low-level .NET Framework debugging. Available in 0.10.4+.",
                             option => config.Trace = option != null)
                        .Add("acceptlicense|accept-license",
                             "AcceptLicense - Accept license dialogs automatically. Reserved for future use.",
                             option => config.AcceptLicense = option != null)
                        .Add("y|yes|confirm",
                             "Confirm all prompts - Chooses affirmative answer instead of prompting. Implies --accept-license",
                             option =>
                             {
                                 config.PromptForConfirmation = option == null;
                                 config.AcceptLicense = option != null;
                             })
                        .Add("f|force",
                             "Force - force the behavior. Do not use force during normal operation - it subverts some of the smart behavior for commands.",
                             option => config.Force = option != null)
                        .Add("noop|whatif|what-if",
                             "NoOp / WhatIf - Don't actually do anything.",
                             option => config.Noop = option != null)
                        .Add("r|limitoutput|limit-output",
                             "LimitOutput - Limit the output to essential information",
                             option => config.RegularOutput = option == null)
                        .Add("timeout=|execution-timeout=",
                             "CommandExecutionTimeout (in seconds) - The time to allow a command to finish before timing out. Overrides the default execution timeout in the configuration of {0} seconds. '0' for infinite starting in 0.10.4.".format_with(config.CommandExecutionTimeoutSeconds.to_string()),
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
                             "UseSystemPowerShell - Execute PowerShell using an external process instead of the built-in PowerShell host. Should only be used when internal host is failing. Available in 0.9.10+.",
                             option => config.Features.UsePowerShellHost = option == null)
                        .Add("no-progress",
                             "Do Not Show Progress - Do not show download progress percentages. Available in 0.10.4+.",
                             option => config.Features.ShowDownloadProgress = option == null)
                        .Add("proxy=",
                            "Proxy Location - Explicit proxy location. Overrides the default proxy location of '{0}'. Available for config settings in 0.9.9.9+, this CLI option available in 0.10.4+.".format_with(config.Proxy.Location),
                            option => config.Proxy.Location = option.remove_surrounding_quotes())
                        .Add("proxy-user=",
                            "Proxy User Name - Explicit proxy user (optional). Requires explicity proxy (`--proxy` or config setting). Overrides the default proxy user of '{0}'. Available for config settings in 0.9.9.9+, this CLI option available in 0.10.4+.".format_with(config.Proxy.User),
                            option => config.Proxy.User = option.remove_surrounding_quotes())
                        .Add("proxy-password=",
                            "Proxy Password - Explicit proxy password (optional) to be used with username. Requires explicity proxy (`--proxy` or config setting) and user name.  Overrides the default proxy password (encrypted in settings if set). Available for config settings in 0.9.9.9+, this CLI option available in 0.10.4+.",
                            option => config.Proxy.EncryptedPassword = NugetEncryptionUtility.EncryptString(option.remove_surrounding_quotes()))
                        .Add("proxy-bypass-list=",
                             "ProxyBypassList - Comma separated list of regex locations to bypass on proxy. Requires explicity proxy (`--proxy` or config setting). Overrides the default proxy bypass list of '{0}'. Available in 0.10.4+.".format_with(config.Proxy.BypassList),
                             option => config.Proxy.BypassList = option.remove_surrounding_quotes())
                        .Add("proxy-bypass-on-local",
                             "Proxy Bypass On Local - Bypass proxy for local connections. Requires explicity proxy (`--proxy` or config setting). Overrides the default proxy bypass on local setting of '{0}'. Available in 0.10.4+.".format_with(config.Proxy.BypassOnLocal),
                             option => config.Proxy.BypassOnLocal = option != null)
                        ;
                },
                (unparsedArgs) =>
                {
                    if (!string.IsNullOrWhiteSpace(config.CommandName))
                    {
                        // save help for next menu
                        config.HelpRequested = false;
                        config.UnsuccessfulParsing = false;
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
 * **Periods in PowerShell**: If you need to pass a period as part of a 
   value or a path, PowerShell doesn't always handle it well. Please 
   quote those values using ""Quote Values"" section above.
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
            config.Information.Is64BitOperatingSystem = Environment.Is64BitOperatingSystem;
            config.Information.Is64BitProcess = (IntPtr.Size == 8);
            config.Information.IsInteractive = Environment.UserInteractive;
            config.Information.IsUserAdministrator = ProcessInformation.user_is_administrator();
            config.Information.IsProcessElevated = ProcessInformation.process_is_elevated();

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("https_proxy")) && string.IsNullOrWhiteSpace(config.Proxy.Location))
            {
                config.Proxy.Location = Environment.GetEnvironmentVariable("https_proxy");
            }

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("http_proxy")) && string.IsNullOrWhiteSpace(config.Proxy.Location))
            {
                config.Proxy.Location = Environment.GetEnvironmentVariable("http_proxy");
            }

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("no_proxy")) && string.IsNullOrWhiteSpace(config.Proxy.BypassList))
            {
                config.Proxy.BypassList = Environment.GetEnvironmentVariable("no_proxy");
            }
        }

        private static void set_licensed_options(ChocolateyConfiguration config, ChocolateyLicense license, ConfigFileSettings configFileSettings)
        {
            config.Information.IsLicensedVersion = license.is_licensed_version();
            config.Information.LicenseType = license.LicenseType.get_description_or_value();

            if (license.AssemblyLoaded)
            {
                Type licensedConfigBuilder = license.Assembly.GetType(ApplicationParameters.LicensedConfigurationBuilder, throwOnError: false, ignoreCase: true);

                if (licensedConfigBuilder == null)
                {
                    if (config.RegularOutput) "chocolatey".Log().Warn(ChocolateyLoggers.Important,
                        @"Unable to set licensed configuration. Please upgrade to a newer 
 licensed version (choco upgrade chocolatey.extension).");
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
                    var isDebug = ApplicationParameters.is_debug_mode_cli_primitive();
                    if (config.Debug) isDebug = true;
                    var message = isDebug ? ex.ToString() : ex.Message;

                    if (isDebug && ex.InnerException != null)
                    {
                        message += "{0}{1}".format_with(Environment.NewLine, ex.ToString());
                    }

                    "chocolatey".Log().Error(
                        ChocolateyLoggers.Important,
                        @"Error when setting configuration for '{0}':{1} {2}".format_with(
                            licensedConfigBuilder.FullName,
                            Environment.NewLine,
                            message
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

                            var errorMessage = "When FIPS Mode is enabled, Chocolatey requires {0} feature also be enabled.".format_with(ApplicationParameters.Features.UseFipsCompliantChecksums);
                            if (string.IsNullOrWhiteSpace(config.CommandName))
                            {
                                "chocolatey".Log().Error(errorMessage);
                                return;
                            }

                            throw new ApplicationException(errorMessage);
                        }

                        throw;
                    }
                }
            }
        }
    }
}
