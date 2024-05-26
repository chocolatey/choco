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
using System.ComponentModel;
using System.Text;
using chocolatey.infrastructure.adapters;
using chocolatey.infrastructure.cryptography;

namespace chocolatey.infrastructure.app.nuget
{
    public static class NugetEncryptionUtility
    {
        private static Lazy<IEncryptionUtility> _encryptionUtility = new Lazy<IEncryptionUtility>(() => new DefaultEncryptionUtility());

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void InitializeWith(Lazy<IEncryptionUtility> encryptionUtility)
        {
            _encryptionUtility = encryptionUtility;
        }

        private static IEncryptionUtility EncryptionUtility
        {
            get { return _encryptionUtility.Value; }
        }

        public static string EncryptString(string cleartextValue)
        {
            return EncryptionUtility.EncryptString(cleartextValue);
        }

        public static string DecryptString(string encryptedString)
        {
            return EncryptionUtility.DecryptString(encryptedString);
        }

        public static string GenerateUniqueToken(string caseInsensitiveKey)
        {
            return EncryptionUtility.GenerateUniqueToken(caseInsensitiveKey);
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void initialize_with(Lazy<IEncryptionUtility> encryptionUtility)
            => InitializeWith(encryptionUtility);
#pragma warning restore IDE0022, IDE1006
    }
}
