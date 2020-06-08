﻿// Copyright © 2017 - 2018 Chocolatey Software, Inc
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
    using System.Linq;
    using System.Text.RegularExpressions;
    using configuration;
    using domain;
    using infrastructure.commands;
    using logging;
    using results;

    public sealed class WebPiService : ISourceRunner
    {
        private readonly ICommandExecutor _commandExecutor;
        private readonly INugetService _nugetService;
        private const string PACKAGE_NAME_TOKEN = "{{packagename}}";
        private const string EXE_PATH = "webpicmd.exe";
        private const string APP_NAME = "Web Platform Installer";
        public const string WEB_PI_PACKAGE = "webpicmd";
        public const string PACKAGE_NAME_GROUP = "PkgName";
        public static readonly Regex InstallingRegex = new Regex(@"Started installing:", RegexOptions.Compiled);
        public static readonly Regex InstalledRegex = new Regex(@"Install completed \(Success\):", RegexOptions.Compiled);
        public static readonly Regex AlreadyInstalledRegex = new Regex(@"No products to be installed \(either not available or already installed\)", RegexOptions.Compiled);
        //public static readonly Regex NotInstalled = new Regex(@"not installed", RegexOptions.Compiled);
        public static readonly Regex PackageNameRegex = new Regex(@"'(?<{0}>[^']*)'".format_with(PACKAGE_NAME_GROUP), RegexOptions.Compiled);
      
        private readonly IDictionary<string, ExternalCommandArgument> _listArguments = new Dictionary<string, ExternalCommandArgument>(StringComparer.InvariantCultureIgnoreCase);
        private readonly IDictionary<string, ExternalCommandArgument> _installArguments = new Dictionary<string, ExternalCommandArgument>(StringComparer.InvariantCultureIgnoreCase);

        public WebPiService(ICommandExecutor commandExecutor, INugetService nugetService)
        {
            _commandExecutor = commandExecutor;
            _nugetService = nugetService;
            set_cmd_args_dictionaries();
        }

        /// <summary>
        ///   Set any command arguments dictionaries necessary for the service
        /// </summary>
        private void set_cmd_args_dictionaries()
        {
            set_list_dictionary(_listArguments);
            set_install_dictionary(_installArguments);
        }

        /// <summary>
        ///   Sets list dictionary
        /// </summary>
        private void set_list_dictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            args.Add("_action_", new ExternalCommandArgument {ArgumentOption = "/List", Required = true});
            args.Add("_list_option_", new ExternalCommandArgument {ArgumentOption = "/ListOption:All", Required = true});
        }

        /// <summary>
        ///   Sets install dictionary
        /// </summary>
        private void set_install_dictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            args.Add("_action_", new ExternalCommandArgument {ArgumentOption = "/Install", Required = true});
            args.Add("_accept_eula_", new ExternalCommandArgument {ArgumentOption = "/AcceptEula", Required = true});
            args.Add("_suppress_reboot_", new ExternalCommandArgument {ArgumentOption = "/SuppressReboot", Required = true});
            args.Add("_package_name_", new ExternalCommandArgument
                {
                    ArgumentOption = "/Products:",
                    ArgumentValue = PACKAGE_NAME_TOKEN,
                    QuoteValue = false,
                    Required = true
                });
        }

        public SourceType SourceType
        {
            get { return SourceType.webpi; }
        }

        public void ensure_source_app_installed(ChocolateyConfiguration config, Action<PackageResult> ensureAction)
        {
            var runnerConfig = new ChocolateyConfiguration
                {
                    PackageNames = WEB_PI_PACKAGE,
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

            if (!localPackages.Any(p => p.Name.is_equal_to(WEB_PI_PACKAGE)))
            {
                runnerConfig.Sources = ApplicationParameters.ChocolateyCommunityFeedSource;

                var prompt = config.PromptForConfirmation;
                config.PromptForConfirmation = false;
                _nugetService.install_run(runnerConfig, ensureAction.Invoke);
                config.PromptForConfirmation = prompt;
            }
        }

        public int count_run(ChocolateyConfiguration config)
        {
            throw new NotImplementedException("Count is not supported for this source runner.");
        }

        public void list_noop(ChocolateyConfiguration config)
        {
            var args = ExternalCommandArgsBuilder.build_arguments(config, _listArguments);
            this.Log().Info("Would have run '{0} {1}'".format_with(EXE_PATH.escape_curly_braces(), args.escape_curly_braces()));
        }

        public IEnumerable<PackageResult> list_run(ChocolateyConfiguration config)
        {
            var packageResults = new List<PackageResult>();
            var args = ExternalCommandArgsBuilder.build_arguments(config, _listArguments);

            //var whereToStartRecording = "---";
            //var whereToStopRecording = "--";
            //var recordingValues = false;

            Environment.ExitCode = _commandExecutor.execute(
                EXE_PATH,
                args,
                config.CommandExecutionTimeoutSeconds,
                workingDirectory: ApplicationParameters.ShimsLocation,
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

                        //if (recordingValues)
                        //{
                        //    var lineParts = logMessage.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        //    if (lineParts.Length > 1)
                        //    {
                        //        var pkgResult = new PackageResult(lineParts[0], null, null);
                        //        packageResults.GetOrAdd(lineParts[0], pkgResult);
                        //    }
                        //}

                        //if (logMessage.Contains(whereToStartRecording)) recordingValues = true;
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
            var args = ExternalCommandArgsBuilder.build_arguments(config, _installArguments);
            args = args.Replace(PACKAGE_NAME_TOKEN, config.PackageNames.Replace(';', ','));
            this.Log().Info("Would have run '{0} {1}'".format_with(EXE_PATH.escape_curly_braces(), args.escape_curly_braces()));
        }

        public ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            var packageResults = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);
            var args = ExternalCommandArgsBuilder.build_arguments(config, _installArguments);

            foreach (var packageToInstall in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                var argsForPackage = args.Replace(PACKAGE_NAME_TOKEN, packageToInstall);
                var exitCode = _commandExecutor.execute(
                    EXE_PATH,
                    argsForPackage,
                    config.CommandExecutionTimeoutSeconds,
                    ApplicationParameters.ShimsLocation,
                    (s, e) =>
                        {
                            var logMessage = e.Data;
                            if (string.IsNullOrWhiteSpace(logMessage)) return;
                            this.Log().Info(() => " [{0}] {1}".format_with(APP_NAME, logMessage.escape_curly_braces()));

                            var packageName = get_value_from_output(logMessage, PackageNameRegex, PACKAGE_NAME_GROUP);
                            var results = packageResults.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                            if (AlreadyInstalledRegex.IsMatch(logMessage))
                            {
                                results.Messages.Add(new ResultMessage(ResultType.Inconclusive, packageName));
                                this.Log().Warn(ChocolateyLoggers.Important, " [{0}] {1} already installed or doesn't exist. --force has no effect.".format_with(APP_NAME, string.IsNullOrWhiteSpace(packageName) ? packageToInstall : packageName));
                                return;
                            }

                            if (InstallingRegex.IsMatch(logMessage))
                            {
                                this.Log().Info(ChocolateyLoggers.Important, "{0}".format_with(packageName));
                                return;
                            }
                           
                            if (InstalledRegex.IsMatch(logMessage))
                            {
                                this.Log().Info(ChocolateyLoggers.Important, " {0} has been installed successfully.".format_with(string.IsNullOrWhiteSpace(packageName) ? packageToInstall : packageName));
                            }
                        },
                    (s, e) =>
                        {
                            if (string.IsNullOrWhiteSpace(e.Data)) return;
                            this.Log().Error(() => "[{0}] {1}".format_with(APP_NAME, e.Data.escape_curly_braces()));
                        },
                    updateProcessPath: false,
                    allowUseWindow:true
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
            this.Log().Warn(ChocolateyLoggers.Important, "{0} does not implement upgrade".format_with(APP_NAME));
            return new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);
        }

        public ConcurrentDictionary<string, PackageResult> upgrade_run(ChocolateyConfiguration config, Action<PackageResult> continueAction, Action<PackageResult> beforeUpgradeAction = null)
        {
            throw new NotImplementedException("{0} does not implement upgrade".format_with(APP_NAME));
        }

        public void uninstall_noop(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            this.Log().Warn(ChocolateyLoggers.Important, "{0} does not implement uninstall".format_with(APP_NAME));
        }

        public ConcurrentDictionary<string, PackageResult> uninstall_run(ChocolateyConfiguration config, Action<PackageResult> continueAction, Action<PackageResult> beforeUninstallAction = null)
        {
            throw new NotImplementedException("{0} does not implement uninstall".format_with(APP_NAME));
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