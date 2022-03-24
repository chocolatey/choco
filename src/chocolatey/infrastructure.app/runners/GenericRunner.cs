// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.app.runners
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using events;
    using filesystem;
    using infrastructure.events;
    using infrastructure.registration;
    using infrastructure.tasks;
    using SimpleInjector;
    using adapters;
    using attributes;
    using commandline;
    using configuration;
    using domain;
    using infrastructure.commands;
    using infrastructure.configuration;
    using logging;
    using Console = System.Console;
    using Environment = System.Environment;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Diagnostics;
    using Alphaleonis.Win32.Filesystem;

    public sealed class GenericRunner
    {
        private ICommand find_command(ChocolateyConfiguration config, Container container, bool isConsole, Action<ICommand> parseArgs)
        {
            var commands = container.GetAllInstances<ICommand>();
            var command = commands.Where((c) =>
                {
                    var attributes = c.GetType().GetCustomAttributes(typeof(CommandForAttribute), false);
                    return attributes.Cast<CommandForAttribute>().Any(attribute => attribute.CommandName.is_equal_to(config.CommandName));
                }).FirstOrDefault();

            if (command == null)
            {
                //todo: #2581 add a search among other location/extensions for the command
                if (!string.IsNullOrWhiteSpace(config.CommandName))
                {
                    throw new Exception(@"Could not find a command registered that meets '{0}'.
 Try choco -? for command reference/help.".format_with(config.CommandName));
                }

                if (isConsole) Environment.ExitCode = 1;
            }
            else
            {
                if (parseArgs != null)
                {
                    parseArgs.Invoke(command);
                }

                if (command.may_require_admin_access())
                {
                    if (!config.Features.AutomaticProcessElevation)
                    { 
                        warn_when_admin_needs_elevation(config);
                    } else {
                        var needElevation = (!config.Information.IsProcessElevated && config.Information.IsUserAdministrator);

                        if (needElevation)
                        {
                            "chocolatey".Log().Info(() => $"Command '{config.CommandName}' requires elevation - starting child process... ('automaticProcessElevation' feature is on)");
                            var args = Environment.GetCommandLineArgs().Skip(1).ToList();
                            //args.AddRange(new[] { "-waitpid", System.Diagnostics.Process.GetCurrentProcess().Id.ToString()});
                            var proc = StartElevatedProcess(true, $"'{config.CommandName}'-command", args.ToArray());
                            proc.WaitForExit();
                            "chocolatey".Log().Info(() => "Exiting with {0}".format_with(proc.ExitCode));
                            Environment.Exit(proc.ExitCode);
                        }
                    }
                }

                set_source_type(config);
                // guaranteed that all settings are set.
                EnvironmentSettings.set_environment_variables(config);

                this.Log().Debug(() => "Configuration: {0}".format_with(config.ToString()));


                if (isConsole && (config.HelpRequested || config.UnsuccessfulParsing))
                {
#if DEBUG
                    Console.WriteLine("Press enter to continue...");
                    Console.ReadKey();
#endif
                    Environment.Exit(config.UnsuccessfulParsing? 1 : 0);
                }

                var token = Assembly.GetExecutingAssembly().get_public_key_token();
                if (string.IsNullOrWhiteSpace(token) || !token.is_equal_to(ApplicationParameters.OfficialChocolateyPublicKey))
                {
                    if (!config.AllowUnofficialBuild)
                    {
                        throw new Exception(@"
Custom unofficial builds are not allowed by default.
 To override this behavior, explicitly set --allow-unofficial.
 See the help menu (choco -h) for options.");
                    }
                    else
                    {
                        this.Log().Warn(config.RegularOutput ? ChocolateyLoggers.Important : ChocolateyLoggers.LogFileOnly, @"
Chocolatey is not an official build (bypassed with --allow-unofficial).
 If you are seeing this message and it is not expected, your system may
 now be in a bad state. Only official builds are to be trusted.
"
                        );

                    }
                }
            }

            return command;
        }

        /// <summary>
        /// Starts console process with elevated priviledges. If end-user presses "No" to elevation prompt will throw exception.
        /// </summary>
        /// <param name="startFromMainApplicationDirectory">true = starts from main application directory, false = start from package directory / package name</param>
        /// <param name="commandName">command name</param>
        /// <param name="args">arguments to calling process</param>
        /// <returns>process if started successfully</returns>
        public static System.Diagnostics.Process StartElevatedProcess(bool startFromMainApplicationDirectory, String commandName, string[] args)
        {
            String cmdLine = EscapeArguments(args);
            bool hideCommandPrompt = args.Contains("-gui");
            String exe;

            exe = ApplicationParameters.ApplicationExecutableName + ".exe";
            if (startFromMainApplicationDirectory)
            {
                exe = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), exe);
            }
            else
            {
                exe = Path.Combine(ApplicationParameters.PackagesLocation, ApplicationParameters.ApplicationExecutableName, exe);
            }

            var startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = exe;
            startInfo.Arguments = cmdLine;
            startInfo.Verb = "runas";
            if (hideCommandPrompt)
            { 
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            }

            try
            {
                System.Diagnostics.Process proc = System.Diagnostics.Process.Start(startInfo);
                return proc;
            }
            catch (Exception ex)
            {
                System.ComponentModel.Win32Exception wex = ex as System.ComponentModel.Win32Exception;
                if (wex != null && wex.NativeErrorCode == 1223 /*ERROR_CANCELLED*/)
                {
                    throw new Exception($"{commandName} requires elevated priviledges to run");
                }

                throw ex;
            }
        }

        private void set_source_type(ChocolateyConfiguration config)
        {
            var sourceType = SourceType.normal;
            Enum.TryParse(config.Sources, true, out sourceType);
            config.SourceType = sourceType;

            this.Log().Debug(() => "The source '{0}' evaluated to a '{1}' source type".format_with(config.Sources, sourceType.to_string()));
        }

        public void fail_when_license_is_missing_or_invalid_if_requested(ChocolateyConfiguration config)
        {
            if (!config.Features.FailOnInvalidOrMissingLicense ||
                config.CommandName.trim_safe().is_equal_to("feature") ||
                config.CommandName.trim_safe().is_equal_to("features")
            ) return;

            if (!config.Information.IsLicensedVersion) throw new ApplicationException("License is missing or invalid.");
        }

        public void run(ChocolateyConfiguration config, Container container, bool isConsole, Action<ICommand> parseArgs)
        {
            var tasks = container.GetAllInstances<ITask>();
            foreach (var task in tasks)
            {
                task.initialize();
            }

            fail_when_license_is_missing_or_invalid_if_requested(config);
            SecurityProtocol.set_protocol(config, provideWarning:true);
            EventManager.publish(new PreRunMessage(config));

            try
            {
                var command = find_command(config, container, isConsole, parseArgs);
                if (command != null)
                {
                    if (config.Noop)
                    {
                        if (config.RegularOutput)
                        {
                            this.Log().Info("_ {0}:{1} - Noop Mode _".format_with(ApplicationParameters.Name, command.GetType().Name));
                        }

                        command.noop(config);
                    }
                    else
                    {
                        this.Log().Debug("_ {0}:{1} - Normal Run Mode _".format_with(ApplicationParameters.Name, command.GetType().Name));
                        command.run(config);
                    }
                }
            }
            finally
            {
                EventManager.publish(new PostRunMessage(config));

                foreach (var task in tasks.or_empty_list_if_null())
                {
                    task.shutdown();
                }

                remove_nuget_cache(container, config);
            }
        }

        /// <summary>
        /// if there is a NuGetScratch cache found, kill it with fire
        /// </summary>
        /// <param name="container">The container.</param>
        private void remove_nuget_cache(Container container)
        {
            remove_nuget_cache(container, Config.get_configuration_settings());
        }

        /// <summary>
        /// if there is a NuGetScratch cache found, kill it with fire
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="config">optional Chocolatey configuration to look at cacheLocation</param>
        private void remove_nuget_cache(Container container, ChocolateyConfiguration config)
        {
            try
            {
                var fileSystem = container.GetInstance<IFileSystem>();
                var scratch = fileSystem.combine_paths(fileSystem.get_temp_path(), "NuGetScratch");
                fileSystem.delete_directory_if_exists(scratch, recursive: true, overrideAttributes: true, isSilent: true);
                var nugetX = fileSystem.combine_paths(fileSystem.get_temp_path(), "x", "nuget");
                fileSystem.delete_directory_if_exists(nugetX, recursive: true, overrideAttributes: true, isSilent: true);

                if (config != null && !string.IsNullOrWhiteSpace(config.CacheLocation)) {
                    scratch = fileSystem.combine_paths(config.CacheLocation, "NuGetScratch");
                    fileSystem.delete_directory_if_exists(scratch, recursive: true, overrideAttributes: true, isSilent: true);
                    nugetX = fileSystem.combine_paths(config.CacheLocation, "x", "nuget");
                    fileSystem.delete_directory_if_exists(nugetX, recursive: true, overrideAttributes: true, isSilent: true);
                }
            }
            catch (Exception ex)
            {
                this.Log().Debug(ChocolateyLoggers.Important, "Not able to cleanup NuGet temp folders. Failure was {0}".format_with(ex.Message));
            }
        }

        public IEnumerable<T> list<T>(ChocolateyConfiguration config, Container container, bool isConsole, Action<ICommand> parseArgs)
        {
            var tasks = container.GetAllInstances<ITask>();
            foreach (var task in tasks)
            {
                task.initialize();
            }

            fail_when_license_is_missing_or_invalid_if_requested(config);
            SecurityProtocol.set_protocol(config, provideWarning: true);
            EventManager.publish(new PreRunMessage(config));

            try
            {
                var command = find_command(config, container, isConsole, parseArgs) as IListCommand<T>;
                if (command == null)
                {
                    if (!string.IsNullOrWhiteSpace(config.CommandName))
                    {
                        throw new Exception("The implementation of '{0}' does not support listing '{1}'".format_with(config.CommandName, typeof(T).Name));
                    }
                    return new List<T>();
                }
                else
                {
                    this.Log().Debug("_ {0}:{1} - Normal List Mode _".format_with(ApplicationParameters.Name, command.GetType().Name));
                    return command.list(config);
                }
            }
            finally
            {
                EventManager.publish(new PostRunMessage(config));

                foreach (var task in tasks.or_empty_list_if_null())
                {
                    task.shutdown();
                }

                remove_nuget_cache(container, config);
            }
        }

        public int count(ChocolateyConfiguration config, Container container, bool isConsole, Action<ICommand> parseArgs)
        {
            fail_when_license_is_missing_or_invalid_if_requested(config);
            SecurityProtocol.set_protocol(config, provideWarning: true);

            var command = find_command(config, container, isConsole, parseArgs) as IListCommand;
            if (command == null)
            {
                if (!string.IsNullOrWhiteSpace(config.CommandName))
                {
                    throw new Exception("The implementation of '{0}' does not support listing.".format_with(config.CommandName));
                }
                return 0;
            }
            else
            {
                this.Log().Debug("_ {0}:{1} - Normal Count Mode _".format_with(ApplicationParameters.Name, command.GetType().Name));
                return command.count(config);
            }
        }

        public void warn_when_admin_needs_elevation(ChocolateyConfiguration config)
        {
            if (config.HelpRequested) return;

            // skip when commands will set or for background mode
            if (!config.Features.ShowNonElevatedWarnings) return;

            var shouldWarn = (!config.Information.IsProcessElevated && config.Information.IsUserAdministrator)
                          || (!config.Information.IsUserAdministrator && ApplicationParameters.InstallLocation.is_equal_to(ApplicationParameters.CommonAppDataChocolatey));

            if (shouldWarn)
            {
                this.Log().Warn(ChocolateyLoggers.Important, @"Chocolatey detected you are not running from an elevated command shell
 (cmd/powershell).");
            }

            // NOTE: blended options may not have been fully initialized yet
            var timeoutInSeconds = config.PromptForConfirmation ? 0 : 20;

            if (shouldWarn)
            {
                this.Log().Warn(ChocolateyLoggers.Important, @"
 You may experience errors - many functions/packages
 require admin rights. Only advanced users should run choco w/out an
 elevated shell. When you open the command shell, you should ensure
 that you do so with ""Run as Administrator"" selected. If you are
 attempting to use Chocolatey in a non-administrator setting, you
 must select a different location other than the default install
 location. See
 https://docs.chocolatey.org/en-us/choco/setup#non-administrative-install
 for details.
");
                var selection = InteractivePrompt.prompt_for_confirmation(@"
 Do you want to continue?", new[] { "yes", "no" },
                        defaultChoice: null,
                        requireAnswer: false,
                        allowShortAnswer: true,
                        shortPrompt: true,
                        timeoutInSeconds: timeoutInSeconds
                        );

                if (selection.is_equal_to("no"))
                {
                    Environment.Exit(-1);
                }
            }
        }

        /// <summary>
        /// Copied from: http://csharptest.net/529/how-to-correctly-escape-command-line-arguments-in-c/index.html
        /// 
        /// Quotes all arguments that contain whitespace, or begin with a quote and returns a single
        /// argument string for use with Process.Start().
        /// </summary>
        /// <param name="args">A list of strings for arguments, may not contain null, '\0', '\r', or '\n'</param>
        /// <returns>The combined list of escaped/quoted strings</returns>
        /// <exception cref="System.ArgumentNullException">Raised when one of the arguments is null</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Raised if an argument contains '\0', '\r', or '\n'</exception>
        public static string EscapeArguments(params string[] args)
        {
            StringBuilder arguments = new StringBuilder();
            Regex invalidChar = new Regex("[\x00\x0a\x0d]");//  these can not be escaped
            Regex needsQuotes = new Regex(@"\s|""");//          contains whitespace or two quote characters
            Regex escapeQuote = new Regex(@"(\\*)(""|$)");//    one or more '\' followed with a quote or end of string
            for (int carg = 0; args != null && carg < args.Length; carg++)
            {
                if (args[carg] == null) { throw new ArgumentNullException("args[" + carg + "]"); }
                if (invalidChar.IsMatch(args[carg])) { throw new ArgumentOutOfRangeException("args[" + carg + "]"); }
                if (args[carg] == String.Empty) { arguments.Append("\"\""); }
                else if (!needsQuotes.IsMatch(args[carg])) { arguments.Append(args[carg]); }
                else
                {
                    arguments.Append('"');
                    arguments.Append(escapeQuote.Replace(args[carg], m =>
                    m.Groups[1].Value + m.Groups[1].Value +
                    (m.Groups[2].Value == "\"" ? "\\\"" : "")
                    ));
                    arguments.Append('"');
                }
                if (carg + 1 < args.Length)
                    arguments.Append(' ');
            }
            return arguments.ToString();
        }

    }
}

