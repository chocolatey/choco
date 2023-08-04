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

namespace chocolatey.infrastructure.app.registration
{
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.nuget;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.app.tasks;
    using chocolatey.infrastructure.app.validations;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.configuration;
    using chocolatey.infrastructure.services;
    using chocolatey.infrastructure.tasks;
    using chocolatey.infrastructure.validations;
    using CryptoHashProvider = cryptography.CryptoHashProvider;
    using IFileSystem = filesystem.IFileSystem;
    using IHashProvider = cryptography.IHashProvider;
    using NuGet.Common;
    using NuGet.PackageManagement;
    using NuGet.Packaging;
    using chocolatey.infrastructure.rules;
    using chocolatey.infrastructure.app.rules;
    using System.Linq;
    using System;
    using System.Security.AccessControl;

    internal class ChocolateyRegistrationModule : IExtensionModule
    {
        public void RegisterDependencies(IContainerRegistrator registrator, ChocolateyConfiguration configuration)
        {
            // Should be replaced by a extension registration instead of a full configuration
            // Which would be possible to override by any extension, which we most likely do
            // not want in the long run.
            registrator.RegisterService<IXmlService, XmlService>();
            registrator.RegisterService<IDateTimeService, SystemDateTimeUtcService>();

            //nuget
            registrator.RegisterService<ILogger, ChocolateyNugetLogger>();
            registrator.RegisterService<INugetService, NugetService>();
            //registrator.register_service<IPackageDownloader, PackageDownloader>();
            registrator.RegisterService<IPowershellService, PowershellService>();
            registrator.RegisterService<IChocolateyPackageInformationService, ChocolateyPackageInformationService>();
            registrator.RegisterService<IShimGenerationService, ShimGenerationService>();
            registrator.RegisterService<IRegistryService, RegistryService>();
            registrator.RegisterService<IPendingRebootService, PendingRebootService>();
            registrator.RegisterService<IFilesService, FilesService>();
            registrator.RegisterService<IConfigTransformService, ConfigTransformService>();
            registrator.RegisterInstance<IHashProvider, CryptoHashProvider>((resolver) => new CryptoHashProvider(resolver.Resolve<IFileSystem>()));
            registrator.RegisterService<ITemplateService, TemplateService>();
            registrator.RegisterService<IChocolateyConfigSettingsService, ChocolateyConfigSettingsService>();
            registrator.RegisterService<IChocolateyPackageService, ChocolateyPackageService>();
            registrator.RegisterService<IAutomaticUninstallerService, AutomaticUninstallerService>();
            registrator.RegisterService<ICommandExecutor, CommandExecutor>();
            registrator.RegisterInstance(() => new adapters.CustomString(string.Empty));

            registrator.RegisterSourceRunner<WindowsFeatureService>();
            registrator.RegisterSourceRunner<CygwinService>();
            registrator.RegisterSourceRunner<PythonService>();
            registrator.RegisterSourceRunner<RubyGemsService>();

            registrator.RegisterService<ISourceRunner>(
                typeof(INugetService));

            registrator.RegisterService<IEventSubscriptionManagerService, EventSubscriptionManagerService>();

            registrator.RegisterService<ITask>(
                typeof(RemovePendingPackagesTask));

            registrator.RegisterService<IValidation>(
                typeof(GlobalConfigurationValidation),
                typeof(SystemStateValidation),
                typeof(CacheFolderLockdownValidation));

            // Rule registrations
            registrator.RegisterService<IRuleService, RuleService>();

            var availableRules = GetType().Assembly
                .GetTypes()
                .Where(t => !t.IsInterface && !t.IsAbstract && typeof(IMetadataRule).IsAssignableFrom(t))
                .ToArray();

            registrator.RegisterService<IMetadataRule>(availableRules);
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void register_dependencies(IContainerRegistrator registrator, ChocolateyConfiguration configuration)
            => RegisterDependencies(registrator, configuration);
#pragma warning restore IDE1006
    }
}
