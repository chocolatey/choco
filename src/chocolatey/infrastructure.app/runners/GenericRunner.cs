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

namespace chocolatey.infrastructure.app.runners
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using chocolatey.infrastructure.app.services;
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

    public sealed class GenericRunner
    {
        private ICommand FindCommand(ChocolateyConfiguration config, Container container, bool isConsole, Action<ICommand> parseArgs)
        {
            var commands = container.GetAllInstances<ICommand>();
            var command = commands.Where((c) =>
                {
                    var attributes = c.GetType().GetCustomAttributes(typeof(CommandForAttribute), false);
                    return attributes.Cast<CommandForAttribute>().Any(attribute => attribute.CommandName.IsEqualTo(config.CommandName));
                }).FirstOrDefault();

            if (command == null)
            {
                //todo: #2581 add a search among other location/extensions for the command
                if (!string.IsNullOrWhiteSpace(config.CommandName))
                {
                    throw new Exception(@"Could not find a command registered that meets '{0}'.
 Try choco -? for command reference/help.".FormatWith(config.CommandName));
                }

                if (isConsole) Environment.ExitCode = 1;
            }
            else
            {
                if (parseArgs != null)
                {
                    parseArgs.Invoke(command);
                }

                if (command.MayRequireAdminAccess())
                {
                    WarnIfAdminAndNeedsElevation(config);
                }

                SetSourceType(config, container);
                // guaranteed that all settings are set.
                EnvironmentSettings.SetEnvironmentVariables(config);

                this.Log().Debug(() => "Configuration: {0}".FormatWith(config.ToString()));

                if (isConsole && (config.HelpRequested || config.UnsuccessfulParsing))
                {
#if DEBUG
                    Console.WriteLine("Press enter to continue...");
                    Console.ReadKey();
#endif
                    Environment.Exit(config.UnsuccessfulParsing ? 1 : 0);
                }

                var token = Assembly.GetExecutingAssembly().GetPublicKeyToken();
                if (string.IsNullOrWhiteSpace(token) || !token.IsEqualTo(ApplicationParameters.OfficialChocolateyPublicKey))
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

        private void SetSourceType(ChocolateyConfiguration config, Container container)
        {
            var sourceRunner = container.GetAllInstances<IAlternativeSourceRunner>()
                .FirstOrDefault(s => s.SourceType.IsEqualTo(config.Sources) || s.SourceType.IsEqualTo(config.Sources + "s"));

            var sourceType = SourceTypes.Normal;
            if (sourceRunner != null)
            {
                sourceType = sourceRunner.SourceType;
            }

            config.SourceType = sourceType;

            this.Log().Debug(() => "The source '{0}' evaluated to a '{1}' source type".FormatWith(config.Sources, sourceType));
        }

        public void FailOnMissingOrInvalidLicenseIfFeatureSet(ChocolateyConfiguration config)
        {
            if (!config.Features.FailOnInvalidOrMissingLicense ||
                config.CommandName.TrimSafe().IsEqualTo("feature") ||
                config.CommandName.TrimSafe().IsEqualTo("features")
            ) return;

            if (!config.Information.IsLicensedVersion) throw new ApplicationException("License is missing or invalid.");
        }

        public void Run(ChocolateyConfiguration config, Container container, bool isConsole, Action<ICommand> parseArgs)
        {
            var tasks = container.GetAllInstances<ITask>();
            foreach (var task in tasks)
            {
                task.Initialize();
            }

            FailOnMissingOrInvalidLicenseIfFeatureSet(config);
            HttpsSecurity.Reset();
            EventManager.Publish(new PreRunMessage(config));

            try
            {
                var command = FindCommand(config, container, isConsole, parseArgs);
                if (command != null)
                {
                    if (config.Noop)
                    {
                        if (config.RegularOutput)
                        {
                            this.Log().Info("_ {0}:{1} - Noop Mode _".FormatWith(ApplicationParameters.Name, command.GetType().Name));
                        }

                        command.DryRun(config);
                    }
                    else
                    {
                        this.Log().Debug("_ {0}:{1} - Normal Run Mode _".FormatWith(ApplicationParameters.Name, command.GetType().Name));
                        command.Run(config);
                    }
                }
            }
            finally
            {
                EventManager.Publish(new PostRunMessage(config));

                foreach (var task in tasks.OrEmpty())
                {
                    task.Shutdown();
                }

                RemoveNuGetCache(container, config);
            }
        }

        /// <summary>
        /// if there is a NuGetScratch cache found, kill it with fire
        /// </summary>
        /// <param name="container">The container.</param>
        private void RemoveNuGetCache(Container container)
        {
            RemoveNuGetCache(container, Config.GetConfigurationSettings());
        }

        /// <summary>
        /// if there is a NuGetScratch cache found, kill it with fire
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="config">optional Chocolatey configuration to look at cacheLocation</param>
        private void RemoveNuGetCache(Container container, ChocolateyConfiguration config)
        {
            try
            {
                var fileSystem = container.GetInstance<IFileSystem>();
                var scratch = fileSystem.CombinePaths(fileSystem.GetTempPath(), "NuGetScratch");
                fileSystem.DeleteDirectoryChecked(scratch, recursive: true, overrideAttributes: true, isSilent: true);
                var nugetX = fileSystem.CombinePaths(fileSystem.GetTempPath(), "x", "nuget");
                fileSystem.DeleteDirectoryChecked(nugetX, recursive: true, overrideAttributes: true, isSilent: true);

                if (config != null && !string.IsNullOrWhiteSpace(config.CacheLocation))
                {
                    scratch = fileSystem.CombinePaths(config.CacheLocation, "NuGetScratch");
                    fileSystem.DeleteDirectoryChecked(scratch, recursive: true, overrideAttributes: true, isSilent: true);
                    nugetX = fileSystem.CombinePaths(config.CacheLocation, "x", "nuget");
                    fileSystem.DeleteDirectoryChecked(nugetX, recursive: true, overrideAttributes: true, isSilent: true);
                }
            }
            catch (Exception ex)
            {
                this.Log().Debug(ChocolateyLoggers.Important, "Not able to cleanup NuGet temp folders. Failure was {0}".FormatWith(ex.Message));
            }
        }

        public IEnumerable<T> List<T>(ChocolateyConfiguration config, Container container, bool isConsole, Action<ICommand> parseArgs)
        {
            var tasks = container.GetAllInstances<ITask>();
            foreach (var task in tasks)
            {
                task.Initialize();
            }

            FailOnMissingOrInvalidLicenseIfFeatureSet(config);
            HttpsSecurity.Reset();
            EventManager.Publish(new PreRunMessage(config));

            try
            {
                var command = FindCommand(config, container, isConsole, parseArgs) as IListCommand<T>;
                if (command == null)
                {
                    if (!string.IsNullOrWhiteSpace(config.CommandName))
                    {
                        throw new Exception("The implementation of '{0}' does not support listing '{1}'".FormatWith(config.CommandName, typeof(T).Name));
                    }
                    return new List<T>();
                }
                else
                {
                    this.Log().Debug("_ {0}:{1} - Normal List Mode _".FormatWith(ApplicationParameters.Name, command.GetType().Name));
                    return command.List(config);
                }
            }
            finally
            {
                EventManager.Publish(new PostRunMessage(config));

                foreach (var task in tasks.OrEmpty())
                {
                    task.Shutdown();
                }

                RemoveNuGetCache(container, config);
            }
        }

        public int Count(ChocolateyConfiguration config, Container container, bool isConsole, Action<ICommand> parseArgs)
        {
            FailOnMissingOrInvalidLicenseIfFeatureSet(config);
            HttpsSecurity.Reset();

            var command = FindCommand(config, container, isConsole, parseArgs) as IListCommand;
            if (command == null)
            {
                if (!string.IsNullOrWhiteSpace(config.CommandName))
                {
                    throw new Exception("The implementation of '{0}' does not support listing.".FormatWith(config.CommandName));
                }
                return 0;
            }
            else
            {
                this.Log().Debug("_ {0}:{1} - Normal Count Mode _".FormatWith(ApplicationParameters.Name, command.GetType().Name));
                return command.Count(config);
            }
        }

        public void WarnIfAdminAndNeedsElevation(ChocolateyConfiguration config)
        {
            if (config.HelpRequested) return;

            // skip when commands will set or for background mode
            if (!config.Features.ShowNonElevatedWarnings) return;

            var shouldWarn = (!config.Information.IsProcessElevated && config.Information.IsUserAdministrator)
                          || (!config.Information.IsUserAdministrator && ApplicationParameters.InstallLocation.IsEqualTo(ApplicationParameters.CommonAppDataChocolatey));

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
                var selection = InteractivePrompt.PromptForConfirmation(@"
 Do you want to continue?", new[] { "yes", "no" },
                        defaultChoice: null,
                        requireAnswer: false,
                        allowShortAnswer: true,
                        shortPrompt: true,
                        timeoutInSeconds: timeoutInSeconds
                        );

                if (selection.IsEqualTo("no"))
                {
                    Environment.Exit(-1);
                }
            }
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void fail_when_license_is_missing_or_invalid_if_requested(ChocolateyConfiguration config)
            => FailOnMissingOrInvalidLicenseIfFeatureSet(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void run(ChocolateyConfiguration config, Container container, bool isConsole, Action<ICommand> parseArgs)
            => Run(config, container, isConsole, parseArgs);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public IEnumerable<T> list<T>(ChocolateyConfiguration config, Container container, bool isConsole, Action<ICommand> parseArgs)
            => List<T>(config, container, isConsole, parseArgs);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public int count(ChocolateyConfiguration config, Container container, bool isConsole, Action<ICommand> parseArgs)
            => Count(config, container, isConsole, parseArgs);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void warn_when_admin_needs_elevation(ChocolateyConfiguration config)
            => WarnIfAdminAndNeedsElevation(config);
#pragma warning restore IDE1006
    }
}
