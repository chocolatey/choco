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

namespace chocolatey.infrastructure.app.configuration
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using adapters;
    using logging;
    using nuget;
    using Environment = adapters.Environment;

    public static class EnvironmentSettings
    {
        private const string SET_ENVIRONMENT_METHOD = "SetEnvironment";
        private static Lazy<IEnvironment> _environmentInitializer = new Lazy<IEnvironment>(() => new Environment());

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void initialize_with(Lazy<IEnvironment> environment)
        {
            _environmentInitializer = environment;
        }

        private static IEnvironment Environment
        {
            get { return _environmentInitializer.Value; }
        }

        public static void reset_environment_variables(ChocolateyConfiguration config)
        {
            Environment.SetEnvironmentVariable(ApplicationParameters.ChocolateyPackageInstallLocationEnvironmentVariableName, null);
            Environment.SetEnvironmentVariable(ApplicationParameters.ChocolateyPackageInstallerTypeEnvironmentVariableName, null);
        }

        public static void set_environment_variables(ChocolateyConfiguration config)
        {
            reset_environment_variables(config);

            Environment.SetEnvironmentVariable(ApplicationParameters.ChocolateyInstallEnvironmentVariableName, ApplicationParameters.InstallLocation);
            Environment.SetEnvironmentVariable("CHOCOLATEY_VERSION", config.Information.ChocolateyVersion);
            Environment.SetEnvironmentVariable("CHOCOLATEY_VERSION_PRODUCT", config.Information.ChocolateyProductVersion);
            Environment.SetEnvironmentVariable("OS_PLATFORM", config.Information.PlatformType.get_description_or_value());
            Environment.SetEnvironmentVariable("OS_VERSION", config.Information.PlatformVersion.to_string());
            Environment.SetEnvironmentVariable("OS_NAME", config.Information.PlatformName.to_string());
            // experimental until we know if this value returns correctly based on the OS and not the current process.
            Environment.SetEnvironmentVariable("OS_IS64BIT", config.Information.Is64Bit ? "true" : "false");
            Environment.SetEnvironmentVariable("IS_ADMIN", config.Information.IsUserAdministrator ? "true" : "false");
            Environment.SetEnvironmentVariable("IS_PROCESSELEVATED", config.Information.IsProcessElevated ? "true" : "false");
            Environment.SetEnvironmentVariable("TEMP", config.CacheLocation);

            if (config.Debug) Environment.SetEnvironmentVariable("ChocolateyEnvironmentDebug", "true");
            if (config.Verbose) Environment.SetEnvironmentVariable("ChocolateyEnvironmentVerbose", "true");
            if (!config.Features.CheckSumFiles) Environment.SetEnvironmentVariable("ChocolateyIgnoreChecksums", "true");
            Environment.SetEnvironmentVariable("chocolateyRequestTimeout", config.WebRequestTimeoutSeconds.to_string() + "000");
            Environment.SetEnvironmentVariable("chocolateyResponseTimeout", config.CommandExecutionTimeoutSeconds.to_string() + "000");

            if (!string.IsNullOrWhiteSpace(config.Proxy.Location))
            {
                var proxyCreds = string.Empty;
                if (!string.IsNullOrWhiteSpace(config.Proxy.User) &&
                    !string.IsNullOrWhiteSpace(config.Proxy.EncryptedPassword)
                    )
                {
                    proxyCreds = "{0}:{1}@".format_with(config.Proxy.User, NugetEncryptionUtility.DecryptString(config.Proxy.EncryptedPassword));

                    Environment.SetEnvironmentVariable("chocolateyProxyUser", config.Proxy.User);
                    Environment.SetEnvironmentVariable("chocolateyProxyPassword", NugetEncryptionUtility.DecryptString(config.Proxy.EncryptedPassword));
                }

                Environment.SetEnvironmentVariable("http_proxy", "{0}{1}".format_with(proxyCreds, config.Proxy.Location));
                Environment.SetEnvironmentVariable("https_proxy", "{0}{1}".format_with(proxyCreds, config.Proxy.Location));
                Environment.SetEnvironmentVariable("chocolateyProxyLocation", config.Proxy.Location);
            }

            if (config.Features.UsePowerShellHost) Environment.SetEnvironmentVariable("ChocolateyPowerShellHost", "true");
            if (config.Force) Environment.SetEnvironmentVariable("ChocolateyForce", "true");
        }
    }
}
