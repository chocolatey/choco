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

using System;

namespace chocolatey.infrastructure.cryptography
{
    public static class HashConverter
    {
        public static (string ConvertedHash, CryptoHashProviderType HashType) ConvertHashToHex(string hash)
        {
            // Sha 1 in base64
            if (hash.Length.Equals(28))
            {
                return (BitConverter.ToString(Convert.FromBase64String(hash)).Replace("-", string.Empty), CryptoHashProviderType.Sha1);
            }

            // Sha 1 in hex
            if (hash.Length.Equals(40))
            {
                return (hash, CryptoHashProviderType.Sha1);
            }

            // Sha 256 in base64
            if (hash.Length.Equals(44))
            {
                return (BitConverter.ToString(Convert.FromBase64String(hash)).Replace("-", string.Empty), CryptoHashProviderType.Sha256);
            }

            // Sha 256 in hex
            if (hash.Length.Equals(64))
            {
                return (hash, CryptoHashProviderType.Sha256);
            }

            // Sha 512 in base64
            if (hash.Length.Equals(88))
            {
                return (BitConverter.ToString(Convert.FromBase64String(hash)).Replace("-", string.Empty), CryptoHashProviderType.Sha512);
            }

            // Sha 512 in hex
            if (hash.Length.Equals(128))
            {
                return (hash, CryptoHashProviderType.Sha512);
            }

            "chocolatey".Log().Warn("Unknown Hash type, Length '{0}', Hash '{1}'".FormatWith(hash, hash.Length));
            return (hash, CryptoHashProviderType.Unknown);
        }
    }
}
