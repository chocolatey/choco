// Copyright © 2023 Chocolatey Software, Inc
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

using System.Collections.Generic;
using System.Linq;

namespace chocolatey.infrastructure.app.commands
{
    /// <summary>
    /// A base class for any Chocolatey commands which need to utilise shared logic.
    /// </summary>
    public abstract class ChocolateyCommandBase
    {
        /// <summary>
        /// Emit a warning to the use if any of the options which are known to be deprecated are found in the <paramref name="unparsedOptions"/>.
        /// </summary>
        /// <param name="unparsedOptions">The list of unrecognised and unparsed options.</param>
        /// <param name="removedOptions">The list of options which are known to be removed and should be warned for.</param>
        protected virtual void WarnForRemovedOptions(IEnumerable<string> unparsedOptions, IEnumerable<string> removedOptions)
        {
            if (!unparsedOptions.OrEmpty().Any() || !removedOptions.OrEmpty().Any())
            {
                return;
            }

            foreach (var removed in removedOptions)
            {
                if (unparsedOptions.Contains(removed))
                {
                    this.Log().Warn("The {0} option is no longer supported.", removed);
                }
            }
        }
    }
}
