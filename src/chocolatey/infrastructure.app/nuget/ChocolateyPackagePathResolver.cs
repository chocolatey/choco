namespace chocolatey.infrastructure.app.nuget
{
    using System.IO;
    using NuGet;

    // ReSharper disable InconsistentNaming

    public sealed class ChocolateyPackagePathResolver : DefaultPackagePathResolver
    {
        private readonly IFileSystem _nugetFileSystem;
        public bool UseSideBySidePaths { get; set; }

        public ChocolateyPackagePathResolver(IFileSystem nugetFileSystem, bool useSideBySidePaths)
            : base(nugetFileSystem, useSideBySidePaths)
        {
            _nugetFileSystem = nugetFileSystem;
            UseSideBySidePaths = useSideBySidePaths;
        }

        public override string GetInstallPath(IPackage package)
        {
            return Path.Combine(_nugetFileSystem.Root, GetPackageDirectory(package));
        }

        public override string GetPackageDirectory(string packageId, SemanticVersion version)
        {
            string directory = packageId;
            if (UseSideBySidePaths)
            {
                directory += "." + version;
            }
            return directory;
        }

        public override string GetPackageFileName(string packageId, SemanticVersion version)
        {
            string fileNameBase = packageId;
            if (UseSideBySidePaths)
            {
                fileNameBase += "." + version;
            }
            return fileNameBase + Constants.PackageExtension;
        }
    }

    // ReSharper restore InconsistentNaming
}