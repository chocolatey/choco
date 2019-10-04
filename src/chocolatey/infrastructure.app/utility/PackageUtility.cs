// Copyright © 2017 - 2018 Chocolatey Software, Inc
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
namespace chocolatey.infrastructure.app.utility
{
    using System;
    using configuration;

    public class PackageUtility
    {
        /// <summary>
        /// Is the package we are installing a dependency? Semi-virtual packages do not count:
        /// .install / .app / .portable / .tool / .commandline
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="packageName">Name of the package.</param>
        /// <returns>true if the package is a dependency, false if the package is the one specified or a virtual/semi-virtual</returns>
        public static bool package_is_a_dependency(ChocolateyConfiguration config, string packageName)
        {
            if (string.IsNullOrWhiteSpace(config.PackageNames)) return true;
            if (string.IsNullOrWhiteSpace(packageName)) return true;

            foreach (var package in config.PackageNames.Split(new[] { ApplicationParameters.PackageNamesSeparator }, StringSplitOptions.RemoveEmptyEntries).or_empty_list_if_null())
            {
                if (packageName.is_equal_to(package)
                    || packageName.contains(package + ".")
                    || (packageName.contains(package)
                        && (packageName.contains(".nupkg")
                            || packageName.contains(".nuspec")
                            || packageName.contains("\\")
                        )
                    )
                )
                {
                    return false;
                }
            }

            return true;
        }
    }
}
