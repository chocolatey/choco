﻿// Copyright © 2017 - 2022 Chocolatey Software, Inc
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

namespace Chocolatey.Infrastructure.App.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using Configuration;
    using Domain;
    using FileSystem;
    using Infrastructure.Commands;
    using Logging;
    using Results;
    using Platforms;

    /// <summary>
    ///   Alternative Source for Enabling Windows Features
    /// </summary>
    /// <remarks>
    ///   <![CDATA[https://technet.microsoft.com/en-us/library/hh825265.aspx?f=255&MSPPError=-2147217396]]>
    ///   Win 7 - https://technet.microsoft.com/en-us/library/dd744311.aspx
    ///   Maybe Win2003/2008 - http://www.wincert.net/forum/files/file/8-deployment-image-servicing-and-management-dism/ | http://wincert.net/leli55PK/DISM/
    /// </remarks>
    public sealed class WindowsFeatureService : ISourceRunner
    {
        private readonly ICommandExecutor _commandExecutor;
        private readonly INugetService _nugetService;
        private readonly IFileSystem _fileSystem;
        private const string AllToken = "{{all}}";
        private const string PackageNameToken = "{{packagename}}";
        private const string LogLevelToken = "{{loglevel}}";
        private const string LogLevelInfo = "3";
        private const string LogLevelDebug = "4";
        private const string FeaturesValue = "/Get-Features";
        private const string FormatValue = "/Format:Table";
        private string _exePath = string.Empty;
        private const string AppName = "Windows Features";

        public const string PackageNameGroup = "PkgName";

        private static readonly IList<string> _exeLocations = new List<string>
            {
                Environment.ExpandEnvironmentVariables("%systemroot%\\sysnative\\dism.exe"),
                Environment.ExpandEnvironmentVariables("%systemroot%\\System32\\dism.exe"),
                "dism.exe"
            };

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
            SetupCommandArgsDictionaries();
        }

        /// <summary>
        ///   Set any command arguments dictionaries necessary for the service
        /// </summary>
        private void SetupCommandArgsDictionaries()
        {
            SetupListDictionary(_listArguments);
            SetupInstallDictionary(_installArguments);
            SetupUninstallDictionary(_uninstallArguments);
        }

        /// <summary>
        ///   Sets list dictionary
        /// </summary>
        private void SetupListDictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            AddCommonArguments(args);
            args.Add("_features_", new ExternalCommandArgument { ArgumentOption = FeaturesValue, Required = true });
            args.Add("_format_", new ExternalCommandArgument { ArgumentOption = FormatValue, Required = true });
        }

        /// <summary>
        ///   Sets install dictionary
        /// </summary>
        private void SetupInstallDictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            AddCommonArguments(args);

            args.Add("_feature_", new ExternalCommandArgument { ArgumentOption = "/Enable-Feature", Required = true });
            args.Add("_package_name_", new ExternalCommandArgument
            {
                ArgumentOption = "/FeatureName:",
                ArgumentValue = PackageNameToken,
                QuoteValue = false,
                Required = true
            });
            // /All should be the final argument.
            args.Add("_all_", new ExternalCommandArgument { ArgumentOption = AllToken, Required = true });
        }

        /// <summary>
        ///   Sets uninstall dictionary
        /// </summary>
        private void SetupUninstallDictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            AddCommonArguments(args);

            // uninstall feature completely in 8/2012+ - /Remove
            // would need /source to bring it back

            args.Add("_feature_", new ExternalCommandArgument { ArgumentOption = "/Disable-Feature", Required = true });
            args.Add("_package_name_", new ExternalCommandArgument
            {
                ArgumentOption = "/FeatureName:",
                ArgumentValue = PackageNameToken,
                QuoteValue = false,
                Required = true
            });
        }

        private void AddCommonArguments(IDictionary<string, ExternalCommandArgument> args)
        {
            args.Add("_online_", new ExternalCommandArgument { ArgumentOption = "/Online", Required = true });
            args.Add("_english_", new ExternalCommandArgument { ArgumentOption = "/English", Required = true });
            args.Add("_loglevel_", new ExternalCommandArgument
            {
                ArgumentOption = "/LogLevel=",
                ArgumentValue = LogLevelToken,
                QuoteValue = false,
                Required = true
            });

            args.Add("_no_restart_", new ExternalCommandArgument { ArgumentOption = "/NoRestart", Required = true });
        }

        public string SourceType
        {
            get { return SourceTypes.WindowsFeatures; }
        }

        public void EnsureSourceAppInstalled(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> ensureAction)
        {
            if (Platform.GetPlatform() != PlatformType.Windows) throw new NotImplementedException("This source is not supported on non-Windows systems");

            EnsureExecutablePathSet();
        }

        public int Count(ChocolateyConfiguration config)
        {
            throw new NotImplementedException("Count is not supported for this source runner.");
        }

        private void EnsureExecutablePathSet()
        {
            if (!string.IsNullOrWhiteSpace(_exePath)) return;

            foreach (var location in _exeLocations)
            {
                if (_fileSystem.FileExists(location))
                {
                    _exePath = location;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(_exePath)) throw new FileNotFoundException("Unable to find suitable location for the executable. Searched the following locations: '{0}'".FormatWith(string.Join("; ", _exeLocations)));
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

        private string BuildArguments(ChocolateyConfiguration config, IDictionary<string, ExternalCommandArgument> argsDictionary)
        {
            var args = ExternalCommandArgsBuilder.BuildArguments(config, argsDictionary);

            // at least Windows 8/2012
            if (config.Information.PlatformVersion.Major > 6 || (config.Information.PlatformVersion.Major == 6 && config.Information.PlatformVersion.Minor >= 2))
            {
                args = args.Replace(AllToken, "/All");
            }
            else
            {
                args = args.Replace(AllToken, string.Empty);
            }

            if (!string.IsNullOrWhiteSpace(config.Input))
            {
                args = args.Replace(FeaturesValue, "/Get-FeatureInfo").Replace(FormatValue, "/FeatureName:{0}".FormatWith(config.Input));
            }

            args = args.Replace(LogLevelToken, config.Debug ? LogLevelDebug : LogLevelInfo);

            return args;
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
                var packageName = packageToInstall;
                var results = packageResults.GetOrAdd(packageToInstall, new PackageResult(packageName, null, null));
                var argsForPackage = args.Replace(PackageNameToken, packageName);

                //todo: #2574 detect windows feature is already enabled
                /*
                      $checkStatement=@"
                    `$dismInfo=(cmd /c `"$dism /Online /Get-FeatureInfo /FeatureName:$packageName`")
                    if(`$dismInfo -contains 'State : Enabled') {return}
                    if(`$dismInfo -contains 'State : Enable Pending') {return}
                    "@
                 */

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

                            if (ErrorRegex.IsMatch(logMessage) || ErrorNotFoundRegex.IsMatch(logMessage))
                            {
                                results.Messages.Add(new ResultMessage(ResultType.Error, packageName));
                            }

                            if (InstalledRegex.IsMatch(logMessage))
                            {
                                results.Messages.Add(new ResultMessage(ResultType.Note, packageName));
                                this.Log().Info(ChocolateyLoggers.Important, " {0} has been installed successfully.".FormatWith(string.IsNullOrWhiteSpace(packageName) ? packageToInstall : packageName));
                            }
                        },
                    (s, e) =>
                        {
                            var logMessage = e.Data;
                            if (string.IsNullOrWhiteSpace(logMessage)) return;
                            this.Log().Error("[{0}] {1}".FormatWith(AppName, logMessage.EscapeCurlyBraces()));

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

        public ConcurrentDictionary<string, PackageResult> UpgradeDryRun(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
        {
            EnsureExecutablePathSet();
            this.Log().Warn(ChocolateyLoggers.Important, "{0} does not implement upgrade".FormatWith(AppName));
            return new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);
        }

        public ConcurrentDictionary<string, PackageResult> Upgrade(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeUpgradeAction = null)
        {
            EnsureExecutablePathSet();
            throw new NotImplementedException("{0} does not implement upgrade".FormatWith(AppName));
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
                var packageName = packageToInstall;
                var results = packageResults.GetOrAdd(packageToInstall, new PackageResult(packageName, null, null));
                var argsForPackage = args.Replace(PackageNameToken, packageName);

                //todo: #2574 detect windows feature is already disabled
                /*
                      $checkStatement=@"
                    `$dismInfo=(cmd /c `"$dism /Online /Get-FeatureInfo /FeatureName:$packageName`")
                    if(`$dismInfo -contains 'State : Disabled') {return}
                    if(`$dismInfo -contains 'State : Disable Pending') {return}
                    "@
                 */

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

                            if (ErrorRegex.IsMatch(logMessage) || ErrorNotFoundRegex.IsMatch(logMessage))
                            {
                                results.Messages.Add(new ResultMessage(ResultType.Error, packageName));
                            }

                            if (InstalledRegex.IsMatch(logMessage))
                            {
                                results.Messages.Add(new ResultMessage(ResultType.Note, packageName));
                                this.Log().Info(ChocolateyLoggers.Important, " {0} has been uninstalled successfully.".FormatWith(string.IsNullOrWhiteSpace(packageName) ? packageToInstall : packageName));
                            }
                        },
                    (s, e) =>
                        {
                            var logMessage = e.Data;
                            if (string.IsNullOrWhiteSpace(logMessage)) return;
                            this.Log().Error("[{0}] {1}".FormatWith(AppName, logMessage.EscapeCurlyBraces()));

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
