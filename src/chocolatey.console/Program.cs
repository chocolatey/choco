﻿// Copyright © 2011 - Present RealDimensions Software, LLC
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
    using infrastructure.app;
    using infrastructure.app.builders;
    using infrastructure.app.configuration;
    using infrastructure.app.runners;
    using infrastructure.commandline;
    using infrastructure.configuration;
    using infrastructure.extractors;
    using infrastructure.licensing;
    using infrastructure.logging;
    using infrastructure.registration;
    using resources;
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
            try
            {
                add_assembly_resolver();

                string loggingLocation = ApplicationParameters.LoggingLocation;
                //no file system at this point
                if (!Directory.Exists(loggingLocation)) Directory.CreateDirectory(loggingLocation);

                Log4NetAppenderConfiguration.configure(loggingLocation);
                Bootstrap.initialize();
                Bootstrap.startup();
                var license = License.validate_license();
                var container = SimpleInjectorContainer.Container;

                var config = container.GetInstance<ChocolateyConfiguration>();
                var fileSystem = container.GetInstance<IFileSystem>();

                var warnings = new List<string>();

                ConfigurationBuilder.set_up_configuration(
                     args,
                     config,
                     container,
                     license,
                     warning => { warnings.Add(warning); }
                     );
                Config.initialize_with(config);

                report_version_and_exit_if_requested(args, config);

                trap_exit_scenarios(config);

                warn_on_nuspec_or_nupkg_usage(args, config);

                if (config.RegularOutput)
                {
                    "LogFileOnly".Log().Info(() => "".PadRight(60, '='));
#if DEBUG
                    "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "{0} v{1}{2} (DEBUG BUILD)".format_with(ApplicationParameters.Name, config.Information.ChocolateyProductVersion, license.is_licensed_version() ? " {0}".format_with(license.LicenseType) : string.Empty));
#else
                    if (config.Information.ChocolateyVersion == config.Information.ChocolateyProductVersion && args.Any())
                    {
                        "LogFileOnly".Log().Info(() => "{0} v{1}{2}".format_with(ApplicationParameters.Name, config.Information.ChocolateyProductVersion, license.is_licensed_version() ? " {0}".format_with(license.LicenseType) : string.Empty));
                    }
                    else
                    {
                        "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "{0} v{1}{2}".format_with(ApplicationParameters.Name, config.Information.ChocolateyProductVersion, license.is_licensed_version() ? " {0}".format_with(license.LicenseType) : string.Empty));
                    }
#endif
                    if (args.Length == 0)
                    {
                        "chocolatley".Log().Info(ChocolateyLoggers.Important, () => "Please run 'choco -?' or 'choco <command> -?' for help menu.");
                    }
                }

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
                    Environment.Exit(config.UnsuccessfulParsing? 1 : 0);
                }

                Log4NetAppenderConfiguration.set_logging_level_debug_when_debug(config.Debug, excludeLoggerName: "{0}LoggingColoredConsoleAppender".format_with(ChocolateyLoggers.Verbose.to_string()));
                Log4NetAppenderConfiguration.set_verbose_logger_when_verbose(config.Verbose, config.Debug, "{0}LoggingColoredConsoleAppender".format_with(ChocolateyLoggers.Verbose.to_string()));
                Log4NetAppenderConfiguration.set_trace_logger_when_trace(config.Trace, "{0}LoggingColoredConsoleAppender".format_with(ChocolateyLoggers.Trace.to_string()));
                "chocolatey".Log().Debug(() => "{0} is running on {1} v {2}".format_with(ApplicationParameters.Name, config.Information.PlatformType, config.Information.PlatformVersion.to_string()));
                //"chocolatey".Log().Debug(() => "Command Line: {0}".format_with(Environment.CommandLine));

                remove_old_chocolatey_exe(fileSystem);

                //refactor - thank goodness this is temporary, cuz manifest resource streams are dumb
                IList<string> folders = new List<string>
                    {
                        "helpers",
                        "functions",
                        "redirects",
                        "tools"
                    };
                AssemblyFileExtractor.extract_all_resources_to_relative_directory(fileSystem, Assembly.GetAssembly(typeof(ChocolateyResourcesAssembly)), ApplicationParameters.InstallLocation, folders, ApplicationParameters.ChocolateyFileResources);

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
                }

                Environment.ExitCode = 1;
            }
            finally
            {
                "chocolatey".Log().Debug(() => "Exiting with {0}".format_with(Environment.ExitCode));
#if DEBUG
                "chocolatey".Log().Info(() => "Exiting with {0}".format_with(Environment.ExitCode));
#endif
                pause_execution_if_debug();
                Bootstrap.shutdown();
                Environment.Exit(Environment.ExitCode);
            }
        }

        private static void warn_on_nuspec_or_nupkg_usage(string[] args, ChocolateyConfiguration config)
        {
            if (!args.Any(a => a.contains("pack") || a.contains("push")) && args.Any(a => a.contains(".nupkg") || a.contains(".nuspec")))
            {
                if (config.RegularOutput) "chocolatey".Log().Warn("The use of .nupkg or .nuspec in for package name or source is known to cause issues. Please use the package id from the nuspec `<id />` with `-s .` (for local folder where nupkg is found).");
            }
        }

        private static ResolveEventHandler _handler = null;

        private static void add_assembly_resolver()
        {
            _handler = (sender, args) =>
            {
                var requestedAssembly = new AssemblyName(args.Name);
                if (requestedAssembly.get_public_key_token().is_equal_to(ApplicationParameters.OfficialChocolateyPublicKey)
                    && !requestedAssembly.Name.is_equal_to("chocolatey.licensed")
                    && !requestedAssembly.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                {
                    return typeof(ConsoleApplication).Assembly;
                }

                try
                {
                    if (requestedAssembly.get_public_key_token().is_equal_to(ApplicationParameters.OfficialChocolateyPublicKey)
                        && requestedAssembly.Name.is_equal_to("chocolatey.licensed"))
                    {
                        return Assembly.LoadFile(ApplicationParameters.LicensedAssemblyLocation).UnderlyingType;
                    }
                }
                catch (Exception ex)
                {
                    "chocolatey".Log().Warn("Unable to load chocolatey.licensed assembly. {0}".format_with(ex.Message));
                }

                return null;
            };

            AppDomain.CurrentDomain.AssemblyResolve += _handler;
        }

        private static void report_version_and_exit_if_requested(string[] args, ChocolateyConfiguration config)
        {
            if (args == null || args.Length == 0) return;

            var firstArg = args.FirstOrDefault();
            if (firstArg.is_equal_to("-v") || firstArg.is_equal_to("--version"))
            {
                "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "{0}".format_with(config.Information.ChocolateyProductVersion));
                pause_execution_if_debug();
                Environment.Exit(0);
            }
        }

        private static void trap_exit_scenarios(ChocolateyConfiguration config)
        {
            ExitScenarioHandler.SetHandler();
        }

        private static void remove_old_chocolatey_exe(IFileSystem fileSystem)
        {
            try
            {
                fileSystem.delete_file(fileSystem.get_current_assembly_path() + ".old");
                fileSystem.delete_file(fileSystem.combine_paths(AppDomain.CurrentDomain.BaseDirectory, "choco.exe.old"));
            }
            catch (Exception ex)
            {
                "chocolatey".Log().Warn("Attempting to delete choco.exe.old ran into an issue:{0} {1}".format_with(Environment.NewLine, ex.Message));
            }
        }

        private static void pause_execution_if_debug()
        {
#if DEBUG
            Console.WriteLine("Press enter to continue...");
            Console.ReadKey();
#endif
        }
    }
}