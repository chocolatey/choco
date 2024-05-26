﻿// Copyright © 2017 - 2021 Chocolatey Software, Inc
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
using chocolatey.infrastructure.app.domain;
using Microsoft.Win32;
using Registry = chocolatey.infrastructure.app.domain.Registry;

namespace chocolatey.infrastructure.app.services
{
    public interface IRegistryService
    {
        Registry GetInstallerKeys();
        Registry GetInstallerKeysChanged(Registry before, Registry after);
        IEnumerable<GenericRegistryValue> GetEnvironmentValues();
        IEnumerable<GenericRegistryValue> GetNewAndModifiedEnvironmentValues(IEnumerable<GenericRegistryValue> before, IEnumerable<GenericRegistryValue> after);
        IEnumerable<GenericRegistryValue> GetRemovedEnvironmentValues(IEnumerable<GenericRegistryValue> before, IEnumerable<GenericRegistryValue> after);
        void SaveRegistrySnapshot(Registry snapshot, string filePath);
        Registry ReadRegistrySnapshot(string filePath);
        bool InstallerKeyExists(string keyPath);
        RegistryKey GetKey(RegistryHive hive, string subKeyPath);

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        Registry get_installer_keys();
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        Registry get_installer_key_differences(Registry before, Registry after);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        IEnumerable<GenericRegistryValue> get_environment_values();
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        IEnumerable<GenericRegistryValue> get_added_changed_environment_differences(IEnumerable<GenericRegistryValue> before, IEnumerable<GenericRegistryValue> after);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        IEnumerable<GenericRegistryValue> get_removed_environment_differences(IEnumerable<GenericRegistryValue> before, IEnumerable<GenericRegistryValue> after);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void save_to_file(Registry snapshot, string filePath);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        Registry read_from_file(string filePath);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        bool installer_value_exists(string keyPath, string value);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        RegistryKey get_key(RegistryHive hive, string subKeyPath);
#pragma warning restore IDE0022, IDE1006
    }
}
