// Copyright © 2017 - 2019 Chocolatey Software, Inc
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
    using System.Security;
    using Microsoft.Win32;

    public static class RegistryValueExtensions
    {
        public static string get_value_as_string(this RegistryKey key, string name)
        {
            if (key == null) return string.Empty;

            // Since it is possible that registry keys contain characters that are not valid
            // in XML files, ensure that all content is escaped, prior to serialization
            // https://docs.microsoft.com/en-us/dotnet/api/system.security.securityelement.escape?view=netframework-4.0
            return SecurityElement.Escape(key.GetValue(name).to_string()).to_string()
                                  .Replace("&quot;", "\"")
                                  .Replace("&apos;", "'")
                                  .Replace("\0", string.Empty);
        }
    }
}
