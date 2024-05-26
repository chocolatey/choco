// Copyright © 2017 - 2023 Chocolatey Software, Inc
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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using chocolatey.infrastructure.app.domain;
using chocolatey.infrastructure.logging;
using chocolatey.infrastructure.platforms;

namespace chocolatey.infrastructure.app.configuration
{
    /// <summary>
    ///   The chocolatey configuration.
    /// </summary>
    [Serializable]
    public class ChocolateyConfiguration
    {
        [NonSerialized]
        private Stack<ChocolateyConfiguration> _configurationBackups;

        public ChocolateyConfiguration()
        {
            RegularOutput = true;
            PromptForConfirmation = true;
            DisableCompatibilityChecks = false;
            CacheExpirationInMinutes = 30;
            SourceType = SourceTypes.Normal;
            Information = new InformationCommandConfiguration();
            Features = new FeaturesConfiguration();
            NewCommand = new NewCommandConfiguration();
            ListCommand = new ListCommandConfiguration();
            UpgradeCommand = new UpgradeCommandConfiguration();
            SourceCommand = new SourcesCommandConfiguration();
            MachineSources = new List<MachineSourceConfiguration>();
            FeatureCommand = new FeatureCommandConfiguration();
            ConfigCommand = new ConfigCommandConfiguration();
            ApiKeyCommand = new ApiKeyCommandConfiguration();
            PackCommand = new PackCommandConfiguration();
            PushCommand = new PushCommandConfiguration();
            PinCommand = new PinCommandConfiguration();
            OutdatedCommand = new OutdatedCommandConfiguration();
            Proxy = new ProxyConfiguration();
            ExportCommand = new ExportCommandConfiguration();
            TemplateCommand = new TemplateCommandConfiguration();
            CacheCommand = new CacheCommandConfiguration();
            RuleCommand = new RuleCommandConfiguration();
#if DEBUG
            AllowUnofficialBuild = true;
#endif
        }

        /// <summary>
        /// Creates a backup of the current version of the configuration class.
        /// </summary>
        /// <exception cref="System.Runtime.Serialization.SerializationException">One or more objects in the class or child classes are not serializable.</exception>
        public void CreateBackup()
        {
            if (_configurationBackups == null)
            {
                _configurationBackups = new Stack<ChocolateyConfiguration>();
            }

            // We do this the easy way to ensure that we have a clean copy
            // of the original configuration file.
            _configurationBackups.Push(this.DeepCopy());
        }

        /// <summary>
        /// Restore the backup that has previously been created to the initial
        /// state, without making the class reference types the same to prevent
        /// the initial configuration class being updated at the same time if a
        /// value changes.
        /// </summary>
        /// <param name="removeBackup">Whether a backup that was previously made should be removed after resetting the configuration.</param>
        /// <exception cref="InvalidOperationException">No backup has been created before trying to reset the current configuration, and removal of the backup was not requested.</exception>
        /// <remarks>
        /// This call may make quite a lot of allocations on the Gen0 heap, as such
        /// it is best to keep the calls to this method at a minimum.
        /// </remarks>
        public void RevertChanges(bool removeBackup = false)
        {
            if (_configurationBackups == null || _configurationBackups.Count == 0)
            {
                if (removeBackup)
                {
                    // If we will also be removing the backup, we do not care if it is already
                    // null or not, as that is the intended state when this method returns.
                    this.Log().Debug("Requested removal of a configuration backup that does not exist: the backup stack is empty.");
                    return;
                }

                throw new InvalidOperationException("No backup has been created before trying to reset the current configuration, and removal of the backup was not requested.");
            }

            // Runtime type lookup ensures this also fully works with derived classes (for example: licensed configuration)
            // without needing to re-implement this method / make it overridable.
            var t = GetType();

            var backup = removeBackup ? _configurationBackups.Pop() : _configurationBackups.Peek();

            foreach (var property in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.Name == "_configurationBackups")
                {
                    continue;
                }

                try
                {
                    if (property.Name == "PromptForConfirmation")
                    {
                        // We do not overwrite this value between backups as it is intended to be a global setting;
                        // if a user has selected a "[A] yes to all" prompt interactively, this option is
                        // set and should be retained for the duration of the operations.
                        continue;
                    }

                    var originalValue = property.GetValue(backup, new object[0]);

                    if (removeBackup || property.DeclaringType.IsPrimitive || property.DeclaringType.IsValueType || property.DeclaringType == typeof(string))
                    {
                        // If the property is a primitive, a value type or a string, then a copy of the value
                        // will be created by the .NET Runtime automatically, and we do not need to create a deep clone of the value.
                        // Additionally, if we will be removing the backup there is no need to create a deep copy
                        // for any reference types. We won't have any duplicate references because the backup is being discarded.
                        property.SetValue(this, originalValue, new object[0]);
                    }
                    else if (originalValue != null)
                    {
                        // We need to do a deep copy of the value so it won't copy the reference itself,
                        // but rather the actual values we are interested in.
                        property.SetValue(this, originalValue.DeepCopy(), new object[0]);
                    }
                    else
                    {
                        property.SetValue(this, null, new object[0]);
                    }
                }
                catch (Exception ex)
                {
                    throw new ApplicationException("Unable to restore the value for the property '{0}'.".FormatWith(property.Name), ex);
                }
            }
        }

