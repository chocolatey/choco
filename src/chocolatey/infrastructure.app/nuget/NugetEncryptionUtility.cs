// Copyright © 2017 - 2018 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.app.nuget
{
    using System;
    using System.ComponentModel;
    using System.Text;
    using adapters;
    using cryptography;

    // ReSharper disable InconsistentNaming

    public static class NugetEncryptionUtility
    {
        private static Lazy<IEncryptionUtility> _encryptionUtility = new Lazy<IEncryptionUtility>(() => new DefaultEncryptionUtility());

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void initialize_with(Lazy<IEncryptionUtility> encryptionUtility)
        {
            _encryptionUtility = encryptionUtility;
        }

        private static IEncryptionUtility EncryptionUtility
        {
            get { return _encryptionUtility.Value; }
        }

        public static string EncryptString(string cleartextValue)
        {
            return EncryptionUtility.encrypt_string(cleartextValue);
        }

        public static string DecryptString(string encryptedString)
        {
            return EncryptionUtility.decrypt_string(encryptedString);
        }

        public static string GenerateUniqueToken(string caseInsensitiveKey)
        {
            return EncryptionUtility.generate_unique_token(caseInsensitiveKey);
        }
    }

    // ReSharper restore InconsistentNaming
}
