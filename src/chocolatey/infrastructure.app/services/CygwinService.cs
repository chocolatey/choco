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
    using Microsoft.Win32;
    using configuration;
    using domain;
    using filesystem;
    using infrastructure.commands;
    using logging;
    using results;
    using platforms;

    /// <summary>
    ///   Alternative Source for Cygwin
    /// </summary>
    /// <remarks>
    ///   https://cygwin.com/faq/faq.html#faq.setup.cli
    /// </remarks>
    public sealed class CygwinService : IBootstrappableSourceRunner, IInstallSourceRunner
    {
        private readonly ICommandExecutor _commandExecutor;
        private readonly INugetService _nugetService;
        private readonly IFileSystem _fileSystem;
        private readonly IRegistryService _registryService;
        private const string PackageNameToken = "{{packagename}}";
        private const string InstallRootToken = "{{installroot}}";
        private const string CygwinPackage = "cygwin";

        private string _rootDirectory;
        private string RootDirectory
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_rootDirectory))
                {
                    _rootDirectory = GetRootDirectory();
                }

                return _rootDirectory;
            }
        }

        private const string AppName = "Cygwin";
        private const string PackageNameGroup = "PkgName";
        private static readonly Regex _installedRegex = new Regex(@"Extracting from file", RegexOptions.Compiled);
        private static readonly Regex _packageNameRegex = new Regex(@"/(?<{0}>[^/]*).tar.".FormatWith(PackageNameGroup), RegexOptions.Compiled);

        private readonly IDictionary<string, ExternalCommandArgument> _installArguments = new Dictionary<string, ExternalCommandArgument>(StringComparer.InvariantCultureIgnoreCase);

        public CygwinService(ICommandExecutor commandExecutor, INugetService nugetService, IFileSystem fileSystem, IRegistryService registryService)
        {
            _commandExecutor = commandExecutor;
            _nugetService = nugetService;
            _fileSystem = fileSystem;
            _registryService = registryService;
            StoreCommandArgs();
        }

        /// <summary>
        ///   Set any command arguments dictionaries necessary for the service
        /// </summary>
        private void StoreCommandArgs()
        {
            InitializeInstallDictionary(_installArguments);
        }

        /// <summary>
        ///   Sets install dictionary
        /// </summary>
        private void InitializeInstallDictionary(IDictionary<string, ExternalCommandArgument> args)
        {
            //args.Add("_cmd_c_", new ExternalCommandArgument { ArgumentOption = "/c", Required = true });
            //args.Add("_app_", new ExternalCommandArgument
            //{
            //    ArgumentOption = "",
            //    ArgumentValue = _fileSystem.combine_paths(INSTALL_ROOT_TOKEN, "cygwinsetup.exe"),
            //    QuoteValue = false,
            //    UseValueOnly = true,
            //    Required = true
            //});
            args.Add("_quiet_", new ExternalCommandArgument { ArgumentOption = "--quiet-mode", Required = true });
            args.Add("_no_desktop_", new ExternalCommandArgument { ArgumentOption = "--no-desktop", Required = true });
            args.Add("_no_startmenu_", new ExternalCommandArgument { ArgumentOption = "--no-startmenu", Required = true });
            args.Add("_root_", new ExternalCommandArgument
            {
                ArgumentOption = "--root ",
                ArgumentValue = InstallRootToken,
                QuoteValue = false,
                Required = true
            });
            args.Add("_local_pkgs_dir_", new ExternalCommandArgument
            {
                ArgumentOption = "--local-package-dir ",
                ArgumentValue = "{0}\\packages".FormatWith(InstallRootToken),
                QuoteValue = false,
                Required = true
            });

            args.Add("_site_", new ExternalCommandArgument
            {
                ArgumentOption = "--site ",
                ArgumentValue = "http://mirrors.kernel.org/sourceware/cygwin/",
                QuoteValue = false,
                Required = true
            });

            args.Add("_package_name_", new ExternalCommandArgument
            {
                ArgumentOption = "--packages ",
                ArgumentValue = PackageNameToken,
                QuoteValue = false,
                Required = true
            });
        }

        public string SourceType
        {
            get { return SourceTypes.Cygwin; }
        }

        public void EnsureSourceAppInstalled(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> ensureAction)
        {
            if (Platform.GetPlatform() != PlatformType.Windows) throw new NotImplementedException("This source is not supported on non-Windows systems");

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

            if (!localPackages.Any(p => p.Name.IsEqualTo(CygwinPackage)))
            {
                runnerConfig.PackageNames = CygwinPackage;
                runnerConfig.Sources = ApplicationParameters.ChocolateyCommunityFeedSource;

                var prompt = config.PromptForConfirmation;
                config.PromptForConfirmation = false;
                _nugetService.Install(runnerConfig, ensureAction.Invoke);
                config.PromptForConfirmation = prompt;
            }
        }

        private string GetRootDirectory()
        {
            var setupKey = _registryService.GetKey(RegistryHive.LocalMachine, "SOFTWARE\\Cygwin\\setup");
            if (setupKey != null)
            {
                return setupKey.GetValue("rootdir", string.Empty).ToStringSafe();
            }

            var binRoot = Environment.GetEnvironmentVariable("ChocolateyBinRoot");
            if (string.IsNullOrWhiteSpace(binRoot)) binRoot = "c:\\tools";

            return _fileSystem.CombinePaths(binRoot, "cygwin");
        }

        private string GetCygwinPath(string rootpath)
        {
            return _fileSystem.CombinePaths(rootpath, "cygwinsetup.exe");
        }

        private string BuildArgs(ChocolateyConfiguration config, IDictionary<string, ExternalCommandArgument> argsDictionary)
        {
            var args = ExternalCommandArgsBuilder.BuildArguments(config, argsDictionary);

            args = args.Replace(InstallRootToken, RootDirectory);

            return args;
        }

        public void InstallDryRun(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
        {
            var args = BuildArgs(config, _installArguments);
            this.Log().Info("Would have run '{0} {1}'".FormatWith(GetCygwinPath(RootDirectory).EscapeCurlyBraces(), args.EscapeCurlyBraces()));
        }

        public ConcurrentDictionary<string, PackageResult> Install(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction)
        {
            return Install(config, continueAction, beforeModifyAction: null);
        }

        public ConcurrentDictionary<string, PackageResult> Install(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> continueAction, Action<PackageResult, ChocolateyConfiguration> beforeModifyAction)
        {
            var args = BuildArgs(config, _installArguments);
            var packageResults = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var packageToInstall in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                var argsForPackage = args.Replace(PackageNameToken, packageToInstall);

                var exitCode = _commandExecutor.Execute(
                    GetCygwinPath(RootDirectory),
                    argsForPackage,
                    config.CommandExecutionTimeoutSeconds,
                    _fileSystem.GetCurrentDirectory(),
                    (s, e) =>
                        {
                            var logMessage = e.Data;
                            if (string.IsNullOrWhiteSpace(logMessage)) return;
                            this.Log().Info(() => " [{0}] {1}".FormatWith(AppName, logMessage.EscapeCurlyBraces()));

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
        public const string CYGWIN_PACKAGE = CygwinPackage;

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public const string PACKAGE_NAME_GROUP = PackageNameGroup;

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static readonly Regex PackageNameRegex = _packageNameRegex;

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void ensure_source_app_installed(ChocolateyConfiguration config, Action<PackageResult, ChocolateyConfiguration> ensureAction)
            => EnsureSourceAppInstalled(config, ensureAction);

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
