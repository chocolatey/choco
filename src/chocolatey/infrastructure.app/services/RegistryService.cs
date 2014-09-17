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
    using Registry = domain.Registry;

    /// <summary>
    ///   Allows comparing registry
    /// </summary>
    public class RegistryService : IRegistryService
    {
        private readonly IXmlService _xmlService;
        private readonly IFileSystem _fileSystem;

        public RegistryService(IXmlService xmlService, IFileSystem fileSystem)
        {
            _xmlService = xmlService;
            _fileSystem = fileSystem;
        }

        public Registry get_installer_keys()
        {
            var snapshot = new Registry();
            var windowsIdentity = WindowsIdentity.GetCurrent();
            if (windowsIdentity != null) snapshot.User = windowsIdentity.User.to_string();

            IList<RegistryKey> keys = new List<RegistryKey>();
            if (Environment.Is64BitOperatingSystem)
            {
                keys.Add(RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64));
                keys.Add(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64));
            }

            keys.Add(RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32));
            keys.Add(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32));

            foreach (var registryKey in keys)
            {
                var uninstallKey = registryKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey);
                if (uninstallKey != null)
                {
                    //Console.WriteLine("Evaluating {0} of {1}".format_with(uninstallKey.View, uninstallKey.Name));
                    evaluate_keys(uninstallKey, snapshot);
                }
                registryKey.Close();
                registryKey.Dispose();
            }

            //Console.WriteLine("");
            //Console.WriteLine("A total of {0} unrecognized apps".format_with(snapshot.RegistryKeys.Where((p) => p.InstallerType == InstallerType.Unknown).Count()));
            //Console.WriteLine("");

            //Console.WriteLine("");
            //Console.WriteLine("A total of {0} of {1} are programs and features apps".format_with(snapshot.RegistryKeys.Where((p) => p.is_in_programs_and_features()).Count(), snapshot.RegistryKeys.Count));
            //Console.WriteLine("");

            return snapshot;
        }

        /// <summary>
        ///   Evaluates registry keys and updates the snapshop with items
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="snapshot">The snapshot.</param>
        public void evaluate_keys(RegistryKey key, Registry snapshot)
        {
            foreach (var subKeyName in key.GetSubKeyNames())
            {
                evaluate_keys(key.OpenSubKey(subKeyName), snapshot);
            }

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

                //if (appKey.InstallerType == InstallerType.Msi)
                //{
                //Console.WriteLine("");
                //if (!string.IsNullOrWhiteSpace(appKey.UninstallString))
                //{
                //    Console.WriteLine(appKey.UninstallString.to_string().Split(new[] { " /", " -" }, StringSplitOptions.RemoveEmptyEntries)[0]);
                //    key.UninstallString.to_string().Split(new[] { " /", " -" }, StringSplitOptions.RemoveEmptyEntries);
                //}
                //foreach (var name in key.GetValueNames())
                //{
                //    var kind = key.GetValueKind(name);
                //    var value = key.GetValue(name);
                //    Console.WriteLine("key - {0}, name - {1}, kind - {2}, value - {3}".format_with(key.Name, name, kind, value.to_string()));
                //}
                //}

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

        public Registry read_from_file(string filePath)
        {
            if (!_fileSystem.file_exists(filePath))
            {
                return null;
            }

            return _xmlService.deserialize<Registry>(filePath);
        }
    }
}