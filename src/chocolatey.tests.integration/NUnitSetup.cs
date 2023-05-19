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

[assembly: chocolatey.tests.Categories.Integration]

namespace chocolatey.tests.integration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.builders;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.licensing;
    using chocolatey.infrastructure.platforms;
    using chocolatey.infrastructure.registration;
    using NUnit.Framework;
    using SimpleInjector;

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
        ///   Most of the application parameters are already set by runtime and are readonly values.
        ///   They need to be updated, so we can do that with reflection.
        /// </summary>
        private static void FixApplicationParameterVariables(Container container)
        {
            var fileSystem = container.GetInstance<IFileSystem>();

            var applicationLocation = fileSystem.GetDirectoryName(fileSystem.GetCurrentAssemblyPath());

            var field = typeof(ApplicationParameters).GetField("InstallLocation");
            field.SetValue(null, applicationLocation);

            field = typeof(ApplicationParameters).GetField("LoggingLocation");
            field.SetValue(null, fileSystem.CombinePaths(ApplicationParameters.InstallLocation, "logs"));

            field = typeof(ApplicationParameters).GetField("GlobalConfigFileLocation");
            field.SetValue(null, fileSystem.CombinePaths(ApplicationParameters.InstallLocation, "config", "chocolatey.config"));

            field = typeof(ApplicationParameters).GetField("LicenseFileLocation");
            field.SetValue(null, fileSystem.CombinePaths(ApplicationParameters.InstallLocation, "license", "chocolatey.license.xml"));

            field = typeof(ApplicationParameters).GetField("PackagesLocation");
            field.SetValue(null, fileSystem.CombinePaths(ApplicationParameters.InstallLocation, "lib"));

            field = typeof(ApplicationParameters).GetField("PackageFailuresLocation");
            field.SetValue(null, fileSystem.CombinePaths(ApplicationParameters.InstallLocation, "lib-bad"));

            field = typeof(ApplicationParameters).GetField("PackageBackupLocation");
            field.SetValue(null, fileSystem.CombinePaths(ApplicationParameters.InstallLocation, "lib-bkp"));

            field = typeof(ApplicationParameters).GetField("ShimsLocation");
            field.SetValue(null, fileSystem.CombinePaths(ApplicationParameters.InstallLocation, "bin"));

            field = typeof(ApplicationParameters).GetField("ChocolateyPackageInfoStoreLocation");
            field.SetValue(null, fileSystem.CombinePaths(ApplicationParameters.InstallLocation, ".chocolatey"));

            field = typeof(ApplicationParameters).GetField("ExtensionsLocation");
            field.SetValue(null, fileSystem.CombinePaths(ApplicationParameters.HooksLocation, "extensions"));

            field = typeof(ApplicationParameters).GetField("TemplatesLocation");
            field.SetValue(null, fileSystem.CombinePaths(ApplicationParameters.HooksLocation, "templates"));

            field = typeof(ApplicationParameters).GetField("HooksLocation");
            field.SetValue(null, fileSystem.CombinePaths(ApplicationParameters.InstallLocation, "hooks"));

            field = typeof(ApplicationParameters).GetField("LockTransactionalInstallFiles");
            field.SetValue(null, false);

            // we need to speed up specs a bit, so only try filesystem locking operations twice
            field = fileSystem.GetType().GetField("TIMES_TO_TRY_OPERATION", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(fileSystem, 2);
            }
        }

        private void UnpackSelf(Container container, ChocolateyConfiguration config)
        {
            var unpackCommand = container.GetInstance<ChocolateyUnpackSelfCommand>();
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
