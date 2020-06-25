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

    public class ShimTargetManager
    {
        private readonly ShimTargetFileFinder _finder;
        private readonly ShimTargetPathResolver _resolver;
        private readonly string _packageDir;
        private readonly string _includeFile;
        private ShimTargetList _includes;
        private ShimTargetList _excludes;

        /// <summary>
        /// Creates a ShimTargetManager instance.
        /// </summary>
        /// <param name="packageDir">The package directory.</param>
        public ShimTargetManager(string packageDir)
        {
            var toolsDir = Path.Combine(packageDir, "tools");
            _packageDir = packageDir;
            _includeFile = Path.Combine(toolsDir, ".shiminclude");
            _finder = new ShimTargetFileFinder();
            _resolver = new ShimTargetPathResolver(toolsDir);
        }

        /// <summary>
        /// Searches for executables to shim.
        /// </summary>
        /// <returns>Executables to shim or an empty list.</returns>
        public IEnumerable<string> get_shim_targets()
        {
            if (!File.Exists(_includeFile))
            {
                return _finder.get_targets(_packageDir);
            }

            _includes = new ShimTargetList();
            _excludes = new ShimTargetList();

            var directives = File.ReadAllLines(_includeFile, System.Text.Encoding.UTF8);
            parse_include_file(directives);

            return _finder.get_targets(_includes, _excludes);
        }

        /// <summary>
        /// Evaluates the .shiminclude file directives.
        /// </summary>
        /// <param name="directives">The list of directives.</param>
        /// <remarks>Populates _includes and _excludes lists.</remarks>
        private void parse_include_file(string[] directives)
        {
            var includedEntries = new ShimTargetList();
            var excludedEntries = new ShimTargetList();

            foreach (string lineEntry in directives)
            {
                var line = lineEntry.Trim();

                // skip blank or comment lines
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                {
                    continue;
                }

                var entryList = includedEntries;

                if (line.StartsWith("\\#") || line.StartsWith("\\!"))
                {
                    // backslash escapes
                    line = line.Substring(1);
                }
                else if (line.StartsWith("!"))
                {
                    // make sure we have enough characters
                    if (line.Length == 1) continue;

                    line = line.Substring(1);
                    entryList = excludedEntries;
                }

                // transform to single backslashes, and remove duplicate asterisks
                var path = Regex.Replace(line.Replace('/', '\\'), @"\\+", "\\");
                path = Regex.Replace(path, @"\*+", "*");

                // relative paths only - skip if rooted or drive path
                if (path.StartsWith("\\") || Regex.IsMatch(path, @"^[A-Za-z]:")) continue;

                // check the last segment to determine if we are a filename or a directory
                var lastSegment = Path.GetFileName(path);
                var filePattern = "*.exe";

                if (Regex.IsMatch(lastSegment, @"\.(exe|bat|cmd)$", RegexOptions.IgnoreCase))
                {
                    path = Path.GetDirectoryName(path);
                    filePattern = lastSegment;
                }

                // normalize the path
                path = path.TrimEnd('\\');
                if (string.IsNullOrEmpty(path))
                {
                    path = ".";
                }

                entryList.add_directive(path, filePattern);
            }

            resolve_directives(includedEntries, _includes);
            resolve_directives(excludedEntries, _excludes);
            remove_excluded();
        }

        /// <summary>
        /// Resolves directives and adds them to the list.
        /// </summary>
        /// <param name="entryList">The parsed directives from the file.</param>
        /// <param name="pathList">The resolved directives.</param>
        public void resolve_directives(ShimTargetList entryList, ShimTargetList pathList)
        {
            foreach (var path in entryList.Items.Keys)
            {
                var resolvedPaths = _resolver.resolve(path);

                // check we have results and that they are in the package directory
                if (resolvedPaths.Count == 0 || !resolvedPaths[0].StartsWith(_packageDir, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                var filePatterns = entryList.Items[path];

                foreach (var filePattern in filePatterns)
                {
                    foreach (var resolvedPath in resolvedPaths)
                    {
                        pathList.add_directive(resolvedPath, filePattern);
                    }
                }
            }

            sanitize_directives(pathList);
        }

        /// <summary>
        /// Removes redundant file patterns.
        /// </summary>
        /// <param name="list">List of resolved directives.</param>
        private void sanitize_directives(ShimTargetList list)
        {
            foreach (var filePatterns in list.Items.Values)
            {
                if (filePatterns.Count == 0) continue;

                var anyPatterns = new List<string>();
                var specificPatterns = new List<string>();

                // split into anyPatterns (like *.exe) and specificPatterns (like file.exe)
                foreach (var filePattern in filePatterns)
                {
                    var match = Regex.Match(filePattern, @"^\*\.(exe|bat|cmd)$").Groups[1];
                    if (match.Success)
                    {
                        anyPatterns.Add(match.Value);
                    }
                    else
                    {
                        specificPatterns.Add(filePattern);
                    }
                }

                // remove specific patterns (file.exe) if there is a matching any pattern (*.exe)
                foreach (var pattern in specificPatterns)
                {
                    var extension = _finder.get_extension(pattern);

                    if (anyPatterns.Contains(extension))
                    {
                        filePatterns.Remove(pattern);
                    }
                }
            }
        }

        /// <summary>
        /// Removes matching path-patterns from the included and excluded lists.
        /// </summary>
        private void remove_excluded()
        {
            var redundantExcludes = new List<string>();

            foreach (var path in _excludes.Items.Keys)
            {
                var excludedPatterns = _excludes.Items[path];
                List<string> includedPatterns;

                // add to redundantExcludes if not in includes
                if (!_includes.Items.TryGetValue(path, out includedPatterns))
                {
                    redundantExcludes.Add(path);
                    continue;
                }

                // remove from includes list if no patterns left
                var removedPatterns = exclude_patterns(excludedPatterns, includedPatterns);

                // remove from includes list if no patterns left
                if (includedPatterns.Count == 0)
                {
                    _includes.Items.Remove(path);
                }

                // remove matched patterns from excluded patterns
                foreach (var pattern in removedPatterns)
                {
                    excludedPatterns.Remove(pattern);
                }

                // add to redundantExcludes if no patterns left
                if (excludedPatterns.Count == 0)
                {
                    redundantExcludes.Add(path);
                }
            }

            // remove redundant excludes
            foreach (var path in redundantExcludes)
            {
                _excludes.Items.Remove(path);
            }
        }

        /// <summary>
        /// Removes matching file patterns from a list of included patterns.
        /// </summary>
        /// <param name="excludedPatterns">The excluded file patterns.</param>
        /// <param name="includedPatterns">The included file patterns.</param>
        /// <returns>The file patterns that have been removed.</returns>
        private List<string> exclude_patterns(List<string> excludedPatterns, List<string> includedPatterns)
        {
            var result = new List<string>();

            foreach (var exPattern in excludedPatterns)
            {
                var match = Regex.Match(exPattern, @"^\*\.(exe|bat|cmd)$").Groups[1];
                if (match.Success)
                {
                    // we match any pattern (like *.exe)
                    var extension = match.Value;
                    var removals = new List<string>();

                    // add patterns with the same extension for removal
                    foreach (var incPattern in includedPatterns)
                    {
                        if (extension == _finder.get_extension(incPattern))
                        {
                            removals.Add(incPattern);
                        }
                    }

                    // remove the patterns
                    foreach (var pattern in removals)
                    {
                        includedPatterns.Remove(pattern);
                    }

                    result.Add(exPattern);
                }
                else if (includedPatterns.Contains(exPattern))
                {
                    includedPatterns.Remove(exPattern);
                    result.Add(exPattern);
                }
            }

            return result;
        }
    }
}
