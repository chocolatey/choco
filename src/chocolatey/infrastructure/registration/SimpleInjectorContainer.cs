namespace chocolatey.infrastructure.registration
{
    using System;
    using SimpleInjector;
    using app.registration;

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
        public static Container initialize()
        {
            Container.Options.AllowOverridingRegistrations = true;
            var originalConstructorResolutionBehavior = Container.Options.ConstructorResolutionBehavior;
            Container.Options.ConstructorResolutionBehavior = new SimpleInjectorContainerResolutionBehavior(originalConstructorResolutionBehavior);

            initialize_container(Container);

#if DEBUG
            Container.Verify();
#endif

            return Container;
        }

        private static void initialize_container(Container container)
        {
            var binding = new ContainerBinding();
            binding.RegisterComponents(container);
        }
    }
}