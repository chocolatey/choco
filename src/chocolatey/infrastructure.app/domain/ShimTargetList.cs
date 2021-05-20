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

    public class ShimTargetList
    {
        public IDictionary<string, List<string>> Items;

        /// <summary>
        /// Creates a ShimTargetList instance.
        /// </summary>
        public ShimTargetList()
        {
            Items = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Adds a path and file pattern to the items collection.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="filePattern">The file pattern.</param>
        public void add_directive(string path, string filePattern)
        {
            List<string> filePatterns;

            // important - always lowercase the file pattern
            filePattern = filePattern.ToLower();

            if (Items.TryGetValue(path, out filePatterns))
            {
                if (!filePatterns.Contains(filePattern))
                {
                    filePatterns.Add(filePattern);
                }
            }
            else
            {
                filePatterns = new List<string> { filePattern };
                Items.Add(path, filePatterns);
            }
        }
    }
}
