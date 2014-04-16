namespace chocolatey.infrastructure.services
{
    using app.configuration;

    public interface IConfigFileSettingsService
    {
        ConfigFileSettings get_settings(string filePath);
        //void Save();
    }
}