namespace chocolatey.infrastructure.app.nuget
{
    using System;
    using System.Collections.Generic;
    using NuGet;
    using configuration;

    // ReSharper disable InconsistentNaming

    public sealed class NugetCommon
    {
        public static IPackageRepository GetRepository(ChocolateyConfiguration configuration, ILogger nugetLogger)
        {
            IEnumerable<string> sources = configuration.Source.Split(new[] {";", ","}, StringSplitOptions.RemoveEmptyEntries);


            IList<IPackageRepository> repositories = new List<IPackageRepository>();
            foreach (var source in sources.or_empty_list_if_null())
            {
                var uri = new Uri(source);
                if (uri.IsFile || uri.IsUnc)
                {
                    repositories.Add(new LocalPackageRepository(uri.LocalPath));
                }
                else
                {
                    repositories.Add(new DataServicePackageRepository(uri));
                }
            }

            var repository = new AggregateRepository(repositories) {Logger = nugetLogger};
            return repository;
        }
    }

    // ReSharper restore InconsistentNaming
}