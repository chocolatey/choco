﻿// Copyright © 2017 - 2021 Chocolatey Software, Inc
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
using System.Security.Cryptography;
using System.Text;
using chocolatey.infrastructure.adapters;
using chocolatey.infrastructure.platforms;

namespace chocolatey.infrastructure.cryptography
{
    public class DefaultEncryptionUtility : IEncryptionUtility
    {
        private readonly byte[] _entropyBytes = Encoding.UTF8.GetBytes("Chocolatey");

        public string EncryptString(string cleartextValue)
        {
            if (string.IsNullOrWhiteSpace(cleartextValue))
            {
                return null;
            }

            var decryptedByteArray = Encoding.UTF8.GetBytes(cleartextValue);
            byte[] encryptedByteArray;
            try
            {
                encryptedByteArray = ProtectedData.Protect(decryptedByteArray, _entropyBytes, DataProtectionScope.LocalMachine);
            }
            catch (Exception ex)
            {
                if (Platform.GetPlatform() != PlatformType.Windows && ex is CryptographicException)
                {
                    this.Log().Warn(@"Could not encrypt with LocalMachine scope.
Falling back to CurrentUser scope for encryption.
This is can be because the machine keyfile cannot be written as a normal user.
Anything encrypted as CurrentUser can only be decrypted by your current user.");
                    encryptedByteArray = ProtectedData.Protect(decryptedByteArray, _entropyBytes, DataProtectionScope.CurrentUser);
                }
                else
                {
                    throw;
                }
            }
            var encryptedString = Convert.ToBase64String(encryptedByteArray);

            return encryptedString;
        }

        public string DecryptString(string encryptedString)
        {
            var encryptedByteArray = Convert.FromBase64String(encryptedString);
            byte[] decryptedByteArray;

            try
            {
                decryptedByteArray = ProtectedData.Unprotect(encryptedByteArray, _entropyBytes, DataProtectionScope.LocalMachine);
            }
            catch (Exception ex)
            {
                if (Platform.GetPlatform() != PlatformType.Windows && ex is CryptographicException)
                {
                    this.Log().Warn(@"Could not decrypt with LocalMachine scope.
Falling back to CurrentUser scope for decryption.
Anything encrypted as CurrentUser can only be decrypted by your current user.");
                    decryptedByteArray = ProtectedData.Unprotect(encryptedByteArray, _entropyBytes, DataProtectionScope.CurrentUser);
                }
                else
                {
                    throw;
                }
            }

            return Encoding.UTF8.GetString(decryptedByteArray);
        }

        public string GenerateUniqueToken(string caseInsensitiveKey)
        {
            // SHA256 is case sensitive; given that our key is case insensitive, we upper case it
            var pathBytes = Encoding.UTF8.GetBytes(caseInsensitiveKey.ToUpperInvariant());
            var hashProvider = new global::NuGet.Common.CryptoHashProvider("SHA256");

            return Convert.ToBase64String(hashProvider.CalculateHash(pathBytes)).ToUpperInvariant();
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public string encrypt_string(string cleartextValue)
            => EncryptString(cleartextValue);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public string decrypt_string(string encryptedString)
            => DecryptString(encryptedString);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public string generate_unique_token(string caseInsensitiveKey)
            => GenerateUniqueToken(caseInsensitiveKey);
#pragma warning restore IDE0022, IDE1006
    }
}
