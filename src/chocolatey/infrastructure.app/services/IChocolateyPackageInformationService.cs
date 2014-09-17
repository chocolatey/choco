namespace chocolatey.infrastructure.app.services
{
    using NuGet;
    using domain;

    public interface IChocolateyPackageInformationService
    {
        ChocolateyPackageInformation get_package_information(IPackage package);
        void save_package_information(ChocolateyPackageInformation packageInformation);
        void remove_package_information(IPackage package);
    }
}