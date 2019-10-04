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
    ///   Alternative Source for Cygwin
    /// </summary>
    /// <remarks>
    ///   https://cygwin.com/faq/faq.html#faq.setup.cli
    /// </remarks>
    public sealed class CygwinService : ISourceRunner
    {
        private readonly ICommandExecutor _commandExecutor;
        private readonly INugetService _nugetService;
        private readonly IFileSystem _fileSystem;
        private readonly IRegistryService _registryService;
        private const string PACKAGE_NAME_TOKEN = "{{packagename}}";
        private const string INSTALL_ROOT_TOKEN = "{{installroot}}";
        public const string CYGWIN_PACKAGE = "cygwin";
        private string _rootDirectory = string.Empty;

        private const string APP_NAME = "Cygwin";
        public const string PACKAGE_NAME_GROUP = "PkgName";
        public static readonly Regex InstalledRegex = new Regex(@"Extracting from file", RegexOptions.Compiled);
        public static readonly Regex PackageNameRegex = new Regex(@"/(?<{0}>[^/]*).tar.".format_with(PACKAGE_NAME_GROUP), RegexOptions.Compiled);

        private readonly IDictionary<string, ExternalCommandArgument> _installArguments = new Dictionary<string, ExternalCommandArgument>(StringComparer.InvariantCultureIgnoreCase);

        public CygwinService(ICommandExecutor commandExecutor, INugetService nugetService, IFileSystem fileSystem, IRegistryService registryService)
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
            set_install_dictionary(_installArguments);
        }

        /// <summary>
        ///   Sets install dictionary
        /// </summary>
        private void set_install_dictionary(IDictionary<string, ExternalCommandArgument> args)
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
            args.Add("_quiet_", new ExternalCommandArgument {ArgumentOption = "--quiet-mode", Required = true});
            args.Add("_no_desktop_", new ExternalCommandArgument {ArgumentOption = "--no-desktop", Required = true});
            args.Add("_no_startmenu_", new ExternalCommandArgument {ArgumentOption = "--no-startmenu", Required = true});
            args.Add("_root_", new ExternalCommandArgument
                {
                    ArgumentOption = "--root ",
                    ArgumentValue = INSTALL_ROOT_TOKEN,
                    QuoteValue = false,
                    Required = true
                });
            args.Add("_local_pkgs_dir_", new ExternalCommandArgument
                {
                    ArgumentOption = "--local-package-dir ",
                    ArgumentValue = "{0}\\packages".format_with(INSTALL_ROOT_TOKEN),
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
                    ArgumentValue = PACKAGE_NAME_TOKEN,
                    QuoteValue = false,
                    Required = true
                });
        }

        public SourceType SourceType
        {
            get { return SourceType.cygwin; }
        }

        public void ensure_source_app_installed(ChocolateyConfiguration config, Action<PackageResult> ensureAction)
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

            if (!localPackages.Any(p => p.Name.is_equal_to(CYGWIN_PACKAGE)))
            {
                runnerConfig.PackageNames = CYGWIN_PACKAGE;
                runnerConfig.Sources = ApplicationParameters.ChocolateyCommunityFeedSource;

                var prompt = config.PromptForConfirmation;
                config.PromptForConfirmation = false;
                _nugetService.install_run(runnerConfig, ensureAction.Invoke);
                config.PromptForConfirmation = prompt;
            }

            set_root_dir_if_not_set();
        }

        public int count_run(ChocolateyConfiguration config)
        {
            throw new NotImplementedException("Count is not supported for this source runner.");
        }

        public void set_root_dir_if_not_set()
        {
            if (!string.IsNullOrWhiteSpace(_rootDirectory)) return;

            var setupKey = _registryService.get_key(RegistryHive.LocalMachine, "SOFTWARE\\Cygwin\\setup");
            if (setupKey != null)
            {
                _rootDirectory = setupKey.GetValue("rootdir", string.Empty).to_string();
            }

            if (string.IsNullOrWhiteSpace(_rootDirectory))
            {
                var binRoot = Environment.GetEnvironmentVariable("ChocolateyBinRoot");
                if (string.IsNullOrWhiteSpace(binRoot)) binRoot = "c:\\tools";

                _rootDirectory = _fileSystem.combine_paths(binRoot,"cygwin");
            }
        }

        public string get_exe(string rootpath)
        {
            return _fileSystem.combine_paths(rootpath, "cygwinsetup.exe");
        }

        public void list_noop(ChocolateyConfiguration config)
        {
            this.Log().Warn(ChocolateyLoggers.Important, "{0} does not implement list".format_with(APP_NAME));
        }

        public IEnumerable<PackageResult> list_run(ChocolateyConfiguration config)
        {
            throw new NotImplementedException("{0} does not implement list".format_with(APP_NAME));
        }

        public string build_args(ChocolateyConfiguration config, IDictionary<string, ExternalCommandArgument> argsDictionary)
        {
            var args = ExternalCommandArgsBuilder.build_arguments(config, argsDictionary);

            args = args.Replace(INSTALL_ROOT_TOKEN, _rootDirectory);

            return args;
        }

        public void install_noop(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            var args = build_args(config, _installArguments);
            this.Log().Info("Would have run '{0} {1}'".format_with(get_exe(_rootDirectory).escape_curly_braces(), args.escape_curly_braces()));
        }

        public ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration config, Action<PackageResult> continueAction)
        {
            var args = build_args(config, _installArguments);
            var packageResults = new ConcurrentDictionary<string, PackageResult>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var packageToInstall in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                var argsForPackage = args.Replace(PACKAGE_NAME_TOKEN, packageToInstall);

                var exitCode = _commandExecutor.execute(
                    get_exe(_rootDirectory),
                    argsForPackage,
                    config.CommandExecutionTimeoutSeconds,
                    _fileSystem.get_current_directory(),
                    (s, e) =>
                        {
                            var logMessage = e.Data;
                            if (string.IsNullOrWhiteSpace(logMessage)) return;
                            this.Log().Info(() => " [{0}] {1}".format_with(APP_NAME, logMessage.escape_curly_braces()));

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
            throw new NotImplementedException("{0} does not implement upgrade".format_with(APP_NAME));
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