// Copyright © 2017 - 2025 Chocolatey Software, Inc
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using chocolatey.infrastructure.app;
using chocolatey.infrastructure.app.attributes;
using chocolatey.infrastructure.app.builders;
using chocolatey.infrastructure.app.commands;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.commands;
using chocolatey.infrastructure.filesystem;
using chocolatey.infrastructure.licensing;
using chocolatey.infrastructure.platforms;
using chocolatey.infrastructure.registration;
using NUnit.Framework;
using SimpleInjector;
[assembly: chocolatey.tests.Categories.Integration]

namespace chocolatey.tests.integration
{
    // ReSharper disable InconsistentNaming

    [SetUpFixture]
    public class NUnitSetup : tests.NUnitSetup
    {
        public static Container Container { get; set; }

        public override void BeforeEverything()
        {
            Container = SimpleInjectorContainer.Container;
            FixApplicationParameterVariables(Container);

            base.BeforeEverything();

            // deep copy so we don't have the same configuration and
            // don't have to worry about issues using it
            var config = Container.GetInstance<ChocolateyConfiguration>().DeepCopy();
            config.Information.PlatformType = Platform.GetPlatform();
            config.Information.IsInteractive = false;
            config.PromptForConfirmation = false;
            config.Force = true;
            UnpackSelf(Container, config);
            BuildPackages(Container, config);

            ConfigurationBuilder.SetupConfiguration(new List<string>(), config, Container, new ChocolateyLicense(), null);

            MockLogger.Reset();
        }

        /// <summary>
        ///   Most of the application parameters are set by the runtime to the machine
        ///   install location. Tests need them pointed at the test output directory, so
        ///   we override them here. (These were previously initonly and set via
        ///   reflection; .NET no longer allows writing initonly fields, so the fields are
        ///   now settable and assigned directly.)
        /// </summary>
        private static void FixApplicationParameterVariables(Container container)
        {
            var fileSystem = container.GetInstance<IFileSystem>();

            var applicationLocation = fileSystem.GetDirectoryName(fileSystem.GetCurrentAssemblyPath());

            ApplicationParameters.InstallLocation = applicationLocation;
            ApplicationParameters.LoggingLocation = fileSystem.CombinePaths(ApplicationParameters.InstallLocation, "logs");
            ApplicationParameters.GlobalConfigFileLocation = fileSystem.CombinePaths(ApplicationParameters.InstallLocation, "config", "chocolatey.config");
            ApplicationParameters.LicenseFileLocation = fileSystem.CombinePaths(ApplicationParameters.InstallLocation, "license", "chocolatey.license.xml");
            ApplicationParameters.PackagesLocation = fileSystem.CombinePaths(ApplicationParameters.InstallLocation, "lib");
            ApplicationParameters.PackageFailuresLocation = fileSystem.CombinePaths(ApplicationParameters.InstallLocation, "lib-bad");
            ApplicationParameters.PackageBackupLocation = fileSystem.CombinePaths(ApplicationParameters.InstallLocation, "lib-bkp");
            ApplicationParameters.ShimsLocation = fileSystem.CombinePaths(ApplicationParameters.InstallLocation, "bin");
            ApplicationParameters.ChocolateyPackageInfoStoreLocation = fileSystem.CombinePaths(ApplicationParameters.InstallLocation, ".chocolatey");
            ApplicationParameters.ExtensionsLocation = fileSystem.CombinePaths(ApplicationParameters.HooksLocation, "extensions");
            ApplicationParameters.TemplatesLocation = fileSystem.CombinePaths(ApplicationParameters.HooksLocation, "templates");
            ApplicationParameters.HooksLocation = fileSystem.CombinePaths(ApplicationParameters.InstallLocation, "hooks");
            ApplicationParameters.LockTransactionalInstallFiles = false;
        }

        private void UnpackSelf(Container container, ChocolateyConfiguration config)
        {
            var commands = container.GetAllInstances<ICommand>();
            var unpackCommand = commands.Where((c) =>
            {
                var attributes = c.GetType().GetCustomAttributes(typeof(CommandForAttribute), false);
                return attributes.Cast<CommandForAttribute>().Any(attribute => attribute.CommandName.IsEqualTo("unpackself"));
            }).FirstOrDefault();

            unpackCommand.Run(config);
        }

        private void BuildPackages(Container container, ChocolateyConfiguration config)
        {
            var fileSystem = container.GetInstance<IFileSystem>();
            var contextDir = fileSystem.CombinePaths(fileSystem.GetDirectoryName(fileSystem.GetCurrentAssemblyPath()), "context");

            // short-circuit building packages if they are already there.
            if (fileSystem.GetFiles(contextDir, "*.nupkg").Any())
            {
                Console.WriteLine("Packages have already been built. Skipping... - If you need to rebuild packages, delete all nupkg files in {0}.".FormatWith(contextDir));
                return;
            }

            var files = fileSystem.GetFiles(contextDir, "*.nuspec", SearchOption.AllDirectories);

            config.PackCommand.PackThrowOnUnsupportedElements = false;
            var command = container.GetInstance<ChocolateyPackCommand>();
            foreach (var file in files.OrEmpty())
            {
                config.Input = file;
                Console.WriteLine("Building {0}".FormatWith(file));
                command.Run(config);
            }
            config.PackCommand.PackThrowOnUnsupportedElements = true;

            Console.WriteLine("Moving all nupkgs in {0} to context directory.".FormatWith(fileSystem.GetCurrentDirectory()));
            var nupkgs = fileSystem.GetFiles(fileSystem.GetCurrentDirectory(), "*.nupkg");

            foreach (var nupkg in nupkgs.OrEmpty())
            {
                fileSystem.CopyFile(nupkg, fileSystem.CombinePaths(contextDir, fileSystem.GetFileName(nupkg)), overwriteExisting: true);
                fileSystem.DeleteFile(nupkg);
            }

            //concurrency issues when packages are first built out during testing
            Thread.Sleep(2000);
            Console.WriteLine("Continuing with tests now after waiting for files to finish moving.");
        }
    }

    // ReSharper restore InconsistentNaming
}
