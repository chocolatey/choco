using System;
using System.Collections.Generic;
using System.Text;

namespace Chocolatey.PowerShell.Helpers
{
    /// <summary>
    /// Provides information on the current running architecture width of the process.
    /// </summary>
    internal static class ArchitectureWidth
    {
        /// <summary>
        /// Returns either 64 or 32, depending on whether the current process environment is 64-bit.
        /// </summary>
        /// <returns>The current architecture width as an integer.</returns>
        internal static int Get()
        {
            return Environment.Is64BitProcess ? 64 : 32;
        }

        /// <summary>
        /// Compares the current architecture to the expected value.
        /// </summary>
        /// <param name="compareTo">The architecture width to compare to.</param>
        /// <returns>True if the provided value matches the current architecture width, otherwise false.</returns>
        internal static bool Matches(int compareTo)
        {
            return Get() == compareTo;
        }
    }
}
