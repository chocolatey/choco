// Copyright © 2017 - 2026 Chocolatey Software, Inc
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
using System.Linq;
using System.Management.Automation;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    /// <summary>
    /// Uninstall keys to search instead of the registry. Pass <see cref="GetKeyPaths"/> and
    /// <see cref="GetProperties"/> to <see cref="Chocolatey.PowerShell.Helpers.RegistryHelper"/>.
    /// </summary>
    public class FakeUninstallRegistry
    {
        public const string Wow6432NodeLocation = @"HKLM:\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
        public const string MachineLocation = @"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        public const string UserLocation = @"HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall";

        public FakeUninstallRegistry()
        {
            Locations[Wow6432NodeLocation] = new List<(string Path, PSObject Key)>();
            Locations[MachineLocation] = new List<(string Path, PSObject Key)>();
            Locations[UserLocation] = new List<(string Path, PSObject Key)>();
        }

        /// <summary>
        /// The keys in each location, in order. A key that is <c>null</c> has no values. A location that is removed
        /// from here cannot be listed.
        /// </summary>
        public Dictionary<string, List<(string Path, PSObject Key)>> Locations { get; } = new Dictionary<string, List<(string Path, PSObject Key)>>();

        /// <summary>
        /// The keys that cannot be read, and the exception reading each one fails with.
        /// </summary>
        public Dictionary<string, Exception> BadKeys { get; } = new Dictionary<string, Exception>();

        /// <summary>
        /// Adds a key named <paramref name="name"/> to a location. The key has a <c>DisplayName</c> value unless
        /// <paramref name="displayName"/> is <c>null</c>, and a <c>PSChildName</c> to tell it apart in results.
        /// </summary>
        /// <returns>The path of the key.</returns>
        public string AddKey(string location, string name, string displayName)
        {
            var key = new PSObject();
            if (!(displayName is null))
            {
                key.Properties.Add(new PSNoteProperty("DisplayName", displayName));
            }

            key.Properties.Add(new PSNoteProperty("PSChildName", name));

            return Add(location, name, key);
        }

        /// <summary>
        /// Adds a key named <paramref name="name"/> with no values to a location.
        /// </summary>
        /// <returns>The path of the key.</returns>
        public string AddKeyWithoutValues(string location, string name)
        {
            return Add(location, name, null);
        }

        public IEnumerable<string> GetKeyPaths(string location)
        {
            if (!Locations.TryGetValue(location, out var keys))
            {
                throw new ItemNotFoundException($"Cannot find path '{location}' because it does not exist.");
            }

            return keys.Select(key => key.Path).ToList();
        }

        public PSObject GetProperties(string keyPath)
        {
            if (BadKeys.TryGetValue(keyPath, out var failure))
            {
                throw failure;
            }

            return Locations.Values.SelectMany(keys => keys).Single(key => key.Path == keyPath).Key;
        }

        private string Add(string location, string name, PSObject key)
        {
            var path = location + "\\" + name;
            Locations[location].Add((path, key));

            return path;
        }
    }
}
