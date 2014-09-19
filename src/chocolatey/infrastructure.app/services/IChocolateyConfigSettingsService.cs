namespace chocolatey.infrastructure.app.services
{
    using configuration;

    public interface IChocolateyConfigSettingsService
    {
        void noop(ChocolateyConfiguration configuration);
        void source_list(ChocolateyConfiguration configuration);
        void source_add(ChocolateyConfiguration configuration);
        void source_remove(ChocolateyConfiguration configuration);
        void source_disable(ChocolateyConfiguration configuration);
        void source_enable(ChocolateyConfiguration configuration);
        string get_api_key(ChocolateyConfiguration configuration);
        void set_api_key(ChocolateyConfiguration configuration);
    }
}