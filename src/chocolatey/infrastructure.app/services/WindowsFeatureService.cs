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
    using System.Text.RegularExpressions;
    using configuration;
    using domain;
    using filesystem;
    using infrastructure.commands;
    using logging;
    using results;

    /// <summary>
    ///   Alternative Source for Enabling Windows Features
    /// </summary>
    /// <remarks>
    ///   https://technet.microsoft.com/en-us/library/hh825265.aspx?f=255&MSPPError=-2147217396
    ///   Win 7 - https://technet.microsoft.com/en-us/library/dd744311.aspx
    ///   Maybe Win2003/2008 - http://www.wincert.net/forum/files/file/8-deployment-image-servicing-and-management-dism/ | http://wincert.net/leli55PK/DISM/
    /// </remarks>
    public sealed class WindowsFeatureService : ISourceRunner
    {
        private readonly ICommandExecutor _commandExecutor;
        private readonly INugetService _nugetService;
        private readonly IFileSystem _fileSystem;
        private const string ALL_TOKEN = "{{all}}";
        private const string PACKAGE_NAME_TOKEN = "{{packagename}}";
        private const string LOG_LEVEL_TOKEN = "{{loglevel}}";
        private const string LOG_LEVEL_INFO = "3";
        private const string LOG_LEVEL_DEBUG = "4";
        private const string FEATURES_VALUE = "/Get-Features";
        private const string FORMAT_VALUE = "/Format:Table";
        private string _exePath = string.Empty;

        private static readonly IList<string> _exeLocations = new List<string>
            {
                Environment.ExpandEnvironmentVariables("%systemroot%\\sysnative\\dism.exe"),
                Environment.ExpandEnvironmentVariables("%systemroot%\\System32\\dism.exe"),
                "dism.exe"
            };

        private const string APP_NAME = "Windows Features";
        public const string PACKAGE_NAME_GROUP = "PkgName";
        public static readonly Regex InstalledRegex = new Regex(@"The operation completed successfully.", RegexOptions.Compiled);
        public static readonly Regex ErrorRegex = new Regex(@"Error:", RegexOptions.Compiled);
        public static readonly Regex ErrorNotFoundRegex = new Regex(@"Feature name .* is unknown", RegexOptions.Compiled);

        private readonly IDictionary<string, ExternalCommandArgument> _listArguments = new Dictionary<string, ExternalCommandArgument>(StringComparer.InvariantCultureIgnoreCase);
        private readonly IDictionary<string, ExternalCommandArgument> _installArguments = new Dictionary<string, ExternalCommandArgument>(StringComparer.InvariantCultureIgnoreCase);
        private readonly IDictionary<string, ExternalCommandArgument> _uninstallArguments = new Dictionary<string, ExternalCommandArgument>(StringComparer.InvariantCultureIgnoreCase);

        public WindowsFeatureService(ICommandExecutor commandExecutor, INugetService nugetService, IFileSystem fileSystem)
        {
            _commandExecutor = commandExecutor;
            _nugetService = nugetService;
            _fileSystem = fileSystem;
            set_cmd_args_dictionaries();
        }

        /// <summary>
        ///   Set any command arguments dictionaries necessary for the service
        /// </summary>
        private void set_cmd_args_dictionaries()
        {
            set_list_dictionary(_listArguments);
            set_install_dictionary(_installArguments);
            set_uninstall_dictionary(_uninstallArguments);
        }

        /// <summary>
        ///   Sets list dictionary
        /// </summary>
        private void set_list_dictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            set_common_args(args);
            args.Add("_features_", new ExternalCommandArgument {ArgumentOption = FEATURES_VALUE, Required = true});
            args.Add("_format_", new ExternalCommandArgument {ArgumentOption = FORMAT_VALUE, Required = true});
        }

        /// <summary>
        ///   Sets install dictionary
        /// </summary>
        private void set_install_dictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            set_common_args(args);

            args.Add("_feature_", new ExternalCommandArgument {ArgumentOption = "/Enable-Feature", Required = true});
            args.Add("_package_name_", new ExternalCommandArgument
                {
                    ArgumentOption = "/FeatureName:",
                    ArgumentValue = PACKAGE_NAME_TOKEN,
                    QuoteValue = false,
                    Required = true
                });
            // /All should be the final argument.
            args.Add("_all_", new ExternalCommandArgument {ArgumentOption = ALL_TOKEN, Required = true});
        }

        /// <summary>
        ///   Sets uninstall dictionary
        /// </summary>
        private void set_uninstall_dictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            set_common_args(args);

            // uninstall feature completely in 8/2012+ - /Remove
            // would need /source to bring it back

            args.Add("_feature_", new ExternalCommandArgument {ArgumentOption = "/Disable-Feature", Required = true});
            args.Add("_package_name_", new ExternalCommandArgument
                {
                    ArgumentOption = "/FeatureName:",
                    ArgumentValue = PACKAGE_NAME_TOKEN,
                    QuoteValue = false,
                    Required = true
                });
        }

        private void set_common_args(IDictionary<string, ExternalCommandArgument> args)
        {
            args.Add("_online_", new ExternalCommandArgument {ArgumentOption = "/Online", Required = true});
            args.Add("_english_", new ExternalCommandArgument {ArgumentOption = "/English", Required = true});
            args.Add("_loglevel_", new ExternalCommandArgument
                {
                    ArgumentOption = "/LogLevel=",
                    ArgumentValue = LOG_LEVEL_TOKEN,
                    QuoteValue = false,
                    Required = true
                });

            args.Add("_no_restart_", new ExternalCommandArgument {ArgumentOption = "/NoRestart", Required = true});
        }

        public SourceType SourceType
        {
            get { return SourceType.windowsfeatures; }
        }

        public void ensure_source_app_installed(ChocolateyConfiguration config, Action<PackageResult> ensureAction)
        {
            set_executable_path_if_not_set();
        }

        public int count_run(ChocolateyConfiguration config)
        {
            throw new NotImplementedException("Count is not supported for this source runner.");
        }

        public void set_executable_path_if_not_set()
        {
            if (!string.IsNullOrWhiteSpace(_exePath)) return;

            foreach (var location in _exeLocations)
            {
                if (_fileSystem.file_exists(location))
                {
                    _exePath = location;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(_exePath)) throw new FileNotFoundException("Unable to find suitable location for the executable. Searched the following locations: '{0}'".format_with(string.Join("; ", _exeLocations)));
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

        public string build_args(ChocolateyConfiguration config, IDictionary<string, ExternalCommandArgument> argsDictionary)
        {
            var args = ExternalCommandArgsBuilder.build_arguments(config, argsDictionary);

            // at least Windows 8/2012
            if (config.Information.PlatformVersion.Major > 6 || (config.Information.PlatformVersion.Major == 6 && config.Information.PlatformVersion.Minor >= 2))
            {
                args = args.Replace(ALL_TOKEN, "/All");
            }
            else
            {
                args = args.Replace(ALL_TOKEN, string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(config.Input))
            {
                args = args.Replace(FEATURES_VALUE, "/Get-FeatureInfo").Replace(FORMAT_VALUE, "/FeatureName:{0}".format_with(config.Input));
            }

            args = args.Replace(LOG_LEVEL_TOKEN, config.Debug ? LOG_LEVEL_DEBUG : LOG_LEVEL_INFO);

            return args;
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
                var packageName = packageToInstall;
                var results = packageResults.GetOrAdd(packageToInstall, new PackageResult(packageName, null, null));
                var argsForPackage = args.Replace(PACKAGE_NAME_TOKEN, packageName);

                //todo: detect windows feature is already enabled
                /*
                      $checkStatement=@"
                    `$dismInfo=(cmd /c `"$dism /Online /Get-FeatureInfo /FeatureName:$packageName`")
                    if(`$dismInfo -contains 'State : Enabled') {return}
                    if(`$dismInfo -contains 'State : Enable Pending') {return}
                    "@
                 */

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
                                results.Messages.Add(new ResultMessage(ResultType.Error, packageName));
                            }

                            if (InstalledRegex.IsMatch(logMessage))
                            {
                                results.Messages.Add(new ResultMessage(ResultType.Note, packageName));
                                this.Log().Info(ChocolateyLoggers.Important, " {0} has been installed successfully.".format_with(string.IsNullOrWhiteSpace(packageName) ? packageToInstall : packageName));
                            }
                        },
                    (s, e) =>
                        {
                            var logMessage = e.Data;
                            if (string.IsNullOrWhiteSpace(logMessage)) return;
                            this.Log().Error("[{0}] {1}".format_with(APP_NAME, logMessage.escape_curly_braces()));

                            results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
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
            this.Log().Warn(ChocolateyLoggers.Important, "{0} does not implement upgrade".format_with(APP_NAME));
            return new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);
        }

        public ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config, Action<PackageResult> continueAction, Action<PackageResult> beforeUpgradeAction = null)
        {
            set_executable_path_if_not_set();
            throw new NotImplementedException("{0} does not implement upgrade".format_with(APP_NAME));
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
                var packageName = packageToInstall;
                var results = packageResults.GetOrAdd(packageToInstall, new PackageResult(packageName, null, null));
                var argsForPackage = args.Replace(PACKAGE_NAME_TOKEN, packageName);

                //todo: detect windows feature is already disabled
                /*
                      $checkStatement=@"
                    `$dismInfo=(cmd /c `"$dism /Online /Get-FeatureInfo /FeatureName:$packageName`")
                    if(`$dismInfo -contains 'State : Disabled') {return}
                    if(`$dismInfo -contains 'State : Disable Pending') {return}
                    "@
                 */

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
                                results.Messages.Add(new ResultMessage(ResultType.Error, packageName));
                            }

                            if (InstalledRegex.IsMatch(logMessage))
                            {
                                results.Messages.Add(new ResultMessage(ResultType.Note, packageName));
                                this.Log().Info(ChocolateyLoggers.Important, " {0} has been uninstalled successfully.".format_with(string.IsNullOrWhiteSpace(packageName) ? packageToInstall : packageName));
                            }
                        },
                    (s, e) =>
                        {
                            var logMessage = e.Data;
                            if (string.IsNullOrWhiteSpace(logMessage)) return;
                            this.Log().Error("[{0}] {1}".format_with(APP_NAME, logMessage.escape_curly_braces()));

                            results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));
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
    }
}
