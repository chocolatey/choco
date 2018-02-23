// Copyright © 2017 - 2018 Chocolatey Software, Inc
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

    /// <summary>
    ///   Alternative Source for Installing Python packages
    /// </summary>
    public sealed class PythonService : ISourceRunner
    {
        private readonly ICommandExecutor _commandExecutor;
        private readonly INugetService _nugetService;
        private readonly IFileSystem _fileSystem;
        private readonly IRegistryService _registryService;
        private const string PACKAGE_NAME_TOKEN = "{{packagename}}";
        private const string LOG_LEVEL_TOKEN = "{{loglevel}}";
        private const string FORCE_TOKEN = "{{force}}";
        public const string PYTHON_PACKAGE = "python";
        private string _exePath = string.Empty;

        private const string APP_NAME = "Python";
        public const string PACKAGE_NAME_GROUP = "PkgName";
        public static readonly Regex InstalledRegex = new Regex(@"Successfully installed", RegexOptions.Compiled);
        public static readonly Regex UninstalledRegex = new Regex(@"Successfully uninstalled", RegexOptions.Compiled);
        public static readonly Regex PackageNameRegex = new Regex(@"\s(?<{0}>[^-\s]*)-".format_with(PACKAGE_NAME_GROUP), RegexOptions.Compiled);
        public static readonly Regex ErrorRegex = new Regex(@"Error:", RegexOptions.Compiled);
        public static readonly Regex ErrorNotFoundRegex = new Regex(@"Could not find any downloads that", RegexOptions.Compiled);

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
            set_cmd_args_dictionaries();
        }

        /// <summary>
        ///   Set any command arguments dictionaries necessary for the service
        /// </summary>
        private void set_cmd_args_dictionaries()
        {
            set_list_dictionary(_listArguments);
            set_install_dictionary(_installArguments);
            set_upgrade_dictionary(_upgradeArguments);
            set_uninstall_dictionary(_uninstallArguments);
        }

        /// <summary>
        ///   Sets list dictionary
        /// </summary>
        private void set_list_dictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            set_common_args(args);
            args.Add("_command_", new ExternalCommandArgument { ArgumentOption = "list", Required = true });
        }

        /// <summary>
        ///   Sets install dictionary
        /// </summary>
        private void set_install_dictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            set_common_args(args);

            args.Add("_command_", new ExternalCommandArgument { ArgumentOption = "install", Required = true });
            args.Add("_package_name_", new ExternalCommandArgument
                {
                    ArgumentOption = "",
                    ArgumentValue = PACKAGE_NAME_TOKEN,
                    QuoteValue = false,
                    UseValueOnly = true,
                    Required = true
                });
        }

        /// <summary>
        ///   Sets install dictionary
        /// </summary>
        private void set_upgrade_dictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            set_common_args(args);

            args.Add("_command_", new ExternalCommandArgument { ArgumentOption = "install", Required = true });
            args.Add("_upgrade_", new ExternalCommandArgument { ArgumentOption = "--upgrade", Required = true });
            args.Add("_package_name_", new ExternalCommandArgument
                {
                    ArgumentOption = "",
                    ArgumentValue = PACKAGE_NAME_TOKEN,
                    QuoteValue = false,
                    UseValueOnly = true,
                    Required = true
                });
        }

        /// <summary>
        ///   Sets uninstall dictionary
        /// </summary>
        private void set_uninstall_dictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            set_common_args(args);

            args.Add("_command_", new ExternalCommandArgument { ArgumentOption = "uninstall", Required = true });
            args.Add("_confirm_", new ExternalCommandArgument { ArgumentOption = "-y", Required = true });
            args.Add("_package_name_", new ExternalCommandArgument
                {
                    ArgumentOption = "",
                    ArgumentValue = PACKAGE_NAME_TOKEN,
                    QuoteValue = false,
                    UseValueOnly = true,
                    Required = true
                });
        }

        private void set_common_args(IDictionary<string, ExternalCommandArgument> args)
        {
            args.Add("_loglevel_", new ExternalCommandArgument
            {
                ArgumentOption = "",
                ArgumentValue = LOG_LEVEL_TOKEN,
                QuoteValue = false,
                UseValueOnly = true,
                Required = true
            });

            args.Add("_force_", new ExternalCommandArgument
            {
                ArgumentOption = "",
                ArgumentValue = FORCE_TOKEN,
                QuoteValue = false,
                UseValueOnly = true,
                Required = true
            });


        }

        public SourceType SourceType
        {
            get { return SourceType.python; }
        }

        public void ensure_source_app_installed(ChocolateyConfiguration config, Action<PackageResult> ensureAction)
        {
            //ensure at least python 2.7.9 is installed
            var python = _fileSystem.get_executable_path("python");
            //python -V

            if (python.is_equal_to("python"))
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

                var localPackages = _nugetService.list_run(runnerConfig);

                if (!localPackages.Any(p => p.Name.is_equal_to(PYTHON_PACKAGE)))
                {
                    runnerConfig.PackageNames = PYTHON_PACKAGE;
                    runnerConfig.Sources = ApplicationParameters.ChocolateyCommunityFeedSource;

                    var prompt = config.PromptForConfirmation;
                    config.PromptForConfirmation = false;
                    _nugetService.install_run(runnerConfig, ensureAction.Invoke);
                    config.PromptForConfirmation = prompt;
                }
            }
        }

        public int count_run(ChocolateyConfiguration config)
        {
            throw new NotImplementedException("Count is not supported for this source runner.");
        }

        public void set_executable_path_if_not_set()
        {
            if (!string.IsNullOrWhiteSpace(_exePath)) return;

            var python = _fileSystem.get_executable_path("python");

            var pipPath = string.Empty;
            if (!python.is_equal_to("python"))
            {
                pipPath = _fileSystem.combine_paths(_fileSystem.get_directory_name(python), "Scripts", "pip.exe");
                if (_fileSystem.file_exists(pipPath))
                {
                    _exePath = pipPath;
                    return;
                }
            }
            
            var topLevelPath = string.Empty;
            var python34PathKey = _registryService.get_key(RegistryHive.LocalMachine, "SOFTWARE\\Python\\PythonCore\\3.4\\InstallPath");
            if (python34PathKey != null)
            {
                topLevelPath = python34PathKey.GetValue("", string.Empty).to_string();
            }
            if (string.IsNullOrWhiteSpace(topLevelPath))
            {
                var python27PathKey = _registryService.get_key(RegistryHive.LocalMachine, "SOFTWARE\\Python\\PythonCore\\2.7\\InstallPath");
                if (python27PathKey != null)
                {
                    topLevelPath = python27PathKey.GetValue("", string.Empty).to_string();
                }
            }
            
            if (string.IsNullOrWhiteSpace(topLevelPath))
            {
                var binRoot = Environment.GetEnvironmentVariable("ChocolateyBinRoot");
                if (string.IsNullOrWhiteSpace(binRoot)) binRoot = "c:\\tools";

                topLevelPath = _fileSystem.combine_paths(binRoot, "python");
            }

            pipPath = _fileSystem.combine_paths(_fileSystem.get_directory_name(topLevelPath), "Scripts", "pip.exe");
            if (_fileSystem.file_exists(pipPath))
            {
                _exePath = pipPath;
            }
            
            if (string.IsNullOrWhiteSpace(_exePath)) throw new FileNotFoundException("Unable to find pip");
        }

        public string build_args(ChocolateyConfiguration config, IDictionary<string, ExternalCommandArgument> argsDictionary)
        {
            var args = ExternalCommandArgsBuilder.build_arguments(config, argsDictionary);

            args = args.Replace(LOG_LEVEL_TOKEN, config.Debug ? "-vvv" : "");

            if (config.CommandName.is_equal_to("intall"))
            {
                args = args.Replace(FORCE_TOKEN, config.Force ? "--ignore-installed" : "");
            }
            else if (config.CommandName.is_equal_to("upgrade"))
            {
                args = args.Replace(FORCE_TOKEN, config.Force ? "--force-reinstall" : "");
            }
            else
            {
                args = args.Replace(FORCE_TOKEN, "");
            }

            return args;
        }

        public void list_noop(ChocolateyConfiguration config)
        {
            set_executable_path_if_not_set();
            var args = build_args(config, _listArguments);
            this.Log().Info("Would have run '{0} {1}'".format_with(_exePath.escape_curly_braces(), args.escape_curly_braces()));
        }

        public IEnumerable<PackageResult> list_run(ChocolateyConfiguration config)
        {
            set_executable_path_if_not_set();
            var args = build_args(config, _listArguments);
            var packageResults = new List<PackageResult>();

            Environment.ExitCode = _commandExecutor.execute(
                _exePath,
                args,
                config.CommandExecutionTimeoutSeconds,
                _fileSystem.get_current_directory(),
                stdOutAction: (s, e) =>
                    {
                        var logMessage = e.Data;
                        if (string.IsNullOrWhiteSpace(logMessage)) return;
                        if (!config.QuietOutput)
                        {
                            this.Log().Info(logMessage.escape_curly_braces());
                        }
                        else
                        {
                            this.Log().Debug(() => "[{0}] {1}".format_with(APP_NAME, logMessage.escape_curly_braces()));
                        }
                    },
                stdErrAction: (s, e) =>
                    {
                        if (string.IsNullOrWhiteSpace(e.Data)) return;
                        this.Log().Error(() => "{0}".format_with(e.Data.escape_curly_braces()));
                    },
                updateProcessPath: false,
                allowUseWindow: true
                );

            return packageResults;
        }

        public void install_noop(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            set_executable_path_if_not_set();
            var args = build_args(config, _installArguments);
            this.Log().Info("Would have run '{0} {1}'".format_with(_exePath.escape_curly_braces(), args.escape_curly_braces()));
        }

        public ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            set_executable_path_if_not_set();
            var args = build_args(config, _installArguments);
            var packageResults = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var packageToInstall in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                var pkgName = packageToInstall;
                if (!string.IsNullOrWhiteSpace(config.Version))
                {
                    pkgName = "{0}=={1}".format_with(packageToInstall, config.Version);
                }
                var argsForPackage = args.Replace(PACKAGE_NAME_TOKEN, pkgName);

                var exitCode = _commandExecutor.execute(
                    _exePath,
                    argsForPackage,
                    config.CommandExecutionTimeoutSeconds,
                    _fileSystem.get_current_directory(),
                    (s, e) =>
                    {
                        var logMessage = e.Data;
                        if (string.IsNullOrWhiteSpace(logMessage)) return;
                        this.Log().Info(() => " [{0}] {1}".format_with(APP_NAME, logMessage.escape_curly_braces()));

                        if (ErrorRegex.IsMatch(logMessage) || ErrorNotFoundRegex.IsMatch(logMessage))
                        {
                            var results = packageResults.GetOrAdd(packageToInstall, new PackageResult(packageToInstall, null, null));
                            results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                        }

                        if (InstalledRegex.IsMatch(logMessage))
                        {
                            var packageName = get_value_from_output(logMessage, PackageNameRegex, PACKAGE_NAME_GROUP);
                            var results = packageResults.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                            results.Messages.Add(new ResultMessage(ResultType.Note, packageName));
                            this.Log().Info(ChocolateyLoggers.Important, " {0} has been installed successfully.".format_with(string.IsNullOrWhiteSpace(packageName) ? packageToInstall : packageName));
                        }
                    },
                    (s, e) =>
                    {
                        var logMessage = e.Data;
                        if (string.IsNullOrWhiteSpace(logMessage)) return;
                        this.Log().Error("[{0}] {1}".format_with(APP_NAME, logMessage.escape_curly_braces()));

                        if (ErrorRegex.IsMatch(logMessage) || ErrorNotFoundRegex.IsMatch(logMessage))
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

        public ConcurrentDictionary<string, PackageResult> upgrade_noop(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            set_executable_path_if_not_set();
            var args = build_args(config, _upgradeArguments);
            this.Log().Info("Would have run '{0} {1}'".format_with(_exePath.escape_curly_braces(), args.escape_curly_braces()));
            return new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);
        }

        public ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config, Action<PackageResult> continueAction, Action<PackageResult> beforeUpgradeAction = null)
        {
            set_executable_path_if_not_set();
            var args = build_args(config, _upgradeArguments);
            var packageResults = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var packageToInstall in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                var pkgName = packageToInstall;
                if (!string.IsNullOrWhiteSpace(config.Version))
                {
                    pkgName = "{0}=={1}".format_with(packageToInstall, config.Version);
                }

                var argsForPackage = args.Replace(PACKAGE_NAME_TOKEN, pkgName);

                var exitCode = _commandExecutor.execute(
                    _exePath,
                    argsForPackage,
                    config.CommandExecutionTimeoutSeconds,
                    _fileSystem.get_current_directory(),
                    (s, e) =>
                    {
                        var logMessage = e.Data;
                        if (string.IsNullOrWhiteSpace(logMessage)) return;
                        this.Log().Info(() => " [{0}] {1}".format_with(APP_NAME, logMessage.escape_curly_braces()));

                        if (ErrorRegex.IsMatch(logMessage) || ErrorNotFoundRegex.IsMatch(logMessage))
                        {
                            var results = packageResults.GetOrAdd(packageToInstall, new PackageResult(packageToInstall, null, null));
                            results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
                        }

                        if (InstalledRegex.IsMatch(logMessage))
                        {
                            var packageName = get_value_from_output(logMessage, PackageNameRegex, PACKAGE_NAME_GROUP);
                            var results = packageResults.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                            results.Messages.Add(new ResultMessage(ResultType.Note, packageName));
                            this.Log().Info(ChocolateyLoggers.Important, " {0} has been installed successfully.".format_with(string.IsNullOrWhiteSpace(packageName) ? packageToInstall : packageName));
                        }
                    },
                    (s, e) =>
                    {
                        var logMessage = e.Data;
                        if (string.IsNullOrWhiteSpace(logMessage)) return;
                        this.Log().Error("[{0}] {1}".format_with(APP_NAME, logMessage.escape_curly_braces()));

                        if (ErrorRegex.IsMatch(logMessage) || ErrorNotFoundRegex.IsMatch(logMessage))
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

        public void uninstall_noop(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            set_executable_path_if_not_set();
            var args = build_args(config, _uninstallArguments);
            this.Log().Info("Would have run '{0} {1}'".format_with(_exePath.escape_curly_braces(), args.escape_curly_braces()));
        }

        public ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config, Action<PackageResult> continueAction, Action<PackageResult> beforeUninstallAction = null)
        {
            set_executable_path_if_not_set();
            var args = build_args(config, _uninstallArguments);
            var packageResults = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var packageToInstall in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                var argsForPackage = args.Replace(PACKAGE_NAME_TOKEN, packageToInstall);

                var exitCode = _commandExecutor.execute(
                    _exePath,
                    argsForPackage,
                    config.CommandExecutionTimeoutSeconds,
                    _fileSystem.get_current_directory(),
                    (s, e) =>
                    {
                        var logMessage = e.Data;
                        if (string.IsNullOrWhiteSpace(logMessage)) return;
                        this.Log().Info(() => " [{0}] {1}".format_with(APP_NAME, logMessage.escape_curly_braces()));

                        if (ErrorRegex.IsMatch(logMessage) || ErrorNotFoundRegex.IsMatch(logMessage))
                        {
                            var results = packageResults.GetOrAdd(packageToInstall, new PackageResult(packageToInstall, null, null));
                            results.Messages.Add(new ResultMessage(ResultType.Error, packageToInstall));
                        }

                        if (UninstalledRegex.IsMatch(logMessage))
                        {
                            var packageName = get_value_from_output(logMessage, PackageNameRegex, PACKAGE_NAME_GROUP);
                            var results = packageResults.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                            results.Messages.Add(new ResultMessage(ResultType.Note, packageName));
                            this.Log().Info(ChocolateyLoggers.Important, " {0} has been uninstalled successfully.".format_with(string.IsNullOrWhiteSpace(packageName) ? packageToInstall : packageName));
                        }
                    },
                    (s, e) =>
                    {
                        var logMessage = e.Data;
                        if (string.IsNullOrWhiteSpace(logMessage)) return;
                        this.Log().Error("[{0}] {1}".format_with(APP_NAME, logMessage.escape_curly_braces()));

                        if (ErrorRegex.IsMatch(logMessage) || ErrorNotFoundRegex.IsMatch(logMessage))
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
        private static string get_value_from_output(string output, Regex regex, string groupName)
        {
            var matchGroup = regex.Match(output).Groups[groupName];
            if (matchGroup != null)
            {
                return matchGroup.Value;
            }

            return string.Empty;
        }
    }
}