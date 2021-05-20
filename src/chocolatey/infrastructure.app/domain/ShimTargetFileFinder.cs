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
    using System.IO;

    public class ShimTargetFileFinder
    {
        /// <summary>
        /// Returns the last three characters of the file name.
        /// </summary>
        /// <param name="fileName">The filename</param>
        /// <returns>The extension</returns>
        /// <remarks>
        /// Only specific three-character extensions are used.
        /// </remarks>
        public string get_extension(string fileName)
        {
            return fileName.Substring(fileName.Length - 3).ToLower();
        }

        /// <summary>
        /// Searches the package for all exe files.
        /// </summary>
        /// <param name="path">The package folder location.</param>
        /// <returns>The exe files to shim.</returns>
        public IEnumerable<string> get_targets(string path)
        {
            var result = find(path, "*.exe", SearchOption.AllDirectories);

            return remove_ignored_files(result);
        }

        /// <summary>
        /// Resolves executable file patterns from an include and exclude list.
        /// </summary>
        /// <param name="includes">The include list.</param>
        /// <param name="excludes">The exclude list.</param>
        /// <returns>The executable files to shim.</returns>
        public IEnumerable<string> get_targets(ShimTargetList includes, ShimTargetList excludes)
        {
            var result = new List<string>();

            foreach (var path in includes.Items.Keys)
            {
                var includedFiles = get_files(path, includes.Items[path]);
                var excludedFiles = get_files(excludes, path);

                if (excludedFiles.Count == 0)
                {
                    result.AddRange(includedFiles);
                    continue;
                }

                foreach (var file in includedFiles)
                {
                    if (!excludedFiles.Contains(file))
                    {
                        result.Add(file);
                    }
                }
            }

            return remove_ignored_files(result);
        }

        /// <summary>
        /// Searches for files in a specified path, optionally searching subdirectories.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="pattern">The search pattern.</param>
        /// <param name="searchOption">TopDirectoryOnly or AllDirectories.</param>
        /// <returns>The matched file names.</returns>
        private IEnumerable<string> find(string path, string pattern, SearchOption searchOption)
        {
            var extension = get_extension(pattern);

            return Directory.EnumerateFiles(path, pattern, searchOption)
                .Where(f => f.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Searches for files from a list of search patterns.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="patterns">List of search patterns.</param>
        /// <returns>The matched file names.</returns>
        private List<string> get_files(string path, IEnumerable<string> patterns)
        {
            var result = new List<string>();

            foreach (var pattern in patterns)
            {
                var files = find(path, pattern, SearchOption.TopDirectoryOnly);
                result.AddRange(files);
            }

            return result;
        }

        /// <summary>
        /// Searches for files if a list contains a specific path.
        /// </summary>
        /// <param name="list">The list </param>
        /// <param name="path">The path.</param>
        /// <returns>The matched file names or an empty List.</returns>
        private List<string> get_files(ShimTargetList list, string path)
        {
            if (list.Items.ContainsKey(path))
            {
                return get_files(path, list.Items[path]);
            }

            return new List<string>();
        }

        /// <summary>
        /// Filters out files that should not be shimmed.
        /// </summary>
        /// <param name="targetFiles">The candidate files to shim.</param>
        /// <returns>The intended files to shim</returns>
        private List<string> remove_ignored_files(IEnumerable<string> targetFiles)
        {
            var result = new List<string>();

            foreach (var file in targetFiles)
            {
                // ignore the file if there is a matching file suffixed '.ignore'
                if (!File.Exists(file + ".ignore")) result.Add(file);
            }

            return result;
        }
    }
}
