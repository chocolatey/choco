namespace chocolatey.infrastructure.adapters
{
    // ReSharper disable InconsistentNaming

    public interface IDateTime
    {
        /// <summary>
        /// Gets a <see cref="T:System.DateTime"/> object that is set to the current date and time on this computer, expressed as the local time.
        /// 
        /// </summary>
        /// 
        /// <returns>
        /// A <see cref="T:System.DateTime"/> whose value is the current local date and time.
        /// 
        /// </returns>
        /// <filterpriority>1</filterpriority>
        System.DateTime Now { get; }

        /// <summary>
        /// Gets a <see cref="T:System.DateTime"/> object that is set to the current date and time on this computer, expressed as the Coordinated Universal Time (UTC).
        /// 
        /// </summary>
        /// 
        /// <returns>
        /// A <see cref="T:System.DateTime"/> whose value is the current UTC date and time.
        /// 
        /// </returns>
        /// <filterpriority>1</filterpriority>
        System.DateTime UtcNow { get; }
    }

    // ReSharper restore InconsistentNaming
}