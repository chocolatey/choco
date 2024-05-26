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

using chocolatey.infrastructure.adapters;
using System;
using DateTime = System.DateTime;

namespace chocolatey.infrastructure.licensing
{
    public sealed class ChocolateyLicense
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ChocolateyLicenseType LicenseType { get; set; }
        public bool IsValid { get; set; }
        public bool AssemblyLoaded { get; set; }
        public IAssembly Assembly { get; set; }
        //todo: #2566 get version
        public string Version { get; set; }
        public string InvalidReason { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public bool IsCompatible { get; set; }

        public bool IsLicensedVersion()
        {
            return IsValid
                   && LicenseType != ChocolateyLicenseType.Unknown
                   && LicenseType != ChocolateyLicenseType.Foss
                ;
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public bool is_licensed_version()
            => IsLicensedVersion();
#pragma warning restore IDE0022, IDE1006
    }
}
