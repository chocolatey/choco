namespace chocolatey.infrastructure.services
{
    using System;

    /// <summary>
    ///   Uses information from the system
    /// </summary>
    public class SystemDateTimeService : IDateTimeService
    {
        public DateTime? get_current_date_time()
        {
            return DateTime.Now;
        }
    }
}