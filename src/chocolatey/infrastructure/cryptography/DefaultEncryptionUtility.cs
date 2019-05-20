// Copyright © 2017 - 2019 Chocolatey Software, Inc
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
    using System.Security.Cryptography;
    using System.Text;
    using adapters;

    public class DefaultEncryptionUtility : IEncryptionUtility
    {
        private readonly byte[] _entropyBytes = Encoding.UTF8.GetBytes("Chocolatey");

        public string encrypt_string(string cleartextValue)
        {
            if (string.IsNullOrWhiteSpace(cleartextValue)) return null;

            var decryptedByteArray = Encoding.UTF8.GetBytes(cleartextValue);
            var encryptedByteArray = ProtectedData.Protect(decryptedByteArray, _entropyBytes, DataProtectionScope.LocalMachine);
            var encryptedString = Convert.ToBase64String(encryptedByteArray);

            return encryptedString;
        }

        public string decrypt_string(string encryptedString)
        {
            var encryptedByteArray = Convert.FromBase64String(encryptedString);
            var decryptedByteArray = ProtectedData.Unprotect(encryptedByteArray, _entropyBytes, DataProtectionScope.LocalMachine);

            return Encoding.UTF8.GetString(decryptedByteArray);
        }

        public string generate_unique_token(string caseInsensitiveKey)
        {
            // SHA256 is case sensitive; given that our key is case insensitive, we upper case it
            var pathBytes = Encoding.UTF8.GetBytes(caseInsensitiveKey.ToUpperInvariant());
            var hashProvider = new NuGet.CryptoHashProvider("SHA256");

            return Convert.ToBase64String(hashProvider.CalculateHash(pathBytes)).ToUpperInvariant();
        }
    }
}
