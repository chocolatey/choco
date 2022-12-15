// Copyright © 2022-Present Chocolatey Software, Inc
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

namespace chocolatey
{
    using NuGet.Versioning;

    /// <summary>
    /// Helper methods for dealing with the the nuget version returned by
    /// the NuGet.Client libraries to ensure they can be easily used.
    /// </summary>
    /// <remarks>The class is marked as internal on purpose to ensure it will not be part of the public API</remarks>
    internal static class NuGetVersionExtensions
    {
        /// <summary>
        /// Wrapper object to prevent null reference exceptions happening.
        /// Will return an empty string if the passed in <paramref name="version"/> is <c>null</c>.
        /// Otherwise it will return the result of its call to <c>ToFullString()</c>.
        /// </summary>
        /// <param name="version">The NuGet version to transform to a string.</param>
        /// <returns>An empty string if <paramref name="version"/> is <c>null</c>; otherwise the result of its call to <c>ToFullString</c>.</returns>
        public static string to_full_string(this NuGetVersion version)
        {
            if (version is null)
            {
                return string.Empty;
            }

#pragma warning disable RS0030 // Do not used banned APIs
            return version.ToFullString();
#pragma warning restore RS0030 // Do not used banned APIs
        }

        public static string to_normalized_string(this NuGetVersion version)
        {
            if (version is null)
            {
                return string.Empty;
            }

#pragma warning disable RS0030 // Do not used banned APIs
            return version.ToNormalizedString();
#pragma warning restore RS0030 // Do not used banned APIs
        }
    }
}
