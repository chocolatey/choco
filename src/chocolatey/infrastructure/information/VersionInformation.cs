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

namespace chocolatey.infrastructure.information
{
    using System.Diagnostics;
    using adapters;

    public sealed class VersionInformation
    {
        public static string get_current_assembly_version(IAssembly assembly = null)
        {
            string version = null;
            if (assembly == null) assembly = Assembly.GetExecutingAssembly();
            string location = assembly != null ? assembly.Location : string.Empty;

            if (!string.IsNullOrEmpty(location))
            {
                version = FileVersionInfo.GetVersionInfo(location).FileVersion;
            }

            return version;
        } 
        
        public static string get_current_informational_version(IAssembly assembly = null)
        {
            string version = null;
            if (assembly == null) assembly = Assembly.GetExecutingAssembly();
            string location = assembly != null ? assembly.Location : string.Empty;

            if (!string.IsNullOrEmpty(location))
            {
                version = FileVersionInfo.GetVersionInfo(location).ProductVersion;
            }

            return version;
        }
    }
}