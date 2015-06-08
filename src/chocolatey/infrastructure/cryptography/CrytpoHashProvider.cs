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
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using adapters;
    using app;
    using filesystem;
    using platforms;
    using Environment = System.Environment;
    using HashAlgorithm = adapters.HashAlgorithm;

    public sealed class CrytpoHashProvider : IHashProvider
    {
        private readonly IFileSystem _fileSystem;
        private readonly IHashAlgorithm _hashAlgorithm;
        private const int ERROR_LOCK_VIOLATION = 33;
        private const int ERROR_SHARING_VIOLATION = 32;

        public CrytpoHashProvider(IFileSystem fileSystem, CryptoHashProviderType providerType)
        {
            _fileSystem = fileSystem;

            switch (providerType)
            {
                case CryptoHashProviderType.Md5:
                    _hashAlgorithm = new HashAlgorithm(MD5.Create());
                    break;
                case CryptoHashProviderType.Sha1:
                    _hashAlgorithm = new HashAlgorithm(SHA1.Create());
                    break;
                case CryptoHashProviderType.Sha256:
                    _hashAlgorithm = new HashAlgorithm(SHA256.Create());
                    break;
                case CryptoHashProviderType.Sha512:
                    _hashAlgorithm = new HashAlgorithm(SHA512.Create());
                    break;
            }
        }

        public CrytpoHashProvider(IFileSystem fileSystem, IHashAlgorithm hashAlgorithm)
        {
            _fileSystem = fileSystem;
            _hashAlgorithm = hashAlgorithm;
        }

        public string hash_file(string filePath)
        {
            if (!_fileSystem.file_exists(filePath)) return string.Empty;

            try
            {
                var hash = _hashAlgorithm.ComputeHash(_fileSystem.read_file_bytes(filePath));

                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
            catch (IOException ex)
            {
                this.Log().Warn(() => "Error computing hash for '{0}'{1} Hash will be special code for locked file or file too big instead.{1} Captured error:{1}  {2}".format_with(filePath, Environment.NewLine, ex.Message));

                if (file_is_locked(ex))
                {
                    return ApplicationParameters.HashProviderFileLocked;
                }

                //IO.IO_FileTooLong2GB (over Int32.MaxValue)
                return ApplicationParameters.HashProviderFileTooBig;
            }
        }

        private static bool file_is_locked(Exception exception)
        {
            var errorCode = 0;

            var hresult = Marshal.GetHRForException(exception);

            errorCode = hresult & ((1 << 16) - 1);

            return errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION;
        }

    }
}