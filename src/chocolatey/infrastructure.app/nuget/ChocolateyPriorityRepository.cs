using NuGet;

namespace chocolatey.infrastructure.app.nuget
{
    public class ChocolateyPriorityRepository : PriorityPackageRepository
    {
        public ChocolateyPriorityRepository(IPackageRepository primaryRepository, IPackageRepository secondaryRepository)
            : base(primaryRepository, secondaryRepository)
        {
            PrimaryRepository = primaryRepository;
            SecondaryRepository = secondaryRepository;
        }

        public IPackageRepository PrimaryRepository { get; set; }

        public IPackageRepository SecondaryRepository { get; set; }
    }
}