// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.cryptography
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Text;
    using adapters;
    using app;
    using filesystem;
    using Environment = System.Environment;
    using HashAlgorithm = adapters.HashAlgorithm;

    public class CryptoHashProvider : IHashProvider
    {
        private readonly IFileSystem _fileSystem;
        private IHashAlgorithm _hashAlgorithm;
        private const int ErrorLockViolation = 33;
        private const int ErrorSharingViolation = 32;

        public void SetHashAlgorithm(CryptoHashProviderType algorithmType)
        {
            _hashAlgorithm = GetHashAlgorithmStatic(algorithmType);
        }

        private static IHashAlgorithm GetHashAlgorithmStatic(CryptoHashProviderType algorithmType)
        {

            var fipsOnly = false;
            try
            {
                fipsOnly = CryptoConfig.AllowOnlyFipsAlgorithms;
            }
            catch (Exception ex)
            {
                "chocolatey".Log().Debug("Unable to get FipsPolicy from CryptoConfig:{0} {1}".FormatWith(Environment.NewLine, ex.Message));
            }

            HashAlgorithm hashAlgorithm = null;
            switch (algorithmType)
            {
                case CryptoHashProviderType.Md5:
                    hashAlgorithm = new HashAlgorithm(MD5.Create());
                    break;
                case CryptoHashProviderType.Sha1:
                    hashAlgorithm = new HashAlgorithm(fipsOnly ? new SHA1Cng() : SHA1.Create());
                    break;
                case CryptoHashProviderType.Sha256:
                    hashAlgorithm = new HashAlgorithm(fipsOnly ? new SHA256Cng() : SHA256.Create());
                    break;
                case CryptoHashProviderType.Sha512:
                    hashAlgorithm = new HashAlgorithm(fipsOnly ? new SHA512Cng() : SHA512.Create());
                    break;
            }

            return hashAlgorithm;
        }

        public CryptoHashProvider(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            SetHashAlgorithm(CryptoHashProviderType.Sha256);
        }

        public CryptoHashProvider(IFileSystem fileSystem, IHashAlgorithm hashAlgorithm)
        {
            _fileSystem = fileSystem;
            _hashAlgorithm = hashAlgorithm;
        }

        public string ComputeFileHash(string filePath)
        {
            if (!_fileSystem.FileExists(filePath)) return string.Empty;

            try
            {
                var hash = _hashAlgorithm.ComputeHash(_fileSystem.ReadFileBytes(filePath));

                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
            catch (IOException ex)
            {
                this.Log().Warn(() => "Error computing hash for '{0}'{1} Hash will be special code for locked file or file too big instead.{1} Captured error:{1}  {2}".FormatWith(filePath, Environment.NewLine, ex.Message));

                if (IsFileLocked(ex))
                {
                    return ApplicationParameters.HashProviderFileLocked;
                }

                //IO.IO_FileTooLong2GB (over Int32.MaxValue)
                return ApplicationParameters.HashProviderFileTooBig;
            }
        }

        public string ComputeByteArrayHash(byte[] buffer)
        {
            var hash = _hashAlgorithm.ComputeHash(buffer);

            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        public string ComputeStreamHash(Stream inputStream)
        {
            var hash = _hashAlgorithm.ComputeHash(inputStream);

            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        private static bool IsFileLocked(Exception exception)
        {
            var errorCode = 0;

            var hresult = Marshal.GetHRForException(exception);

            errorCode = hresult & ((1 << 16) - 1);

            return errorCode == ErrorSharingViolation || errorCode == ErrorLockViolation;
        }

        public static string ComputeStringHash(string originalText, CryptoHashProviderType providerType)
        {
            IHashAlgorithm hashAlgorithm = GetHashAlgorithmStatic(providerType);
            if (hashAlgorithm == null) return string.Empty;

             var hash = hashAlgorithm.ComputeHash(Encoding.ASCII.GetBytes(originalText));
             return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void set_hash_algorithm(CryptoHashProviderType algorithmType)
            => SetHashAlgorithm(algorithmType);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        private static IHashAlgorithm get_hash_algorithm_static(CryptoHashProviderType algorithmType)
            => GetHashAlgorithmStatic(algorithmType);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public string hash_file(string filePath)
            => ComputeFileHash(filePath);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public string hash_byte_array(byte[] buffer)
            => ComputeByteArrayHash(buffer);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public string hash_stream(Stream inputStream)
            => ComputeStreamHash(inputStream);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        private static bool file_is_locked(Exception exception)
            => IsFileLocked(exception);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static string hash_value(string originalText, CryptoHashProviderType providerType)
            => ComputeStringHash(originalText, providerType);
#pragma warning restore IDE1006
    }
}
