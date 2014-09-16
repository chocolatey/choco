namespace chocolatey.infrastructure.app.services
{
    using domain;

    public interface IRegistryService
    {
        RegistryInstallSnapshot get_installer_keys_snapshot();
        RegistryInstallSnapshot get_snapshot_differences(RegistryInstallSnapshot before, RegistryInstallSnapshot after);
        void save_to_file(RegistryInstallSnapshot snapshot, string filePath);
        RegistryInstallSnapshot read_from_file(string filePath);
    }
}