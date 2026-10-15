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
using chocolatey;
using System.ComponentModel;

namespace Chocolatey.PowerShell.Shared
{

    [Obsolete("This class has been deprecated, make use of chocolatey.StringResources.EnvironmentVariables instead.", error: false)]
    public static class EnvironmentVariables
    {
        public const string ChocolateyLastPathUpdate = StringResources.EnvironmentVariables.Package.ChocolateyLastPathUpdate;
        public const string ComputerName = StringResources.EnvironmentVariables.System.ComputerName;
        public const string Path = StringResources.EnvironmentVariables.System.Path;
        public const string ProcessorArchitecture = StringResources.EnvironmentVariables.System.ProcessorArchitecture;
        public const string PSModulePath = StringResources.EnvironmentVariables.System.PSModulePath;
        [Obsolete("This constant has been replaced by EnvironmentNames.System.")]
        public const string System = EnvironmentNames.System;
        public const string SystemRoot = StringResources.EnvironmentVariables.System.SystemRoot;
        public const string Username = StringResources.EnvironmentVariables.System.Username;
    }
}
