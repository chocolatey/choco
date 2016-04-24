// Copyright © 2011 - Present RealDimensions Software, LLC
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
    using SimpleInjector;
    using adapters;
    using attributes;
    using commandline;
    using configuration;
    using domain;
    using infrastructure.commands;
    using logging;
    using Console = System.Console;
    using Environment = System.Environment;

    public sealed class GenericRunner
    {
        private ICommand find_command(ChocolateyConfiguration config, Container container, bool isConsole, Action<ICommand> parseArgs)
        {
            var commands = container.GetAllInstances<ICommand>();
            var command = commands.Where((c) =>
                {
                    var attributes = c.GetType().GetCustomAttributes(typeof (CommandForAttribute), false);
                    return attributes.Cast<CommandForAttribute>().Any(attribute => attribute.CommandName.is_equal_to(config.CommandName));
                }).FirstOrDefault();

            if (command == null)
            {
                //todo add a search among other location/extensions for the command
                if (!string.IsNullOrWhiteSpace(config.CommandName))
                {
                    throw new Exception("Could not find a command registered that meets '{0}'".format_with(config.CommandName));
                }

                if (isConsole) Environment.ExitCode = 1;
            }
            else
            {
                if (command.may_require_admin_access())
                {
                    warn_when_admin_needs_elevation(config);
                }

                if (parseArgs != null)
                {
                    parseArgs.Invoke(command);
                }

                set_source_type(config);

                this.Log().Debug(() => "Configuration: {0}".format_with(config.ToString()));


                if (isConsole && config.HelpRequested)
                {
#if DEBUG
                    Console.WriteLine("Press enter to continue...");
                    Console.ReadKey();
#endif
                    Environment.Exit(1);
                }

                var token = Assembly.GetExecutingAssembly().get_public_key_token();
                if (string.IsNullOrWhiteSpace(token) || token != ApplicationParameters.OfficialChocolateyPublicKey)
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
                        this.Log().Warn(ChocolateyLoggers.Important, @"
Chocolatey is not an official build (bypassed with --allow-unofficial).
 If you are seeing this message and it is not expected, your system may 
 now be in a bad state. Only official builds are to be trusted.
"
                        );

                    }
                }

                if (config.Noop)
                {
                    if (config.RegularOutput)
                    {
                        this.Log().Info("_ {0}:{1} - Noop Mode _".format_with(ApplicationParameters.Name, command.GetType().Name));
                    }

                    command.noop(config);
                    return null;
                }
            }
            return command;
        }

        private void set_source_type(ChocolateyConfiguration config)
        {
            var sourceType = SourceType.normal;
            Enum.TryParse(config.Sources, true, out sourceType);
            config.SourceType = sourceType;

            this.Log().Debug(()=> "The source '{0}' evaluated to a '{1}' source type".format_with(config.Sources,sourceType.to_string()));
        }

        public void fail_when_license_is_missing_or_invalid_if_requested(ChocolateyConfiguration config)
        {
            if (!config.Features.FailOnInvalidOrMissingLicense) return;
           
            if (!config.Information.IsLicensedVersion) throw new ApplicationException("License is missing or invalid.");
        }

        public void run(ChocolateyConfiguration config, Container container, bool isConsole, Action<ICommand> parseArgs)
        {
            fail_when_license_is_missing_or_invalid_if_requested(config);

            var command = find_command(config, container, isConsole, parseArgs);
            if(command != null)
            {
                this.Log().Debug("_ {0}:{1} - Normal Run Mode _".format_with(ApplicationParameters.Name, command.GetType().Name));
                command.run(config);
            }
        }

        public IEnumerable<T> list<T>(ChocolateyConfiguration config, Container container, bool isConsole, Action<ICommand> parseArgs)
        {
            fail_when_license_is_missing_or_invalid_if_requested(config);

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

        public int count(ChocolateyConfiguration config, Container container, bool isConsole, Action<ICommand> parseArgs)
        {
            fail_when_license_is_missing_or_invalid_if_requested(config);

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
            var shouldWarn = (!config.Information.IsProcessElevated && config.Information.IsUserAdministrator);

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
 that you do so with ""Run as Administrator"" selected.
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

    }
}

