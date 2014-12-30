namespace chocolatey.infrastructure.adapters
{
    // ReSharper disable InconsistentNaming

    public interface IEnvironment
    {
        /// <summary>
        /// Gets an <see cref="T:System.OperatingSystem"/> object that contains the current platform identifier and version number.
        /// 
        /// </summary>
        /// 
        /// <returns>
        /// An <see cref="T:System.OperatingSystem"/> object.
        /// 
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">This property was unable to obtain the system version.
        /// 
        ///                     -or-
        /// 
        ///                     The obtained platform identifier is not a member of <see cref="T:System.PlatformID"/>.
        ///                 </exception><filterpriority>1</filterpriority>
        System.OperatingSystem OSVersion { get; }
    }

    // ReSharper restore InconsistentNaming
}