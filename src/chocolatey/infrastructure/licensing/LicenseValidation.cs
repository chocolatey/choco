// Copyright © 2017 Chocolatey Software, Inc
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
    using System.IO;
    using app;
    using Rhino.Licensing;

    public sealed class LicenseValidation
    {
        private const string PUBLIC_KEY =
            @"<RSAKeyValue><Modulus>rznyhs3OslLqL7A7qav9bSHYGQmgWVsP/L47dWU7yF3EHsiYZuJNLlq8tQkPql/LB1FfLihiGsOKKUF1tmxihcRUrDaYkK1IYY3A+uJWkBglDUOUjnoDboI1FgF3wmXSb07JC8JCVYWjchq+h6MV9aDZaigA5MqMKNj9FE14f68=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        public static ChocolateyLicense validate()
        {
            var chocolateyLicense = new ChocolateyLicense
            {
                LicenseType = ChocolateyLicenseType.Unknown
            };

            string licenseFile = ApplicationParameters.LicenseFileLocation;
            var userLicenseFile = ApplicationParameters.UserLicenseFileLocation;
            if (File.Exists(userLicenseFile)) licenseFile = userLicenseFile;

            //no IFileSystem at this point
            if (File.Exists(licenseFile))
            {
                var license = new LicenseValidator(PUBLIC_KEY, licenseFile);

                try
                {
                    license.AssertValidLicense();
                    chocolateyLicense.IsValid = true;
                }
                catch (LicenseFileNotFoundException e)
                {
                    chocolateyLicense.IsValid = false;
                    chocolateyLicense.InvalidReason = e.Message;
                    "chocolatey".Log().Error("A license was not found for a licensed version of Chocolatey:{0} {1}{0} {2}".format_with(Environment.NewLine, e.Message,
                        "A license was also not found in the user profile: '{0}'.".format_with(ApplicationParameters.UserLicenseFileLocation)));
                }
                catch (Exception e)
                {
                    //license may be invalid
                    chocolateyLicense.IsValid = false;
                    chocolateyLicense.InvalidReason = e.Message;
                    "chocolatey".Log().Error("A license was found for a licensed version of Chocolatey, but is invalid:{0} {1}".format_with(Environment.NewLine, e.Message));
                }

                var chocolateyLicenseType = ChocolateyLicenseType.Unknown;
                try
                {
                    Enum.TryParse(license.LicenseType.to_string(), true, out chocolateyLicenseType);
                }
                catch (Exception)
                {
                    chocolateyLicenseType = ChocolateyLicenseType.Unknown;
                }

                if (license.LicenseType == LicenseType.Trial)
                {
                    chocolateyLicenseType = ChocolateyLicenseType.BusinessTrial;
                }
                else if (license.LicenseType == LicenseType.Education)
                {
                    chocolateyLicenseType = ChocolateyLicenseType.Educational;
                }

                chocolateyLicense.LicenseType = chocolateyLicenseType;
                chocolateyLicense.ExpirationDate = license.ExpirationDate;
                chocolateyLicense.Name = license.Name;
                chocolateyLicense.Id = license.UserId.to_string();

                //todo: if it is expired, provide a warning. 
                // one month after it should stop working
            }
            else
            {
                //free version
                chocolateyLicense.LicenseType = ChocolateyLicenseType.Foss;
            }

            return chocolateyLicense;
        }
    }
}
