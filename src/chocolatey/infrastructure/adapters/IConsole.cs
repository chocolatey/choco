// Copyright © 2011 - Present RealDimensions Software, LLC
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

namespace chocolatey.infrastructure.adapters
{
    using System.IO;

    // ReSharper disable InconsistentNaming

    public interface IConsole
    {
        /// <summary>
        ///   Reads the next line of characters from the standard input stream.
        /// </summary>
        /// <returns>
        ///   The next line of characters from the input stream, or null if no more lines are available.
        /// </returns>
        /// <exception cref="T:System.IO.IOException">
        ///   An I/O error occurred.
        /// </exception>
        /// <exception cref="T:System.OutOfMemoryException">
        ///   There is insufficient memory to allocate a buffer for the returned string.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///   The number of characters in the next line of characters is greater than <see cref="F:System.Int32.MaxValue" />.
        /// </exception>
        /// <filterpriority>1</filterpriority>
        string ReadLine();

        /// <summary>
        ///   Gets the standard error output stream.
        /// </summary>
        /// <returns>
        ///   A <see cref="T:System.IO.TextWriter" /> that represents the standard error output stream.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        TextWriter Error { get; }
    }

    // ReSharper restore InconsistentNaming
}