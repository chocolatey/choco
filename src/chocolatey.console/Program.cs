// Copyright © 2017 - 2023 Chocolatey Software, Inc
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
        private static void Main(string[] args)
        {
            ChocolateyConfiguration config = null;
            ChocolateyLicense license = null;

            try
            {
                AddAssemblyResolver();

                string loggingLocation = ApplicationParameters.LoggingLocation;
                //no file system at this point
                if (!Directory.Exists(loggingLocation)) Directory.CreateDirectory(loggingLocation);

                Log4NetAppenderConfiguration.Configure(loggingLocation, excludeLoggerNames: ChocolateyLoggers.Trace.ToStringSafe());
                Bootstrap.Initialize();
                Bootstrap.Startup();
                license = License.ValidateLicense();
                var container = SimpleInjectorContainer.Container;

                "LogFileOnly".Log().Info(() => "".PadRight(60, '='));

                config = container.GetInstance<ChocolateyConfiguration>();
                var fileSystem = container.GetInstance<IFileSystem>();

                var warnings = new List<string>();

                if (license.AssemblyLoaded && !IsLicensedAssemblyLoaded(container))
                {
                    license.AssemblyLoaded = false;
                    license.IsCompatible = false;
                }

                ConfigurationBuilder.SetupConfiguration(
                     args,
                     config,
                     container,
                     license,
                     warning => { warnings.Add(warning); }
                     );

                if (license.AssemblyLoaded && license.IsLicensedVersion() && !license.IsCompatible && !config.DisableCompatibilityChecks)
                {
                    WriteWarningForIncompatibleExtensionVersion();
                }

                if (config.Features.LogWithoutColor)
                {
                    ApplicationParameters.Log4NetConfigurationResource = @"chocolatey.infrastructure.logging.log4net.nocolor.config.xml";
                    Log4NetAppenderConfiguration.Configure(loggingLocation, excludeLoggerNames: ChocolateyLoggers.Trace.ToStringSafe());
                }

                if (!string.IsNullOrWhiteSpace(config.AdditionalLogFileLocation))
                {
                    Log4NetAppenderConfiguration.SetupAdditionalLogFile(fileSystem.GetFullPath(config.AdditionalLogFileLocation));
                }

                ReportVersionAndExitIfRequested(args, config);

                TrapExitScenarios(config);

                if (config.RegularOutput)
                {
#if DEBUG
                    "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "{0} v{1}{2} (DEBUG BUILD)".FormatWith(ApplicationParameters.Name, config.Information.ChocolateyProductVersion, license.IsLicensedVersion() ? " {0}".FormatWith(license.LicenseType) : string.Empty));
#else
                    "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "{0} v{1}{2}".FormatWith(ApplicationParameters.Name, config.Information.ChocolateyProductVersion, license.IsLicensedVersion() ? " {0}".FormatWith(license.LicenseType) : string.Empty));
#endif
                    if (args.Length == 0)
                    {
                        "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "Please run 'choco -?' or 'choco <command> -?' for help menu.");
                    }
                }

                ThrowIfNotDotNet48();

                if (warnings.Count != 0 && config.RegularOutput)
                {
                    foreach (var warning in warnings.OrEmpty())
                    {
                        "chocolatey".Log().Warn(ChocolateyLoggers.Important, warning);
                    }
                }

                if (config.HelpRequested || config.UnsuccessfulParsing)
                {
                    PauseIfDebug();
                    Environment.Exit(config.UnsuccessfulParsing ? 1 : 0);
                }

                var verboseAppenderName = "{0}LoggingColoredConsoleAppender".FormatWith(ChocolateyLoggers.Verbose.ToStringSafe());
                var traceAppenderName = "{0}LoggingColoredConsoleAppender".FormatWith(ChocolateyLoggers.Trace.ToStringSafe());
                Log4NetAppenderConfiguration.EnableDebugLoggingIf(config.Debug, verboseAppenderName, traceAppenderName);
                Log4NetAppenderConfiguration.EnableVerboseLoggingIf(config.Verbose, config.Debug, verboseAppenderName);
                Log4NetAppenderConfiguration.EnableTraceLoggingIf(config.Trace, traceAppenderName);
                "chocolatey".Log().Debug(() => "{0} is running on {1} v {2}".FormatWith(ApplicationParameters.Name, config.Information.PlatformType, config.Information.PlatformVersion.ToStringSafe()));
                //"chocolatey".Log().Debug(() => "Command Line: {0}".format_with(Environment.CommandLine));

                RemoveOldChocoExe(fileSystem);

                AssemblyFileExtractor.ExtractAssemblyResourcesToRelativeDirectory(fileSystem, Assembly.GetAssembly(typeof(Program)), ApplicationParameters.InstallLocation, new List<string>(), "chocolatey.console", throwError: false);
                //refactor - thank goodness this is temporary, cuz manifest resource streams are dumb
                IList<string> folders = new List<string>
                    {
                        "helpers",
                        "functions",
                        "redirects",
                        "tools"
                    };
#if !NoResources
                AssemblyFileExtractor.ExtractAssemblyResourcesToRelativeDirectory(fileSystem, Assembly.GetAssembly(typeof(ChocolateyResourcesAssembly)), ApplicationParameters.InstallLocation, folders, ApplicationParameters.ChocolateyFileResources, throwError: false);
#endif
                var application = new ConsoleApplication();
                application.Run(args, config, container);
            }
            catch (Exception ex)
            {
                if (ApplicationParameters.IsDebugModeCliPrimitive())
                {
                    "chocolatey".Log().Error(() => "{0} had an error occur:{1}{2}".FormatWith(
                        ApplicationParameters.Name,
                        Environment.NewLine,
                        ex.ToString()));
                }
                else
                {
                    "chocolatey".Log().Error(ChocolateyLoggers.Important, () => "{0}".FormatWith(ex.Message));
                    "chocolatey".Log().Error(ChocolateyLoggers.LogFileOnly, () => "More Details: {0}".FormatWith(ex.ToString()));
                }

                if (Environment.ExitCode == 0) Environment.ExitCode = 1;
            }
            finally
            {
                if (license != null && license.AssemblyLoaded && license.IsLicensedVersion() && !license.IsCompatible && config != null && !config.DisableCompatibilityChecks)
                {
                    WriteWarningForIncompatibleExtensionVersion();
                }

                "chocolatey".Log().Debug(() => "Exiting with {0}".FormatWith(Environment.ExitCode));
#if DEBUG
                "chocolatey".Log().Info(() => "Exiting with {0}".FormatWith(Environment.ExitCode));
#endif
                PauseIfDebug();
                Bootstrap.Shutdown();
                Environment.Exit(Environment.ExitCode);
            }
        }

        private static bool IsLicensedAssemblyLoaded(Container container)
        {
            var allExtensions = container.GetAllInstances<ExtensionInformation>();

            foreach (var extension in allExtensions)
            {
                if (extension.Name.IsEqualTo("chocolatey.licensed"))
                {
                    return extension.Status == ExtensionStatus.Enabled || extension.Status == ExtensionStatus.Loaded;
                }
            }

            // We will be going by an assumption that it has been loaded in this case.
            // This is mostly to prevent that the licensed extension won't be disabled
            // if it has been loaded using old method.

            return true;
        }

        private static void AddAssemblyResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolution.ResolveExtensionOrMergedAssembly;
        }

        private static void ReportVersionAndExitIfRequested(string[] args, ChocolateyConfiguration config)
        {
            if (args == null || args.Length == 0) return;

            var firstArg = args.FirstOrDefault();
            if (firstArg.IsEqualTo("-v") || firstArg.IsEqualTo("--version"))
            {
                "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "{0}".FormatWith(config.Information.ChocolateyProductVersion));
                PauseIfDebug();
                "chocolatey".Log().Debug(() => "Exiting with 0");
                Environment.Exit(0);
            }
        }

        private static void TrapExitScenarios(ChocolateyConfiguration config)
        {
            ExitScenarioHandler.SetHandler();
        }

        private static void RemoveOldChocoExe(IFileSystem fileSystem)
        {
            FaultTolerance.TryCatchWithLoggingException(
                () =>
                {
                    fileSystem.DeleteFile(fileSystem.GetCurrentAssemblyPath() + ".old");
                    fileSystem.DeleteFile(fileSystem.CombinePaths(AppDomain.CurrentDomain.BaseDirectory, "choco.exe.old"));
                },
                errorMessage: "Attempting to delete choco.exe.old ran into an issue",
                throwError: false,
                logWarningInsteadOfError: true,
                logDebugInsteadOfError: false,
                isSilent: true
                );
        }

        private static void PauseIfDebug()
        {
#if DEBUG
            Console.WriteLine("Press enter to continue...");
            Console.ReadKey();
#endif
        }

        private static void WriteWarningForIncompatibleExtensionVersion()
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

        private static void ThrowIfNotDotNet48()
        {
            if (Platform.GetPlatform() == PlatformType.Windows)
            {
                // https://learn.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#minimum-version
                const int net48ReleaseBuild = 528040;
                const string regKey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

                using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(regKey))
                {
                    if (ndpKey == null || ndpKey.GetValue("Release") == null || (int)ndpKey.GetValue("Release") < net48ReleaseBuild)
                    {
                        throw new ApplicationException(
                            @".NET 4.8 is not installed or may need a reboot to complete installation.
Please install .NET Framework 4.8 manually and reboot the system.
Download at 'https://download.visualstudio.microsoft.com/download/pr/2d6bb6b2-226a-4baa-bdec-798822606ff1/8494001c276a4b96804cde7829c04d7f/ndp48-x86-x64-allos-enu.exe'"
                                .FormatWith(Environment.NewLine));
                    }
                }
            }
        }
    }
}
