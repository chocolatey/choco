namespace chocolatey.infrastructure.app.services
{
    using configuration;

    public interface ITemplateService
    {
        void noop(ChocolateyConfiguration configuration);
        void generate(ChocolateyConfiguration configuration);
    }
}