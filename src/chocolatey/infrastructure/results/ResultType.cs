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

namespace chocolatey.infrastructure.results
{
    /// <summary>
    ///   When working with results, this identifies the type of result
    /// </summary>
    public enum ResultType
    {
        /// <summary>
        ///   The default result type.
        /// </summary>
        None,

        /// <summary>
        ///   Debugging messages that may help the recipient determine items leading up to errors
        /// </summary>
        Debug,

        /// <summary>
        ///   Verbose messages that may help the recipient determine items leading up to errors
        /// </summary>
        Verbose,

        /// <summary>
        ///   These are notes to pass along with the result
        /// </summary>
        Note,

        /// <summary>
        ///   There was no result.
        /// </summary>
        Inconclusive,

        /// <summary>
        ///   These are warnings
        /// </summary>
        Warn,

        /// <summary>
        ///   These are errors
        /// </summary>
        Error,
    }
}
