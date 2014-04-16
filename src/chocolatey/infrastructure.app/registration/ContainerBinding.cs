namespace chocolatey.infrastructure.app.registration
{
    using SimpleInjector;
    using configuration;
    using infrastructure.configuration;
    using logging;

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

            //container.Register<IEventAggregator, EventAggregator>(Lifestyle.Singleton);
            //container.Register<IMessageSubscriptionManagerService, MessageSubscriptionManagerService>(Lifestyle.Singleton);
            //EventManager.InitializeWith(() => container.GetInstance<IMessageSubscriptionManagerService>());
            //container.Register<IDateTimeService, SystemDateTimeUtcService>(Lifestyle.Singleton);

            RegisterOverrideableComponents(container, configuration);
        }

        /// <summary>
        ///     Registers the components that might be overridden in the front end.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="configuration">The configuration.</param>
        private void RegisterOverrideableComponents(Container container, IConfigurationSettings configuration)
        {
            var singletonLifeStyle = Lifestyle.Singleton;
        }
    }
}