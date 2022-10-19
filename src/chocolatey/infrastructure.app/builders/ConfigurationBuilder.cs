// Copyright © 2017 - 2022 Chocolatey Software, Inc
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
    using chocolatey.infrastructure.app.commands;
    using configuration;
    using cryptography;
    using extractors;
    using filesystem;
    using information;
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

        public static bool is_compatibility_checks_disabled(IFileSystem filesystem, IXmlService xmlService)
        {
            var config = get_config_file_settings(filesystem, xmlService);

            var feature = config.Features.FirstOrDefault(f => f.Name.is_equal_to("disableCompatibilityChecks"));

            return feature != null && feature.Enabled;
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
                logDebugInsteadOfError: true,
                isSilent: shouldLogSilently);
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
                VisibleToAdminsOnly = false,
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
            set_sources_in_priority_order(config, configFileSettings);
            set_machine_sources(config, configFileSettings);
            set_config_items(config, configFileSettings, fileSystem);

            FaultTolerance.try_catch_with_logging_exception(
                () => fileSystem.create_directory_if_not_exists(config.CacheLocation),
                "Could not create cache location / temp directory at '{0}'".format_with(config.CacheLocation),
                logWarningInsteadOfError: true);

            set_feature_flags(config, configFileSettings);
        }

        private static void set_sources_in_priority_order(ChocolateyConfiguration config, ConfigFileSettings configFileSettings)
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
        }

        private static void set_machine_sources(ChocolateyConfiguration config, ConfigFileSettings configFileSettings)
        {
            var defaultSourcesInOrder = configFileSettings.Sources.Where(s => !s.Disabled).or_empty_list_if_null().ToList();
            if (configFileSettings.Sources.Any(s => s.Priority > 0))
            {
                defaultSourcesInOrder = configFileSettings.Sources.Where(s => !s.Disabled && s.Priority != 0).OrderBy(s => s.Priority).or_empty_list_if_null().ToList();
                defaultSourcesInOrder.AddRange(configFileSettings.Sources.Where(s => !s.Disabled && s.Priority == 0).or_empty_list_if_null().ToList());
            }

            foreach (var source in defaultSourcesInOrder)
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
                    VisibleToAdminsOnly = source.VisibleToAdminsOnly
                });
            }
        }

        private static void set_config_items(ChocolateyConfiguration config, ConfigFileSettings configFileSettings, IFileSystem fileSystem)
        {
            config.CacheLocation = Environment.ExpandEnvironmentVariables(set_config_item(ApplicationParameters.ConfigSettings.CacheLocation, configFileSettings, string.IsNullOrWhiteSpace(configFileSettings.CacheLocation) ? string.Empty : configFileSettings.CacheLocation, "Cache location if not TEMP folder. Replaces `$env:TEMP` value for choco.exe process. It is highly recommended this be set to make Chocolatey more deterministic in cleanup."));
            if (string.IsNullOrWhiteSpace(config.CacheLocation))
            {
                config.CacheLocation = fileSystem.get_temp_path(); // System.Environment.GetEnvironmentVariable("TEMP");
                // TEMP gets set in EnvironmentSettings, so it may already have
                // chocolatey in the path when it installs the next package from
                // the API.
                if (!string.Equals(fileSystem.get_directory_info_for(config.CacheLocation).Name, "chocolatey", StringComparison.OrdinalIgnoreCase))
                {
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
                set_config_item(ApplicationParameters.ConfigSettings.CommandExecutionTimeoutSeconds, configFileSettings, ApplicationParameters.DefaultWaitForExitInSeconds.to_string(), "Default timeout for command execution. '0' for infinite (starting in 0.10.4). It is recommended that organizations bump this up to at least 4 hours (14400).", forceSettingValue: true);
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
            config.UpgradeCommand.PackageNamesToSkip = set_config_item(ApplicationParameters.ConfigSettings.UpgradeAllExceptions, configFileSettings, string.Empty, "A comma-separated list of package names that should not be upgraded when running `choco upgrade all'. Defaults to empty. Available in 0.10.14+.");
            config.DefaultTemplateName = set_config_item(ApplicationParameters.ConfigSettings.DefaultTemplateName, configFileSettings, string.Empty, "Default template name used when running 'choco new' command. Available in 0.12.0+.");
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
            config.Features.FailOnStandardError = set_feature_flag(ApplicationParameters.Features.FailOnStandardError, configFileSettings, defaultEnabled: false, description: "Fail if install provider writes to stderr. Not recommended for use. Available in 0.9.10+.");
            config.Features.UsePowerShellHost = set_feature_flag(ApplicationParameters.Features.UsePowerShellHost, configFileSettings, defaultEnabled: true, description: "Use Chocolatey's built-in PowerShell host. Available in 0.9.10+.");
            config.Features.LogEnvironmentValues = set_feature_flag(ApplicationParameters.Features.LogEnvironmentValues, configFileSettings, defaultEnabled: false, description: "Log Environment Values - will log values of environment before and after install (could disclose sensitive data). Available in 0.9.10+.");
            config.Features.VirusCheck = set_feature_flag(ApplicationParameters.Features.VirusCheck, configFileSettings, defaultEnabled: false, description: "Virus Check - perform virus checking on downloaded files. Available in 0.9.10+. Licensed versions only.");
            config.Features.FailOnInvalidOrMissingLicense = set_feature_flag(ApplicationParameters.Features.FailOnInvalidOrMissingLicense, configFileSettings, defaultEnabled: false, description: "Fail On Invalid Or Missing License - allows knowing when a license is expired or not applied to a machine. Available in 0.9.10+.");
            config.Features.IgnoreInvalidOptionsSwitches = set_feature_flag(ApplicationParameters.Features.IgnoreInvalidOptionsSwitches, configFileSettings, defaultEnabled: true, description: "Ignore Invalid Options/Switches - If a switch or option is passed that is not recognized, should choco fail? Available in 0.9.10+.");
            config.Features.UsePackageExitCodes = set_feature_flag(ApplicationParameters.Features.UsePackageExitCodes, configFileSettings, defaultEnabled: true, description: "Use Package Exit Codes - Package scripts can provide exit codes. With this on, package exit codes will be what choco uses for exit when non-zero (this value can come from a dependency package). Chocolatey defines valid exit codes as 0, 1605, 1614, 1641, 3010. With this feature off, choco will exit with 0, 1, or -1 (matching previous behavior). Available in 0.9.10+.");
            config.Features.UseEnhancedExitCodes = set_feature_flag(ApplicationParameters.Features.UseEnhancedExitCodes, configFileSettings, defaultEnabled: false, description: "Use Enhanced Exit Codes - Chocolatey is able to provide enhanced exit codes surrounding list, search, info, outdated and other commands that don't deal directly with package operations. To see enhanced exit codes and their meanings, please run `choco [cmdname] -?`. With this feature off, choco will exit with 0, 1, or -1  (matching previous behavior). Available in 0.10.12+.");
            config.Features.ExitOnRebootDetected = set_feature_flag(ApplicationParameters.Features.ExitOnRebootDetected, configFileSettings, defaultEnabled: false, description: "Exit On Reboot Detected - Stop running install, upgrade, or uninstall when a reboot request is detected. Requires '{0}' feature to be turned on. Will exit with either {1} or {2}. When it exits with {1}, it means pending reboot discovered prior to running operation. When it exits with {2}, it means some work completed prior to reboot request being detected. Available in 0.10.12+.".format_with(ApplicationParameters.Features.UsePackageExitCodes, ApplicationParameters.ExitCodes.ErrorFailNoActionReboot, ApplicationParameters.ExitCodes.ErrorInstallSuspend));
            config.Features.UseFipsCompliantChecksums = set_feature_flag(ApplicationParameters.Features.UseFipsCompliantChecksums, configFileSettings, defaultEnabled: false, description: "Use FIPS Compliant Checksums - Ensure checksumming done by choco uses FIPS compliant algorithms. Not recommended unless required by FIPS Mode. Enabling on an existing installation could have unintended consequences related to upgrades/uninstalls. Available in 0.9.10+.");
            config.Features.ShowNonElevatedWarnings = set_feature_flag(ApplicationParameters.Features.ShowNonElevatedWarnings, configFileSettings, defaultEnabled: true, description: "Show Non-Elevated Warnings - Display non-elevated warnings. Available in 0.10.4+.");
            config.Features.ShowDownloadProgress = set_feature_flag(ApplicationParameters.Features.ShowDownloadProgress, configFileSettings, defaultEnabled: true, description: "Show Download Progress - Show download progress percentages in the CLI. Available in 0.10.4+.");
            config.Features.StopOnFirstPackageFailure = set_feature_flag(ApplicationParameters.Features.StopOnFirstPackageFailure, configFileSettings, defaultEnabled: false, description: "Stop On First Package Failure - Stop running install, upgrade or uninstall on first package failure instead of continuing with others. As this will affect upgrade all, it is normally recommended to leave this off. Available in 0.10.4+.");
            config.Features.UseRememberedArgumentsForUpgrades = set_feature_flag(ApplicationParameters.Features.UseRememberedArgumentsForUpgrades, configFileSettings, defaultEnabled: false, description: "Use Remembered Arguments For Upgrades - When running upgrades, use arguments for upgrade that were used for installation ('remembered'). This is helpful when running upgrade for all packages. Available in 0.10.4+. This is considered in preview for 0.10.4 and will be flipped to on by default in a future release.");
            config.Features.IgnoreUnfoundPackagesOnUpgradeOutdated = set_feature_flag(ApplicationParameters.Features.IgnoreUnfoundPackagesOnUpgradeOutdated, configFileSettings, defaultEnabled: false, description: "Ignore Unfound Packages On Upgrade Outdated - When checking outdated or upgrades, if a package is not found against sources specified, don't report the package at all. Available in 0.10.9+.");
            config.Features.SkipPackageUpgradesWhenNotInstalled = set_feature_flag(ApplicationParameters.Features.SkipPackageUpgradesWhenNotInstalled, configFileSettings, defaultEnabled: false, description: "Skip Packages Not Installed During Upgrade - if a package is not installed, do not install it during the upgrade process. Available in 0.10.12+.");
            config.Features.RemovePackageInformationOnUninstall = set_feature_flag(ApplicationParameters.Features.RemovePackageInformationOnUninstall, configFileSettings, defaultEnabled: false, description: "Remove Stored Package Information On Uninstall - When a package is uninstalled, should the stored package information also be removed?  Available in 0.10.9+.");
            config.Features.LogWithoutColor = set_feature_flag(ApplicationParameters.Features.LogWithoutColor, configFileSettings, defaultEnabled: false, description: "Log without color - Do not show colorization in logging output. Available in 0.10.9+.");
            config.Features.LogValidationResultsOnWarnings = set_feature_flag(ApplicationParameters.Features.LogValidationResultsOnWarnings, configFileSettings, defaultEnabled: true, description: "Log validation results on warnings - Should the validation results be logged if there are warnings? Available in 0.10.12+.");
            config.Features.UsePackageRepositoryOptimizations = set_feature_flag(ApplicationParameters.Features.UsePackageRepositoryOptimizations, configFileSettings, defaultEnabled: true, description: "Use Package Repository Optimizations - Turn on optimizations for reducing bandwidth with repository queries during package install/upgrade/outdated operations. Should generally be left enabled, unless a repository needs to support older methods of query. When disabled, this makes queries similar to the way they were done in Chocolatey v0.10.11 and before. Available in 0.10.14+.");
            config.PromptForConfirmation = !set_feature_flag(ApplicationParameters.Features.AllowGlobalConfirmation, configFileSettings, defaultEnabled: false, description: "Prompt for confirmation in scripts or bypass.");
            config.DisableCompatibilityChecks = set_feature_flag(ApplicationParameters.Features.DisableCompatibilityChecks, configFileSettings, defaultEnabled: false, description: "Disable Compatibility Checks - Should a warning we shown, before and after command execution, when a runtime compatibility check determines that there is an incompatibility between Chocolatey and Chocolatey Licensed Extension. Available in 1.1.0+");
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
                        .Add("nocolor|no-color",
                             "No Color - Do not show colorization in logging output. This overrides the feature '{0}', set to '{1}'. Available in 0.10.9+.".format_with(ApplicationParameters.Features.LogWithoutColor, config.Features.LogWithoutColor),
                             option => config.Features.LogWithoutColor = option != null)
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
                                var timeoutString = option.remove_surrounding_quotes();
                                int.TryParse(timeoutString, out timeout);
                                if (timeout > 0 || timeoutString.is_equal_to("0"))
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
                            "Proxy User Name - Explicit proxy user (optional). Requires explicit proxy (`--proxy` or config setting). Overrides the default proxy user of '{0}'. Available for config settings in 0.9.9.9+, this CLI option available in 0.10.4+.".format_with(config.Proxy.User),
                            option => config.Proxy.User = option.remove_surrounding_quotes())
                        .Add("proxy-password=",
                            "Proxy Password - Explicit proxy password (optional) to be used with username. Requires explicit proxy (`--proxy` or config setting) and user name.  Overrides the default proxy password (encrypted in settings if set). Available for config settings in 0.9.9.9+, this CLI option available in 0.10.4+.",
                            option => config.Proxy.EncryptedPassword = NugetEncryptionUtility.EncryptString(option.remove_surrounding_quotes()))
                        .Add("proxy-bypass-list=",
                             "ProxyBypassList - Comma separated list of regex locations to bypass on proxy. Requires explicit proxy (`--proxy` or config setting). Overrides the default proxy bypass list of '{0}'. Available in 0.10.4+.".format_with(config.Proxy.BypassList),
                             option => config.Proxy.BypassList = option.remove_surrounding_quotes())
                        .Add("proxy-bypass-on-local",
                             "Proxy Bypass On Local - Bypass proxy for local connections. Requires explicit proxy (`--proxy` or config setting). Overrides the default proxy bypass on local setting of '{0}'. Available in 0.10.4+.".format_with(config.Proxy.BypassOnLocal),
                             option => config.Proxy.BypassOnLocal = option != null)
                         .Add("log-file=",
                             "Log File to output to in addition to regular loggers. Available in 0.10.8+.",
                             option => config.AdditionalLogFileLocation = option.remove_surrounding_quotes())
                        .Add("skipcompatibilitychecks|skip-compatibility-checks",
                            "SkipCompatibilityChecks - Prevent warnings being shown before and after command execution when a runtime compatibility problem is found between the version of Chocolatey and the Chocolatey Licensed Extension. Available in 1.1.0+",
                            option => config.DisableCompatibilityChecks = option != null)
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
                    ChocolateyHelpCommand.display_help_message(container);
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
            config.Information.Is64BitProcess = Environment.Is64BitProcess;
            config.Information.IsInteractive = Environment.UserInteractive;
            config.Information.UserName = System.Environment.UserName;
            config.Information.UserDomainName = System.Environment.UserDomainName;
            config.Information.CurrentDirectory = Environment.CurrentDirectory;
            config.Information.IsUserAdministrator = ProcessInformation.user_is_administrator();
            config.Information.IsUserSystemAccount = ProcessInformation.user_is_system();
            config.Information.IsUserRemoteDesktop = ProcessInformation.user_is_terminal_services();
            config.Information.IsUserRemote = ProcessInformation.user_is_remote();
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
                        message += "{0}{1}".format_with(Environment.NewLine, ex.InnerException.ToString());
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
