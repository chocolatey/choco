﻿// Copyright © 2017 - 2025 Chocolatey Software, Inc
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

using System;
using System.IO;
using System.Linq;
using chocolatey.infrastructure.app;
using chocolatey.infrastructure.logging;
using Rhino.Licensing;

namespace chocolatey.infrastructure.licensing
{
    public sealed class LicenseValidation
    {
        private const string PublicKey =
            @"<RSAKeyValue><Modulus>rznyhs3OslLqL7A7qav9bSHYGQmgWVsP/L47dWU7yF3EHsiYZuJNLlq8tQkPql/LB1FfLihiGsOKKUF1tmxihcRUrDaYkK1IYY3A+uJWkBglDUOUjnoDboI1FgF3wmXSb07JC8JCVYWjchq+h6MV9aDZaigA5MqMKNj9FE14f68=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        public static ChocolateyLicense Validate()
        {
            var chocolateyLicense = new ChocolateyLicense
            {
                LicenseType = ChocolateyLicenseType.Unknown,
                IsCompatible = true
            };

            var regularLogOutput = ShouldLogErrorsToConsole();
            var normalLogger = regularLogOutput ? ChocolateyLoggers.Normal : ChocolateyLoggers.LogFileOnly;
            var importantLogger = regularLogOutput ? ChocolateyLoggers.Important : ChocolateyLoggers.LogFileOnly;

            var licenseFile = ApplicationParameters.LicenseFileLocation;
            var userLicenseFile = ApplicationParameters.UserLicenseFileLocation;
            if (File.Exists(userLicenseFile))
            {
                licenseFile = userLicenseFile;
            }

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
                        "chocolatey".Log().Error(normalLogger, @"Files found in directory '{0}' but not a
 valid license file. License should be named '{1}'.".FormatWith(licenseDirectory, licenseFileName));
                        "chocolatey".Log().Warn(importantLogger, @" Rename license file to '{0}' to allow commercial features.".FormatWith(licenseFileName));
                    }
                }


                // - user put the license file in the top level location and/or forgot to rename it
                if (File.Exists(Path.Combine(ApplicationParameters.InstallLocation, licenseFileName)) || File.Exists(Path.Combine(ApplicationParameters.InstallLocation, licenseFileName + ".txt")))
                {
                    "chocolatey".Log().Error(normalLogger, @"Chocolatey license found in the wrong location. File must be located at
 '{0}'.".FormatWith(ApplicationParameters.LicenseFileLocation));
                    "chocolatey".Log().Warn(importantLogger, @" Move license file to '{0}' to allow commercial features.".FormatWith(ApplicationParameters.LicenseFileLocation));
                }
            }

            // no IFileSystem at this point
            if (File.Exists(licenseFile))
            {
                "chocolatey".Log().Debug("Evaluating license file found at '{0}'".FormatWith(licenseFile));
                var license = new LicenseValidator(PublicKey, licenseFile);

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
                    "chocolatey".Log().Error(normalLogger, "A license was not found for a licensed version of Chocolatey:{0} {1}{0} {2}".FormatWith(Environment.NewLine, e.Message,
                        "A license was also not found in the user profile: '{0}'.".FormatWith(ApplicationParameters.UserLicenseFileLocation)));
                }
                catch (Exception e)
                {
                    //license may be invalid
                    chocolateyLicense.IsValid = false;
                    chocolateyLicense.InvalidReason = e.Message;
                    "chocolatey".Log().Error(normalLogger, "A license was found for a licensed version of Chocolatey, but is invalid:{0} {1}".FormatWith(Environment.NewLine, e.Message));
                }

                var chocolateyLicenseType = ChocolateyLicenseType.Unknown;
                try
                {
                    Enum.TryParse(license.LicenseType.ToStringSafe(), true, out chocolateyLicenseType);
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
                chocolateyLicense.Id = license.UserId.ToStringSafe();
            }
            else
            {
                //free version
                chocolateyLicense.LicenseType = ChocolateyLicenseType.Foss;
            }

            return chocolateyLicense;
        }

        private static bool ShouldLogErrorsToConsole()
        {
            var limitOutputArguments = new string[]
            {
                "--limit-output",
                "--limitoutput",
                "-r"
            };
            var args = Environment.GetCommandLineArgs();
            // I think this check is incorrect??? if --version is supposed to return false, it can return true at this point?
            if (args == null || args.Length < 2)
            {
                return true;
            }

            var firstArg = args[1].ToStringSafe();
            if (firstArg.IsEqualTo("-v") || firstArg.IsEqualTo("--version"))
            {
                return false;
            }

            if (args.Count(argument => limitOutputArguments.Contains(argument)) > 0)
            {
                return false;
            }

            return true;
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static ChocolateyLicense validate()
            => Validate();
#pragma warning restore IDE0022, IDE1006
    }
}
