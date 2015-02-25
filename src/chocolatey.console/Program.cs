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
    using infrastructure.adapters;
    using infrastructure.app;
    using infrastructure.app.builders;
    using infrastructure.app.configuration;
    using infrastructure.app.runners;
    using infrastructure.commandline;
    using infrastructure.configuration;
    using infrastructure.extractors;
    using infrastructure.filesystem;
    using infrastructure.licensing;
    using infrastructure.logging;
    using infrastructure.platforms;
    using infrastructure.registration;
    using infrastructure.services;
    using resources;
    using Console = System.Console;
    using Environment = System.Environment;

    public sealed class Program
    {
// ReSharper disable InconsistentNaming
        private static void Main(string[] args)
// ReSharper restore InconsistentNaming
        {
            try
            {
                string loggingLocation = ApplicationParameters.LoggingLocation;
                //no file system at this point
                if (!Directory.Exists(loggingLocation)) Directory.CreateDirectory(loggingLocation);

                Log4NetAppenderConfiguration.configure(loggingLocation);
                Bootstrap.initialize();
                Bootstrap.startup();

                var container = SimpleInjectorContainer.Container;
                var config = container.GetInstance<ChocolateyConfiguration>();
                var fileSystem = container.GetInstance<IFileSystem>();

                var warnings = new List<string>();

                ConfigurationBuilder.set_up_configuration(
                    args,
                    config,
                    fileSystem,
                    container.GetInstance<IXmlService>(),
                    warning => { warnings.Add(warning); }
                    );
                Config.initialize_with(config);

                trap_exit_scenarios(config);

                if (config.RegularOuptut)
                {
                    "logfile".Log().Info(() => "".PadRight(60, '='));
#if DEBUG
                    "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "{0} v{1} (DEBUG BUILD)".format_with(ApplicationParameters.Name, config.Information.ChocolateyProductVersion));
#else
                    "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "{0} v{1}".format_with(ApplicationParameters.Name, config.Information.ChocolateyProductVersion));
#endif
                }

                if (warnings.Count != 0)
                {
                    foreach (var warning in warnings.or_empty_list_if_null())
                    {
                        "chocolatey".Log().Warn(ChocolateyLoggers.Important, warning);    
                    }
                }

                if (config.HelpRequested)
                {
                    pause_execution_if_debug();
                    Environment.Exit(-1);
                }

                Log4NetAppenderConfiguration.set_verbose_logger_when_verbose(config.Verbose, ChocolateyLoggers.Verbose.to_string());
                Log4NetAppenderConfiguration.set_logging_level_debug_when_debug(config.Debug);
                "chocolatey".Log().Debug(() => "{0} is running on {1} v {2}".format_with(ApplicationParameters.Name, config.Information.PlatformType, config.Information.PlatformVersion.to_string()));
                //"chocolatey".Log().Debug(() => "Command Line: {0}".format_with(Environment.CommandLine));

                warn_when_admin_needs_elevation(config);
                remove_old_chocolatey_exe(fileSystem);

                LicenseValidation.validate(fileSystem);

                //refactor - thank goodness this is temporary, cuz manifest resource streams are dumb
                IList<string> folders = new List<string>
                    {
                        "helpers",
                        "functions",
                        "redirects",
                        "tools"
                    };
                AssemblyFileExtractor.extract_all_resources_to_relative_directory(fileSystem, Assembly.GetAssembly(typeof (ChocolateyResourcesAssembly)), ApplicationParameters.InstallLocation, folders, ApplicationParameters.ChocolateyFileResources);

                var application = new ConsoleApplication();
                application.run(args, config, container);
            }
            catch (Exception ex)
            {
                var debug = false;
                // no access to the good stuff here, need to go a bit primitive in parsing args
                foreach (var arg in args.or_empty_list_if_null())
                {
                    if (arg.Contains("debug") || arg.is_equal_to("-d") || arg.is_equal_to("/d"))
                    {
                        debug = true;
                        break;
                    }
                }

                if (debug)
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

        static EventHandler _handler;

        private static void trap_exit_scenarios(ChocolateyConfiguration config)
        {
            ExitScenarioHandler.SetHandler();
        }

        private static void warn_when_admin_needs_elevation(ChocolateyConfiguration config)
        {
            // NOTE: blended options may not have been fully initialized yet
            if (!config.PromptForConfirmation) return;

            if (!config.Information.IsProcessElevated && config.Information.IsUserAdministrator)
            {
                var selection = InteractivePrompt.prompt_for_confirmation(@"
Chocolatey detected you are not running from an elevated command shell
 (cmd/powershell). You may experience errors - many functions/packages
 require admin rights. Only advanced users should run choco w/out an
 elevated shell. When you open the command shell, you should ensure 
 that you do so with ""Run as Administrator"" selected.

 Do you want to continue?", new[] {"yes", "no"}, "no", requireAnswer: true);

                if (selection.is_equal_to("no"))
                {
                    pause_execution_if_debug();
                    Environment.Exit(-1);
                }
            }
        }

        private static void remove_old_chocolatey_exe(IFileSystem fileSystem)
        {
            try
            {
                fileSystem.delete_file(Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", string.Empty) + ".old");
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