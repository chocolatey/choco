using chocolatey.infrastructure.information;

namespace chocolatey.infrastructure.app.services
{
    /// <summary>
    /// Collector service that will get information about the processes in the current execution.
    /// </summary>
    /// <remarks>
    /// This service is used to build the correct user agent we want to send to the remote servers.
    /// </remarks>
    public interface IProcessCollectorService
    {
        /// <summary>
        /// Gets the friendly name of the currently running process.
        /// </summary>
        /// <remarks>
        /// If no user agent process name is specified, the current process in the process tree will be used instead.
        /// </remarks>
        string UserAgentProcessName { get; }

        /// <summary>
        /// Gets the version number of the currently running process.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If no user agent process version is specified, the version number of the currently
        /// running proccess will be looked up.
        /// </para>
        /// <para>
        /// This property will only be used if <see cref="UserAgentProcessName"/> have also been specified.
        /// </para>
        /// </remarks>
        string UserAgentProcessVersion { get; }

        /// <summary>
        /// Gets the full details of the process tree that Chocolatey CLI is part of. This includes
        /// the top level parent, the closest parent, the current process name and all other
        /// processes between these.
        /// </summary>
        /// <returns>
        /// The found process tree, returning null from this will throw an exception in Chocolatey CLI.
        /// </returns>
        ProcessTree GetProcessTree();
    }
}
