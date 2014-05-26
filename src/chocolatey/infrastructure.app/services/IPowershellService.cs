namespace chocolatey.infrastructure.app.services
{
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
        /// <param name="packageResult">The package result.</param>
        void install(PackageResult packageResult);
    }
}