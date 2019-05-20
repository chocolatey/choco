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

namespace chocolatey.infrastructure.licensing
{
    using System;
    using System.IO;
    using app;
    using logging;
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

            var regularLogOutput = determine_if_regular_output_for_logging();

            string licenseFile = ApplicationParameters.LicenseFileLocation;
            var userLicenseFile = ApplicationParameters.UserLicenseFileLocation;
            if (File.Exists(userLicenseFile)) licenseFile = userLicenseFile;

            // no IFileSystem at this point
            if (!File.Exists(licenseFile))
            {
                var licenseFileName = Path.GetFileName(ApplicationParameters.LicenseFileLocation);
                var licenseDirectory = Path.GetDirectoryName(ApplicationParameters.LicenseFileLocation);

                // look for misnamed files and locations
                // - look in the license directory for misnamed files
                if (Directory.Exists(licenseDirectory))
                {
                    if (Directory.GetFiles(licenseDirectory).Length != 0)
                    {
                        "chocolatey".Log().Error(regularLogOutput ? ChocolateyLoggers.Normal : ChocolateyLoggers.LogFileOnly, @"Files found in directory '{0}' but not a 
 valid license file. License should be named '{1}'.".format_with(licenseDirectory, licenseFileName));
                        "chocolatey".Log().Warn(ChocolateyLoggers.Important,@" Rename license file to '{0}' to allow commercial features.".format_with(licenseFileName));
                    }
                }


                // - user put the license file in the top level location and/or forgot to rename it
                if (File.Exists(Path.Combine(ApplicationParameters.InstallLocation, licenseFileName)) || File.Exists(Path.Combine(ApplicationParameters.InstallLocation, licenseFileName + ".txt")))
                {
                    "chocolatey".Log().Error(regularLogOutput ? ChocolateyLoggers.Normal : ChocolateyLoggers.LogFileOnly, @"Chocolatey license found in the wrong location. File must be located at 
 '{0}'.".format_with(ApplicationParameters.LicenseFileLocation));
                    "chocolatey".Log().Warn(regularLogOutput ? ChocolateyLoggers.Important : ChocolateyLoggers.LogFileOnly, @" Move license file to '{0}' to allow commercial features.".format_with(ApplicationParameters.LicenseFileLocation));
                }
            }
            
            // no IFileSystem at this point
            if (File.Exists(licenseFile))
            {
                "chocolatey".Log().Debug("Evaluating license file found at '{0}'".format_with(licenseFile));
                var license = new LicenseValidator(PUBLIC_KEY, licenseFile);

                try
                {
                    license.AssertValidLicense();

                    // There is a lease expiration timer within Rhino.Licensing, which by
                    // default re-asserts the license every 5 minutes.  Since we assert a
                    // valid license on each attempt to execute an action with Chocolatey,
                    // re-checking of the license for the current session is not required.
                    license.DisableFutureChecks();

                    chocolateyLicense.IsValid = true;
                }
                catch (LicenseFileNotFoundException e)
                {
                    chocolateyLicense.IsValid = false;
                    chocolateyLicense.InvalidReason = e.Message;
                    "chocolatey".Log().Error(regularLogOutput ? ChocolateyLoggers.Normal : ChocolateyLoggers.LogFileOnly, "A license was not found for a licensed version of Chocolatey:{0} {1}{0} {2}".format_with(Environment.NewLine, e.Message,
                        "A license was also not found in the user profile: '{0}'.".format_with(ApplicationParameters.UserLicenseFileLocation)));
                }
                catch (Exception e)
                {
                    //license may be invalid
                    chocolateyLicense.IsValid = false;
                    chocolateyLicense.InvalidReason = e.Message;
                    "chocolatey".Log().Error(regularLogOutput ? ChocolateyLoggers.Normal : ChocolateyLoggers.LogFileOnly, "A license was found for a licensed version of Chocolatey, but is invalid:{0} {1}".format_with(Environment.NewLine, e.Message));
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

        private static bool determine_if_regular_output_for_logging()
        {
            var args = Environment.GetCommandLineArgs();
            if (args == null || args.Length < 2) return true;

            var firstArg = args[1].to_string();
            if (firstArg.is_equal_to("-v") || firstArg.is_equal_to("--version")) return false;

            return true;
        }
    }
}
