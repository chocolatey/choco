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
            //no file system at this point
            if (File.Exists(licenseFile))
            {
                var license = new LicenseValidator(PUBLIC_KEY, licenseFile);

                try
                {
                    license.AssertValidLicense();
                    chocolateyLicense.IsValid = true;
                }
                catch (Exception e)
                {
                    //license may be invalid
                    chocolateyLicense.IsValid = false;
                    chocolateyLicense.InvalidReason = e.Message;
                    "chocolatey".Log().Error("A license was found for a licensed version of Chocolatey, but is invalid:{0} {1}".format_with(Environment.NewLine, e.Message));
                }

                switch (license.LicenseType)
                {
                    case LicenseType.Professional :
                        chocolateyLicense.LicenseType = ChocolateyLicenseType.Professional;
                        break;
                    case LicenseType.Business :
                        chocolateyLicense.LicenseType = ChocolateyLicenseType.Business;
                        break;
                    case LicenseType.Enterprise :
                        chocolateyLicense.LicenseType = ChocolateyLicenseType.Enterprise;
                        break;
                }

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
