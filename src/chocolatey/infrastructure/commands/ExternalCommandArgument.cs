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

namespace chocolatey.infrastructure.commands
{
    /// <summary>
    ///   Used with ExternalCommandArgsBuilder to build arguments to pass to an external command.
    /// </summary>
    public struct ExternalCommandArgument
    {
        /// <summary>
        ///   The argument option, include everything on the left side of equal (including equal or space) e.g. "-debug", "-source=", "-name "
        /// </summary>
        public string ArgumentOption;

        /// <summary>
        ///   The argument value - Leave this empty to be set by argsbuilder
        ///   If ArgumentValue is set, it will not be set again based on matching properties, the value indicated by ArgumentValue will just be used
        /// </summary>
        public string ArgumentValue;

        /// <summary>
        ///   Use the value only, not the argument option
        /// </summary>
        public bool UseValueOnly;

        /// <summary>
        ///   Quote the value, even if not already quoted. If argument value contains spaces, it will be quoted automatically
        /// </summary>
        public bool QuoteValue;

        /// <summary>
        ///   This argument is required, so include it unconditionally whether a matching property is found or not.
        /// </summary>
        public bool Required;
    }
}