namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using NuGet;
    using builders;
    using configuration;
    using guards;
    using infrastructure.commands;
    using logging;
    using nuget;
    using results;
    using IFileSystem = filesystem.IFileSystem;

    public class NugetService : INugetService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _nugetLogger;
        private const string PACKAGE_NAME_TOKEN = "{{packagename}}";
        private readonly string _nugetExePath = ApplicationParameters.Tools.NugetExe;
        private readonly IDictionary<string, ExternalCommandArgument> _nugetInstallArguments = new Dictionary<string, ExternalCommandArgument>();

        /// <summary>
        ///   Initializes a new instance of the <see cref="NugetService" /> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="nugetLogger">The nuget logger</param>
        public NugetService(IFileSystem fileSystem, ILogger nugetLogger)
        {
            _fileSystem = fileSystem;
            _nugetLogger = nugetLogger;
            set_nuget_args_dictionaries();
        }

        /// <summary>
        ///   Set any args dictionaries
        /// </summary>
        private void set_nuget_args_dictionaries()
        {
            set_nuget_install_dictionary();
        }

        /// <summary>
        ///   Sets the nuget install args dictionary
        /// </summary>
        private void set_nuget_install_dictionary()
        {
            _nugetInstallArguments.Add("_install_", new ExternalCommandArgument { ArgumentOption = "install", Required = true });
            _nugetInstallArguments.Add("_package_name_", new ExternalCommandArgument { ArgumentOption = PACKAGE_NAME_TOKEN, Required = true });
            _nugetInstallArguments.Add("Version", new ExternalCommandArgument { ArgumentOption = "-version ", });
            _nugetInstallArguments.Add("_output_directory_", new ExternalCommandArgument
                {
                    ArgumentOption = "-outputdirectory ",
                    ArgumentValue = "{0}".format_with(ApplicationParameters.PackagesLocation),
                    QuoteValue = true,
                    Required = true
                });
            _nugetInstallArguments.Add("Source", new ExternalCommandArgument { ArgumentOption = "-source ", QuoteValue = true });
            _nugetInstallArguments.Add("Prerelease", new ExternalCommandArgument { ArgumentOption = "-prerelease" });
            _nugetInstallArguments.Add("_non_interactive_", new ExternalCommandArgument { ArgumentOption = "-noninteractive", Required = true });
            _nugetInstallArguments.Add("_no_cache_", new ExternalCommandArgument { ArgumentOption = "-nocache", Required = true });
        }

        public void list_noop(ChocolateyConfiguration configuration)
        {
            this.Log().Info("{0} would have searched for '{1}' against the following source(s) :\"{2}\"".format_with(
                ApplicationParameters.Name,
                configuration.Input,
                configuration.Source
                                ));
        }

        public ConcurrentDictionary<string, PackageResult> list_run(ChocolateyConfiguration configuration, bool logResults = true)
        {
            var packageResults = new ConcurrentDictionary<string, PackageResult>();

            foreach (var package in NugetList.GetPackages(configuration, _nugetLogger).or_empty_list_if_null())
            {
                if (logResults)
                {
                    this.Log().Info("{0} {1}".format_with(package.Id, package.Version.to_string()));
                    if (configuration.Verbose)
                    {
                        this.Log().Info(" {0}{1} Tags:{2}".format_with(package.Description, Environment.NewLine, package.Tags));
                    }
                }
                else
                {
                    this.Log().Debug(() => "[Nuget] {0} {1}".format_with(package.Id, package.Version.to_string()));
                }
                packageResults.GetOrAdd(package.Id, new PackageResult(package.Id, package.Version.to_string()));
            }

            return packageResults;
        }

        public void install_noop(ChocolateyConfiguration configuration, Action<PackageResult> continueAction)
        {
            var args = ExternalCommandArgsBuilder.build_arguments(configuration, _nugetInstallArguments);
            this.Log().Info("{0} would have run the following to install packages:{1}'\"{2}\" {3}'".format_with(
                ApplicationParameters.Name,
                Environment.NewLine,
                _nugetExePath,
                args
                                ));

            var tempInstallsLocation = _fileSystem.combine_paths(_fileSystem.get_temp_path(), ApplicationParameters.Name, "TempInstalls_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_ffff"));
            _fileSystem.create_directory_if_not_exists(tempInstallsLocation);

            _nugetInstallArguments["_output_directory_"] = new ExternalCommandArgument
                {
                    ArgumentOption = "-outputdirectory ",
                    ArgumentValue = "{0}".format_with(tempInstallsLocation),
                    QuoteValue = true,
                    Required = true
                };

            install_run(configuration, continueAction);

            _fileSystem.delete_directory(tempInstallsLocation, recursive: true);
        }

        public ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration configuration, Action<PackageResult> continueAction)
        {
            //todo: upgrade this to IPackage
            var packageInstalls = new ConcurrentDictionary<string, PackageResult>();
            var args = ExternalCommandArgsBuilder.build_arguments(configuration, _nugetInstallArguments);

            foreach (var packageToInstall in configuration.PackageNames.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var argsForPackage = args.Replace(PACKAGE_NAME_TOKEN, packageToInstall);
                var exitCode = CommandExecutor.execute(
                    _nugetExePath, argsForPackage, true,
                    (s, e) =>
                    {
                        var logMessage = e.Data;
                        if (string.IsNullOrWhiteSpace(logMessage)) return;
                        this.Log().Debug(() => "[Nuget] {0}".format_with(logMessage));

                        var packageName = get_value_from_output(logMessage, ApplicationParameters.OutputParser.Nuget.PackageName, ApplicationParameters.OutputParser.Nuget.PACKAGE_NAME_GROUP);
                        var packageVersion = get_value_from_output(logMessage, ApplicationParameters.OutputParser.Nuget.PackageVersion, ApplicationParameters.OutputParser.Nuget.PACKAGE_VERSION_GROUP);

                        if (ApplicationParameters.OutputParser.Nuget.ResolvingDependency.IsMatch(logMessage))
                        {
                            return;
                        }

                        //todo: ignore dependencies
                        var results = packageInstalls.GetOrAdd(packageName, new PackageResult(packageName, packageVersion, _nugetInstallArguments["_output_directory_"].ArgumentValue));

                        if (ApplicationParameters.OutputParser.Nuget.NotInstalled.IsMatch(logMessage))
                        {
                            this.Log().Error("{0} not installed: {1}".format_with(packageName, logMessage));
                            results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));

                            return;
                        }

                        if (ApplicationParameters.OutputParser.Nuget.Installing.IsMatch(logMessage))
                        {
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(packageName)) return;

                        this.Log().Info(ChocolateyLoggers.Important, "{0} {1}".format_with(packageName, !string.IsNullOrWhiteSpace(packageVersion) ? "v" + packageVersion : string.Empty));

                        if (ApplicationParameters.OutputParser.Nuget.AlreadyInstalled.IsMatch(logMessage) && !configuration.Force)
                        {
                            results.Messages.Add(new ResultMessage(ResultType.Inconclusive, packageName));
                            this.Log().Warn(" Already installed.");
                            this.Log().Warn(ChocolateyLoggers.Important, " Use -force if you want to reinstall.".format_with(Environment.NewLine));
                            return;
                        }

                        results.Messages.Add(new ResultMessage(ResultType.Debug, "Moving forward with chocolatey portion of install."));
                        if (continueAction != null)
                        {
                            continueAction.Invoke(results);
                        }
                    },
                    (s, e) =>
                    {
                        if (string.IsNullOrWhiteSpace(e.Data)) return;
                        this.Log().Error(() => "{0}".format_with(e.Data));
                    }
                    );

                if (exitCode != 0)
                {
                    Environment.ExitCode = exitCode;
                }
            }

            return packageInstalls;
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