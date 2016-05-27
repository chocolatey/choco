// Copyright © 2011 - Present RealDimensions Software, LLC
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

namespace chocolatey.infrastructure.licensing
{
    using System;
    using adapters;
    using app;
    using information;
    using registration;
    using Environment = System.Environment;

    public static class License
    {
        public static ChocolateyLicense validate_license()
        {
            var license = LicenseValidation.validate();
            if (license.is_licensed_version())
            {
                try
                {
                    var licensedAssembly = Assembly.LoadFile(ApplicationParameters.LicensedAssemblyLocation);
                    license.AssemblyLoaded = true;
                    license.Assembly = licensedAssembly;
                    license.Version = VersionInformation.get_current_informational_version(licensedAssembly);
                    Type licensedComponent = licensedAssembly.GetType(ApplicationParameters.LicensedComponentRegistry, throwOnError: false, ignoreCase: true);
                    SimpleInjectorContainer.add_component_registry_class(licensedComponent);
                }
                catch (Exception ex)
                {
                    "chocolatey".Log().Error(
@"Error when attempting to load chocolatey licensed assembly. Ensure
 that chocolatey.licensed.dll exists at 
 '{0}'.
 Install with `choco install chocolatey.extension`.
 The error message itself may be helpful as well:{1} {2}".format_with(
                    ApplicationParameters.LicensedAssemblyLocation,
                    Environment.NewLine,
                    ex.Message
                    ));
                }
            }

            return license;
        }
    }
}
