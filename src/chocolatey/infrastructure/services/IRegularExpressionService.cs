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
using System.Text.RegularExpressions;

namespace chocolatey.infrastructure.services
{
    /// <summary>
    ///   Regular expressions helper
    /// </summary>
    public interface IRegularExpressionService
    {
        /// <summary>
        ///   Replaces the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="matchEvaluator">The match evaluator.</param>
        /// <returns></returns>
        string Replace(string input, string pattern, MatchEvaluator matchEvaluator);

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        string replace(string input, string pattern, MatchEvaluator matchEvaluator);
#pragma warning restore IDE0022, IDE1006
    }
}
