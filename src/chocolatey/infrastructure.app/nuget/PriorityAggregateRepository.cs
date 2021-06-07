using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using chocolatey.infrastructure.app.configuration;
using NuGet;

namespace chocolatey.infrastructure.app.nuget
{
    /// <summary>
    /// Helper file for grouping repositories with a distinct priory set.
    /// </summary>
    /// <seealso cref="NuGet.AggregateRepository" />
    public class PriorityAggregateRepository : PackageRepositoryBase, IPackageLookup, IDependencyResolver
    {
        private const string SourceValue = "(Priority Aggregate source)";
        private readonly ConcurrentDictionary<int, List<IPackageRepository>> _repositories = new ConcurrentDictionary<int, List<IPackageRepository>>();
        private readonly ChocolateyConfiguration _configuration;

        public ILogger Logger { get; set; }

        public override string Source { get { return SourceValue; } }

        public override bool SupportsPrereleasePackages
        {
            get
            {
                return _repositories.Any(r => r.Value.Any(p => p.SupportsPrereleasePackages));
            }
        }

        public PriorityAggregateRepository(IPackageRepository repository, ChocolateyConfiguration configuration)
        {
            Logger = NullLogger.Instance;
            _configuration = configuration;
            AddRepository(0, repository);
        }

        public void AddRepository(int priority, IPackageRepository repository)
        {
            _repositories.AddOrUpdate(priority, (p) => new List<IPackageRepository> { repository }, (p, existingRepositories) =>
             {
                 existingRepositories.Add(repository);
                 return existingRepositories;
             });
        }

        public override IQueryable<IPackage> GetPackages()
        {
            return GetOrderedRepositories()
                .SelectMany(r => r.Item2.SelectMany(i => i.GetPackages())).AsQueryable();
        }

        /// <summary>
        /// Gets the ordered repositories in the order of priories that is wanted.
        /// Will first return any positive non-zero integer in a descending order,
        /// and will negative and zero integer priories in a descending order.
        /// </summary>
        public IEnumerable<Tuple<int,List<IPackageRepository>>> GetOrderedRepositories()
        {
            var result = new List<Tuple<int, List<IPackageRepository>>>();
            foreach (var repository in _repositories.Where(r => r.Key > 0).OrderBy(r => r.Key))
            {
                result.Add(new Tuple<int, List<IPackageRepository>>(repository.Key, repository.Value));
            }

            foreach (var repository in _repositories.Where(r => r.Key <= 0).OrderByDescending(r => r.Key))
            {
                result.Add(new Tuple<int, List<IPackageRepository>>(repository.Key, repository.Value));
            }

            return result;
        }

        public bool Exists(string packageId, SemanticVersion version)
        {
            throw new NotImplementedException();
        }

        public IPackage FindPackage(string packageId, SemanticVersion version)
        {
            return NugetList.find_package(packageId, version, _configuration, this);
        }

        public IEnumerable<IPackage> FindPackagesById(string packageId)
        {
            foreach (var repository in GetOrderedRepositories())
            {
                var results = repository.Item2.SelectMany(i => i.FindPackagesById(packageId)).ToList();
                if (results.Count > 0)
                {
                    return results;
                }
            }

            return Enumerable.Empty<IPackage>();
        }

        public IPackage ResolveDependency(PackageDependency dependency, IPackageConstraintProvider constraintProvider, bool allowPrereleaseVersions, bool preferListedPackages, DependencyVersion dependencyVersion)
        {
            // Naive dependency resolving
            foreach (var repositories in GetOrderedRepositories())
            {
                IPackage package = null;
                foreach (var repository in repositories.Item2)
                {
                    var foundPackage = repository.ResolveDependency(dependency, constraintProvider, allowPrereleaseVersions, preferListedPackages, dependencyVersion);
                    if (package == null)
                    {
                        package = foundPackage;
                    }
                    else if (foundPackage.Version > package.Version)
                    {
                        package = foundPackage;
                    }
                }

                if (package != null)
                {
                    return package;
                }
            }

            return null;
        }

        public AggregateRepository ToAggregateRepository()
        {
            var repositories = GetRepositories(GetOrderedRepositories().SelectMany(r => r.Item2));

            return new AggregateRepository(repositories, ignoreFailingRepositories: true)
            {
                IgnoreFailingRepositories = true,
                Logger = Logger,
                ResolveDependenciesVertically = true
            };
        }

        private IEnumerable<IPackageRepository> GetRepositories(IEnumerable<IPackageRepository> repositories)
        {
            var newRepositories = new List<IPackageRepository>();

            foreach (var repository in repositories)
            {
                var aggregateRepository = repository as AggregateRepository;
                if (aggregateRepository != null)
                {
                    newRepositories.AddRange(GetRepositories(aggregateRepository.Repositories));
                }
                else
                {
                    newRepositories.Add(repository);
                }
            }

            return newRepositories;
        }
    }
}
