namespace chocolatey.infrastructure.app.services
{
    using configuration;
    using results;

    public interface IPowershellService
    {
        /// <summary>
        /// Noops the specified package result.
        /// </summary>
        /// <param name="packageResult">The package result.</param>
        void install_noop(PackageResult packageResult);

        /// <summary>
        /// Installs the specified package result.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="packageResult">The package result.</param>
        void install(ChocolateyConfiguration configuration, PackageResult packageResult);
    }
}