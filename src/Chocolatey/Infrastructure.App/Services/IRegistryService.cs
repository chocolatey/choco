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

namespace Chocolatey.Infrastructure.App.Services
{
    using System.Collections.Generic;
    using Domain;
    using Microsoft.Win32;
    using Registry = Domain.Registry;

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
    }
}
