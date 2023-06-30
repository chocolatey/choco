// Copyright © 2017 - 2025 Chocolatey Software, Inc
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using chocolatey.infrastructure.adapters;
using chocolatey.infrastructure.logging;
using chocolatey.infrastructure.app.nuget;
using Environment = chocolatey.infrastructure.adapters.Environment;
using static chocolatey.StringResources;

namespace chocolatey.infrastructure.app.configuration
{
    public static class EnvironmentSettings
    {
        private const string SetEnvironmentMethod = "SetEnvironment";
        private static Lazy<IEnvironment> _environmentInitializer = new Lazy<IEnvironment>(() => new Environment());

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void InitializeWith(Lazy<IEnvironment> environment)
        {
            _environmentInitializer = environment;
        }

        private static IEnvironment Environment
        {
            get { return _environmentInitializer.Value; }
        }

#pragma warning disable IDE0060 // Unused method parameter
        public static void ResetEnvironmentVariables(ChocolateyConfiguration config)
#pragma warning restore IDE0060 // Unused method parameter
        {
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyPackageInstallLocation, null);
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyInstallerType, null);
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyExitCode, null);

            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyIgnoreChecksums, null);
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyAllowEmptyChecksums, null);
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyAllowEmptyChecksumsSecure, null);
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyPowerShellHost, null);
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyForce, null);
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyExitOnRebootDetected, null);

            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyProxyLocation, null);
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyProxyBypassList, null);
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyProxyBypassOnLocal, null);
        }

        public static void SetEnvironmentVariables(ChocolateyConfiguration config)
        {
            ResetEnvironmentVariables(config);

            Environment.SetEnvironmentVariable(EnvironmentVariables.System.ChocolateyInstall, ApplicationParameters.InstallLocation);
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyVersion, config.Information.ChocolateyVersion);
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyProductVersion, config.Information.ChocolateyProductVersion);
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.OsPlatform, config.Information.PlatformType.DescriptionOrValue());
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.OsVersion, config.Information.PlatformVersion.ToStringSafe());
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.OsName, config.Information.PlatformName.ToStringSafe());
            // experimental until we know if this value returns correctly based on the OS and not the current process.
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.OsIs64Bit, config.Information.Is64BitOperatingSystem ? "true" : "false");
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ProcessIs64Bit, config.Information.Is64BitProcess ? "true" : "false");
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.Username, config.Information.UserName);
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.UserDomainName, config.Information.UserDomainName);
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.IsAdmin, config.Information.IsUserAdministrator ? "true" : "false");
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.IsUserSystemAccount, config.Information.IsUserSystemAccount ? "true" : "false");
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.IsRemoteDesktop, config.Information.IsUserRemoteDesktop ? "true" : "false");
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.IsRemote, config.Information.IsUserRemote ? "true" : "false");
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.IsProcessElevated, config.Information.IsProcessElevated ? "true" : "false");
            Environment.SetEnvironmentVariable(EnvironmentVariables.System.Temp, config.CacheLocation);
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.Tmp, config.CacheLocation);

            if (config.Debug)
            {
                Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyEnvironmentDebug, "true");
            }

            if (config.Verbose)
            {
                Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyEnvironmentVerbose, "true");
            }

            if (!config.Features.ChecksumFiles)
            {
                Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyIgnoreChecksums, "true");
            }

            if (config.Features.AllowEmptyChecksums)
            {
                Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyAllowEmptyChecksums, "true");
            }

            if (config.Features.AllowEmptyChecksumsSecure)
            {
                Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyAllowEmptyChecksumsSecure, "true");
            }

            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyRequestTimeout, config.WebRequestTimeoutSeconds.ToStringSafe() + "000");

            if (config.CommandExecutionTimeoutSeconds != 0)
            {
                Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyResponseTimeout, config.CommandExecutionTimeoutSeconds.ToStringSafe() + "000");
            }

            if (!string.IsNullOrWhiteSpace(config.Proxy.Location))
            {
                var proxyCreds = string.Empty;
                if (!string.IsNullOrWhiteSpace(config.Proxy.User) &&
                    !string.IsNullOrWhiteSpace(config.Proxy.EncryptedPassword)
                    )
                {
                    proxyCreds = "{0}:{1}@".FormatWith(config.Proxy.User, NugetEncryptionUtility.DecryptString(config.Proxy.EncryptedPassword));

                    Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyProxyUser, config.Proxy.User);
                    Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyProxyPassword, NugetEncryptionUtility.DecryptString(config.Proxy.EncryptedPassword));
                }

                Environment.SetEnvironmentVariable(EnvironmentVariables.System.HttpProxy, "{0}{1}".FormatWith(proxyCreds, config.Proxy.Location));
                Environment.SetEnvironmentVariable(EnvironmentVariables.System.HttpsProxy, "{0}{1}".FormatWith(proxyCreds, config.Proxy.Location));
                Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyProxyLocation, config.Proxy.Location);

                if (!string.IsNullOrWhiteSpace(config.Proxy.BypassList))
                {
                    Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyProxyBypassList, config.Proxy.BypassList);
                    Environment.SetEnvironmentVariable(EnvironmentVariables.System.NoProxy  , config.Proxy.BypassList);

                }

                if (config.Proxy.BypassOnLocal)
                {
                    Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyProxyBypassOnLocal, "true");
                }
            }

            if (config.Features.UsePowerShellHost)
            {
                Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyPowerShellHost, "true");
            }

            if (config.Force)
            {
                Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyForce, "true");
            }

            if (config.Features.ExitOnRebootDetected)
            {
                Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyExitOnRebootDetected, "true");
            }

            SetLicensedEnvironment(config);
        }

        private static void SetLicensedEnvironment(ChocolateyConfiguration config)
        {
            Environment.SetEnvironmentVariable(EnvironmentVariables.Package.ChocolateyLicenseType, config.Information.LicenseType);

            if (!(config.Information.IsLicensedVersion && config.Information.IsLicensedAssemblyLoaded))
            {
                return;
            }

            var licenseAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name.IsEqualTo("chocolatey.licensed"));

            if (licenseAssembly != null)
            {
                Type licensedEnvironmentSettings = licenseAssembly.GetType(ApplicationParameters.LicensedEnvironmentSettings, throwOnError: false, ignoreCase: true);

                if (licensedEnvironmentSettings == null)
                {
                    if (config.RegularOutput)
                    {
                        "chocolatey".Log().Warn(
                        ChocolateyLoggers.Important, @"Unable to set licensed environment settings. Please upgrade to a newer
 licensed version (choco upgrade chocolatey.extension).");
                    }

                    return;
                }
                try
                {
                    var componentClass = Activator.CreateInstance(licensedEnvironmentSettings);

                    licensedEnvironmentSettings.InvokeMember(
                        SetEnvironmentMethod,
                        BindingFlags.InvokeMethod,
                        null,
                        componentClass,
                        new object[] { config }
                        );
                }
                catch (Exception ex)
                {
                    var isDebug = ApplicationParameters.IsDebugModeCliPrimitive();
                    if (config.Debug)
                    {
                        isDebug = true;
                    }

                    var message = isDebug ? ex.ToString() : ex.Message;

                    if (isDebug && ex.InnerException != null)
                    {
                        message += "{0}{1}".FormatWith(Environment.NewLine, ex.InnerException.ToString());
                    }

                    "chocolatey".Log().Error(
                        ChocolateyLoggers.Important,
                        @"Error when setting environment for '{0}':{1} {2}".FormatWith(
                            licensedEnvironmentSettings.FullName,
                            Environment.NewLine,
                            message
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
        public static void UpdateEnvironmentVariables()
        {
            // grab original values
            var originalEnvironmentVariables = ConvertToCaseInsensitiveDictionary(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process));
            var userName = originalEnvironmentVariables[EnvironmentVariables.System.Username].ToStringSafe();
            var architecture = originalEnvironmentVariables[EnvironmentVariables.System.ProcessorArchitecture].ToStringSafe();
            var originalPath = originalEnvironmentVariables[EnvironmentVariables.System.Path]
                .ToStringSafe()
                .Split(new[] { ApplicationParameters.Environment.EnvironmentSeparator }, StringSplitOptions.RemoveEmptyEntries);
            var originalPathExt = originalEnvironmentVariables[EnvironmentVariables.System.PathExtensions]
                .ToStringSafe()
                .Split(new[] { ApplicationParameters.Environment.EnvironmentSeparator }, StringSplitOptions.RemoveEmptyEntries);
            var originalPsModulePath = originalEnvironmentVariables[EnvironmentVariables.System.PSModulePath]
                .ToStringSafe()
                .Split(new[] { ApplicationParameters.Environment.EnvironmentSeparator }, StringSplitOptions.RemoveEmptyEntries);

            // get updated values from the registry
            var machineVariables = ConvertToCaseInsensitiveDictionary(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine));
            var userVariables = ConvertToCaseInsensitiveDictionary(Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User));

            // refresh current values with updated values, machine first
            RefreshEnvironmentVariables(machineVariables);

            //if the user is SYSTEM, we should not even look at user Variables
            var setUserEnvironmentVariables = true;
            try
            {
                var userIdentity = WindowsIdentity.GetCurrent();
                if (userIdentity != null && userIdentity.User == ApplicationParameters.LocalSystemSid)
                {
                    setUserEnvironmentVariables = false;
                }
            }
            catch (Exception ex)
            {
                "chocolatey".Log().Debug("Unable to determine current user to determine if LocalSystem account (to skip user env vars).{0} Reported error: {1}".FormatWith(Environment.NewLine, ex.Message));
            }

            if (setUserEnvironmentVariables)
            {
                RefreshEnvironmentVariables(userVariables);
            }

            // restore process overridden variables
            if (originalEnvironmentVariables.Contains(EnvironmentVariables.System.Username))
            {
               
            {
                Environment.SetEnvironmentVariable(EnvironmentVariables.System.Username, userName);
            }

            }

            if (originalEnvironmentVariables.Contains(EnvironmentVariables.System.ProcessorArchitecture))
            {
                Environment.SetEnvironmentVariable(EnvironmentVariables.System.ProcessorArchitecture, architecture);
            }

            // combine environment values that append together
            var updatedPath = "{0};{1};".FormatWith(
                machineVariables[EnvironmentVariables.System.Path].ToStringSafe(),
                userVariables[EnvironmentVariables.System.Path].ToStringSafe()
                ).Replace(";;", ";");
            var updatedPathExt = "{0};{1};".FormatWith(
                machineVariables[EnvironmentVariables.System.PathExtensions].ToStringSafe(),
                userVariables[EnvironmentVariables.System.PathExtensions].ToStringSafe()
                ).Replace(";;", ";");
            var updatedPsModulePath = "{0};{1};".FormatWith(
                userVariables[EnvironmentVariables.System.PSModulePath].ToStringSafe(),
                machineVariables[EnvironmentVariables.System.PSModulePath].ToStringSafe()
                ).Replace(";;", ";");

            // add back in process items
            updatedPath += GetProcessOnlyItems(updatedPath, originalPath);
            updatedPathExt += GetProcessOnlyItems(updatedPathExt, originalPathExt);
            updatedPsModulePath = "{0};{1}".FormatWith(GetProcessOnlyItems(updatedPsModulePath, originalPsModulePath), updatedPsModulePath);

            if (!updatedPsModulePath.ContainsSafe(ApplicationParameters.PowerShellModulePathProcessProgramFiles))
            {
                updatedPsModulePath = "{0};{1}".FormatWith(ApplicationParameters.PowerShellModulePathProcessProgramFiles, updatedPsModulePath).Replace(";;", ";");
            }

            if (!updatedPsModulePath.ContainsSafe(ApplicationParameters.PowerShellModulePathProcessDocuments))
            {
                updatedPsModulePath = "{0};{1}".FormatWith(ApplicationParameters.PowerShellModulePathProcessDocuments, updatedPsModulePath).Replace(";;", ";");
            }

            if (updatedPsModulePath.StartsWith(";"))
            {
                updatedPsModulePath = updatedPsModulePath.Remove(0, 1);
            }

            Environment.SetEnvironmentVariable(EnvironmentVariables.System.Path, updatedPath);
            Environment.SetEnvironmentVariable(EnvironmentVariables.System.PathExtensions, updatedPathExt);
            Environment.SetEnvironmentVariable(EnvironmentVariables.System.PSModulePath, updatedPsModulePath);
        }

        private static IDictionary ConvertToCaseInsensitiveDictionary(IDictionary originalDictionary)
        {
            if (originalDictionary == null)
            {
                return new Hashtable(new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
            }

            return new Hashtable(originalDictionary, StringComparer.OrdinalIgnoreCase);
        }

        private static void RefreshEnvironmentVariables(IDictionary environmentVariables)
        {
            foreach (DictionaryEntry variable in environmentVariables.OrEmpty())
            {
                Environment.SetEnvironmentVariable(variable.Key.ToStringSafe(), variable.Value.ToStringSafe());
            }
        }

        private static string GetProcessOnlyItems(string currentValues, IEnumerable<string> originalValues)
        {
            var additionalItems = new StringBuilder();
            var items = currentValues.Split(
                new[] { ApplicationParameters.Environment.EnvironmentSeparator },
                StringSplitOptions.RemoveEmptyEntries
                );

            foreach (var originalValue in originalValues.OrEmpty())
            {
                if (!items.Contains(originalValue, StringComparer.InvariantCultureIgnoreCase))
                {
                    additionalItems.AppendFormat("{0};", originalValue);
                }
            }

            return additionalItems.ToStringSafe();
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void initialize_with(Lazy<IEnvironment> environment)
            => InitializeWith(environment);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static void reset_environment_variables(ChocolateyConfiguration config)
            => ResetEnvironmentVariables(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static void set_environment_variables(ChocolateyConfiguration config)
            => SetEnvironmentVariables(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static void update_environment_variables()
            => UpdateEnvironmentVariables();
#pragma warning restore IDE0022, IDE1006
    }
}
