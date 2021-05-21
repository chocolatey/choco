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

namespace chocolatey.infrastructure.app.domain
{
    public class ShimRecord
    {
        /// <summary>
        /// The exe file from the shim directory.
        /// </summary>
        public string ExeFile { get; set; }

        /// <summary>
        /// The package name (could be empty).
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// The file being shimmed (could be empty).
        /// </summary>
        public string TargetFile { get; set; }

        /// <summary>
        /// Creates a ShimRecord instance.
        /// </summary>
        /// <param name="exeFile">The exe file from the shim directory.</param>
        public ShimRecord(string exeFile)
        {
            ExeFile = exeFile;
            PackageName = string.Empty;
            TargetFile = string.Empty;
        }

        /// <summary>
        /// Creates a ShimRecord instance.
        /// </summary>
        /// <param name="exeFile">The exe file from the shim directory.</param>
        /// <param name="packageName">The package name.</param>
        /// <param name="targetFile">The file being shimmed.</param>
        public ShimRecord(string exeFile, string packageName, string targetFile)
        {
            ExeFile = exeFile;
            PackageName = packageName;
            TargetFile = targetFile;
        }
    }
}
