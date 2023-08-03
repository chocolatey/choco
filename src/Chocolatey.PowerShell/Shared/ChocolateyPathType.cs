// Copyright © 2017 - 2025 Chocolatey Software, Inc
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
using System.Text;

namespace Chocolatey.PowerShell.Shared
{
    /// <summary>
    /// The type of path to be retrieved by the <see cref="GetChocolateyPathCmdlet"/>
    /// </summary>
    public enum ChocolateyPathType
    {
        /// <summary>
        /// Retrieves the path to the package folder; <c>$env:ChocolateyInstall\lib\$env:ChocolateyPackageName</c>
        /// </summary>
        PackagePath,
        /// <summary>
        /// Retrieves the path to the Chocolatey install folder; <c>$env:ChocolateyInstall</c> or an appropriate fallback.
        /// </summary>
        InstallPath,
    }
}
