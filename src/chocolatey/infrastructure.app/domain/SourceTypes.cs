// Copyright © 2017 - 2022 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.app.domain
{
    /// <summary>
    /// This class contains the default source types that are implemented in
    /// the Chocolatey CLI codebase. This is replacing the enumeration previously
    /// available through <see cref="SourceType" />.
    /// </summary>
    public static class SourceTypes
    {
        /// <summary>
        /// The source is of type Cygwin and need to be handled by an
        /// alternative source runner.
        /// </summary>
        public const string CYGWIN = "cygwin";

        /// <summary>
        /// The source is a normal type, ie a chocolatey/nuget source.
        /// </summary>
        public const string NORMAL = "normal";

        /// <summary>
        /// The source is of type Python and need to be handled by an
        /// alternative source runner.
        /// </summary>
        public const string PYTHON = "python";

        /// <summary>
        /// The source is of type Ruby and need to be handled by an
        /// alternative source runner.
        /// </summary>
        public const string RUBY = "ruby";

        /// <summary>
        /// The source is of type Web PI and need to be handled by an
        /// alternative source runner.
        /// </summary>
        public const string WEBPI = "webpi";

        /// <summary>
        /// The source is a windows feature and is only provided as an
        /// alias for <see cref="WINDOWS_FEATURES" />
        /// </summary>
        public const string WINDOWS_FEATURE = "windowsfeature";

        /// <summary>
        /// The source is a windows feature and need to be handled by an
        /// alternative source runner.
        /// </summary>
        public const string WINDOWS_FEATURES = "windowsfeatures";
    }
}
