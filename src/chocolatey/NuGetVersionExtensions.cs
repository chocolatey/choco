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
    using System;

    /// <summary>
    /// Helper methods for dealing with the the nuget version returned by
    /// the NuGet.Client libraries to ensure they can be easily used.
    /// </summary>
    public static class NuGetVersionExtensions
    {
#pragma warning disable RS0030 // Do not used banned APIs
        /// <summary>
        /// Wrapper method to prevent null reference exceptions, calls into <see cref="SemanticVersion.ToFullString" />.
        /// </summary>
        /// <param name="version">The NuGet version to transform to a string.</param>
        /// <returns>An empty string if <paramref name="version"/> is <c>null</c>; otherwise the result of its call to <c>ToFullString</c>.</returns>
        public static string ToFullStringChecked(this NuGetVersion version)
        {
            if (version is null)
            {
                return string.Empty;
            }

            return version.ToFullString();
        }
#pragma warning restore RS0030 // Do not used banned APIs


#pragma warning disable RS0030 // Do not used banned APIs
        /// <summary>
        /// Wrapper method to prevent null reference exceptions, calls into <see cref="SemanticVersion.ToNormalizedString" />.
        /// </summary>
        /// <param name="version">The NuGet version to transform to a string.</param>
        /// <returns>An empty string if <paramref name="version"/> is <c>null</c>; otherwise the result of its call to <c>ToFullString</c>.</returns>
        public static string ToNormalizedStringChecked(this NuGetVersion version)
        {
            if (version is null)
            {
                return string.Empty;
            }

            return version.ToNormalizedString();
        }
#pragma warning restore RS0030 // Do not used banned APIs

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static string to_full_string(this NuGetVersion version)
            => ToFullStringChecked(version);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static string to_normalized_string(this NuGetVersion version)
            => ToNormalizedStringChecked(version);
#pragma warning restore IDE1006
    }
}
