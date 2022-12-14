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

namespace chocolatey.console
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Win32;
    using chocolatey.infrastructure.information;
    using infrastructure.app;
    using infrastructure.app.builders;
    using infrastructure.app.configuration;
    using infrastructure.app.runners;
    using infrastructure.commandline;
    using infrastructure.extractors;
    using infrastructure.licensing;
    using infrastructure.logging;
    using infrastructure.platforms;
    using infrastructure.registration;
    using infrastructure.tolerance;
    using SimpleInjector;

#if !NoResources

    using resources;

#endif

    using Assembly = infrastructure.adapters.Assembly;
    using Console = System.Console;
    using Environment = System.Environment;
    using IFileSystem = infrastructure.filesystem.IFileSystem;

    public sealed class Program
    {
        // ReSharper disable InconsistentNaming
        private static void Main(string[] args)
        // ReSharper restore InconsistentNaming
        {
            ChocolateyConfiguration config = null;
            ChocolateyLicense license = null;

            try
            {
                add_assembly_resolver();

                string loggingLocation = ApplicationParameters.LoggingLocation;
                //no file system at this point
                if (!Directory.Exists(loggingLocation)) Directory.CreateDirectory(loggingLocation);

                Log4NetAppenderConfiguration.configure(loggingLocation, excludeLoggerNames: ChocolateyLoggers.Trace.to_string());
                Bootstrap.initialize();
                Bootstrap.startup();
                license = License.validate_license();
                var container = SimpleInjectorContainer.Container;

                "LogFileOnly".Log().Info(() => "".PadRight(60, '='));

                config = container.GetInstance<ChocolateyConfiguration>();
                var fileSystem = container.GetInstance<IFileSystem>();

                var warnings = new List<string>();

                if (license.AssemblyLoaded && !is_licensed_assembly_loaded(container))
                {
                    license.AssemblyLoaded = false;
                    license.IsCompatible = false;
                }

                ConfigurationBuilder.set_up_configuration(
                     args,
                     config,
                     container,
                     license,
                     warning => { warnings.Add(warning); }
                     );

                if (license.AssemblyLoaded && license.is_licensed_version() && !license.IsCompatible && !config.DisableCompatibilityChecks)
                {
                    write_warning_for_incompatible_versions();
                }

                if (config.Features.LogWithoutColor)
                {
                    ApplicationParameters.Log4NetConfigurationResource = @"chocolatey.infrastructure.logging.log4net.nocolor.config.xml";
                    Log4NetAppenderConfiguration.configure(loggingLocation, excludeLoggerNames: ChocolateyLoggers.Trace.to_string());
                }

                if (!string.IsNullOrWhiteSpace(config.AdditionalLogFileLocation))
                {
                    Log4NetAppenderConfiguration.configure_additional_log_file(fileSystem.get_full_path(config.AdditionalLogFileLocation));
                }

                report_version_and_exit_if_requested(args, config);

                trap_exit_scenarios(config);

                warn_on_nuspec_or_nupkg_usage(args, config);

                if (config.RegularOutput)
                {
#if DEBUG
                    "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "{0} v{1}{2} (DEBUG BUILD)".format_with(ApplicationParameters.Name, config.Information.ChocolateyProductVersion, license.is_licensed_version() ? " {0}".format_with(license.LicenseType) : string.Empty));
#else
                    "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "{0} v{1}{2}".format_with(ApplicationParameters.Name, config.Information.ChocolateyProductVersion, license.is_licensed_version() ? " {0}".format_with(license.LicenseType) : string.Empty));
#endif
                    if (args.Length == 0)
                    {
                        "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "Please run 'choco -?' or 'choco <command> -?' for help menu.");
                    }
                }

                check_installed_dotnetfx_version();

                if (warnings.Count != 0 && config.RegularOutput)
                {
                    foreach (var warning in warnings.or_empty_list_if_null())
                    {
                        "chocolatey".Log().Warn(ChocolateyLoggers.Important, warning);
                    }
                }

                if (config.HelpRequested || config.UnsuccessfulParsing)
                {
                    pause_execution_if_debug();
                    Environment.Exit(config.UnsuccessfulParsing ? 1 : 0);
                }

                var verboseAppenderName = "{0}LoggingColoredConsoleAppender".format_with(ChocolateyLoggers.Verbose.to_string());
                var traceAppenderName = "{0}LoggingColoredConsoleAppender".format_with(ChocolateyLoggers.Trace.to_string());
                Log4NetAppenderConfiguration.set_logging_level_debug_when_debug(config.Debug, verboseAppenderName, traceAppenderName);
                Log4NetAppenderConfiguration.set_verbose_logger_when_verbose(config.Verbose, config.Debug, verboseAppenderName);
                Log4NetAppenderConfiguration.set_trace_logger_when_trace(config.Trace, traceAppenderName);
                "chocolatey".Log().Debug(() => "{0} is running on {1} v {2}".format_with(ApplicationParameters.Name, config.Information.PlatformType, config.Information.PlatformVersion.to_string()));
                //"chocolatey".Log().Debug(() => "Command Line: {0}".format_with(Environment.CommandLine));

                remove_old_chocolatey_exe(fileSystem);

                AssemblyFileExtractor.extract_all_resources_to_relative_directory(fileSystem, Assembly.GetAssembly(typeof(Program)), ApplicationParameters.InstallLocation, new List<string>(), "chocolatey.console", throwError: false);
                //refactor - thank goodness this is temporary, cuz manifest resource streams are dumb
                IList<string> folders = new List<string>
                    {
                        "helpers",
                        "functions",
                        "redirects",
                        "tools"
                    };
#if !NoResources
                AssemblyFileExtractor.extract_all_resources_to_relative_directory(fileSystem, Assembly.GetAssembly(typeof(ChocolateyResourcesAssembly)), ApplicationParameters.InstallLocation, folders, ApplicationParameters.ChocolateyFileResources, throwError: false);
#endif
                var application = new ConsoleApplication();
                application.run(args, config, container);
            }
            catch (Exception ex)
            {
                if (ApplicationParameters.is_debug_mode_cli_primitive())
                {
                    "chocolatey".Log().Error(() => "{0} had an error occur:{1}{2}".format_with(
                        ApplicationParameters.Name,
                        Environment.NewLine,
                        ex.ToString()));
                }
                else
                {
                    "chocolatey".Log().Error(ChocolateyLoggers.Important, () => "{0}".format_with(ex.Message));
                    "chocolatey".Log().Error(ChocolateyLoggers.LogFileOnly, () => "More Details: {0}".format_with(ex.ToString()));
                }

                if (Environment.ExitCode == 0) Environment.ExitCode = 1;
            }
            finally
            {
                if (license != null && license.AssemblyLoaded && license.is_licensed_version() && !license.IsCompatible && config != null && !config.DisableCompatibilityChecks)
                {
                    write_warning_for_incompatible_versions();
                }

                "chocolatey".Log().Debug(() => "Exiting with {0}".format_with(Environment.ExitCode));
#if DEBUG
                "chocolatey".Log().Info(() => "Exiting with {0}".format_with(Environment.ExitCode));
#endif
                pause_execution_if_debug();
                Bootstrap.shutdown();
                Environment.Exit(Environment.ExitCode);
            }
        }

        private static bool is_licensed_assembly_loaded(Container container)
        {
            var allExtensions = container.GetAllInstances<ExtensionInformation>();

            foreach (var extension in allExtensions)
            {
                if (extension.Name.is_equal_to("chocolatey.licensed"))
                {
                    return extension.Status == ExtensionStatus.Enabled || extension.Status == ExtensionStatus.Loaded;
                }
            }

            // We will be going by an assumption that it has been loaded in this case.
            // This is mostly to prevent that the licensed extension won't be disabled
            // if it has been loaded using old method.

            return true;
        }

        private static void warn_on_nuspec_or_nupkg_usage(string[] args, ChocolateyConfiguration config)
        {
            var commandLine = Environment.CommandLine;
            if (!(commandLine.contains(" pack ") || commandLine.contains(" push ") || commandLine.contains("convert")) && (commandLine.contains(".nupkg") || commandLine.contains(".nuspec")))
            {
                if (config.RegularOutput) "chocolatey".Log().Warn("The use of .nupkg or .nuspec in for package name or source is known to cause issues. Please use the package id from the nuspec `<id />` with `-s .` (for local folder where nupkg is found).");
            }
        }

        private static void add_assembly_resolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolution.resolve_extension_or_merged_assembly;
        }

        private static void report_version_and_exit_if_requested(string[] args, ChocolateyConfiguration config)
        {
            if (args == null || args.Length == 0) return;

            var firstArg = args.FirstOrDefault();
            if (firstArg.is_equal_to("-v") || firstArg.is_equal_to("--version"))
            {
                "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "{0}".format_with(config.Information.ChocolateyProductVersion));
                pause_execution_if_debug();
                "chocolatey".Log().Debug(() => "Exiting with 0");
                Environment.Exit(0);
            }
        }

        private static void trap_exit_scenarios(ChocolateyConfiguration config)
        {
            ExitScenarioHandler.SetHandler();
        }

        private static void remove_old_chocolatey_exe(IFileSystem fileSystem)
        {
            FaultTolerance.try_catch_with_logging_exception(
                () =>
                {
                    fileSystem.delete_file(fileSystem.get_current_assembly_path() + ".old");
                    fileSystem.delete_file(fileSystem.combine_paths(AppDomain.CurrentDomain.BaseDirectory, "choco.exe.old"));
                },
                errorMessage: "Attempting to delete choco.exe.old ran into an issue",
                throwError: false,
                logWarningInsteadOfError: true,
                logDebugInsteadOfError: false,
                isSilent: true
                );
        }

        private static void pause_execution_if_debug()
        {
#if DEBUG
            Console.WriteLine("Press enter to continue...");
            Console.ReadKey();
#endif
        }

        private static void write_warning_for_incompatible_versions()
        {
            "chocolatey".Log().Warn(
                ChocolateyLoggers.Important,
                @"WARNING!

You are running a version of Chocolatey that may not be compatible with
the currently installed version of the chocolatey.extension package.
Running Chocolatey with the current version of the chocolatey.extension
package is an unsupported configuration.

See https://ch0.co/compatibility for more information.

If you are in the process of modifying the chocolatey.extension package,
you can ignore this warning.

Additionally, you can ignore these warnings by either setting the
DisableCompatibilityChecks feature:

choco feature enable --name=""'disableCompatibilityChecks'""

Or by passing the --skip-compatibility-checks option when executing a
command.");
        }

        private static void check_installed_dotnetfx_version()
        {
            if (Platform.get_platform() == PlatformType.Windows)
            {
                // https://learn.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#minimum-version
                const int NET48RELEASEBUILD = 528040;
                const string REGKEY = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

                using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(REGKEY))
                {
                    if (ndpKey == null || ndpKey.GetValue("Release") == null || (int)ndpKey.GetValue("Release") < NET48RELEASEBUILD)
                    {
                        throw new ApplicationException(
                            @".NET 4.8 is not installed or may need a reboot to complete installation.
Please install .NET Framework 4.8 manually and reboot the system.
Download at 'https://download.visualstudio.microsoft.com/download/pr/2d6bb6b2-226a-4baa-bdec-798822606ff1/8494001c276a4b96804cde7829c04d7f/ndp48-x86-x64-allos-enu.exe'"
                                .format_with(Environment.NewLine));
                    }
                }
            }
        }
    }
}
