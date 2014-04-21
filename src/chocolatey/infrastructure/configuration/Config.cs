namespace chocolatey.infrastructure.configuration
{
    using app.configuration;

    /// <summary>
    ///   Configuration initialization
    /// </summary>
    public sealed class Config
    {
        private static ChocolateyConfiguration _configuration = new ChocolateyConfiguration();

        /// <summary>
        ///   Initializes application configuration with a configuration instance.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public static void initialize_with(ChocolateyConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        ///   Gets the configuration settings.
        /// </summary>
        /// <returns>
        ///   An instance of <see cref="ChocolateyConfiguration" /> if one has been initialized; defaults to new instance of
        ///   <see
        ///     cref="ChocolateyConfiguration" />
        ///   if one has not been.
        /// </returns>
        public static ChocolateyConfiguration get_configuration_settings()
        {
            return _configuration;
        }
    }
}