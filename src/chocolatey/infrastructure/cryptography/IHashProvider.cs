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

namespace chocolatey.infrastructure.cryptography
{
    /// <summary>
    /// A hash provider for hashing files
    /// </summary>
    public interface IHashProvider
    {
        /// <summary>
        /// Returns a hash of the specified file path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>A computed hash of the file, based on the contents.</returns>
        string hash_file(string filePath);
    }
}