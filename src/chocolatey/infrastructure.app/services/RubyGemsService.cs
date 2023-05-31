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
    using System.Linq;
    using System.Text.RegularExpressions;
    using configuration;
    using domain;
    using infrastructure.commands;
    using logging;
    using results;
    using platforms;

    public sealed class RubyGemsService : IBootstrappableSourceRunner, IListSourceRunner, IInstallSourceRunner
    {
        private readonly ICommandExecutor _commandExecutor;
        private readonly INugetService _nugetService;

        private const string PackageNameToken = "{{packagename}}";
        private const string ExePath = "cmd.exe";
        private const string AppName = "Ruby Gems";

        public const string RubyPortalPackage = "ruby.portable";
        public const string RubyPackage = "ruby";
        public const string PackageNameGroup = "PkgName";

        private static readonly Regex _installingRegex = new Regex(@"Fetching:", RegexOptions.Compiled);
        private static readonly Regex _installedRegex = new Regex(@"Successfully installed", RegexOptions.Compiled);
        private static readonly Regex _errorNotFoundRegex = new Regex(@"ERROR:  Could not find a valid gem", RegexOptions.Compiled);
        private static readonly Regex _packageNameFetchingRegex = new Regex(@"Fetching: (?<{0}>.*)\-".FormatWith(PackageNameGroup), RegexOptions.Compiled);
        private static readonly Regex _packageNameInstalledRegex = new Regex(@"Successfully installed (?<{0}>.*)\-".FormatWith(PackageNameGroup), RegexOptions.Compiled);
        private static readonly Regex _packageNameErrorRegex = new Regex(@"'(?<{0}>[^']*)'".FormatWith(PackageNameGroup), RegexOptions.Compiled);

        private readonly IDictionary<string, ExternalCommandArgument> _listArguments = new Dictionary<string, ExternalCommandArgument>(StringComparer.InvariantCultureIgnoreCase);
        private readonly IDictionary<string, ExternalCommandArgument> _installArguments = new Dictionary<string, ExternalCommandArgument>(StringComparer.InvariantCultureIgnoreCase);

        public RubyGemsService(ICommandExecutor commandExecutor, INugetService nugetService)
        {
            _commandExecutor = commandExecutor;
            _nugetService = nugetService;
            SetupCommandArgsDictionaries();
        }

        /// <summary>
        ///   Set any command arguments dictionaries necessary for the service
        /// </summary>
        private void SetupCommandArgsDictionaries()
        {
            SetupListDictionary(_listArguments);
            SetupInstallDictionary(_installArguments);
        }

        /// <summary>
        ///   Sets list dictionary
        /// </summary>
        private void SetupListDictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            args.Add("_cmd_c_", new ExternalCommandArgument { ArgumentOption = "/c", Required = true });
            args.Add("_gem_", new ExternalCommandArgument { ArgumentOption = "gem", Required = true });
            args.Add("_action_", new ExternalCommandArgument { ArgumentOption = "list", Required = true });
        }

        /// <summary>
        ///   Sets install dictionary
        /// </summary>
        private void SetupInstallDictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            args.Add("_cmd_c_", new ExternalCommandArgument { ArgumentOption = "/c", Required = true });
            args.Add("_gem_", new ExternalCommandArgument { ArgumentOption = "gem", Required = true });
            args.Add("_action_", new ExternalCommandArgument { ArgumentOption = "install", Required = true });
            args.Add("_package_name_", new ExternalCommandArgument
            {
                ArgumentOption = "package name ",
                ArgumentValue = PackageNameToken,
                QuoteValue = false,
                UseValueOnly = true,
                Required = true
            });

            args.Add("Force", new ExternalCommandArgument
            {
                ArgumentOption = "-f ",
                QuoteValue = false,
                Required = false
            });

            args.Add("Version", new ExternalCommandArgument
            {
                ArgumentOption = "--version ",
                QuoteValue = false,
                Required = false
            });
        }

        public string SourceType
        {
            get { return SourceTypes.Ruby; }
        }

        public void EnsureSourceAppInstalled(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> ensureAction)
        {
            if (Platform.GetPlatform() != PlatformType.Windows) throw new NotImplementedException("This source is not supported on non-Windows systems");

            var runnerConfig = new ChocolateyConfiguration
            {
                PackageNames = RubyPortalPackage,
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

            if (!localPackages.Any(p => p.Name.IsEqualTo(RubyPackage) || p.Name.IsEqualTo(RubyPortalPackage)))
            {
                runnerConfig.Sources = ApplicationParameters.ChocolateyCommunityFeedSource;

                var prompt = config.PromptForConfirmation;
                config.PromptForConfirmation = false;
                _nugetService.Install(runnerConfig, ensureAction.Invoke);
                config.PromptForConfirmation = prompt;
            }
        }

        public void ListDryRun(ChocolateyConfiguration config)
        {
            var args = ExternalCommandArgsBuilder.BuildArguments(config, _listArguments);
            this.Log().Info("Would have run '{0} {1}'".FormatWith(ExePath.EscapeCurlyBraces(), args.EscapeCurlyBraces()));
        }

        public IEnumerable<PackageResult> List(ChocolateyConfiguration config)
        {
            var packageResults = new List<PackageResult>();
            var args = ExternalCommandArgsBuilder.BuildArguments(config, _listArguments);

            Environment.ExitCode = _commandExecutor.Execute(
                ExePath,
                args,
                config.CommandExecutionTimeoutSeconds,
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
                updateProcessPath: false
                );

            return packageResults;
        }

        public void InstallDryRun(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
        {
            var args = ExternalCommandArgsBuilder.BuildArguments(config, _installArguments);
            args = args.Replace(PackageNameToken, config.PackageNames.Replace(';', ','));
            this.Log().Info("Would have run '{0} {1}'".FormatWith(ExePath.EscapeCurlyBraces(), args.EscapeCurlyBraces()));
        }

        public ConcurrentDictionary<string, PackageResult> Install(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
        {
            return Install(config, continueAction, beforeModifyAction: null);
        }

        public ConcurrentDictionary<string, PackageResult> Install(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeModifyAction)
        {
            var packageResults = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);
            var args = ExternalCommandArgsBuilder.BuildArguments(config, _installArguments);

            foreach (var packageToInstall in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                var argsForPackage = args.Replace(PackageNameToken, packageToInstall);
                var exitCode = _commandExecutor.Execute(
                    ExePath,
                    argsForPackage,
                    config.CommandExecutionTimeoutSeconds,
                    (s, e) =>
                        {
                            var logMessage = e.Data;
                            if (string.IsNullOrWhiteSpace(logMessage)) return;
                            this.Log().Info(() => " [{0}] {1}".FormatWith(AppName, logMessage.EscapeCurlyBraces()));

                            if (_installingRegex.IsMatch(logMessage))
                            {
                                var packageName = GetValueFromOutput(logMessage, _packageNameFetchingRegex, PackageNameGroup);
                                var results = packageResults.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                                this.Log().Info(ChocolateyLoggers.Important, "{0}".FormatWith(packageName));
                                return;
                            }

                            //if (string.IsNullOrWhiteSpace(packageName)) return;

                            if (_installedRegex.IsMatch(logMessage))
                            {
                                var packageName = GetValueFromOutput(logMessage, _packageNameInstalledRegex, PackageNameGroup);
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

                            var packageName = GetValueFromOutput(logMessage, _packageNameErrorRegex, PackageNameGroup);

                            if (_errorNotFoundRegex.IsMatch(logMessage))
                            {
                                var results = packageResults.GetOrAdd(packageName, new PackageResult(packageName, null, null));
                                results.Messages.Add(new ResultMessage(ResultType.Error, packageName));
                            }
                        },
                    updateProcessPath: false
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
        public const string RUBY_PORTABLE_PACKAGE = "ruby.portable";
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public const string RUBY_PACKAGE = "ruby";
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public const string PACKAGE_NAME_GROUP = "PkgName";
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static readonly Regex InstallingRegex = _installingRegex;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static readonly Regex InstalledRegex = _installedRegex;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static readonly Regex ErrorNotFoundRegex = _errorNotFoundRegex;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static readonly Regex PackageNameFetchingRegex = _packageNameFetchingRegex;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static readonly Regex PackageNameInstalledRegex = _packageNameInstalledRegex;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static readonly Regex PackageNameErrorRegex = _packageNameErrorRegex;

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void ensure_source_app_installed(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> ensureAction)
            => EnsureSourceAppInstalled(config, ensureAction);

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
#pragma warning restore IDE1006
    }
}
