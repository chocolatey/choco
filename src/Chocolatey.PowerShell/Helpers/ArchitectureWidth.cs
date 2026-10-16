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
using SystemVariables = chocolatey.StringResources.EnvironmentVariables.System;

namespace Chocolatey.PowerShell.Helpers
{
    /// <summary>
    /// Provides information on the architecture width of the operating system that Chocolatey is running on.
    /// </summary>
    internal static class ArchitectureWidth
    {
        private const string Arm64 = "ARM64";

        /// <summary>
        /// Gets the architecture width for the current environment.
        /// </summary>
        /// <returns>The architecture width, either 64 or 32.</returns>
        internal static int Get()
        {
            return Get(
                IntPtr.Size,
                EnvironmentHelper.GetVariable(SystemVariables.ProcessorArchitecture),
                EnvironmentHelper.GetVariable(SystemVariables.ProcessorArchitectureW6432));
        }

        /// <summary>
        /// Works out the architecture width for the given process and environment details.
        /// </summary>
        /// <remarks>
        /// A 32-bit process on a 64-bit operating system is reported as 64, and ARM64 is reported as 32 as it
        /// runs 32-bit x86 programs through emulation and there is no 64-bit x86 emulation for it.
        /// </remarks>
        /// <param name="pointerSize">The size of a pointer in the current process, in bytes.</param>
        /// <param name="processorArchitecture">The value of the <c>PROCESSOR_ARCHITECTURE</c> variable, or <c>null</c> if it is not set.</param>
        /// <param name="processorArchitectureW6432">The value of the <c>PROCESSOR_ARCHITEW6432</c> variable, or <c>null</c> if it is not set.</param>
        /// <returns>The architecture width, either 64 or 32.</returns>
        internal static int Get(int pointerSize, string processorArchitecture, string processorArchitectureW6432)
        {
            var width = pointerSize == 4 && processorArchitectureW6432 is null ? 32 : 64;

            if (IsArm64(processorArchitecture) || IsArm64(processorArchitectureW6432))
            {
                width = 32;
            }

            return width;
        }

        /// <summary>
        /// Compares the current architecture width to the expected value.
        /// </summary>
        /// <param name="compareTo">The architecture width to compare to.</param>
        /// <returns>True if the provided value matches the current architecture width, otherwise false.</returns>
        internal static bool Matches(object compareTo)
        {
            return Matches(compareTo, Get());
        }

        /// <summary>
        /// Compares the given architecture width to the expected value, in the way that PowerShell's <c>-eq</c>
        /// operator compares its operands.
        /// </summary>
        /// <param name="compareTo">The architecture width to compare to.</param>
        /// <param name="width">The architecture width to compare against.</param>
        /// <returns>True if the provided value matches the architecture width, otherwise false.</returns>
        internal static bool Matches(object compareTo, int width)
        {
            return PSHelper.IsEqual(compareTo, width);
        }

        private static bool IsArm64(string value)
        {
            return string.Equals(value, Arm64, StringComparison.OrdinalIgnoreCase);
        }
    }
}