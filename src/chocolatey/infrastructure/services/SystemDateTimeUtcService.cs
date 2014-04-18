namespace chocolatey.infrastructure.services
{
    using System;

    /// <summary>
    ///   Uses information from the system
    /// </summary>
    public class SystemDateTimeUtcService : IDateTimeService
    {
        public DateTime? GetCurrentDateTime()
        {
            return DateTime.UtcNow;
        }
    }
}