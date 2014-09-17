namespace chocolatey.infrastructure.app.services
{
    using domain;

    public interface IRegistryService
    {
        Registry get_installer_keys();
        Registry get_differences(Registry before, Registry after);
        void save_to_file(Registry snapshot, string filePath);
        Registry read_from_file(string filePath);
    }
}