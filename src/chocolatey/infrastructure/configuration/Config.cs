namespace chocolatey.infrastructure.configuration
{
    using app.configuration;

    /// <summary>
    /// Configuration initialization
    /// </summary>
    public class Config
    {
        private static IConfigurationSettings _configuration = new ConfigurationSettings();

        /// <summary>
        /// Initializes application configuration with a configuration instance.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public static void InitializeWith(IConfigurationSettings configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gets the configuration settings.
        /// </summary>
        /// <returns>An instance of <see cref="IConfigurationSettings"/> if one has been initialized; defaults to <see cref="ConfigurationSettings"/> if one has not been.</returns>
        public static IConfigurationSettings GetConfigurationSettings()
        {
            return _configuration;
        }
    }
}