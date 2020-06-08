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
    ///   The installer registry as a snapshot
    /// </summary>
    [Serializable]
    [XmlType("registrySnapshot")]
    public class Registry
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="Registry" /> class.
        /// </summary>
        public Registry()
            : this(string.Empty, new HashSet<RegistryApplicationKey>())
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Registry" /> class.
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="keys">The keys.</param>
        public Registry(string user, IEnumerable<RegistryApplicationKey> keys)
        {
            User = user;
            if (keys != null)
            {
                RegistryKeys = keys.ToList();
            }
            else
            {
                RegistryKeys = new List<RegistryApplicationKey>();
            }
        }

        [XmlElement(ElementName = "user")]
        public string User { get; set; }

        /// <summary>
        ///   Gets the registry keys.
        /// </summary>
        /// <value>
        ///   The registry keys.
        /// </value>
        /// <remarks>
        ///   On .NET 4.0, get error CS0200 when private set - see http://stackoverflow.com/a/23809226/18475
        /// </remarks>
        [XmlArray("keys")]
        public List<RegistryApplicationKey> RegistryKeys { get; set; }
    }
}
