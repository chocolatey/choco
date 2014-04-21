namespace chocolatey.infrastructure.app.commands
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using attributes;
    using builders;
    using commandline;
    using configuration;
    using infrastructure.commands;
    using logging;
    using results;

    [CommandFor(CommandNameType.install)]
    public sealed class ChocolateyInstallCommand : ICommand
    {
        private const string PACKAGE_NAME_TOKEN = "{{packagename}}";
        private readonly string _nugetExePath = ApplicationParameters.Tools.NugetExe;
        private readonly IDictionary<string, ExternalCommandArgument> _nugetArguments = new Dictionary<string, ExternalCommandArgument>();

        public ChocolateyInstallCommand()
        {
            set_nuget_args_dictionary();
        }

        private void set_nuget_args_dictionary()
        {
            _nugetArguments.Add("_install_", new ExternalCommandArgument {ArgumentOption = "install", Required = true});
            _nugetArguments.Add("_package_name_", new ExternalCommandArgument {ArgumentOption = PACKAGE_NAME_TOKEN, Required = true});
            _nugetArguments.Add("Version", new ExternalCommandArgument {ArgumentOption = "-version ",});
            _nugetArguments.Add("_output_directory_", new ExternalCommandArgument
                {
                    ArgumentOption = "-outputdirectory ",
                    ArgumentValue = "{0}".format_with(ApplicationParameters.PackagesLocation),
                    QuoteValue = true,
                    Required = true
                });
            _nugetArguments.Add("Source", new ExternalCommandArgument {ArgumentOption = "-source ", QuoteValue = true});
            _nugetArguments.Add("Prerelease", new ExternalCommandArgument {ArgumentOption = "-prerelease"});
            _nugetArguments.Add("_non_interactive_", new ExternalCommandArgument {ArgumentOption = "-noninteractive", Required = true});
            _nugetArguments.Add("_no_cache_", new ExternalCommandArgument {ArgumentOption = "-nocache", Required = true});
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("s=|source=",
                     "Source - The source to find the package(s) to install. Special sources include: ruby, webpi, cygwin, windowsfeatures, and python. Defaults to default feeds.",
                     option => configuration.Source = option)
                .Add("version=",
                     "Version - A specific version to install. Defaults to unspecified.",
                     option => configuration.Version = option)
                .Add("pre|prerelease",
                     "Prerelease - Include Prereleases? Defaults to false.",
                     option => configuration.Prerelease = option != null)
                .Add("x86|forcex86",
                     "ForceX86 - Force x86 (32bit) installation on 64 bit systems. Defaults to false.",
                     option => configuration.ForceX86 = option != null)
                .Add("ia=|installargs=|installarguments=",
                     "InstallArguments - Install Arguments to pass to the native installer in the package. Defaults to unspecified.",
                     option => configuration.InstallArguments = option)
                .Add("o|override|overrideargs|overridearguments",
                     "OverrideArguments - Should install arguments be used exclusively without appending to current package passed arguments? Defaults to false.",
                     option => configuration.OverrideArguments = option != null)
                .Add("notsilent",
                     "NotSilent - Do not install this silently. Defaults to false.",
                     option => configuration.NotSilent = option != null)
                .Add("params=|parameters=|pkgparameters=|packageparameters=",
                     "PackageParameters - Parameters to pass to the package. Defaults to unspecified.",
                     option => configuration.PackageParameters = option)
                .Add("ignoredependencies",
                     "IgnoreDependencies - Ignore dependencies when installing package(s). Defaults to false.",
                     option => configuration.IgnoreDependencies = option != null)
                ;
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.PackageNames = string.Join(" ", unparsedArguments);
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Warn(ChocolateyLoggers.Important, "Install Command");
            this.Log().Info(@"
Installs a package or a list of packages (sometimes passed as a packages.config).

Usage: choco install pkg|packages.config [pkg2 pkgN] [options/switches]

NOTE: `all` is a special package keyword that will allow you to install all
 packages from a custom feed. Will not work with Chocolatey default feed.

");
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            this.Log().Info("{0} would have run the following to install packages:{1}'\"{2}\" {3}'".format_with(
                ApplicationParameters.Name,
                Environment.NewLine,
                _nugetExePath,
                ExternalCommandArgsBuilder.build_arguments(configuration, _nugetArguments)
                                )
                );
        }

        public void run(ChocolateyConfiguration configuration)
        {
            //todo:is this a packages.config? If so run that command (that will call back into here).
            //todo:are we installing from an alternate source? If so run that command instead

            //todo: upgrade this to IPackage
            var packageInstalls = new ConcurrentDictionary<string, PackageInstallResult>();
            var args = ExternalCommandArgsBuilder.build_arguments(configuration, _nugetArguments);

            this.Log().Info(@"Installing the following packages:");
            this.Log().Info(ChocolateyLoggers.Important, @"{0}".format_with(configuration.PackageNames));
            this.Log().Info(@"
By installing you accept licenses for the packages.
");

            foreach (var packageToInstall in configuration.PackageNames.Split(' '))
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
                            var results = packageInstalls.GetOrAdd(packageName, new PackageInstallResult(packageName, packageVersion));

                            
                            if (ApplicationParameters.OutputParser.Nuget.NotInstalled.IsMatch(logMessage))
                            {
                                this.Log().Error("{0} not installed: {1}".format_with(packageName, logMessage));
                                results.Messages.Add(new ResultMessage(ResultType.Error, logMessage));

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

                foreach (var packageInstall in packageInstalls.Where(p => p.Value.Success && !p.Value.Inconclusive).or_empty_list_if_null())
                {
                    //todo:move forward with packages that are able to be installed

                    //powershell


                    //batch/shim redirection

                    this.Log().Info(" {0} has been installed.".format_with(packageInstall.Value.Name));
                }
            }

            var installFailures = packageInstalls.Count(p => !p.Value.Success);
            this.Log().Info(() => @"{0}{1} installed {2}/{3} packages. {4} packages failed.{0}See the log for details.".format_with(
                Environment.NewLine,
                ApplicationParameters.Name,
                packageInstalls.Count(p => p.Value.Success && !p.Value.Inconclusive),
                packageInstalls.Count,
                installFailures));

            this.Log().Warn("Command not yet fully functional, stay tuned...");

            if (installFailures != 0 && Environment.ExitCode == 0)
            {
                Environment.ExitCode = 1;
            }
        }

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