namespace chocolatey.infrastructure.adapters
{
    // ReSharper disable InconsistentNaming

    public interface IDateTime
    {
        System.DateTime Now { get; }
        System.DateTime UtcNow { get; }
    }

    // ReSharper restore InconsistentNaming
}