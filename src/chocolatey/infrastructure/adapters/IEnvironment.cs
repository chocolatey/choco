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
    using System;

    // ReSharper disable InconsistentNaming

    public interface IEnvironment
    {
        /// <summary>
        ///   Gets an <see cref="T:System.OperatingSystem" /> object that contains the current platform identifier and version number.
        /// </summary>
        /// <returns>
        ///   An <see cref="T:System.OperatingSystem" /> object.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">
        ///   This property was unable to obtain the system version.
        ///   -or-
        ///   The obtained platform identifier is not a member of <see cref="T:System.PlatformID" />.
        /// </exception>
        /// <filterpriority>1</filterpriority>
        OperatingSystem OSVersion { get; }
    }

    // ReSharper restore InconsistentNaming
}