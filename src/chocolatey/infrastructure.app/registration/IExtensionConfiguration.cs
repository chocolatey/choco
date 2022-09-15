// Copyright © 2017 - 2022 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.app.registration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Placeholder for the future to prevent the need to do a breaking release of
    /// Chocolatey Licensed Extension.
    /// </summary>
    public interface IExtensionConfiguration
    {
        /// <summary>
        /// Creates the initial configuration for this extension.
        /// This will be automatically populated with the correct values
        /// from the configuration file by Chocolatey CLI.
        /// </summary>
        /// <returns>The initial configuration for the settings.</returns>
        /// <remarks>This is not used, and is only a placeholder for the future.</remarks>
        object create_initial_extension_configuration();
    }
}
