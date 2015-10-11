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

namespace chocolatey.tests.integration
{
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using SimpleInjector;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.builders;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.registration;
    using chocolatey.infrastructure.services;

    // ReSharper disable InconsistentNaming

    [SetUpFixture]
    public class NUnitSetup : tests.NUnitSetup
    {
        public static Container Container { get; set; }

        public override void BeforeEverything()
        {
            Container = SimpleInjectorContainer.Container;
            fix_application_parameter_variables(Container);
            var config = Container.GetInstance<ChocolateyConfiguration>();
            var force = config.Force;
            config.Force = true;
            unpack_self(Container,config);
            config.Force = force;

            base.BeforeEverything();

            ConfigurationBuilder.set_up_configuration(new List<string>(), config, Container.GetInstance<IFileSystem>(), Container.GetInstance<IXmlService>(), null);
        }

        /// <summary>
        /// Most of the application parameters are already set by runtime and are readonly values.
        ///  They need to be updated, so we can do that with reflection.
        /// </summary>
        private static void fix_application_parameter_variables(Container container)
        {
            var fileSystem = container.GetInstance<IFileSystem>();

            var applicationLocation = fileSystem.get_directory_name(fileSystem.get_current_assembly_path());

            var field = typeof (ApplicationParameters).GetField("InstallLocation");
            field.SetValue(null, applicationLocation);

            field = typeof (ApplicationParameters).GetField("LicenseFileLocation");
            field.SetValue(null, fileSystem.combine_paths(ApplicationParameters.InstallLocation, "license", "chocolatey.license.xml"));

            field = typeof (ApplicationParameters).GetField("LoggingLocation");
            field.SetValue(null, fileSystem.combine_paths(ApplicationParameters.InstallLocation, "logs"));

            field = typeof (ApplicationParameters).GetField("GlobalConfigFileLocation");
            field.SetValue(null, fileSystem.combine_paths(ApplicationParameters.InstallLocation, "config", "chocolatey.config"));

            field = typeof (ApplicationParameters).GetField("PackagesLocation");
            field.SetValue(null, fileSystem.combine_paths(ApplicationParameters.InstallLocation, "lib"));

            field = typeof (ApplicationParameters).GetField("PackageFailuresLocation");
            field.SetValue(null, fileSystem.combine_paths(ApplicationParameters.InstallLocation, "lib-bad"));

            field = typeof(ApplicationParameters).GetField("PackageBackupLocation");
            field.SetValue(null, fileSystem.combine_paths(ApplicationParameters.InstallLocation, "lib-bkp"));

            field = typeof (ApplicationParameters).GetField("ShimsLocation");
            field.SetValue(null, fileSystem.combine_paths(ApplicationParameters.InstallLocation, "bin"));

            field = typeof (ApplicationParameters).GetField("ChocolateyPackageInfoStoreLocation");
            field.SetValue(null, fileSystem.combine_paths(ApplicationParameters.InstallLocation, ".chocolatey"));

            // we need to speed up specs a bit, so only try filesystem locking operations twice
            field = fileSystem.GetType().GetField("TIMES_TO_TRY_OPERATION", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(fileSystem, 2);
            }
        }

        private void unpack_self(Container container, ChocolateyConfiguration config)
        {
           var unpackCommand = container.GetInstance<ChocolateyUnpackSelfCommand>();
            unpackCommand.run(config);
        }
    }

    // ReSharper restore InconsistentNaming
}
