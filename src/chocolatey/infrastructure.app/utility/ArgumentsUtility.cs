// Copyright © 2017 - 2021 Chocolatey Software, Inc
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
using System.Collections.Generic;
using chocolatey.infrastructure.app.nuget;
using chocolatey.infrastructure.filesystem;

namespace chocolatey.infrastructure.app.utility
{
    //todo: #2560 maybe find a better name/location for this

    public static class ArgumentsUtility
    {
        public static bool SensitiveArgumentsProvided(string commandArguments)
        {
            //todo: #2561 this check is naive, we should switch to regex
            //this picks up cases where arguments are passed with '-' and '--'
            return commandArguments.ContainsSafe("-install-arguments-sensitive")
             || commandArguments.ContainsSafe("-package-parameters-sensitive")
             || commandArguments.ContainsSafe("apikey ")
             || commandArguments.ContainsSafe("config ")
             || commandArguments.ContainsSafe("push ") // push can be passed w/out parameters, it's fine to log it then
             || commandArguments.ContainsSafe("-p ")
             || commandArguments.ContainsSafe("-p=")
             || commandArguments.ContainsSafe("-password")
             || commandArguments.ContainsSafe("-cp ")
             || commandArguments.ContainsSafe("-cp=")
             || commandArguments.ContainsSafe("-certpassword")
             || commandArguments.ContainsSafe("-k ")
             || commandArguments.ContainsSafe("-k=")
             || commandArguments.ContainsSafe("-key ")
             || commandArguments.ContainsSafe("-key=")
             || commandArguments.ContainsSafe("-apikey")
             || commandArguments.ContainsSafe("-api-key")
             || commandArguments.ContainsSafe("-u ")
             || commandArguments.ContainsSafe("-u=")
             || commandArguments.ContainsSafe("-user ")
             || commandArguments.ContainsSafe("-user=")
            ;
        }

        public static IEnumerable<string> DecryptPackageArgumentsFile(IFileSystem fileSystem, string id, string version)
        {
            return DecryptPackageArgumentsFile(fileSystem, id, version, redactSensitiveArguments: true, throwOnFailure: false);
        }

        public static IEnumerable<string> DecryptPackageArgumentsFile(
            IFileSystem fileSystem,
            string id,
            string version,
            bool redactSensitiveArguments,
            bool throwOnFailure)
        {
            var argumentsPath = fileSystem.CombinePaths(ApplicationParameters.InstallLocation, ".chocolatey", "{0}.{1}".FormatWith(id, version));
            var argumentsFile = fileSystem.CombinePaths(argumentsPath, ".arguments");

            var arguments = string.Empty;

            // Get the arguments decrypted in here and return them
            try
            {
                if (fileSystem.FileExists(argumentsFile))
                {
                    arguments = fileSystem.ReadFile(argumentsFile);
                }
            }
            catch (Exception)
            {
                "chocolatey".Log().Error("There was an error attempting to read the contents of the .arguments file for version '{0}' of package '{1}'.  See log file for more information.".FormatWith(version, id));
            }

            if (string.IsNullOrEmpty(arguments))
            {
                "chocolatey".Log().Debug("Unable to locate .arguments file for version '{0}' of package '{1}'.".FormatWith(version, id));
                yield break;
            }

            string packageArgumentsUnencrypted = string.Empty;

            try
            {
                packageArgumentsUnencrypted = arguments.Contains(" --") && arguments.ToStringSafe().Length > 4
                    ? arguments
                    : NugetEncryptionUtility.DecryptString(arguments);

            }
            catch (Exception ex)
            {
                var firstMessage = "There was an error attempting to decrypt the contents of the .arguments file for version '{0}' of package '{1}'.  See log file for more information.".FormatWith(version, id);
                var secondMessage = "We failed to decrypt '{0}'. Error from decryption:{1}  '{2}'".FormatWith(argumentsFile, Environment.NewLine, ex.Message.Trim());

                if (throwOnFailure)
                {
                    "chocolatey".Log().Error(firstMessage);
                    "chocolatey".Log().Error(secondMessage);
                    throw;
                }

                "chocolatey".Log().Debug(firstMessage);
                "chocolatey".Log().Debug(secondMessage);
            }

            // Lets do a global check first to see if there are any sensitive arguments
            // before we filter out the values used later.
            var sensitiveArgs = SensitiveArgumentsProvided(packageArgumentsUnencrypted);

            var packageArgumentsSplit =
                packageArgumentsUnencrypted.Split(new[] { " --" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var packageArgument in packageArgumentsSplit.OrEmpty())
            {
                var isSensitiveArgument = sensitiveArgs && SensitiveArgumentsProvided(string.Concat("--", packageArgument));

                var packageArgumentSplit =
                    packageArgument.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

                var optionName = packageArgumentSplit[0].ToStringSafe();
                var optionValue = string.Empty;

                if (packageArgumentSplit.Length == 2 && isSensitiveArgument && redactSensitiveArguments)
                {
                    optionValue = "[REDACTED ARGUMENT]";
                }
                else if (packageArgumentSplit.Length == 2)
                {
                    optionValue = packageArgumentSplit[1].ToStringSafe().UnquoteSafe();
                    if (optionValue.StartsWith("'"))
                    {
                        optionValue.UnquoteSafe();
                    }
                }

                yield return "--{0}{1}".FormatWith(optionName, string.IsNullOrWhiteSpace(optionValue) ? string.Empty : "=" + optionValue);
            }
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static bool arguments_contain_sensitive_information(string commandArguments)
            => SensitiveArgumentsProvided(commandArguments);
#pragma warning restore IDE0022, IDE1006
    }
}
