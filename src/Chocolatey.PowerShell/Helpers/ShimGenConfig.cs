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

namespace Chocolatey.PowerShell.Helpers
{
    /// <summary>
    /// The install folder, shimgen location, debug setting, and shimgen runner that <see cref="ShimHelper"/>
    /// works against.
    /// </summary>
    internal sealed class ShimGenConfig
    {
        /// <summary>
        /// The Chocolatey install directory. Shims should be installed to a <c>bin</c> folder in this location.
        /// </summary>
        public string InstallRoot { get; set; }

        /// <summary>
        /// The path to shimgen.exe.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Whether debug output should be requested from any external application.
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Runs shimgen with the given path and arguments, returning its exit code and output lines.
        /// </summary>
        public Func<string, string, (int ExitCode, IList<string> Output)> RunAction { get; set; }
    }
}
