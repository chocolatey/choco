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

namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.Win32;
    using configuration;
    using domain;
    using filesystem;
    using infrastructure.commands;
    using logging;
    using results;
    using platforms;

    /// <summary>
    ///   Alternative Source for Installing Python packages
    /// </summary>
    public sealed class PythonService : IBootstrappableSourceRunner, IListSourceRunner, IInstallSourceRunner, IUpgradeSourceRunner, IUninstallSourceRunner
    {
        private readonly ICommandExecutor _commandExecutor;
        private readonly INugetService _nugetService;
        private readonly IFileSystem _fileSystem;
        private readonly IRegistryService _registryService;
        private const string PackageNameToken = "{{packagename}}";
        private const string LogLevelToken = "{{loglevel}}";
        private const string ForceToken = "{{force}}";
        private const string PythonPackage = "python";
        private string _exePath = string.Empty;

        private const string AppName = "Python";
        private const string PackageNameGroup = "PkgName";
        private static readonly Regex _installedRegex = new Regex(@"Successfully installed", RegexOptions.Compiled);
        private static readonly Regex _uninstalledRegex = new Regex(@"Successfully uninstalled", RegexOptions.Compiled);
        private static readonly Regex _packageNameRegex = new Regex(@"\s(?<{0}>[^-\s]*)-".FormatWith(PackageNameGroup), RegexOptions.Compiled);
        private static readonly Regex _errorRegex = new Regex(@"Error:", RegexOptions.Compiled);
        private static readonly Regex _errorNotFoundRegex = new Regex(@"Could not find any downloads that", RegexOptions.Compiled);

        private readonly IDictionary<string, ExternalCommandArgument> _listArguments = new Dictionary<string, ExternalCommandArgument>(StringComparer.InvariantCultureIgnoreCase);
        private readonly IDictionary<string, ExternalCommandArgument> _installArguments = new Dictionary<string, ExternalCommandArgument>(StringComparer.InvariantCultureIgnoreCase);
        private readonly IDictionary<string, ExternalCommandArgument> _upgradeArguments = new Dictionary<string, ExternalCommandArgument>(StringComparer.InvariantCultureIgnoreCase);
        private readonly IDictionary<string, ExternalCommandArgument> _uninstallArguments = new Dictionary<string, ExternalCommandArgument>(StringComparer.InvariantCultureIgnoreCase);

        public PythonService(ICommandExecutor commandExecutor, INugetService nugetService, IFileSystem fileSystem, IRegistryService registryService)
        {
            _commandExecutor = commandExecutor;
            _nugetService = nugetService;
            _fileSystem = fileSystem;
            _registryService = registryService;
            SetupCommandArgsDictionaries();
        }

        /// <summary>
        ///   Set any command arguments dictionaries necessary for the service
        /// </summary>
        private void SetupCommandArgsDictionaries()
        {
            SetupListDictionary(_listArguments);
            SetupInstallDictionary(_installArguments);
            SetupUpgradeDictionary(_upgradeArguments);
            SetupUninstallDictionary(_uninstallArguments);
        }

        /// <summary>
        ///   Sets list dictionary
        /// </summary>
        private void SetupListDictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            AddCommonArguments(args);
            args.Add("_command_", new ExternalCommandArgument { ArgumentOption = "list", Required = true });
        }

        /// <summary>
        ///   Sets install dictionary
        /// </summary>
        private void SetupInstallDictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            AddCommonArguments(args);

            args.Add("_command_", new ExternalCommandArgument { ArgumentOption = "install", Required = true });
            args.Add("_package_name_", new ExternalCommandArgument
            {
                ArgumentOption = "",
                ArgumentValue = PackageNameToken,
                QuoteValue = false,
                UseValueOnly = true,
                Required = true
            });
        }

        /// <summary>
        ///   Sets install dictionary
        /// </summary>
        private void SetupUpgradeDictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            AddCommonArguments(args);

            args.Add("_command_", new ExternalCommandArgument { ArgumentOption = "install", Required = true });
            args.Add("_upgrade_", new ExternalCommandArgument { ArgumentOption = "--upgrade", Required = true });
            args.Add("_package_name_", new ExternalCommandArgument
            {
                ArgumentOption = "",
                ArgumentValue = PackageNameToken,
                QuoteValue = false,
                UseValueOnly = true,
                Required = true
            });
        }

        /// <summary>
        ///   Sets uninstall dictionary
        /// </summary>
        private void SetupUninstallDictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            AddCommonArguments(args);

            args.Add("_command_", new ExternalCommandArgument { ArgumentOption = "uninstall", Required = true });
            args.Add("_confirm_", new ExternalCommandArgument { ArgumentOption = "-y", Required = true });
            args.Add("_package_name_", new ExternalCommandArgument
            {
                ArgumentOption = "",
                ArgumentValue = PackageNameToken,
                QuoteValue = false,
                UseValueOnly = true,
                Required = true
            });
        }

        private void AddCommonArguments(IDictionary<string, ExternalCommandArgument> args)
        {
            args.Add("_loglevel_", new ExternalCommandArgument
            {
                ArgumentOption = "",
                ArgumentValue = LogLevelToken,
                QuoteValue = false,
                UseValueOnly = true,
                Required = true
            });

            args.Add("_force_", new ExternalCommandArgument
            {
                ArgumentOption = "",
                ArgumentValue = ForceToken,
                QuoteValue = false,
                UseValueOnly = true,
                Required = true
            });
        }

        public string SourceType
        {
            get { return SourceTypes.Python; }
        }

        public void EnsureSourceAppInstalled(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> ensureAction)
        {
            if (Platform.GetPlatform() != PlatformType.Windows) throw new NotImplementedException("This source is not supported on non-Windows systems");

            //ensure at least python 2.7.9 is installed
            var python = _fileSystem.GetExecutablePath("python");
            //python -V

            if (python.IsEqualTo("python"))
            {
                var runnerConfig = new ChocolateyConfiguration
                {
                    Sources = ApplicationParameters.PackagesLocation,
                    Debug = config.Debug,
                    Force = config.Force,
                    Verbose = config.Verbose,
                    CommandExecutionTimeoutSeconds = config.CommandExecutionTimeoutSeconds,
                    CacheLocation = config.CacheLocation,
                    RegularOutput = config.RegularOutput,
                    PromptForConfirmation = false,
                    AcceptLicense = true,
                    QuietOutput = true,
                };
                runnerConfig.ListCommand.LocalOnly = true;

                var localPackages = _nugetService.List(runnerConfig);

                if (!localPackages.Any(p => p.Name.IsEqualTo(PythonPackage)))
                {
                    runnerConfig.PackageNames = PythonPackage;
                    runnerConfig.Sources = ApplicationParameters.ChocolateyCommunityFeedSource;

                    var prompt = config.PromptForConfirmation;
                    config.PromptForConfirmation = false;
                    _nugetService.Install(runnerConfig, ensureAction.Invoke);
                    config.PromptForConfirmation = prompt;
                }
            }
        }

        private void EnsureExecutablePathSet()
        {
            if (!string.IsNullOrWhiteSpace(_exePath)) return;

            var python = _fileSystem.GetExecutablePath("python");

            var pipPath = string.Empty;
            if (!python.IsEqualTo("python"))
            {
                pipPath = _fileSystem.CombinePaths(_fileSystem.GetDirectoryName(python), "Scripts", "pip.exe");
                if (_fileSystem.FileExists(pipPath))
                {
                    _exePath = pipPath;
                    return;
                }
            }

            var topLevelPath = string.Empty;
            var python34PathKey = _registryService.GetKey(RegistryHive.LocalMachine, "SOFTWARE\\Python\\PythonCore\\3.4\\InstallPath");
            if (python34PathKey != null)
            {
                topLevelPath = python34PathKey.GetValue("", string.Empty).ToStringSafe();
            }
            if (string.IsNullOrWhiteSpace(topLevelPath))
            {
                var python27PathKey = _registryService.GetKey(RegistryHive.LocalMachine, "SOFTWARE\\Python\\PythonCore\\2.7\\InstallPath");
                if (python27PathKey != null)
                {
                    topLevelPath = python27PathKey.GetValue("", string.Empty).ToStringSafe();
                }
            }

            if (string.IsNullOrWhiteSpace(topLevelPath))
            {
                var binRoot = Environment.GetEnvironmentVariable("ChocolateyBinRoot");
                if (string.IsNullOrWhiteSpace(binRoot)) binRoot = "c:\\tools";

                topLevelPath = _fileSystem.CombinePaths(binRoot, "python");
            }

            pipPath = _fileSystem.CombinePaths(_fileSystem.GetDirectoryName(topLevelPath), "Scripts", "pip.exe");
            if (_fileSystem.FileExists(pipPath))
            {
                _exePath = pipPath;
            }

            if (string.IsNullOrWhiteSpace(_exePath)) throw new FileNotFoundException("Unable to find pip");
        }

        private string BuildArguments(ChocolateyConfiguration config, IDictionary<string, ExternalCommandArgument> argsDictionary)
        {
            var args = ExternalCommandArgsBuilder.BuildArguments(config, argsDictionary);

            args = args.Replace(LogLevelToken, config.Debug ? "-vvv" : "");

            if (config.CommandName.IsEqualTo("install"))
            {
                args = args.Replace(ForceToken, config.Force ? "--ignore-installed" : "");
            }
            else if (config.CommandName.IsEqualTo("upgrade"))
            {
                args = args.Replace(ForceToken, config.Force ? "--force-reinstall" : "");
            }
            else
            {
                args = args.Replace(ForceToken, "");
            }

            return args;
        }

        public void ListDryRun(ChocolateyConfiguration config)
        {
            EnsureExecutablePathSet();
            var args = BuildArguments(config, _listArguments);
            this.Log().Info("Would have run '{0} {1}'".FormatWith(_exePath.EscapeCurlyBraces(), args.EscapeCurlyBraces()));
        }

        public IEnumerable<PackageResult> List(ChocolateyConfiguration config)
        {
            EnsureExecutablePathSet();
            var args = BuildArguments(config, _listArguments);
            var packageResults = new List<PackageResult>();

            Environment.ExitCode = _commandExecutor.Execute(
                _exePath,
                args,
                config.CommandExecutionTimeoutSeconds,
                _fileSystem.GetCurrentDirectory(),
                stdOutAction: (s, e) =>
                    {
                        var logMessage = e.Data;
                        if (string.IsNullOrWhiteSpace(logMessage)) return;
                        if (!config.QuietOutput)
                        {
                            this.Log().Info(logMessage.EscapeCurlyBraces());
                        }
                        else
                        {
                            this.Log().Debug(() => "[{0}] {1}".FormatWith(AppName, logMessage.EscapeCurlyBraces()));
                        }
                    },
                stdErrAction: (s, e) =>
                    {
                        if (string.IsNullOrWhiteSpace(e.Data)) return;
                        this.Log().Error(() => "{0}".FormatWith(e.Data.EscapeCurlyBraces()));
                    },
                updateProcessPath: false,
                allowUseWindow: true
                );

            return packageResults;
        }

        public void InstallDryRun(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
        {
            EnsureExecutablePathSet();
            var args = BuildArguments(config, _installArguments);
            this.Log().Info("Would have run '{0} {1}'".FormatWith(_exePath.EscapeCurlyBraces(), args.EscapeCurlyBraces()));
        }

        public ConcurrentDictionary<string, PackageResult> Install(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
        {
            return Install(config, continueAction, beforeModifyAction: null);
        }

        public ConcurrentDictionary<string, PackageResult> Install(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeModifyAction)
        {
            EnsureExecutablePathSet();
            var args = BuildArguments(config, _installArguments);
            var packageResults = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var packageToInstall in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                var pkgName = packageToInstall;
                if (!string.IsNullOrWhiteSpace(config.Version))
                {
                    pkgName = "{0}=={1}".FormatWith(packageToInstall, config.Version);
                }
                var argsForPackage = args.Replace(PackageNameToken, pkgName);

                var exitCode = _commandExecutor.Execute(
                    _exePath,
                    argsForPackage,
                    config.CommandExecutionTimeoutSeconds,
                    _fileSystem.GetCurrentDirectory(),
                    (s, e) =>
                    {
                        var logMessage = e.Data;
                        if (string.IsNullOrWhiteSpace(logMessage)) return;
                        this.Log().Info(() => " [{0}] {1}".FormatWith(AppName, logMessage.EscapeCurlyBraces()));

                        if (_errorRegex.IsMatch(logMessage) || _errorNotFoundRegex.IsMatch(logMessage))
                        {
                            var results = packageResults.GetOrAdd(packageToInstall, new PackageResult(packageToInstall, null, null));
                            results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                        }

                        if (_installedRegex.IsMatch(logMessage))
                        {
                            var packageName = GetValueFromOutput(logMessage, _packageNameRegex, PackageNameGroup);
                            var results = packageResults.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                            results.Messages.Add(new ResultMessage(ResultType.Note, packageName));
                            this.Log().Info(ChocolateyLoggers.Important, " {0} has been installed successfully.".FormatWith(string.IsNullOrWhiteSpace(packageName) ? packageToInstall : packageName));
                        }
                    },
                    (s, e) =>
                    {
                        var logMessage = e.Data;
                        if (string.IsNullOrWhiteSpace(logMessage)) return;
                        this.Log().Error("[{0}] {1}".FormatWith(AppName, logMessage.EscapeCurlyBraces()));

                        if (_errorRegex.IsMatch(logMessage) || _errorNotFoundRegex.IsMatch(logMessage))
                        {
                            var results = packageResults.GetOrAdd(packageToInstall, new PackageResult(packageToInstall, null, null));
                            results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                        }
                    },
                    updateProcessPath: false,
                    allowUseWindow: true
                    );

                if (exitCode != 0)
                {
                    Environment.ExitCode = exitCode;
                }
            }

            return packageResults;
        }

        public ConcurrentDictionary<string, PackageResult> UpgradeDryRun(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
        {
            EnsureExecutablePathSet();
            var args = BuildArguments(config, _upgradeArguments);
            this.Log().Info("Would have run '{0} {1}'".FormatWith(_exePath.EscapeCurlyBraces(), args.EscapeCurlyBraces()));
            return new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);
        }

        public ConcurrentDictionary<string, PackageResult> Upgrade(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeUpgradeAction = null)
        {
            if (config.PackageNames.IsEqualTo(ApplicationParameters.AllPackages))
            {
                throw new NotImplementedException("The all keyword is not available for alternate sources");
            }

            EnsureExecutablePathSet();
            var args = BuildArguments(config, _upgradeArguments);
            var packageResults = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var packageToInstall in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                var pkgName = packageToInstall;
                if (!string.IsNullOrWhiteSpace(config.Version))
                {
                    pkgName = "{0}=={1}".FormatWith(packageToInstall, config.Version);
                }

                var argsForPackage = args.Replace(PackageNameToken, pkgName);

                var exitCode = _commandExecutor.Execute(
                    _exePath,
                    argsForPackage,
                    config.CommandExecutionTimeoutSeconds,
                    _fileSystem.GetCurrentDirectory(),
                    (s, e) =>
                    {
                        var logMessage = e.Data;
                        if (string.IsNullOrWhiteSpace(logMessage)) return;
                        this.Log().Info(() => " [{0}] {1}".FormatWith(AppName, logMessage.EscapeCurlyBraces()));

                        if (_errorRegex.IsMatch(logMessage) || _errorNotFoundRegex.IsMatch(logMessage))
                        {
                            var results = packageResults.GetOrAdd(packageToInstall, new PackageResult(packageToInstall, null, null));
                            results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                        }

                        if (_installedRegex.IsMatch(logMessage))
                        {
                            var packageName = GetValueFromOutput(logMessage, _packageNameRegex, PackageNameGroup);
                            var results = packageResults.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                            results.Messages.Add(new ResultMessage(ResultType.Note, packageName));
                            this.Log().Info(ChocolateyLoggers.Important, " {0} has been installed successfully.".FormatWith(string.IsNullOrWhiteSpace(packageName) ? packageToInstall : packageName));
                        }
                    },
                    (s, e) =>
                    {
                        var logMessage = e.Data;
                        if (string.IsNullOrWhiteSpace(logMessage)) return;
                        this.Log().Error("[{0}] {1}".FormatWith(AppName, logMessage.EscapeCurlyBraces()));

                        if (_errorRegex.IsMatch(logMessage) || _errorNotFoundRegex.IsMatch(logMessage))
                        {
                            var results = packageResults.GetOrAdd(packageToInstall, new PackageResult(packageToInstall, null, null));
                            results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                        }
                    },
                    updateProcessPath: false,
                    allowUseWindow: true
                    );

                if (exitCode != 0)
                {
                    Environment.ExitCode = exitCode;
                }
            }

            return packageResults;
        }

        public void UninstallDryRun(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
        {
            EnsureExecutablePathSet();
            var args = BuildArguments(config, _uninstallArguments);
            this.Log().Info("Would have run '{0} {1}'".FormatWith(_exePath.EscapeCurlyBraces(), args.EscapeCurlyBraces()));
        }

        public ConcurrentDictionary<string, PackageResult> Uninstall(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeUninstallAction = null)
        {
            EnsureExecutablePathSet();
            var args = BuildArguments(config, _uninstallArguments);
            var packageResults = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var packageToInstall in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                var argsForPackage = args.Replace(PackageNameToken, packageToInstall);

                var exitCode = _commandExecutor.Execute(
                    _exePath,
                    argsForPackage,
                    config.CommandExecutionTimeoutSeconds,
                    _fileSystem.GetCurrentDirectory(),
                    (s, e) =>
                    {
                        var logMessage = e.Data;
                        if (string.IsNullOrWhiteSpace(logMessage)) return;
                        this.Log().Info(() => " [{0}] {1}".FormatWith(AppName, logMessage.EscapeCurlyBraces()));

                        if (_errorRegex.IsMatch(logMessage) || _errorNotFoundRegex.IsMatch(logMessage))
                        {
                            var results = packageResults.GetOrAdd(packageToInstall, new PackageResult(packageToInstall, null, null));
                            results.Messages.Add(new ResultMessage(ResultType.Error, packageToInstall));
                        }

                        if (_uninstalledRegex.IsMatch(logMessage))
                        {
                            var packageName = GetValueFromOutput(logMessage, _packageNameRegex, PackageNameGroup);
                            var results = packageResults.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                            results.Messages.Add(new ResultMessage(ResultType.Note, packageName));
                            this.Log().Info(ChocolateyLoggers.Important, " {0} has been uninstalled successfully.".FormatWith(string.IsNullOrWhiteSpace(packageName) ? packageToInstall : packageName));
                        }
                    },
                    (s, e) =>
                    {
                        var logMessage = e.Data;
                        if (string.IsNullOrWhiteSpace(logMessage)) return;
                        this.Log().Error("[{0}] {1}".FormatWith(AppName, logMessage.EscapeCurlyBraces()));

                        if (_errorRegex.IsMatch(logMessage) || _errorNotFoundRegex.IsMatch(logMessage))
                        {
                            var results = packageResults.GetOrAdd(packageToInstall, new PackageResult(packageToInstall, null, null));
                            results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                        }
                    },
                    updateProcessPath: false,
                    allowUseWindow: true
                    );

                if (exitCode != 0)
                {
                    Environment.ExitCode = exitCode;
                }
            }

            return packageResults;
        }

        /// <summary>
        ///   Grabs a value from the output based on the regex.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="regex">The regex.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <returns></returns>
        private static string GetValueFromOutput(string output, Regex regex, string groupName)
        {
            var matchGroup = regex.Match(output).Groups[groupName];
            if (matchGroup != null)
            {
                return matchGroup.Value;
            }

            return string.Empty;
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public const string PYTHON_PACKAGE = PythonPackage;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public const string PACKAGE_NAME_GROUP = PackageNameGroup;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static readonly Regex InstalledRegex = _installedRegex;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static readonly Regex UninstalledRegex = _uninstalledRegex;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static readonly Regex PackageNameRegex = _packageNameRegex;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static readonly Regex ErrorRegex = _errorRegex;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static readonly Regex ErrorNotFoundRegex = new Regex(@"Could not find any downloads that", RegexOptions.Compiled);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void ensure_source_app_installed(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> ensureAction)
            => EnsureSourceAppInstalled(config, ensureAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void set_executable_path_if_not_set()
            => EnsureExecutablePathSet();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public string build_args(ChocolateyConfiguration config, IDictionary<string, ExternalCommandArgument> argsDictionary)
            => BuildArguments(config, argsDictionary);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void list_noop(ChocolateyConfiguration config)
            => ListDryRun(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public IEnumerable<PackageResult> list_run(ChocolateyConfiguration config)
            => List(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void install_noop(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
            => InstallDryRun(config, continueAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
            => Install(config, continueAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeModifyAction)
            => Install(config, continueAction, beforeModifyAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public ConcurrentDictionary<string, PackageResult> upgrade_noop(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
            => UpgradeDryRun(config, continueAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeUpgradeAction = null)
            => Upgrade(config, continueAction, beforeUpgradeAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void uninstall_noop(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
            => UninstallDryRun(config, continueAction);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeUninstallAction = null)
            => Uninstall(config, continueAction, beforeUninstallAction);
#pragma warning restore IDE1006
    }
}
