﻿// Copyright © 2017 - 2025 Chocolatey Software, Inc
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

using System.Collections.Generic;

namespace chocolatey.infrastructure.results
{
    /// <summary>
    ///   Outcome from some work performed.
    /// </summary>
    public interface IResult
    {
        /// <summary>
        ///   Gets a value indicating whether this <see cref="IResult" /> is successful.
        /// </summary>
        /// <value>
        ///   <c>true</c> if success; otherwise, <c>false</c>.
        /// </value>
        bool Success { get; }

        /// <summary>
        ///   The messages
        /// </summary>
        ICollection<ResultMessage> Messages { get; }
    }
}
