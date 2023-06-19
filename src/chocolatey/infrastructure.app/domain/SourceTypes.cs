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
    using System;

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
        public const string Cygwin = "cygwin";

        /// <summary>
        /// The source is a normal type, ie a chocolatey/nuget source.
        /// </summary>
        public const string Normal = "normal";

        /// <summary>
        /// The source is of type Python and need to be handled by an
        /// alternative source runner.
        /// </summary>
        public const string Python = "python";

        /// <summary>
        /// The source is of type Ruby and need to be handled by an
        /// alternative source runner.
        /// </summary>
        public const string Ruby = "ruby";

        /// <summary>
        /// The source is a windows feature and is only provided as an
        /// alias for <see cref="WindowsFeatures" />
        /// </summary>
        public const string WindowsFeature = "windowsfeature";

        /// <summary>
        /// The source is a windows feature and need to be handled by an
        /// alternative source runner.
        /// </summary>
        public const string WindowsFeatures = "windowsfeatures";

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public const string CYGWIN = Cygwin;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public const string NORMAL = Normal;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public const string PYTHON = Python;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public const string RUBY = Ruby;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public const string WINDOWS_FEATURE = WindowsFeature;
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public const string WINDOWS_FEATURES = WindowsFeatures;
#pragma warning restore IDE1006
    }
}
