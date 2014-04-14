namespace chocolatey.console.infrastructure.registration
{
    using SimpleInjector;

    /// <summary>
    ///     The inversion container binding for the application.
    ///     This is client project specific - contains items that are only available in the client project.
    ///     Look for the broader application container in the core project.
    /// </summary>
    public class ContainerBindingConsole
    {
        /// <summary>
        /// Loads the module into the kernel.
        /// </summary>
        /// <param name="container">The container.</param>
        public void RegisterComponents(Container container)
        {
            //var configuration = Config.GetConfigurationSettings();
            //container.Register<IFileSystem, DotNetFileSystem>(Lifestyle.Singleton);
        }
    }
}