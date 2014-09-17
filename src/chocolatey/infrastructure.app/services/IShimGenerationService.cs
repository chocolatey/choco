namespace chocolatey.infrastructure.app.services
{
    using configuration;
    using results;

    public interface IShimGenerationService
    {
        /// <summary>
        ///   Installs shimgens for the package
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="packageResult">The package result.</param>
        void install(ChocolateyConfiguration configuration, PackageResult packageResult);

        /// <summary>
        ///   Uninstalls shimgens for the package
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="packageResult">The package result.</param>
        void uninstall(ChocolateyConfiguration configuration, PackageResult packageResult);
    }
}