        // overrides
        public override string ToString()
        {
            var properties = new StringBuilder();

            this.Log().Debug(ChocolateyLoggers.Important, @"
NOTE: Hiding sensitive configuration data! Please double and triple
 check to be sure no sensitive data is shown, especially if copying
 output to a gist for review.");
            OutputToString(properties, GetType().GetProperties(), this, "");
            return properties.ToString();
        }

        private void OutputToString(StringBuilder propertyValues, IEnumerable<PropertyInfo> properties, object obj, string prepend)
        {
            foreach (var propertyInfo in properties.OrEmpty())
            {
                // skip sensitive data info
                if (propertyInfo.Name.ContainsSafe("password") || propertyInfo.Name.ContainsSafe("sensitive") || propertyInfo.Name == "Key" || propertyInfo.Name == "ConfigValue" || propertyInfo.Name == "MachineSources")
                {
                    continue;
                }

                var objectValue = propertyInfo.GetValue(obj, null);
                if (propertyInfo.PropertyType.IsBuiltinType())
                {
                    if (!string.IsNullOrWhiteSpace(objectValue.ToStringSafe()))
                    {
                        var output = "{0}{1}='{2}'|".FormatWith(
                            string.IsNullOrWhiteSpace(prepend) ? "" : prepend + ".",
                            propertyInfo.Name,
                            objectValue.ToStringSafe());

                        AppendOutput(propertyValues, output);
                    }
                }
                else if (propertyInfo.PropertyType.IsCollectionType())
                {
                    var list = objectValue as IDictionary<string, string>;
                    foreach (var item in list.OrEmpty())
                    {
                        var output = "{0}{1}.{2}='{3}'|".FormatWith(
                            string.IsNullOrWhiteSpace(prepend) ? "" : prepend + ".",
                            propertyInfo.Name,
                            item.Key,
                            item.Value);

                        AppendOutput(propertyValues, output);
                    }
                }
                else if (objectValue is null)
                {
                    var output = "{0}{1}={{null}}".FormatWith(
                        string.IsNullOrWhiteSpace(prepend) ? "" : prepend + ".",
                        propertyInfo.Name);

                    AppendOutput(propertyValues, output);
                }
                else
                {
                    OutputToString(propertyValues, propertyInfo.PropertyType.GetProperties(), objectValue, propertyInfo.Name);
                }
            }
        }

        private const int MaxConsoleLineLength = 72;
        private int _currentLineLength = 0;

        private void AppendOutput(StringBuilder propertyValues, string append)
        {
            _currentLineLength += append.Length;

            propertyValues.AppendFormat("{0}{1}{2}",
                   _currentLineLength < MaxConsoleLineLength ? string.Empty : Environment.NewLine,
                   append,
                   append.Length < MaxConsoleLineLength ? string.Empty : Environment.NewLine);

            if (_currentLineLength > MaxConsoleLineLength)
            {
                _currentLineLength = append.Length;
            }
        }

        /// <summary>
        ///   Gets or sets the name of the command.
        ///   This is the command that choco runs.
        /// </summary>
        /// <value>
        ///   The name of the command.
        /// </value>
        public string CommandName { get; set; }

        // configuration set variables
        public string CacheLocation { get; set; }

        public int CommandExecutionTimeoutSeconds { get; set; }
        public int WebRequestTimeoutSeconds { get; set; }
        public string DefaultTemplateName { get; set; }

        /// <summary>
        ///   One or more source locations set by configuration or by command line. Separated by semi-colon
        /// </summary>
        public string Sources { get; set; }

        public string SourceType { get; set; }
        public bool IncludeConfiguredSources { get; set; }

        // top level commands

        public bool ShowOnlineHelp { get; set; }
        public bool Debug { get; set; }
        public bool Verbose { get; set; }
        public bool Trace { get; set; }
        public bool Force { get; set; }
        public bool Noop { get; set; }
        public bool HelpRequested { get; set; }

        /// <summary>
        ///   Gets or sets a value indicating whether parsing was successful (everything parsed) or not.
        /// </summary>
        public bool UnsuccessfulParsing { get; set; }

        // todo: #2564 Should look into using mutually exclusive output levels - Debug, Info (Regular), Error (Quiet)
        // Verbose and Important are not part of the levels at all
        /// <summary>
        /// Gets or sets a value indicating whether output should be limited.
        /// This supports the --limit-output parameter.
        /// </summary>
        /// <value><c>true</c> for regular output; <c>false</c> for limited output.</value>
        public bool RegularOutput { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether console logging should be suppressed.
        /// This is for use by API calls which surface results in alternate forms.
        /// </summary>
        /// <value><c>true</c> for no output; <c>false</c> for regular or limited output.</value>
        /// <remarks>This has only been implemented for NuGet List</remarks>
        public bool QuietOutput { get; set; }

        public bool PromptForConfirmation { get; set; }

        /// <summary>
        /// Gets or set a value indicating whether runtime Chocolatey compatibility checks
        /// should be completed or not. Overriding this value is only expected on systems
        /// where the user is explicitly opting out of these checks, for example, when
        /// they are running a perpetual license, where the version is known to not be
        /// compliant, but also that most things "should" work.
        /// </summary>
        public bool DisableCompatibilityChecks { get; set; }

        public bool AcceptLicense { get; set; }
        public bool AllowUnofficialBuild { get; set; }
        public string AdditionalLogFileLocation { get; set; }

        /// <summary>
        ///   Usually related to unparsed arguments.
        /// </summary>
        public string Input { get; set; }

        // command level options
        public string Version { get; set; }

        public bool AllVersions { get; set; }
        public bool SkipPackageInstallProvider { get; set; }
        public string OutputDirectory { get; set; }
        public bool SkipHookScripts { get; set; }

        // install/update
        /// <summary>
        ///   Gets or sets the package names. Space separated
        /// </summary>
        /// <value>
        ///   Space separated package names.
        /// </value>
        public string PackageNames { get; set; }

        public bool Prerelease { get; set; }
        public bool ForceX86 { get; set; }
        public string InstallArguments { get; set; }
        public bool OverrideArguments { get; set; }
        public bool NotSilent { get; set; }
        public string PackageParameters { get; set; }
        public bool ApplyPackageParametersToDependencies { get; set; }
        public bool ApplyInstallArgumentsToDependencies { get; set; }
        public bool IgnoreDependencies { get; set; }

        /// <summary>
        /// Gets or sets the time before the cache is considered to have expired in minutes.
        /// </summary>
        /// <value>
        /// The cache expiration in minutes.
        /// </value>
        /// <remarks>specifying a negative number disables the caching completely.</remarks>
        public int CacheExpirationInMinutes { get; set; }

        public bool AllowDowngrade { get; set; }
        public bool ForceDependencies { get; set; }
        public string DownloadChecksum { get; set; }
        public string DownloadChecksum64 { get; set; }
        public string DownloadChecksumType { get; set; }
        public string DownloadChecksumType64 { get; set; }
        public bool PinPackage { get; set; }

        /// <summary>
        ///   Configuration values provided by choco.
        /// </summary>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        public InformationCommandConfiguration Information { get; set; }

        /// <summary>
        ///   Configuration related to features and whether they are enabled.
        /// </summary>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        public FeaturesConfiguration Features { get; set; }

        /// <summary>
        ///   Configuration related specifically to List command
        /// </summary>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        public ListCommandConfiguration ListCommand { get; set; }

        /// <summary>
        ///   Configuration related specifically to Upgrade command
        /// </summary>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        public UpgradeCommandConfiguration UpgradeCommand { get; set; }

        /// <summary>
        ///   Configuration related specifically to New command
        /// </summary>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        public NewCommandConfiguration NewCommand { get; set; }

        /// <summary>
        ///   Configuration related specifically to Source command
        /// </summary>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        public SourcesCommandConfiguration SourceCommand { get; set; }

        /// <summary>
        ///   Default Machine Sources Configuration
        /// </summary>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        public IList<MachineSourceConfiguration> MachineSources { get; set; }

        /// <summary>
        /// Configuration related specifically to the Feature command
        /// </summary>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        public FeatureCommandConfiguration FeatureCommand { get; set; }

        /// <summary>
        /// Configuration related to the configuration file.
        /// </summary>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        public ConfigCommandConfiguration ConfigCommand { get; set; }

        /// <summary>
        ///   Configuration related specifically to ApiKey command
        /// </summary>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        public ApiKeyCommandConfiguration ApiKeyCommand { get; set; }

        /// <summary>
        ///   Configuration related specifically to the Pack command.
        /// </summary>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        public PackCommandConfiguration PackCommand { get; set; }

        /// <summary>
        ///   Configuration related specifically to Push command
        /// </summary>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        public PushCommandConfiguration PushCommand { get; set; }

        /// <summary>
        /// Configuration related specifically to Pin command
        /// </summary>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        public PinCommandConfiguration PinCommand { get; set; }

        /// <summary>
        /// Configuration related specifically to Outdated command
        /// </summary>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        public OutdatedCommandConfiguration OutdatedCommand { get; set; }

        public ExportCommandConfiguration ExportCommand { get; set; }

        /// <summary>
        /// Configuration related specifically to proxies.
        /// </summary>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        public ProxyConfiguration Proxy { get; set; }

        /// <summary>
        ///   Configuration related specifically to Template command
        /// </summary>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        public TemplateCommandConfiguration TemplateCommand { get; set; }

        /// <summary>
        /// Gets or sets the configuration related specifically to the Cache command.
        /// </summary>
        public CacheCommandConfiguration CacheCommand { get; set; }

        /// <summary>
        /// Gets or sets the configuration related specifically to the Rule command.
        /// </summary>
        public RuleCommandConfiguration RuleCommand { get; set; }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void start_backup()
            => CreateBackup();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void reset_config(bool removeBackup = false)
            => RevertChanges(removeBackup);
#pragma warning restore IDE0022, IDE1006
    }

    [Serializable]
    public sealed class InformationCommandConfiguration
    {
        // application set variables
        public PlatformType PlatformType { get; set; }

        public Version PlatformVersion { get; set; }
        public string PlatformName { get; set; }
        public string ChocolateyVersion { get; set; }
        public string ChocolateyProductVersion { get; set; }
        public string FullName { get; set; }
        public bool Is64BitOperatingSystem { get; set; }
        public bool Is64BitProcess { get; set; }
        public bool IsInteractive { get; set; }
        public string UserName { get; set; }
        public string UserDomainName { get; set; }
        public bool IsUserAdministrator { get; set; }
        public bool IsUserSystemAccount { get; set; }
        public bool IsUserRemoteDesktop { get; set; }
        public bool IsUserRemote { get; set; }
        public bool IsProcessElevated { get; set; }
        public bool IsLicensedVersion { get; set; }
        public bool IsLicensedAssemblyLoaded { get; set; }
        public string LicenseType { get; set; }
        public string CurrentDirectory { get; set; }
    }

    [Serializable]
    public sealed class FeaturesConfiguration
    {
        public bool AutoUninstaller { get; set; }
        public bool ChecksumFiles { get; set; }
        public bool AllowEmptyChecksums { get; set; }
        public bool AllowEmptyChecksumsSecure { get; set; }
        public bool FailOnAutoUninstaller { get; set; }
        public bool FailOnStandardError { get; set; }
        public bool UsePowerShellHost { get; set; }
        public bool LogEnvironmentValues { get; set; }
        public bool LogWithoutColor { get; set; }
        public bool VirusCheck { get; set; }
        public bool FailOnInvalidOrMissingLicense { get; set; }
        public bool IgnoreInvalidOptionsSwitches { get; set; }
        public bool UsePackageExitCodes { get; set; }
        public bool UseEnhancedExitCodes { get; set; }
        public bool UseFipsCompliantChecksums { get; set; }
        public bool ShowNonElevatedWarnings { get; set; }
        public bool ShowDownloadProgress { get; set; }
        public bool StopOnFirstPackageFailure { get; set; }
        public bool UseRememberedArgumentsForUpgrades { get; set; }
        public bool IgnoreUnfoundPackagesOnUpgradeOutdated { get; set; }
        public bool SkipPackageUpgradesWhenNotInstalled { get; set; }
        public bool RemovePackageInformationOnUninstall { get; set; }
        public bool ExitOnRebootDetected { get; set; }
        public bool LogValidationResultsOnWarnings { get; set; }
        public bool UsePackageRepositoryOptimizations { get; set; }
        public bool UsePackageHashValidation { get; set; }
    }

    //todo: #2565 retrofit other command configs this way

    [Serializable]
    public sealed class ListCommandConfiguration
    {
        public ListCommandConfiguration()
        {
            PageSize = 25;
        }

        // list
        public bool LocalOnly { get; set; }

        public bool IdOnly { get; set; }
        public bool IncludeRegistryPrograms { get; set; }
        public int? Page { get; set; }
        public int PageSize { get; set; }
        public bool Exact { get; set; }
        public bool ByIdOnly { get; set; }
        public bool ByTagOnly { get; set; }
        public bool IdStartsWith { get; set; }
        public bool OrderByPopularity { get; set; }
        public bool ApprovedOnly { get; set; }
        public bool DownloadCacheAvailable { get; set; }
        public bool NotBroken { get; set; }
        public bool IncludeVersionOverrides { get; set; }
        public bool ExplicitPageSize { get; set; }
        public bool ExplicitSource { get; set; }
    }

    [Serializable]
    public sealed class UpgradeCommandConfiguration
    {
        public bool FailOnUnfound { get; set; }
        public bool FailOnNotInstalled { get; set; }
        public bool NotifyOnlyAvailableUpgrades { get; set; }
        public string PackageNamesToSkip { get; set; }
        public bool ExcludePrerelease { get; set; }
        public bool IgnorePinned { get; set; }
    }

    [Serializable]
    public sealed class NewCommandConfiguration
    {
        public NewCommandConfiguration()
        {
            TemplateProperties = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        }

        public string TemplateName { get; set; }
        public string Name { get; set; }
        public bool AutomaticPackage { get; set; }
        public IDictionary<string, string> TemplateProperties { get; private set; }
        public bool UseOriginalTemplate { get; set; }
    }

    [Serializable]
    public sealed class SourcesCommandConfiguration
    {
        public string Name { get; set; }
        public SourceCommandType Command { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Priority { get; set; }
        public string Certificate { get; set; }
        public string CertificatePassword { get; set; }
        public bool BypassProxy { get; set; }
        public bool AllowSelfService { get; set; }
        public bool VisibleToAdminsOnly { get; set; }
    }

    [Serializable]
    public sealed class MachineSourceConfiguration
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public string Username { get; set; }
        public string EncryptedPassword { get; set; }
        public int Priority { get; set; }
        public string Certificate { get; set; }
        public string EncryptedCertificatePassword { get; set; }
        public bool BypassProxy { get; set; }
        public bool AllowSelfService { get; set; }
        public bool VisibleToAdminsOnly { get; set; }
    }

    [Serializable]
    public sealed class FeatureCommandConfiguration
    {
        public string Name { get; set; }
        public FeatureCommandType Command { get; set; }
    }

    [Serializable]
    public sealed class ConfigCommandConfiguration
    {
        public string Name { get; set; }
        public string ConfigValue { get; set; }
        public ConfigCommandType Command { get; set; }
    }

    [Serializable]
    public sealed class PinCommandConfiguration
    {
        public string Name { get; set; }
        public PinCommandType Command { get; set; }
    }

    [Serializable]
    public sealed class OutdatedCommandConfiguration
    {
        public bool IgnorePinned { get; set; }
    }

    [Serializable]
    public sealed class ApiKeyCommandConfiguration
    {
        public string Key { get; set; }
        public ApiKeyCommandType Command { get; set; }
    }

    [Serializable]
    public sealed class PackCommandConfiguration
    {
        public PackCommandConfiguration()
        {
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public IDictionary<string, string> Properties { get; private set; }
        public bool PackThrowOnUnsupportedElements = true;
    }

    [Serializable]
    public sealed class PushCommandConfiguration
    {
        public string Key { get; set; }
        public string DefaultSource { get; set; }
        //DisableBuffering?
    }

    [Serializable]
    public sealed class ProxyConfiguration
    {
        public string Location { get; set; }
        public string User { get; set; }
        public string EncryptedPassword { get; set; }
        public string BypassList { get; set; }
        public bool BypassOnLocal { get; set; }
    }

    [Serializable]
    public sealed class ExportCommandConfiguration
    {
        public bool IncludeVersionNumbers { get; set; }

        public string OutputFilePath { get; set; }
    }

    [Serializable]
    public sealed class TemplateCommandConfiguration
    {
        public TemplateCommandType Command { get; set; }
        public string Name { get; set; }
    }

    [Serializable]
    public sealed class CacheCommandConfiguration
    {
        /// <summary>
        /// Gets or sets the type of the command that should be used when running the Cache command.
        /// </summary>
        /// <value>
        /// The command type to use.
        /// </value>
        public CacheCommandType Command { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether only expired items in the cache should be removed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if only expired cache items should be removed; otherwise, <c>false</c>.
        /// </value>
        public bool RemoveExpiredItemsOnly { get; set; }
    }

    [Serializable]
    public sealed class RuleCommandConfiguration
    {
        /// <summary>
        /// Gets or sets the name or identifier of a rule the user wants to use for the command.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the sub command to execute.
        /// </summary>
        public string Command { get; set; }
    }
}
