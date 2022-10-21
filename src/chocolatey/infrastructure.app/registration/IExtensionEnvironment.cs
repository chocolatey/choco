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
    using System.Collections.Generic;
    using chocolatey.infrastructure.app.configuration;

    /// <summary>
    /// Placeholder for the future to prevent the need for a breaking release of Chocolatey Licensed Extension.
    /// </summary>
    public interface IExtensionEnvironment
    {
        /// <summary>
        /// Returns all of the availabe configuration values that are related to the implementing Chocolatey extension.
        /// </summary>
        /// <param name="config">The configuration used for the entire chocolatey ecosystem.</param>
        /// <returns>The configuration values that needs to be set as environment variables.</returns>
        /// <remarks>This is not used, and is only a placeholder for the future.</remarks>
        IDictionary<string, string> get_environment_configuration(ChocolateyConfiguration config);
    }
}
