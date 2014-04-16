namespace chocolatey.console.infrastructure.registration
{
    using System;
    using SimpleInjector;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.registration;
    using chocolatey.infrastructure.registration;

    /// <summary>
    ///   The inversion container
    /// </summary>
    public static class SimpleInjectorContainer
    {
        private static readonly Lazy<Container> _container = new Lazy<Container>(() => new Container());

        /// <summary>
        ///   Gets the container.
        /// </summary>
        public static Container Container
        {
            get { return _container.Value; }
        }

        /// <summary>
        ///   Initializes the container
        /// </summary>
        public static void Initialize()
        {
            "SimpleInjectorContainer".Log().Debug("SimpleInjector is starting up");

            Container.Options.AllowOverridingRegistrations = true;
            var originalConstructorResolutionBehavior = Container.Options.ConstructorResolutionBehavior;
            Container.Options.ConstructorResolutionBehavior = new SimpleInjectorContainerResolutionBehavior(originalConstructorResolutionBehavior);

            InitializeContainer(Container);

            if (ApplicationParameters.IsDebug)
            {
                Container.Verify();
            }
        }

        private static void InitializeContainer(Container container)
        {
            var binding = new ContainerBinding();
            binding.RegisterComponents(container);
            var bindingClient = new ContainerBindingConsole();
            bindingClient.RegisterComponents(container);
        }
    }
}