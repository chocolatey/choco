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

namespace chocolatey.infrastructure.app.domain
{
    using System;

    public static class InstallTokens
    {
        public const string InstallerLocation = "{{INSTALLER_LOCATION}}";
        public const string CustomInstallLocation = "{{CUSTOM_INSTALL_LOCATION}}";
        public const string PackageLocation = "{{PACKAGE_LOCATION}}";
        public const string Language = "{{LANGUAGE}}";
        public const string UninstallerLocation = "{{UNINSTALLER_LOCATION}}";
        public const string TempLocation = "{{TEMP_LOCATION}}";

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public const string INSTALLER_LOCATION = InstallerLocation;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public const string CUSTOM_INSTALL_LOCATION = CustomInstallLocation;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public const string PACKAGE_LOCATION = PackageLocation;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public const string LANGUAGE = Language;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public const string UNINSTALLER_LOCATION = UninstallerLocation;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public const string TEMP_LOCATION = TempLocation;
#pragma warning restore IDE1006
    }
}
