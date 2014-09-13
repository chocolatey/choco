namespace chocolatey.infrastructure.app.nuget
{
    using NuGet;

    // ReSharper disable InconsistentNaming

    public class ChocolateyLocalPackageRepository : LocalPackageRepository
    {
        public ChocolateyLocalPackageRepository(string physicalPath)
            : base(physicalPath)
        {
        }

        public ChocolateyLocalPackageRepository(string physicalPath, bool enableCaching)
            : base(physicalPath, enableCaching)
        {
        }

        public ChocolateyLocalPackageRepository(IPackagePathResolver pathResolver, IFileSystem fileSystem)
            : base(pathResolver, fileSystem)
        {
        }

        public ChocolateyLocalPackageRepository(IPackagePathResolver pathResolver, IFileSystem fileSystem, bool enableCaching)
            : base(pathResolver, fileSystem, enableCaching)
        {
        }

        public override void AddPackage(IPackage package)
        {
            string packageFilePath = GetPackageFilePath(package);
            FileSystem.AddFileWithCheck(packageFilePath, package.GetStream);
        }
    }

    // ReSharper restore InconsistentNaming

}