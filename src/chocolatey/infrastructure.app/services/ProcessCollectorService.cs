// Copyright © 2017 - 2025 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
