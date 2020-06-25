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
    using System.IO;
    using System.Text.RegularExpressions;

    public class ShimTargetPathResolver
    {
        private readonly string _rootPath;

        /// <summary>
        /// Creates a ShimTargetPathResolver instance.
        /// </summary>
        /// <param name="rootPath">The directory on which relative paths are based.</param>
        public ShimTargetPathResolver(string rootPath)
        {
            _rootPath = rootPath;
        }

        /// <summary>
        /// Resolves a path.
        /// </summary>
        /// <param name="path">The path relative to the root path.</param>
        /// <returns>The resolved paths or an empty list.</returns>
        public List<string> resolve(string path)
        {
            var resolvedPaths = new List<string> { _rootPath };
            var subDirs = new Queue<string>(path.Split('\\'));

            return resolve_path(resolvedPaths, subDirs);
        }

        /// <summary>
        /// Recursively steps through the components of a directory path in order to resolve it.
        /// </summary>
        /// <param name="resolvedPaths">The list of already resolved directory paths.</param>
        /// <param name="subDirs">The remaining subdirectories to resolve.</param>
        /// <returns>The resolved directory names as absolute paths.</returns>
        private List<string> resolve_path(List<string> resolvedPaths, Queue<string> subDirs)
        {
            var result = new List<string>();

            // safety, shouldn't happen
            if (subDirs.Count == 0) return result;

            // remove first directory
            var folder = subDirs.Dequeue();
            var wildcard = Regex.IsMatch(folder, @"[?*]");

            foreach (var path in resolvedPaths)
            {
                var resolved = wildcard ? search(path, folder) : test(path, folder);

                if (subDirs.Count > 0 && resolved.Count > 0)
                {
                    resolved = resolve_path(resolved, subDirs);
                }
                result.AddRange(resolved);
            }

            return result;
        }

        /// <summary>
        /// Searches for directories in a specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="pattern">The search pattern.</param>
        /// <returns>The matched directory names as absolute paths.</returns>
        private List<string> search(string path, string pattern)
        {
            var result = new List<string>();
            IEnumerable<string> directories;

            try
            {
                directories = Directory.GetDirectories(path, pattern, SearchOption.TopDirectoryOnly);
            }
            catch
            {
                return result;
            }

            // no need for try-catch as error conditions will have already been caught
            foreach (var dir in directories)
            {
                var fullPath = Path.GetFullPath(dir);
                result.Add(fullPath);
            }

            return result;
        }

        /// <summary>
        /// Tests for a directory in a specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="folder">The directory name to test.</param>
        /// <returns>A list containing the absolute path of any match.</returns>
        private List<string> test(string path, string folder)
        {
            var result = new List<string>();
            string fullPath;

            try
            {
                fullPath = Path.GetFullPath(Path.Combine(path, folder));
            }
            catch
            {
                return result;
            }

            if (Directory.Exists(fullPath))
            {
                result.Add(fullPath);
            }

            return result;
        }
    }
}
