﻿// Copyright © 2011 - Present RealDimensions Software, LLC
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
            set_licensed_environment(config);
        }

        private static void set_licensed_environment(ChocolateyConfiguration config)
        {
            if (!config.Information.IsLicensedVersion) return;

            var licenseAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name.is_equal_to("chocolatey.licensed"));

            if (licenseAssembly != null)
            {
                Type licensedEnvironmentSettings = licenseAssembly.GetType(ApplicationParameters.LicensedEnvironmentSettings, throwOnError: false, ignoreCase: true);

                if (licensedEnvironmentSettings == null)
                {
                    "chocolatey".Log().Warn(
                        ChocolateyLoggers.Important,
                        @"Unable to set licensed environment. This is likely related to a
 missing or outdated licensed DLL.");
                    return;
                }
                try
                {
                    object componentClass = Activator.CreateInstance(licensedEnvironmentSettings);

                    licensedEnvironmentSettings.InvokeMember(
                        SET_ENVIRONMENT_METHOD,
                        BindingFlags.InvokeMethod,
                        null,
                        componentClass,
                        new Object[] { config }
                        );
                }
                catch (Exception ex)
                {
                    "chocolatey".Log().Error(
                        ChocolateyLoggers.Important,
                        @"Error when setting configuration for '{0}':{1} {2}".format_with(
                            licensedEnvironmentSettings.FullName,
                            Environment.NewLine,
                            ex.Message
                            ));
                }
            }
        }

        /// <summary>
        ///   Refreshes the current environment values with the updated values,
        ///   even if updated outside of the current process.
        /// </summary>
        /// <remarks>
        ///   This does not remove environment variables, but will ensure all updates are shown.
        ///   To see actual update with removed variables, one will need to restart a shell.
        /// </remarks>
        public static void update_environment_variables()
        {
            // grab original values 
            var originalEnvironmentVariables = convert_to_case_insensitive_dictionary(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process));
            var userName = originalEnvironmentVariables[ApplicationParameters.Environment.Username].to_string();
            var architecture = originalEnvironmentVariables[ApplicationParameters.Environment.ProcessorArchitecture].to_string();
            var originalPath = originalEnvironmentVariables[ApplicationParameters.Environment.Path]
                .to_string()
                .Split(new[] { ApplicationParameters.Environment.EnvironmentSeparator }, StringSplitOptions.RemoveEmptyEntries);
            var originalPathExt = originalEnvironmentVariables[ApplicationParameters.Environment.PathExtensions]
                .to_string()
                .Split(new[] { ApplicationParameters.Environment.EnvironmentSeparator }, StringSplitOptions.RemoveEmptyEntries);
            var originalPsModulePath = originalEnvironmentVariables[ApplicationParameters.Environment.PsModulePath]
                .to_string()
                .Split(new[] { ApplicationParameters.Environment.EnvironmentSeparator }, StringSplitOptions.RemoveEmptyEntries);

            // get updated values from the registry
            var machineVariables = convert_to_case_insensitive_dictionary(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine));
            var userVariables = convert_to_case_insensitive_dictionary(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User));

            // refresh current values with updated values, mathine first
            refresh_environment_variables(machineVariables);
            refresh_environment_variables(userVariables);

            // restore process overridden variables
            if (originalEnvironmentVariables.Contains(ApplicationParameters.Environment.Username)) Environment.SetEnvironmentVariable(ApplicationParameters.Environment.Username, userName);
            if (originalEnvironmentVariables.Contains(ApplicationParameters.Environment.ProcessorArchitecture)) Environment.SetEnvironmentVariable(ApplicationParameters.Environment.ProcessorArchitecture, architecture);

            // combine environment values that append together
            var updatedPath = "{0};{1};".format_with(
                machineVariables[ApplicationParameters.Environment.Path].to_string(),
                userVariables[ApplicationParameters.Environment.Path].to_string()
                ).Replace(";;", ";");
            var updatedPathExt = "{0};{1};".format_with(
                machineVariables[ApplicationParameters.Environment.PathExtensions].to_string(),
                userVariables[ApplicationParameters.Environment.PathExtensions].to_string()
                ).Replace(";;", ";");
            var updatedPsModulePath = "{0};{1};".format_with(
                userVariables[ApplicationParameters.Environment.PsModulePath].to_string(),
                machineVariables[ApplicationParameters.Environment.PsModulePath].to_string()
                ).Replace(";;", ";");

            // add back in process items
            updatedPath += append_process_items(updatedPath, originalPath);
            updatedPathExt += append_process_items(updatedPathExt, originalPathExt);
            updatedPsModulePath += append_process_items(updatedPsModulePath, originalPsModulePath);

            Environment.SetEnvironmentVariable(ApplicationParameters.Environment.Path, updatedPath);
            Environment.SetEnvironmentVariable(ApplicationParameters.Environment.PathExtensions, updatedPathExt);
            Environment.SetEnvironmentVariable(ApplicationParameters.Environment.PsModulePath, updatedPsModulePath);
        }

        private static IDictionary convert_to_case_insensitive_dictionary(IDictionary originalDictionary)
        {
            return new Hashtable(originalDictionary, StringComparer.InvariantCultureIgnoreCase);
        }

        private static void refresh_environment_variables(IDictionary environmentVariables)
        {
            foreach (DictionaryEntry variable in environmentVariables)
            {
                Environment.SetEnvironmentVariable(variable.Key.to_string(), variable.Value.to_string());
            }
        }

        private static string append_process_items(string currentValues, IEnumerable<string> originalValues)
        {
            var additionalItems = new StringBuilder();
            var items = currentValues.Split(
                new[] { ApplicationParameters.Environment.EnvironmentSeparator },
                StringSplitOptions.RemoveEmptyEntries
                );

            foreach (string originalValue in originalValues.or_empty_list_if_null())
            {
                if (!items.Contains(originalValue, StringComparer.InvariantCultureIgnoreCase))
                {
                    additionalItems.AppendFormat("{0};", originalValue);
                }
            }

            return additionalItems.to_string();
        }
    }
}
