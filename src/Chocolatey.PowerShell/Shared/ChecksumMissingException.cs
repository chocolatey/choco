// Copyright © 2017 - 2024 Chocolatey Software, Inc
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
using System.Runtime.Serialization;

namespace Chocolatey.PowerShell.Shared
{
    [Serializable]
    public class ChecksumMissingException : Exception
    {
        public ChecksumMissingException()
        {
        }

        public ChecksumMissingException(string message) : base(message)
        {
        }

        public ChecksumMissingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ChecksumMissingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}