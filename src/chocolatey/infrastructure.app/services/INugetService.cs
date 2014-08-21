namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Collections.Concurrent;
    using configuration;
    using results;

    public interface INugetService
    {
        /// <summary>
        ///   Run list in noop mode
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        void list_noop(ChocolateyConfiguration configuration);

        /// <summary>
        ///   Lists/searches for pacakge against nuget related feeds.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="logResults">Should results be logged?</param>
        /// <returns></returns>
        ConcurrentDictionary<string, PackageResult> list_run(ChocolateyConfiguration configuration, bool logResults);

        /// <summary>
        ///   Run install in noop mode
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="continueAction">The action to continue with for each noop test install.</param>
        void install_noop(ChocolateyConfiguration configuration, Action<PackageResult> continueAction);

        /// <summary>
        ///   Installs packages from NuGet related feeds
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="continueAction">The action to continue with when install is successful.</param>
        /// <returns>results of installs</returns>
        ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration configuration, Action<PackageResult> continueAction);
    }
}