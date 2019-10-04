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

namespace chocolatey.infrastructure.licensing
{
    using adapters;
    using DateTime = System.DateTime;

    public sealed class ChocolateyLicense
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ChocolateyLicenseType LicenseType { get; set; }
        public bool IsValid { get; set; }
        public bool AssemblyLoaded { get; set; }
        public IAssembly Assembly { get; set; }
        //todo: get version
        public string Version { get; set; }
        public string InvalidReason { get; set; }
        public DateTime? ExpirationDate { get; set; }

        public bool is_licensed_version()
        {
            return IsValid
                   && LicenseType != ChocolateyLicenseType.Unknown
                   && LicenseType != ChocolateyLicenseType.Foss
                ;
        }
    }
}
