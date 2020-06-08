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
    using System.Collections.Generic;
    using domain;
    using Microsoft.Win32;
    using Registry = domain.Registry;

    public interface IRegistryService
    {
        Registry get_installer_keys();
        Registry get_installer_key_differences(Registry before, Registry after);
        IEnumerable<GenericRegistryValue> get_environment_values();
        IEnumerable<GenericRegistryValue> get_added_changed_environment_differences(IEnumerable<GenericRegistryValue> before, IEnumerable<GenericRegistryValue> after);
        IEnumerable<GenericRegistryValue> get_removed_environment_differences(IEnumerable<GenericRegistryValue> before, IEnumerable<GenericRegistryValue> after);
        void save_to_file(Registry snapshot, string filePath);
        Registry read_from_file(string filePath);
        bool installer_value_exists(string keyPath, string value);
        RegistryKey get_key(RegistryHive hive, string subKeyPath);
    }
}
