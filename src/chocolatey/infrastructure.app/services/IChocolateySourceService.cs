namespace chocolatey.infrastructure.app.services
{
    using configuration;

    public interface IChocolateySourceService
    {
        void noop(ChocolateyConfiguration configuration);
        void list(ChocolateyConfiguration configuration);
        void add(ChocolateyConfiguration configuration);
        void remove(ChocolateyConfiguration configuration);
        void disable(ChocolateyConfiguration configuration);
        void enable(ChocolateyConfiguration configuration);
    }
}