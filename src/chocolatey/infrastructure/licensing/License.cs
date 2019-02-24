// Copyright © 2017 - 2019 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.licensing
{
    using System;
    using app;
    using information;
    using logging;
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
                    var licensedAssembly = AssemblyResolution.resolve_or_load_assembly(ApplicationParameters.LicensedChocolateyAssemblySimpleName, ApplicationParameters.OfficialChocolateyPublicKey, ApplicationParameters.LicensedAssemblyLocation);
                    if (licensedAssembly == null) throw new ApplicationException("Unable to load licensed assembly.");
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
 The error message itself may be helpful:{1} {2}".format_with(
                    ApplicationParameters.LicensedAssemblyLocation,
                    Environment.NewLine,
                    ex.Message
                    ));
                    "chocolatey".Log().Warn(ChocolateyLoggers.Important,@" Install the Chocolatey Licensed Extension package with 
 `choco install chocolatey.extension` to remove this license warning. 
 TRIALS: If you have a trial license, you cannot use the above command
 as is and be successful. You need to download nupkgs from the links in
 the trial email as your license will not be registered on the licensed
 repository. Please reference
 https://chocolatey.org/docs/installation-licensed#how-do-i-install-the-trial-edition
 for specific instructions.");
                }
            }

            return license;
        }
    }
}
