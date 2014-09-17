namespace chocolatey.infrastructure.app.domain
{
    using NuGet;

    public sealed partial class ChocolateyPackageInformation
    {
        public ChocolateyPackageInformation(IPackage package)
        {
            Package = package;
        }

        public IPackage Package { get; set; }
        public Registry RegistrySnapshot { get; set; }
        public bool HasSilentUninstall { get; set; }
        public bool IsSideBySide { get; set; }
        public bool IsPinned { get; set; }
    }
}