namespace chocolatey.infrastructure.app.services
{
    using configuration;
    using results;

    public interface IPowershellService
    {
        /// <summary>
        /// Noops the specified package install.
        /// </summary>
        /// <param name="packageResult">The package result.</param>
        void install_noop(PackageResult packageResult);

        /// <summary>
        /// Installs the specified package.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="packageResult">The package result.</param>
        /// <returns>true if the chocolateyInstall.ps1 was found, even if it has failures</returns>
        bool install(ChocolateyConfiguration configuration, PackageResult packageResult);
        
        /// <summary>
        /// Noops the specified package uninstall.
        /// </summary>
        /// <param name="packageResult">The package result.</param>
        void uninstall_noop(PackageResult packageResult);

        /// <summary>
        /// Uninstalls the specified package.
        /// </summary>
        /// <param name="configuration">The configuration</param>
        /// <param name="packageResult">The package result.</param>
        /// <returns>true if the chocolateyUninstall.ps1 was found, even if it has failures</returns>
        bool uninstall(ChocolateyConfiguration configuration, PackageResult packageResult);
    }
}