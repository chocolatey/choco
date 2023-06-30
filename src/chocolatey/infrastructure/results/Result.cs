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

namespace chocolatey.infrastructure.results
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///   Outcome from some work performed
    /// </summary>
    public class Result : IResult
    {
        // Should this just be private, instead of a protected field?
        protected readonly Lazy<List<ResultMessage>> LazyMessages;

        public Result()
        {
            LazyMessages = new Lazy<List<ResultMessage>>();
#pragma warning disable CS0618 // Type or member is obsolete
            _messages = LazyMessages;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public bool Success
        {
            get { return !LazyMessages.Value.Any(x => x.MessageType == ResultType.Error); }
        }

        public ICollection<ResultMessage> Messages
        {
            get { return LazyMessages.Value; }
        }

#pragma warning disable IDE1006 // Naming Styles
        [Obsolete("This field is deprecated and will be removed in v3. Use LazyMessages or Messages instead.")]
        protected readonly Lazy<List<ResultMessage>> _messages;
#pragma warning restore IDE1006 // Naming Styles
    }
}
