using System;
using chocolatey.infrastructure.information;

namespace chocolatey.infrastructure.app.services
{
    public class ProcessCollectorService : IProcessCollectorService
    {
        private static ProcessTree _processTree = null;

        /// <inheritdoc/>
        public virtual string UserAgentProcessName { get; } = string.Empty;

        /// <inheritdoc/>
        public virtual string UserAgentProcessVersion { get; } = string.Empty;

        /// <inheritdoc/>
        /// <remarks>
        /// This method is not overridable on purpose, as once a tree is created it should not be changed.
        /// </remarks>
        public ProcessTree GetProcessTree()
        {
            if (_processTree is null)
            {
                _processTree = ProcessInformation.GetProcessTree();
            }

            return _processTree;
        }
    }
}
