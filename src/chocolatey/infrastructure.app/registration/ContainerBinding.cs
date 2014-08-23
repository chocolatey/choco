namespace chocolatey.infrastructure.app.registration
{
    using System.Collections.Generic;
    using NuGet;
    using SimpleInjector;
    using commands;
    using events;
    using filesystem;
    using infrastructure.commands;
    using infrastructure.configuration;
    using infrastructure.services;
    using logging;
    using nuget;
    using services;
    using IFileSystem = filesystem.IFileSystem;

    // ReSharper disable InconsistentNaming

    /// <summary>
    ///   The main inversion container registration for the application. Look for other container bindings in client projects.
    /// </summary>
    public sealed class ContainerBinding
    {
        /// <summary>
        ///   Loads the module into the kernel.
        /// </summary>
        public void RegisterComponents(Container container)
        {
            var configuration = Config.get_configuration_settings();
            Log.InitializeWith<Log4NetLog>();

            container.Register(() => configuration, Lifestyle.Singleton);
            container.Register<IFileSystem, DotNetFileSystem>(Lifestyle.Singleton);
            container.Register<IXmlService, XmlService>(Lifestyle.Singleton);
            //nuget
            container.Register<ILogger, ChocolateyNugetLogger>(Lifestyle.Singleton);
            container.Register<INugetService, NugetService>(Lifestyle.Singleton);
            container.Register<IPowershellService, PowershellService>(Lifestyle.Singleton);
            container.Register<IChocolateyPackageService, ChocolateyPackageService>(Lifestyle.Singleton);

            //todo:refactor - this should be autowired
            container.Register<IEnumerable<ICommand>>(() =>
                {
                    var list = new List<ICommand>
                        {
                            new ChocolateyInstallCommand(container.GetInstance<IChocolateyPackageService>()),
                            new ChocolateyListCommand(container.GetInstance<IChocolateyPackageService>()),
                            new ChocolateyUnpackSelfCommand(container.GetInstance<IFileSystem>())
                        };

                    return list.AsReadOnly();
                }, Lifestyle.Singleton);

            container.Register<IEventSubscriptionManagerService, EventSubscriptionManagerService>(Lifestyle.Singleton);
            EventManager.initialize_with(container.GetInstance<IEventSubscriptionManagerService>);

            container.Register<IDateTimeService, SystemDateTimeUtcService>(Lifestyle.Singleton);
        }
    }

    // ReSharper restore InconsistentNaming
}