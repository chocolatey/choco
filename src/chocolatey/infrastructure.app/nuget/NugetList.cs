namespace chocolatey.infrastructure.app.nuget
{
    using System.Collections.Generic;
    using System.Linq;
    using NuGet;
    using configuration;

    // ReSharper disable InconsistentNaming

    public sealed class NugetList
    {
        public static IEnumerable<IPackage> GetPackages(ChocolateyConfiguration configuration, ILogger nugetLogger)
        {
            var packageRepository = NugetCommon.GetRemoteRepository(configuration, nugetLogger);
            IQueryable<IPackage> results = packageRepository.Search(configuration.Input, configuration.Prerelease);

            if (configuration.AllVersions)
            {
                return results.Where(PackageExtensions.IsListed).OrderBy(p => p.Id).ToList();
            }

            if (configuration.Prerelease && packageRepository.SupportsPrereleasePackages)
            {
                results = results.Where(p => p.IsAbsoluteLatestVersion);
            }
            else
            {
                results = results.Where(p => p.IsLatestVersion);
            }

            return results.OrderBy(p => p.Id)
                          .AsEnumerable()
                          .Where(PackageExtensions.IsListed)
                          .Where(p => configuration.Prerelease || p.IsReleaseVersion())
                          .distinct_last(PackageEqualityComparer.Id, PackageComparer.Version);
        }
    }

    // ReSharper restore InconsistentNaming
}