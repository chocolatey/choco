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

    internal class ChocolateyRegistrationModule : IExtensionModule
    {
        public void register_dependencies(IContainerRegistrator registrator, ChocolateyConfiguration configuration)
        {
            // Should be replaced by a extension registration instead of a full configuration
            // Which would be possible to override by any extension, which we most likely do
            // not want in the long run.
            registrator.register_service<IXmlService, XmlService>();
            registrator.register_service<IDateTimeService, SystemDateTimeUtcService>();

            //nuget
            registrator.register_service<ILogger, ChocolateyNugetLogger>();
            registrator.register_service<INugetService, NugetService>();
            //registrator.register_service<IPackageDownloader, PackageDownloader>();
            registrator.register_service<IPowershellService, PowershellService>();
            registrator.register_service<IChocolateyPackageInformationService, ChocolateyPackageInformationService>();
            registrator.register_service<IShimGenerationService, ShimGenerationService>();
            registrator.register_service<IRegistryService, RegistryService>();
            registrator.register_service<IPendingRebootService, PendingRebootService>();
            registrator.register_service<IFilesService, FilesService>();
            registrator.register_service<IConfigTransformService, ConfigTransformService>();
            registrator.register_instance<IHashProvider, CryptoHashProvider>((resolver) => new CryptoHashProvider(resolver.resolve<IFileSystem>()));
            registrator.register_service<ITemplateService, TemplateService>();
            registrator.register_service<IChocolateyConfigSettingsService, ChocolateyConfigSettingsService>();
            registrator.register_service<IChocolateyPackageService, ChocolateyPackageService>();
            registrator.register_service<IAutomaticUninstallerService, AutomaticUninstallerService>();
            registrator.register_service<ICommandExecutor, CommandExecutor>();
            registrator.register_instance(() => new adapters.CustomString(string.Empty));

            registrator.register_service<ISourceRunner>(
                typeof(INugetService),
                typeof(WebPiService),
                typeof(WindowsFeatureService),
                typeof(CygwinService),
                typeof(PythonService),
                typeof(RubyGemsService));

            registrator.register_service<IEventSubscriptionManagerService, EventSubscriptionManagerService>();

            registrator.register_service<ITask>(
                typeof(RemovePendingPackagesTask));

            registrator.register_service<IValidation>(
                typeof(GlobalConfigurationValidation),
                typeof(SystemStateValidation));
        }
    }
}
