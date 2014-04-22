namespace chocolatey.infrastructure.app.services
{
    using results;

    public interface IPowershellService
    {
        void install(PackageResult packageResult);
    }
}