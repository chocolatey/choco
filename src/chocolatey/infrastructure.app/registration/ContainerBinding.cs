namespace chocolatey.infrastructure.app.registration
{
    using SimpleInjector;
    using filesystem;
    using infrastructure.configuration;
    using infrastructure.services;
    using logging;
    using services;

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
            var configuration = Config.GetConfigurationSettings();
            Log.InitializeWith<Log4NetLog>();

            container.Register(() => configuration, Lifestyle.Singleton);
            container.Register<IFileSystem, DotNetFileSystem>(Lifestyle.Singleton);
            container.Register<IXmlService, XmlService>(Lifestyle.Singleton);

            //container.Register<IEventAggregator, EventAggregator>(Lifestyle.Singleton);
            //container.Register<IMessageSubscriptionManagerService, MessageSubscriptionManagerService>(Lifestyle.Singleton);
            //EventManager.InitializeWith(() => container.GetInstance<IMessageSubscriptionManagerService>());
            //container.Register<IDateTimeService, SystemDateTimeUtcService>(Lifestyle.Singleton);
        }
    }
}