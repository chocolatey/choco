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

namespace Chocolatey.Infrastructure.App.Registration
{
    using Chocolatey.Infrastructure.App.Configuration;
    using Chocolatey.Infrastructure.App.Nuget;
    using Chocolatey.Infrastructure.App.Services;
    using Chocolatey.Infrastructure.App.Tasks;
    using Chocolatey.Infrastructure.App.Validations;
    using Chocolatey.Infrastructure.Commands;
    using Chocolatey.Infrastructure.Configuration;
    using Chocolatey.Infrastructure.Services;
    using Chocolatey.Infrastructure.Tasks;
    using Chocolatey.Infrastructure.Validations;
    using CryptoHashProvider = Cryptography.CryptoHashProvider;
    using IFileSystem = FileSystem.IFileSystem;
    using IHashProvider = Cryptography.IHashProvider;
    using global::NuGet.Common;
    using global::NuGet.PackageManagement;
    using global::NuGet.Packaging;
    using Chocolatey.Infrastructure.Rules;
    using Chocolatey.Infrastructure.App.Rules;
    using System.Linq;

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
            registrator.RegisterInstance(() => new Adapters.CustomString(string.Empty));

            registrator.RegisterService<ISourceRunner>(
                typeof(INugetService),
                typeof(WindowsFeatureService),
                typeof(CygwinService),
                typeof(PythonService),
                typeof(RubyGemsService));

            registrator.RegisterService<IEventSubscriptionManagerService, EventSubscriptionManagerService>();

            registrator.RegisterService<ITask>(
                typeof(RemovePendingPackagesTask));

            registrator.RegisterService<IValidation>(
                typeof(GlobalConfigurationValidation),
                typeof(SystemStateValidation));

            // Rule registrations
            registrator.RegisterService<IRuleService, RuleService>();

            var availableRules = GetType().Assembly
                .GetTypes()
                .Where(t => !t.IsInterface && !t.IsAbstract && typeof(IMetadataRule).IsAssignableFrom(t))
                .ToArray();

            registrator.RegisterService<IMetadataRule>(availableRules);
        }
    }
}
