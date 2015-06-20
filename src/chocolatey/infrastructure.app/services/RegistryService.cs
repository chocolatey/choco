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

namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using Microsoft.Win32;
    using domain;
    using filesystem;
    using infrastructure.services;
    using tolerance;
    using Registry = domain.Registry;

    /// <summary>
    ///   Allows comparing registry
    /// </summary>
    public sealed class RegistryService : IRegistryService
    {
        private readonly IXmlService _xmlService;
        private readonly IFileSystem _fileSystem;
        private readonly bool _logOutput = false;

        public RegistryService(IXmlService xmlService, IFileSystem fileSystem)
        {
            _xmlService = xmlService;
            _fileSystem = fileSystem;
        }

        private void add_key(IList<RegistryKey> keys, RegistryHive hive, RegistryView view)
        {
            FaultTolerance.try_catch_with_logging_exception(
                () => keys.Add(RegistryKey.OpenBaseKey(hive, view)),
                "Could not open registry hive '{0}' for view '{1}'".format_with(hive.to_string(), view.to_string()),
                logWarningInsteadOfError: true);
        }

        public Registry get_installer_keys()
        {
            var snapshot = new Registry();
            var windowsIdentity = WindowsIdentity.GetCurrent();
            if (windowsIdentity != null) snapshot.User = windowsIdentity.User.to_string();

            IList<RegistryKey> keys = new List<RegistryKey>();
            if (Environment.Is64BitOperatingSystem)
            {
                add_key(keys, RegistryHive.CurrentUser, RegistryView.Registry64);
                add_key(keys, RegistryHive.LocalMachine, RegistryView.Registry64);
            }

            add_key(keys, RegistryHive.CurrentUser, RegistryView.Registry32);
            add_key(keys, RegistryHive.LocalMachine, RegistryView.Registry32);

            foreach (var registryKey in keys)
            {
                var uninstallKey = FaultTolerance.try_catch_with_logging_exception(
                    () => registryKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey),
                    "Could not open uninstall subkey for key '{0}'".format_with(registryKey.Name),
                    logWarningInsteadOfError: true);

                if (uninstallKey != null)
                {
                    //Console.WriteLine("Evaluating {0} of {1}".format_with(uninstallKey.View, uninstallKey.Name));
                    evaluate_keys(uninstallKey, snapshot);
                }
                registryKey.Close();
                registryKey.Dispose();
            }

            if (_logOutput)
            {
                Console.WriteLine("");
                Console.WriteLine("A total of {0} unrecognized apps".format_with(snapshot.RegistryKeys.Where((p) => p.InstallerType == InstallerType.Unknown && p.is_in_programs_and_features()).Count()));
                Console.WriteLine("");

                Console.WriteLine("");
                Console.WriteLine("A total of {0} of {1} are programs and features apps".format_with(snapshot.RegistryKeys.Where((p) => p.is_in_programs_and_features()).Count(), snapshot.RegistryKeys.Count));
                Console.WriteLine("");
            }

            return snapshot;
        }

        /// <summary>
        ///   Evaluates registry keys and updates the snapshop with items
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="snapshot">The snapshot.</param>
        public void evaluate_keys(RegistryKey key, Registry snapshot)
        {
            if (key == null) return;

            FaultTolerance.try_catch_with_logging_exception(
                () =>
                    {
                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            FaultTolerance.try_catch_with_logging_exception(
                                () =>  evaluate_keys(key.OpenSubKey(subKeyName, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey), snapshot),
                                "Failed to open subkey named '{0}' for '{1}', likely due to permissions".format_with(subKeyName, key.Name),
                                logWarningInsteadOfError: true);
                        }
                    },
                "Failed to open subkeys for '{0}', likely due to permissions".format_with(key.Name),
                logWarningInsteadOfError: true);

            var appKey = new RegistryApplicationKey
                {
                    KeyPath = key.Name,
                    RegistryView = key.View,
                    DefaultValue = key.GetValue("").to_string(),
                    DisplayName = key.GetValue("DisplayName").to_string()
                };

            if (string.IsNullOrWhiteSpace(appKey.DisplayName))
            {
                appKey.DisplayName = appKey.DefaultValue;
            }

            if (!string.IsNullOrWhiteSpace(appKey.DisplayName))
            {
                appKey.InstallLocation = key.GetValue("InstallLocation").to_string();
                appKey.UninstallString = key.GetValue("UninstallString").to_string();
                if (key.GetValue("QuietUninstallString") != null)
                {
                    appKey.UninstallString = key.GetValue("QuietUninstallString").to_string();
                    appKey.HasQuietUninstall = true;
                }

                // informational
                appKey.Publisher = key.GetValue("Publisher").to_string();
                appKey.InstallDate = key.GetValue("InstallDate").to_string();
                appKey.InstallSource = key.GetValue("InstallSource").to_string();
                appKey.Language = key.GetValue("Language").to_string();

                // Version
                appKey.DisplayVersion = key.GetValue("DisplayVersion").to_string();
                appKey.Version = key.GetValue("Version").to_string();
                appKey.VersionMajor = key.GetValue("VersionMajor").to_string();
                appKey.VersionMinor = key.GetValue("VersionMinor").to_string();

                // installinformation
                appKey.SystemComponent = key.GetValue("SystemComponent").to_string() == "1";
                appKey.WindowsInstaller = key.GetValue("WindowsInstaller").to_string() == "1";
                appKey.NoRemove = key.GetValue("NoRemove").to_string() == "1";
                appKey.NoModify = key.GetValue("NoModify").to_string() == "1";
                appKey.NoRepair = key.GetValue("NoRepair").to_string() == "1";
                appKey.ReleaseType = key.GetValue("ReleaseType").to_string();
                appKey.ParentKeyName = key.GetValue("ParentKeyName").to_string();

                if (appKey.WindowsInstaller || appKey.UninstallString.to_lower().Contains("msiexec"))
                {
                    appKey.InstallerType = InstallerType.Msi;
                }

                if (key.Name.EndsWith("_is1") || !string.IsNullOrWhiteSpace(key.GetValue("Inno Setup: Setup Version").to_string()))
                {
                    appKey.InstallerType = InstallerType.InnoSetup;
                }

                if (key.GetValue("dwVersionMajor") != null)
                {
                    appKey.InstallerType = InstallerType.Nsis;
                    appKey.VersionMajor = key.GetValue("dwVersionMajor").to_string();
                    appKey.VersionMinor = key.GetValue("dwVersionMinor").to_string();
                    appKey.VersionRevision = key.GetValue("dwVersionRev").to_string();
                    appKey.VersionBuild = key.GetValue("dwVersionBuild").to_string();
                }
                if (appKey.ReleaseType.is_equal_to("Hotfix") || appKey.ReleaseType.is_equal_to("Update Rollup") || appKey.ReleaseType.is_equal_to("Security Update") || appKey.DefaultValue.to_string().StartsWith("KB", ignoreCase: true, culture: CultureInfo.InvariantCulture))
                {
                    appKey.InstallerType = InstallerType.HotfixOrSecurityUpdate;
                }
                if (appKey.ReleaseType.is_equal_to("ServicePack"))
                {
                    appKey.InstallerType = InstallerType.ServicePack;
                }

                if (appKey.InstallerType == InstallerType.Unknown && appKey.HasQuietUninstall)
                {
                    appKey.InstallerType = InstallerType.Custom;
                }

                if (_logOutput)
                {
                    if (appKey.is_in_programs_and_features() && appKey.InstallerType == InstallerType.Unknown)
                    {
                        foreach (var name in key.GetValueNames())
                        {
                            //var kind = key.GetValueKind(name);
                            var value = key.GetValue(name);
                            if (name.is_equal_to("QuietUninstallString") || name.is_equal_to("UninstallString"))
                            {
                                Console.WriteLine("key - {0}|{1}={2}|Type detected={3}".format_with(key.Name, name, value.to_string(), appKey.InstallerType.to_string()));
                            }

                            //Console.WriteLine("key - {0}, name - {1}, kind - {2}, value - {3}".format_with(key.Name, name, kind, value.to_string()));
                        }
                    }
                }

                snapshot.RegistryKeys.Add(appKey);
            }

            key.Close();
            key.Dispose();
        }

        public Registry get_differences(Registry before, Registry after)
        {
            //var difference = after.RegistryKeys.Where(r => !before.RegistryKeys.Contains(r)).ToList();
            return new Registry(after.User, after.RegistryKeys.Except(before.RegistryKeys).ToList());
        }

        public void save_to_file(Registry snapshot, string filePath)
        {
            _xmlService.serialize(snapshot, filePath);
        }

        public bool installer_value_exists(string keyPath, string value)
        {
            return get_installer_keys().RegistryKeys.Any(k => k.KeyPath == keyPath);
        }

        public Registry read_from_file(string filePath)
        {
            if (!_fileSystem.file_exists(filePath))
            {
                return null;
            }

            return _xmlService.deserialize<Registry>(filePath);
        }

        public RegistryKey get_key(RegistryHive hive, string subKeyPath)
        {
            IList<RegistryKey> keyLocations = new List<RegistryKey>();
            if (Environment.Is64BitOperatingSystem)
            {
                keyLocations.Add(RegistryKey.OpenBaseKey(hive, RegistryView.Registry64));
            }

            keyLocations.Add(RegistryKey.OpenBaseKey(hive, RegistryView.Registry32));

            foreach (var topLevelRegistryKey in keyLocations)
            {
                using (topLevelRegistryKey)
                {
                    var key = topLevelRegistryKey.OpenSubKey(subKeyPath, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey);
                    if (key != null)
                    {
                        return key;
                    }
                }
            }

            return null;
        }
    }
}