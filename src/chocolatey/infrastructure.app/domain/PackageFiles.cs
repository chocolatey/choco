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

namespace chocolatey.infrastructure.app.domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    /// <summary>
    ///   The package files snapshot
    /// </summary>
    [Serializable]
    [XmlType("fileSnapshot")]
    public sealed class PackageFiles
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="PackageFiles" /> class.
        /// </summary>
        public PackageFiles()
            : this(new HashSet<PackageFile>())
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="PackageFiles" /> class.
        /// </summary>
        /// <param name="files">The package files.</param>
        public PackageFiles(IEnumerable<PackageFile> files)
        {
            Files = files != null ? files.ToList() : new List<PackageFile>();
        }

        /// <summary>
        ///   Gets or sets the files.
        /// </summary>
        /// <value>
        ///   The files.
        /// </value>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        [XmlArray("files")]
        public List<PackageFile> Files { get; set; }
    }
}
