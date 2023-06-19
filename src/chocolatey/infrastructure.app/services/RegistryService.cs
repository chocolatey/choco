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

        private const string UninstallerKeyName = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
        private const string UninstallerMsiMachineKeyName = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\UserData";
        private const string UserEnvironmentRegistryKeyName = "Environment";
        private const string MachineEnvironmentRegistryKeyName = "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment";

        public RegistryService(IXmlService xmlService, IFileSystem fileSystem)
        {
            _xmlService = xmlService;
            _fileSystem = fileSystem;
        }

        private RegistryKey OpenKey(RegistryHive hive, RegistryView view)
        {
            return FaultTolerance.TryCatchWithLoggingException(
                 () => RegistryKey.OpenBaseKey(hive, view),
                 "Could not open registry hive '{0}' for view '{1}'".FormatWith(hive.ToStringSafe(), view.ToStringSafe()),
                 logWarningInsteadOfError: true);
        }

        private void AddKey(IList<RegistryKey> keys, RegistryHive hive, RegistryView view)
        {
            var key = OpenKey(hive, view);
            if (key != null) keys.Add(key);
        }

        public Registry GetInstallerKeys()
        {
            var snapshot = new Registry();
            var windowsIdentity = WindowsIdentity.GetCurrent();
            if (windowsIdentity != null) snapshot.User = windowsIdentity.User.ToStringSafe();

            IList<RegistryKey> keys = new List<RegistryKey>();
            if (Environment.Is64BitOperatingSystem)
            {
                AddKey(keys, RegistryHive.CurrentUser, RegistryView.Registry64);
                AddKey(keys, RegistryHive.LocalMachine, RegistryView.Registry64);
            }

            AddKey(keys, RegistryHive.CurrentUser, RegistryView.Registry32);
            AddKey(keys, RegistryHive.LocalMachine, RegistryView.Registry32);

            foreach (var registryKey in keys)
            {
                var uninstallKey = FaultTolerance.TryCatchWithLoggingException(
                    () => registryKey.OpenSubKey(UninstallerKeyName, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey),
                    "Could not open uninstall subkey for key '{0}'".FormatWith(registryKey.Name),
                    logWarningInsteadOfError: true);

                if (uninstallKey != null)
                {
                    //Console.WriteLine("Evaluating {0} of {1}".format_with(uninstallKey.View, uninstallKey.Name));
                    UpdateSnapshot(uninstallKey, snapshot);
                }
                registryKey.Close();
                registryKey.Dispose();
            }

            if (_logOutput)
            {
                Console.WriteLine("");
                Console.WriteLine("A total of {0} unrecognized apps".FormatWith(snapshot.RegistryKeys.Where((p) => p.InstallerType == InstallerType.Unknown && p.IsInProgramsAndFeatures()).Count()));
                Console.WriteLine("");

                Console.WriteLine("");
                Console.WriteLine("A total of {0} of {1} are programs and features apps".FormatWith(snapshot.RegistryKeys.Where((p) => p.IsInProgramsAndFeatures()).Count(), snapshot.RegistryKeys.Count));
                Console.WriteLine("");
            }

            return snapshot;
        }

        /// <summary>
        ///   Evaluates registry keys and updates the snapshot with items
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="snapshot">The snapshot.</param>
        public void UpdateSnapshot(RegistryKey key, Registry snapshot)
        {
            if (key == null) return;

            FaultTolerance.TryCatchWithLoggingException(
                () =>
                {
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        FaultTolerance.TryCatchWithLoggingException(
                            () => UpdateSnapshot(key.OpenSubKey(subKeyName, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey), snapshot),
                            "Failed to open subkey named '{0}' for '{1}', likely due to permissions".FormatWith(subKeyName, key.Name),
                            logWarningInsteadOfError: true);
                    }
                },
                "Failed to open subkeys for '{0}', likely due to permissions".FormatWith(key.Name),
                logWarningInsteadOfError: true);

            var appKey = new RegistryApplicationKey
                {
                    KeyPath = key.Name,
                    RegistryView = key.View,
                    DefaultValue = key.AsXmlSafeString(""),
                    DisplayName = key.AsXmlSafeString("DisplayName")
                };

            if (string.IsNullOrWhiteSpace(appKey.DisplayName))
            {
                appKey.DisplayName = appKey.DefaultValue;
            }

            if (!string.IsNullOrWhiteSpace(appKey.DisplayName))
            {
                appKey.InstallLocation = key.AsXmlSafeString("InstallLocation");
                appKey.UninstallString = key.AsXmlSafeString("UninstallString");
                if (!string.IsNullOrWhiteSpace(key.AsXmlSafeString("QuietUninstallString")))
                {
                    appKey.UninstallString = key.AsXmlSafeString("QuietUninstallString");
                    appKey.HasQuietUninstall = true;
                }

                // informational
                appKey.Publisher = key.AsXmlSafeString("Publisher");
                appKey.InstallDate = key.AsXmlSafeString("InstallDate");
                appKey.InstallSource = key.AsXmlSafeString("InstallSource");
                appKey.Language = key.AsXmlSafeString("Language");

                // Version
                appKey.DisplayVersion = key.AsXmlSafeString("DisplayVersion");
                appKey.Version = key.AsXmlSafeString("Version");
                appKey.VersionMajor = key.AsXmlSafeString("VersionMajor");
                appKey.VersionMinor = key.AsXmlSafeString("VersionMinor");

                // installinformation
                appKey.SystemComponent = key.AsXmlSafeString("SystemComponent") == "1";
                appKey.WindowsInstaller = key.AsXmlSafeString("WindowsInstaller") == "1";
                appKey.NoRemove = key.AsXmlSafeString("NoRemove") == "1";
                appKey.NoModify = key.AsXmlSafeString("NoModify") == "1";
                appKey.NoRepair = key.AsXmlSafeString("NoRepair") == "1";
                appKey.ReleaseType = key.AsXmlSafeString("ReleaseType");
                appKey.ParentKeyName = key.AsXmlSafeString("ParentKeyName");

                if (appKey.WindowsInstaller || appKey.UninstallString.ToStringSafe().ToLowerSafe().Contains("msiexec"))
                {
                    appKey.InstallerType = InstallerType.Msi;
                }

                if (key.Name.EndsWith("_is1") || !string.IsNullOrWhiteSpace(key.AsXmlSafeString("Inno Setup: Setup Version")))
                {
                    appKey.InstallerType = InstallerType.InnoSetup;
                }

                if (!string.IsNullOrWhiteSpace(key.AsXmlSafeString("dwVersionMajor")))
                {
                    appKey.InstallerType = InstallerType.Nsis;
                    appKey.VersionMajor = key.AsXmlSafeString("dwVersionMajor");
                    appKey.VersionMinor = key.AsXmlSafeString("dwVersionMinor");
                    appKey.VersionRevision = key.AsXmlSafeString("dwVersionRev");
                    appKey.VersionBuild = key.AsXmlSafeString("dwVersionBuild");
                }
                if (appKey.ReleaseType.IsEqualTo("Hotfix") || appKey.ReleaseType.IsEqualTo("Update Rollup") || appKey.ReleaseType.IsEqualTo("Security Update") || appKey.DefaultValue.ToStringSafe().StartsWith("KB", ignoreCase: true, culture: CultureInfo.InvariantCulture))
                {
                    appKey.InstallerType = InstallerType.HotfixOrSecurityUpdate;
                }
                if (appKey.ReleaseType.IsEqualTo("ServicePack"))
                {
                    appKey.InstallerType = InstallerType.ServicePack;
                }

                // assume NSIS if we still don't know and we find uninst.exe
                if (appKey.InstallerType == InstallerType.Unknown && appKey.UninstallString.ToStringSafe().ToLowerSafe().Contains("uninst.exe"))
                {
                    appKey.InstallerType = InstallerType.Nsis;
                }

                if (appKey.InstallerType == InstallerType.Unknown && appKey.HasQuietUninstall)
                {
                    appKey.InstallerType = InstallerType.Custom;
                }

                if (appKey.InstallerType == InstallerType.Msi)
                {
                    GetMsiInformation(appKey, key);
                }

                if (_logOutput)
                {
                    //if (appKey.is_in_programs_and_features() && appKey.InstallerType == InstallerType.Unknown)
                    //{
                        foreach (var name in key.GetValueNames())
                        {
                            //var kind = key.GetValueKind(name);
                            var value = key.AsXmlSafeString(name);
                            if (name.IsEqualTo("QuietUninstallString") || name.IsEqualTo("UninstallString"))
                            {
                                Console.WriteLine("key - {0}|{1}={2}|Type detected={3}|install location={4}".FormatWith(key.Name, name, value.ToStringSafe(), appKey.InstallerType.ToStringSafe(),appKey.InstallLocation.ToStringSafe()));
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

        private void GetMsiInformation(RegistryApplicationKey appKey, RegistryKey key)
        {
            var userDataProductKeyId = GetMsiUserDataKey(key.Name);
            if (string.IsNullOrWhiteSpace(userDataProductKeyId)) return;

            var hklm = OpenKey(RegistryHive.LocalMachine, RegistryView.Default);
            if (Environment.Is64BitOperatingSystem)
            {
                hklm = OpenKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            }

            FaultTolerance.TryCatchWithLoggingException(
             () =>
             {
                 var msiRegistryKey = hklm.OpenSubKey(UninstallerMsiMachineKeyName, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey);
                 if (msiRegistryKey == null) return;

                 foreach (var subKeyName in msiRegistryKey.GetSubKeyNames())
                 {
                     var msiProductKey = FaultTolerance.TryCatchWithLoggingException(
                         () => msiRegistryKey.OpenSubKey("{0}\\Products\\{1}\\InstallProperties".FormatWith(subKeyName, userDataProductKeyId), RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey),
                         "Failed to open subkey named '{0}' for '{1}', likely due to permissions".FormatWith(subKeyName, msiRegistryKey.Name),
                         logWarningInsteadOfError: true);
                     if (msiProductKey == null) continue;

                     appKey.InstallLocation = SetIfEmpty(appKey.InstallLocation, msiProductKey.AsXmlSafeString("InstallLocation"));
                     // informational
                     appKey.Publisher = SetIfEmpty(appKey.Publisher, msiProductKey.AsXmlSafeString("Publisher"));
                     appKey.InstallDate = SetIfEmpty(appKey.InstallDate, msiProductKey.AsXmlSafeString("InstallDate"));
                     appKey.InstallSource = SetIfEmpty(appKey.InstallSource, msiProductKey.AsXmlSafeString("InstallSource"));
                     appKey.Language = SetIfEmpty(appKey.Language, msiProductKey.AsXmlSafeString("Language"));
                     appKey.LocalPackage = SetIfEmpty(appKey.LocalPackage, msiProductKey.AsXmlSafeString("LocalPackage"));

                     // Version
                     appKey.DisplayVersion = SetIfEmpty(appKey.DisplayVersion, msiProductKey.AsXmlSafeString("DisplayVersion"));
                     appKey.Version = SetIfEmpty(appKey.Version, msiProductKey.AsXmlSafeString("Version"));
                     appKey.VersionMajor = SetIfEmpty(appKey.VersionMajor, msiProductKey.AsXmlSafeString("VersionMajor"));
                     appKey.VersionMinor = SetIfEmpty(appKey.VersionMinor, msiProductKey.AsXmlSafeString("VersionMinor"));
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
             "Failed to open subkeys for '{0}', likely due to permissions".FormatWith(hklm.Name),
             logWarningInsteadOfError: true);
        }

        private string SetIfEmpty(string current, string proposed)
        {
            if (string.IsNullOrWhiteSpace(current)) return proposed;

            return current;
        }

        private Regex _guidRegex = new Regex(@"\{(?<ReverseFull1>\w*)\-(?<ReverseFull2>\w*)\-(?<ReverseFull3>\w*)\-(?<ReversePairs1>\w*)\-(?<ReversePairs2>\w*)\}", RegexOptions.Compiled);
        private Regex _programFilesRegex = new Regex(@"(?<Drive>\w)[\:\?]\\(?<ProgFiles>[Pp]rogram\s[Ff]iles|[Pp]rogram\s[Ff]iles\s\(x86\))\\(?:[Mm]icrosoft[^\\]*|[Cc]ommon\s[Ff]iles|IIS|MSBuild|[Rr]eference\s[Aa]ssemblies|[Ww]indows[^\\]*|(?<Name>[^\\]+))\\", RegexOptions.Compiled);
        private StringBuilder _userDataKey = new StringBuilder();

        private string GetMsiUserDataKey(string name)
        {
            _userDataKey.Clear();
            var match = _guidRegex.Match(name);
            if (!match.Success) return string.Empty;

            for (int i = 0; i < 3; i++)
            {
                var fullGroup = match.Groups["ReverseFull{0}".FormatWith(i + 1)];
                if (fullGroup != null)
                {
                    _userDataKey.Append(fullGroup.Value.ToCharArray().Reverse().ToArray());
                }
            }
            for (int i = 0; i < 2; i++)
            {
                var pairsGroup = match.Groups["ReversePairs{0}".FormatWith(i + 1)];
                if (pairsGroup != null)
                {
                    var pairValue = pairsGroup.Value;
                    for (int j = 0; j < pairValue.Length - 1; j++)
                    {
                        _userDataKey.Append("{1}{0}".FormatWith(pairValue[j], pairValue[j + 1]));
                        j++;
                    }
                }
            }

            return _userDataKey.ToStringSafe();
        }

        public Registry GetInstallerKeysChanged(Registry before, Registry after)
        {
            //var difference = after.RegistryKeys.Where(r => !before.RegistryKeys.Contains(r)).ToList();
            return new Registry(after.User, after.RegistryKeys.Except(before.RegistryKeys).ToList());
        }

        public void SaveRegistrySnapshot(Registry snapshot, string filePath)
        {
            _xmlService.Serialize(snapshot, filePath);
        }

        public bool InstallerKeyExists(string keyPath)
        {
            return GetInstallerKeys().RegistryKeys.Any(k => k.KeyPath == keyPath);
        }

        public Registry ReadRegistrySnapshot(string filePath)
        {
            if (!_fileSystem.FileExists(filePath))
            {
                return null;
            }

            return _xmlService.Deserialize<Registry>(filePath, 2);
        }

        private void GetValues(RegistryKey key, string subKeyName, IList<GenericRegistryValue> values, bool expandValues)
        {
            if (key != null)
            {
                var subKey = FaultTolerance.TryCatchWithLoggingException(
                   () => key.OpenSubKey(subKeyName, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey),
                   "Could not open uninstall subkey for key '{0}'".FormatWith(key.Name),
                   logWarningInsteadOfError: true);

                if (subKey != null)
                {
                    foreach (var valueName in subKey.GetValueNames())
                    {
                        values.Add(new GenericRegistryValue
                        {
                            Name = valueName,
                            ParentKeyName = subKey.Name,
                            Type = (RegistryValueKindType)Enum.Parse(typeof(RegistryValueKindType), subKey.GetValueKind(valueName).ToStringSafe(), ignoreCase: true),
                            Value = subKey.GetValue(valueName, expandValues ? RegistryValueOptions.None : RegistryValueOptions.DoNotExpandEnvironmentNames).ToStringSafe().Replace("\0", string.Empty),
                        });
                    }
                }
            }
        }

        public IEnumerable<GenericRegistryValue> GetEnvironmentValues()
        {
            IList<GenericRegistryValue> environmentValues = new List<GenericRegistryValue>();

            GetValues(OpenKey(RegistryHive.CurrentUser, RegistryView.Default), UserEnvironmentRegistryKeyName, environmentValues, expandValues: false);
            GetValues(OpenKey(RegistryHive.LocalMachine, RegistryView.Default), MachineEnvironmentRegistryKeyName, environmentValues, expandValues: false);

            return environmentValues;
        }

        public IEnumerable<GenericRegistryValue> GetNewAndModifiedEnvironmentValues(IEnumerable<GenericRegistryValue> before, IEnumerable<GenericRegistryValue> after)
        {
            return after.Except(before).ToList();
        }

        public IEnumerable<GenericRegistryValue> GetRemovedEnvironmentValues(IEnumerable<GenericRegistryValue> before, IEnumerable<GenericRegistryValue> after)
        {
            var removals = new List<GenericRegistryValue>();

            foreach (var beforeValue in before.OrEmpty())
            {
                var afterValue = after.FirstOrDefault(a => a.Name.IsEqualTo(beforeValue.Name) && a.ParentKeyName.IsEqualTo(beforeValue.ParentKeyName));
                if (afterValue == null)
                {
                    removals.Add(beforeValue);
                }
            }

            return removals;
        }

        public RegistryKey GetKey(RegistryHive hive, string subKeyPath)
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

        public static GenericRegistryValue GetRegistryValue(RegistryHiveType hive, string subKeyPath, string registryValue)
        {
            var hiveActual = (RegistryHive)Enum.Parse(typeof(RegistryHive), hive.ToStringSafe(), ignoreCase: true);
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
                        value = FaultTolerance.TryCatchWithLoggingException(
                            () =>
                            {
                                if (key.GetValueNames().Contains(registryValue, StringComparer.InvariantCultureIgnoreCase))
                                {
                                    return new GenericRegistryValue
                                    {
                                        Name = registryValue,
                                        ParentKeyName = key.Name,
                                        Type = (RegistryValueKindType)Enum.Parse(typeof(RegistryValueKindType), key.GetValueKind(registryValue).ToStringSafe(), ignoreCase: true),
                                        Value = key.AsXmlSafeString(registryValue),
                                    };
                                }

                                return null;
                            },
                            "Could not get registry value '{0}' from key '{1}'".FormatWith(registryValue, key.Name),
                            logWarningInsteadOfError: true);

                        if (value != null) break;
                    }
                }
            }

            return value;
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public Registry get_installer_keys()
            => GetInstallerKeys();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void evaluate_keys(RegistryKey key, Registry snapshot)
            => UpdateSnapshot(key, snapshot);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public Registry get_installer_key_differences(Registry before, Registry after)
            => GetInstallerKeysChanged(before, after);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void save_to_file(Registry snapshot, string filePath)
            => SaveRegistrySnapshot(snapshot, filePath);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public bool installer_value_exists(string keyPath, string value)
            => InstallerKeyExists(keyPath);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public Registry read_from_file(string filePath)
            => ReadRegistrySnapshot(filePath);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public IEnumerable<GenericRegistryValue> get_environment_values()
            => GetEnvironmentValues();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public IEnumerable<GenericRegistryValue> get_added_changed_environment_differences(IEnumerable<GenericRegistryValue> before, IEnumerable<GenericRegistryValue> after)
            => GetNewAndModifiedEnvironmentValues(before, after);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public IEnumerable<GenericRegistryValue> get_removed_environment_differences(IEnumerable<GenericRegistryValue> before, IEnumerable<GenericRegistryValue> after)
            => GetRemovedEnvironmentValues(before, after);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public RegistryKey get_key(RegistryHive hive, string subKeyPath)
            => GetKey(hive, subKeyPath);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static GenericRegistryValue get_value(RegistryHiveType hive, string subKeyPath, string registryValue)
            => GetRegistryValue(hive, subKeyPath, registryValue);
#pragma warning restore IDE1006
    }
}
