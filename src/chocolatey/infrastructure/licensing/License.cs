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

namespace chocolatey.infrastructure.licensing
{
    using System;
    using app;
    using information;
    using logging;
    using registration;

    public static class License
    {
        public static ChocolateyLicense ValidateLicense()
        {
            var license = LicenseValidation.Validate();
            if (license.IsLicensedVersion())
            {
                try
                {
#if FORCE_CHOCOLATEY_OFFICIAL_KEY
                    var chocolateyPublicKey = ApplicationParameters.OfficialChocolateyPublicKey;
#else
                    var chocolateyPublicKey = ApplicationParameters.UnofficialChocolateyPublicKey;
#endif
                    var licensedAssembly = AssemblyResolution.LoadExtension(ApplicationParameters.LicensedChocolateyAssemblySimpleName);

                    if (licensedAssembly == null)
                    {
                        throw new ApplicationException("Unable to load licensed assembly.");
                    }

                    license.AssemblyLoaded = true;
                    license.Assembly = licensedAssembly;
                    license.Version = VersionInformation.GetCurrentInformationalVersion(licensedAssembly);

                    // The licensed assembly is installed, check its supported Chocolatey versions and/or the assembly
                    // version so we can attempt to determine whether it's compatible with this version of Chocolatey.
                    var minimumChocolateyVersionString = VersionInformation.GetMinimumChocolateyVersion(licensedAssembly);
                    "chocolatey".Log().Debug("Minimum Chocolatey Version: '{0}'".FormatWith(minimumChocolateyVersionString));
                    var currentChocolateyVersionString = VersionInformation.GetCurrentAssemblyVersion();
                    "chocolatey".Log().Debug("Current Chocolatey Version: '{0}'".FormatWith(currentChocolateyVersionString));
                    var currentChocolateyLicensedVersionString = VersionInformation.GetCurrentAssemblyVersion(licensedAssembly);
                    "chocolatey".Log().Debug("Current Chocolatey Licensed Version: '{0}'".FormatWith(currentChocolateyLicensedVersionString));

                    var minimumChocolateyVersion = new Version(minimumChocolateyVersionString);
                    var currentChocolateyVersion = new Version(currentChocolateyVersionString);
                    var currentChocolateyLicensedVersion = new Version(currentChocolateyLicensedVersionString);

                    license.IsCompatible = true;

                    if (currentChocolateyVersion < minimumChocolateyVersion || (minimumChocolateyVersion == Version.Parse("1.0.0") && currentChocolateyLicensedVersion.Major < 4))
                    {
                        license.IsCompatible = false;
                    }

                    Type licensedComponent = licensedAssembly.GetType(ApplicationParameters.LicensedComponentRegistry, throwOnError: false, ignoreCase: true);
                    SimpleInjectorContainer.AddComponentRegistryClass(licensedComponent);
                }
                catch (Exception ex)
                {
                    "chocolatey".Log().Error(
                        @"A valid Chocolatey license was found, but the chocolatey.licensed.dll assembly could not be loaded:
  {0}
Ensure that the chocolatey.licensed.dll exists at the following path:
 '{1}'".FormatWith(ex.Message, ApplicationParameters.LicensedAssemblyLocation));

                    "chocolatey".Log().Warn(
                        ChocolateyLoggers.Important,
                        @"To resolve this, install the Chocolatey Licensed Extension package with
 `choco install chocolatey.extension`");
                }
            }

            return license;
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static ChocolateyLicense validate_license()
            => ValidateLicense();
#pragma warning restore IDE1006
    }
}
