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

namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using System.Text;
    using System.Text.RegularExpressions;
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
        //public RegistryService() {}

        private const string UNINSTALLER_KEY_NAME = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
        private const string UNINSTALLER_MSI_MACHINE_KEY_NAME = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData";
        private const string USER_ENVIRONMENT_REGISTRY_KEY_NAME = "Environment";
        private const string MACHINE_ENVIRONMENT_REGISTRY_KEY_NAME = "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment";

        public RegistryService(IXmlService xmlService, IFileSystem fileSystem)
        {
            _xmlService = xmlService;
            _fileSystem = fileSystem;
        }

        private RegistryKey open_key(RegistryHive hive, RegistryView view)
        {
            return FaultTolerance.try_catch_with_logging_exception(
                 () => RegistryKey.OpenBaseKey(hive, view),
                 "Could not open registry hive '{0}' for view '{1}'".format_with(hive.to_string(), view.to_string()),
                 logWarningInsteadOfError: true);
        }

        private void add_key(IList<RegistryKey> keys, RegistryHive hive, RegistryView view)
        {
            var key = open_key(hive, view);
            if (key != null) keys.Add(key);
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
                    () => registryKey.OpenSubKey(UNINSTALLER_KEY_NAME, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey),
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
                            () => evaluate_keys(key.OpenSubKey(subKeyName, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey), snapshot),
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
                    DefaultValue = key.get_value_as_string(""),
                    DisplayName = key.get_value_as_string("DisplayName")
                };

            if (string.IsNullOrWhiteSpace(appKey.DisplayName))
            {
                appKey.DisplayName = appKey.DefaultValue;
            }

            if (!string.IsNullOrWhiteSpace(appKey.DisplayName))
            {
                appKey.InstallLocation = key.get_value_as_string("InstallLocation");
                appKey.UninstallString = key.get_value_as_string("UninstallString");
                if (!string.IsNullOrWhiteSpace(key.get_value_as_string("QuietUninstallString")))
                {
                    appKey.UninstallString = key.get_value_as_string("QuietUninstallString");
                    appKey.HasQuietUninstall = true;
                }

                // informational
                appKey.Publisher = key.get_value_as_string("Publisher");
                appKey.InstallDate = key.get_value_as_string("InstallDate");
                appKey.InstallSource = key.get_value_as_string("InstallSource");
                appKey.Language = key.get_value_as_string("Language");

                // Version
                appKey.DisplayVersion = key.get_value_as_string("DisplayVersion");
                appKey.Version = key.get_value_as_string("Version");
                appKey.VersionMajor = key.get_value_as_string("VersionMajor");
                appKey.VersionMinor = key.get_value_as_string("VersionMinor");

                // installinformation
                appKey.SystemComponent = key.get_value_as_string("SystemComponent") == "1";
                appKey.WindowsInstaller = key.get_value_as_string("WindowsInstaller") == "1";
                appKey.NoRemove = key.get_value_as_string("NoRemove") == "1";
                appKey.NoModify = key.get_value_as_string("NoModify") == "1";
                appKey.NoRepair = key.get_value_as_string("NoRepair") == "1";
                appKey.ReleaseType = key.get_value_as_string("ReleaseType");
                appKey.ParentKeyName = key.get_value_as_string("ParentKeyName");

                if (appKey.WindowsInstaller || appKey.UninstallString.to_string().to_lower().Contains("msiexec"))
                {
                    appKey.InstallerType = InstallerType.Msi;
                }

                if (key.Name.EndsWith("_is1") || !string.IsNullOrWhiteSpace(key.get_value_as_string("Inno Setup: Setup Version")))
                {
                    appKey.InstallerType = InstallerType.InnoSetup;
                }

                if (!string.IsNullOrWhiteSpace(key.get_value_as_string("dwVersionMajor")))
                {
                    appKey.InstallerType = InstallerType.Nsis;
                    appKey.VersionMajor = key.get_value_as_string("dwVersionMajor");
                    appKey.VersionMinor = key.get_value_as_string("dwVersionMinor");
                    appKey.VersionRevision = key.get_value_as_string("dwVersionRev");
                    appKey.VersionBuild = key.get_value_as_string("dwVersionBuild");
                }
                if (appKey.ReleaseType.is_equal_to("Hotfix") || appKey.ReleaseType.is_equal_to("Update Rollup") || appKey.ReleaseType.is_equal_to("Security Update") || appKey.DefaultValue.to_string().StartsWith("KB", ignoreCase: true, culture: CultureInfo.InvariantCulture))
                {
                    appKey.InstallerType = InstallerType.HotfixOrSecurityUpdate;
                }
                if (appKey.ReleaseType.is_equal_to("ServicePack"))
                {
                    appKey.InstallerType = InstallerType.ServicePack;
                }

                // assume NSIS if we still don't know and we find uninst.exe
                if (appKey.InstallerType == InstallerType.Unknown && appKey.UninstallString.to_string().to_lower().Contains("uninst.exe"))
                {
                    appKey.InstallerType = InstallerType.Nsis;
                }

                if (appKey.InstallerType == InstallerType.Unknown && appKey.HasQuietUninstall)
                {
                    appKey.InstallerType = InstallerType.Custom;
                }

                if (appKey.InstallerType == InstallerType.Msi)
                {
                    get_msi_information(appKey, key);
                }

                if (_logOutput)
                {
                    //if (appKey.is_in_programs_and_features() && appKey.InstallerType == InstallerType.Unknown)
                    //{
                        foreach (var name in key.GetValueNames())
                        {
                            //var kind = key.GetValueKind(name);
                            var value = key.get_value_as_string(name);
                            if (name.is_equal_to("QuietUninstallString") || name.is_equal_to("UninstallString"))
                            {
                                Console.WriteLine("key - {0}|{1}={2}|Type detected={3}|install location={4}".format_with(key.Name, name, value.to_string(), appKey.InstallerType.to_string(),appKey.InstallLocation.to_string()));
                            }

                            //Console.WriteLine("key - {0}, name - {1}, kind - {2}, value - {3}".format_with(key.Name, name, kind, value.to_string()));
                        }
                    //}
                }

                snapshot.RegistryKeys.Add(appKey);
            }

            key.Close();
            key.Dispose();
        }

        private void get_msi_information(RegistryApplicationKey appKey, RegistryKey key)
        {
            var userDataProductKeyId = get_msi_user_data_key(key.Name);
            if (string.IsNullOrWhiteSpace(userDataProductKeyId)) return;

            var hklm = open_key(RegistryHive.LocalMachine, RegistryView.Default);
            if (Environment.Is64BitOperatingSystem)
            {
                hklm = open_key(RegistryHive.LocalMachine, RegistryView.Registry64);
            }

            FaultTolerance.try_catch_with_logging_exception(
             () =>
             {
                 var msiRegistryKey = hklm.OpenSubKey(UNINSTALLER_MSI_MACHINE_KEY_NAME, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey);
                 if (msiRegistryKey == null) return;

                 foreach (var subKeyName in msiRegistryKey.GetSubKeyNames())
                 {
                     var msiProductKey = FaultTolerance.try_catch_with_logging_exception(
                         () => msiRegistryKey.OpenSubKey("{0}\\Products\\{1}\\InstallProperties".format_with(subKeyName, userDataProductKeyId), RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey),
                         "Failed to open subkey named '{0}' for '{1}', likely due to permissions".format_with(subKeyName, msiRegistryKey.Name),
                         logWarningInsteadOfError: true);
                     if (msiProductKey == null) continue;

                     appKey.InstallLocation = set_if_empty(appKey.InstallLocation, msiProductKey.get_value_as_string("InstallLocation"));
                     // informational
                     appKey.Publisher = set_if_empty(appKey.Publisher, msiProductKey.get_value_as_string("Publisher"));
                     appKey.InstallDate = set_if_empty(appKey.InstallDate, msiProductKey.get_value_as_string("InstallDate"));
                     appKey.InstallSource = set_if_empty(appKey.InstallSource, msiProductKey.get_value_as_string("InstallSource"));
                     appKey.Language = set_if_empty(appKey.Language, msiProductKey.get_value_as_string("Language"));
                     appKey.LocalPackage = set_if_empty(appKey.LocalPackage, msiProductKey.get_value_as_string("LocalPackage"));

                     // Version
                     appKey.DisplayVersion = set_if_empty(appKey.DisplayVersion, msiProductKey.get_value_as_string("DisplayVersion"));
                     appKey.Version = set_if_empty(appKey.Version, msiProductKey.get_value_as_string("Version"));
                     appKey.VersionMajor = set_if_empty(appKey.VersionMajor, msiProductKey.get_value_as_string("VersionMajor"));
                     appKey.VersionMinor = set_if_empty(appKey.VersionMinor, msiProductKey.get_value_as_string("VersionMinor"));
                     // int _componentLoopCount = 0;
                     // search components for install location if still empty
                     // the performance of this is very bad - without this the query is sub-second
                     // with this it takes about 15 seconds with around 200 apps installed
                     //if (string.IsNullOrWhiteSpace(appKey.InstallLocation) && !appKey.Publisher.contains("Microsoft"))
                     //{
                     //    var msiComponentsKey = FaultTolerance.try_catch_with_logging_exception(
                     //       () => msiRegistryKey.OpenSubKey("{0}\\Components".format_with(subKeyName), RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey),
                     //       "Failed to open subkey named '{0}' for '{1}', likely due to permissions".format_with(subKeyName, msiRegistryKey.Name),
                     //       logWarningInsteadOfError: true);
                     //    if (msiComponentsKey == null) continue;

                     //    foreach (var msiComponentKeyName in msiComponentsKey.GetSubKeyNames())
                     //    {
                     //        var msiComponentKey = FaultTolerance.try_catch_with_logging_exception(
                     //           () => msiComponentsKey.OpenSubKey(msiComponentKeyName, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey),
                     //           "Failed to open subkey named '{0}' for '{1}', likely due to permissions".format_with(subKeyName, msiRegistryKey.Name),
                     //           logWarningInsteadOfError: true);

                     //        if (msiComponentKey.GetValueNames().Contains(userDataProductKeyId, StringComparer.OrdinalIgnoreCase))
                     //        {
                     //            _componentLoopCount++;
                     //            appKey.InstallLocation = set_if_empty(appKey.InstallLocation, get_install_location_estimate(msiComponentKey.get_value(userDataProductKeyId)));
                     //            if (!string.IsNullOrWhiteSpace(appKey.InstallLocation)) break;
                     //            if (_componentLoopCount >= 10) break;
                     //        }
                     //    }
                     //}
                 }
             },
             "Failed to open subkeys for '{0}', likely due to permissions".format_with(hklm.Name),
             logWarningInsteadOfError: true);
        }

        private string set_if_empty(string current, string proposed)
        {
            if (string.IsNullOrWhiteSpace(current)) return proposed;

            return current;
        }

        private Regex _guidRegex = new Regex(@"\{(?<ReverseFull1>\w*)\-(?<ReverseFull2>\w*)\-(?<ReverseFull3>\w*)\-(?<ReversePairs1>\w*)\-(?<ReversePairs2>\w*)\}", RegexOptions.Compiled);
        private Regex _programFilesRegex = new Regex(@"(?<Drive>\w)[\:\?]\\(?<ProgFiles>[Pp]rogram\s[Ff]iles|[Pp]rogram\s[Ff]iles\s\(x86\))\\(?:[Mm]icrosoft[^\\]*|[Cc]ommon\s[Ff]iles|IIS|MSBuild|[Rr]eference\s[Aa]ssemblies|[Ww]indows[^\\]*|(?<Name>[^\\]+))\\", RegexOptions.Compiled);
        private StringBuilder _userDataKey = new StringBuilder();

        private string get_install_location_estimate(string componentPath)
        {
            var match = _programFilesRegex.Match(componentPath);
            if (!match.Success) return string.Empty;
            if (string.IsNullOrWhiteSpace(match.Groups["Name"].Value)) return string.Empty;

            return "{0}:\\{1}\\{2}".format_with(match.Groups["Drive"].Value, match.Groups["ProgFiles"].Value, match.Groups["Name"].Value);
        }

        private string get_msi_user_data_key(string name)
        {
            _userDataKey.Clear();
            var match = _guidRegex.Match(name);
            if (!match.Success) return string.Empty;

            for (int i = 0; i < 3; i++)
            {
                var fullGroup = match.Groups["ReverseFull{0}".format_with(i + 1)];
                if (fullGroup != null)
                {
                    _userDataKey.Append(fullGroup.Value.ToCharArray().Reverse().ToArray());
                }
            }
            for (int i = 0; i < 2; i++)
            {
                var pairsGroup = match.Groups["ReversePairs{0}".format_with(i + 1)];
                if (pairsGroup != null)
                {
                    var pairValue = pairsGroup.Value;
                    for (int j = 0; j < pairValue.Length - 1; j++)
                    {
                        _userDataKey.Append("{1}{0}".format_with(pairValue[j], pairValue[j + 1]));
                        j++;
                    }
                }
            }

            return _userDataKey.to_string();
        }

        public Registry get_installer_key_differences(Registry before, Registry after)
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

            return _xmlService.deserialize<Registry>(filePath, 2);
        }

        private void get_values(RegistryKey key, string subKeyName, IList<GenericRegistryValue> values, bool expandValues)
        {
            if (key != null)
            {
                var subKey = FaultTolerance.try_catch_with_logging_exception(
                   () => key.OpenSubKey(subKeyName, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey),
                   "Could not open uninstall subkey for key '{0}'".format_with(key.Name),
                   logWarningInsteadOfError: true);

                if (subKey != null)
                {
                    foreach (var valueName in subKey.GetValueNames())
                    {
                        values.Add(new GenericRegistryValue
                        {
                            Name = valueName,
                            ParentKeyName = subKey.Name,
                            Type = (RegistryValueKindType)Enum.Parse(typeof(RegistryValueKindType), subKey.GetValueKind(valueName).to_string(), ignoreCase: true),
                            Value = subKey.GetValue(valueName, expandValues ? RegistryValueOptions.None : RegistryValueOptions.DoNotExpandEnvironmentNames).to_string().Replace("\0", string.Empty),
                        });
                    }
                }
            }
        }

        public IEnumerable<GenericRegistryValue> get_environment_values()
        {
            IList<GenericRegistryValue> environmentValues = new List<GenericRegistryValue>();

            get_values(open_key(RegistryHive.CurrentUser, RegistryView.Default), USER_ENVIRONMENT_REGISTRY_KEY_NAME, environmentValues, expandValues: false);
            get_values(open_key(RegistryHive.LocalMachine, RegistryView.Default), MACHINE_ENVIRONMENT_REGISTRY_KEY_NAME, environmentValues, expandValues: false);

            return environmentValues;
        }

        public IEnumerable<GenericRegistryValue> get_added_changed_environment_differences(IEnumerable<GenericRegistryValue> before, IEnumerable<GenericRegistryValue> after)
        {
            return after.Except(before).ToList();
        }

        public IEnumerable<GenericRegistryValue> get_removed_environment_differences(IEnumerable<GenericRegistryValue> before, IEnumerable<GenericRegistryValue> after)
        {
            var removals = new List<GenericRegistryValue>();

            foreach (var beforeValue in before.or_empty_list_if_null())
            {
                var afterValue = after.FirstOrDefault(a => a.Name.is_equal_to(beforeValue.Name) && a.ParentKeyName.is_equal_to(beforeValue.ParentKeyName));
                if (afterValue == null)
                {
                    removals.Add(beforeValue);
                }
            }

            return removals;
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

        public static GenericRegistryValue get_value(RegistryHiveType hive, string subKeyPath, string registryValue)
        {
            var hiveActual = (RegistryHive)Enum.Parse(typeof(RegistryHive), hive.to_string(), ignoreCase: true);
            IList<RegistryKey> keyLocations = new List<RegistryKey>();
            if (Environment.Is64BitOperatingSystem)
            {
                keyLocations.Add(RegistryKey.OpenBaseKey(hiveActual, RegistryView.Registry64));
            }

            keyLocations.Add(RegistryKey.OpenBaseKey(hiveActual, RegistryView.Registry32));

            GenericRegistryValue value = null;

            foreach (var topLevelRegistryKey in keyLocations)
            {
                using (topLevelRegistryKey)
                {
                    var key = topLevelRegistryKey.OpenSubKey(subKeyPath, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey);
                    if (key != null)
                    {
                        value = FaultTolerance.try_catch_with_logging_exception(
                            () =>
                            {
                                if (key.GetValueNames().Contains(registryValue, StringComparer.InvariantCultureIgnoreCase))
                                {
                                    return new GenericRegistryValue
                                    {
                                        Name = registryValue,
                                        ParentKeyName = key.Name,
                                        Type = (RegistryValueKindType)Enum.Parse(typeof(RegistryValueKindType), key.GetValueKind(registryValue).to_string(), ignoreCase: true),
                                        Value = key.get_value_as_string(registryValue),
                                    };
                                }

                                return null;
                            },
                            "Could not get registry value '{0}' from key '{1}'".format_with(registryValue, key.Name),
                            logWarningInsteadOfError: true);

                        if (value != null) break;
                    }
                }
            }

            return value;
        }
    }

}
