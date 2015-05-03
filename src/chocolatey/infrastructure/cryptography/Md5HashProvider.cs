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
    using System;
    using System.Security.Cryptography;
    using filesystem;

    public sealed class Md5HashProvider : IHashProvider
    {
        private readonly IFileSystem _fileSystem;
        private readonly MD5 _cryptoProvider;

        public Md5HashProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _cryptoProvider = MD5.Create();
        }

        public string hash_file(string filePath)
        {
            if (!_fileSystem.file_exists(filePath)) return string.Empty;

            var hash = _cryptoProvider.ComputeHash(_fileSystem.read_file_bytes(filePath));

            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }
    }
}