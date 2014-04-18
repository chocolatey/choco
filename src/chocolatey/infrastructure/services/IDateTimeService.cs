namespace chocolatey.infrastructure.services
{
    using System;

    /// <summary>
    ///   This handles date/time information
    /// </summary>
    public interface IDateTimeService
    {
        /// <summary>
        ///   Gets the current date time.
        /// </summary>
        DateTime? GetCurrentDateTime();
    }
}