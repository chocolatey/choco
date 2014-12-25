namespace chocolatey.infrastructure.adapters
{
    // ReSharper disable InconsistentNaming

    public interface IEnvironment
    {
        System.OperatingSystem OSVersion { get; }
    }

    // ReSharper restore InconsistentNaming
}