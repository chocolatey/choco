// Copyright © 2023-Present Chocolatey Software, Inc
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

namespace chocolatey
{
    using System.ComponentModel;

    public static class StringResources
    {
        /// <summary>
        /// Resources for the names of available environment variables
        /// that will be created or used as part of executing
        /// Chocolatey CLI.
        /// </summary>
        /// <remarks>
        /// DEV NOTICE: Mark anything that is not meant for public consumption as
        /// internal constants and not browsable, even if used in other projects.
        /// </remarks>
        public static class EnvironmentVariables
        {
            /// <summary>
            /// The version of the package that is being handled as it is defined in the embedded
            /// nuspec file.
            /// </summary>
            /// <remarks>
            /// Will be sets during package installs, upgrades and uninstalls.
            /// Environment variable is only for internal uses.
            /// </remarks>
            /// <seealso cref="PackageNuspecVersion" />
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Browsable(false)]
            internal const string ChocolateyPackageNuspecVersion = "chocolateyPackageNuspecVersion";

            /// <summary>
            /// The version of the package that is being handled as it is defined in the embedded
            /// nuspec file.
            /// </summary>
            /// <remarks>
            /// Will be sets during package installs, upgrades and uninstalls.
            /// Environment variable is only for internal uses.
            /// </remarks>
            /// <seealso cref="ChocolateyPackageNuspecVersion" />
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Browsable(false)]
            internal const string PackageNuspecVersion = "packageNuspecVersion";
        }
    }
}