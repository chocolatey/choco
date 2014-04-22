namespace chocolatey.infrastructure.app.services
{
    using System.Collections.Concurrent;
    using configuration;
    using results;

    public interface IChocolateyPackageService
    {
        /// <summary>
        ///   Run list in noop mode
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        void list_noop(ChocolateyConfiguration configuration);

        /// <summary>
        ///   Lists/searches for packages that meet a search criteria
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="logResults">Should results be logged?</param>
        /// <returns></returns>
        void list_run(ChocolateyConfiguration configuration, bool logResults);

        /// <summary>
        ///   Run install in noop mode
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        void install_noop(ChocolateyConfiguration configuration);

        /// <summary>
        ///   Installs packages
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>results of installs</returns>
        ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration configuration);
    }
}