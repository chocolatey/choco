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
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using filesystem;

    public class ShimRegistry
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _shimsLocation;
        private readonly string _packagesLocation;
        private readonly string _pathRoot;
        private readonly Regex _packageRegex;
        private ShimStore _store;

        /// <summary>
        /// The level required when accessing the data.
        /// </summary>
        public enum DataLevel
        {
            Current,
            Latest
        }

        /// <summary>
        /// Creates a ShimRegistry instance.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        public ShimRegistry(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _shimsLocation = ApplicationParameters.ShimsLocation;
            _packagesLocation = ApplicationParameters.PackagesLocation;
            _pathRoot = Path.GetPathRoot(_shimsLocation);
            _packageRegex = new Regex(@"{0}\\(.[^\\]+)\\".format_with(_packagesLocation.Replace(@"\", @"\\")),
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Adds a shim to the store.
        /// </summary>
        /// <param name="exeFile">The shim exe file.</param>
        /// <param name="packageName">The package name.</param>
        /// <param name="targetFile">The file being shimmed.</param>
        public void add(string exeFile, string packageName, string targetFile)
        {
            ensure_storage_exists(DataLevel.Current);
            var record = new ShimRecord(exeFile, packageName, targetFile);
            _store.add_record(record);
        }

        /// <summary>
        /// Gets a shim record from the store it if exists.
        /// </summary>
        /// <param name="exeFile">The shim exe file.</param>
        /// <returns>The record or null.</returns>
        public ShimRecord get(string exeFile)
        {
            ensure_storage_exists(DataLevel.Current);
            return _store.get_record(exeFile);
        }

        /// <summary>
        /// Updates the store from the file system.
        /// </summary>
        public void create_snapshot()
        {
            ensure_storage_exists(DataLevel.Latest);
        }

        /// <summary>
        /// Returns all shim records for the package.
        /// </summary>
        /// <param name="packageName">The package name.</param>
        /// <returns>The shim records.</returns>
        public IList<ShimRecord> get_all(string packageName)
        {
            ensure_storage_exists(DataLevel.Latest);
            var exeFiles = get_shim_exe_files();

            return _store.get_all_records(packageName);
        }

        /// <summary>
        /// Returns all shim records for the package that have not been modified.
        /// </summary>
        /// <param name="packageName">The package name.</param>
        /// <returns>The unmodified shim records.</returns>
        public IList<ShimRecord> get_snapshot(string packageName)
        {
            ensure_storage_exists(DataLevel.Current);
            var exeFiles = get_shim_exe_files();

            return _store.get_snapshot_records(exeFiles, packageName);
        }

        /// <summary>
        /// Removes a shim from the store.
        /// </summary>
        /// <param name="exeFile">The shim exe file.</param>
        public void remove(string exeFile)
        {
            ensure_storage_exists(DataLevel.Current);
            _store.remove_record(exeFile);
        }

        /// <summary>
        /// Initializes or updates the store.
        /// </summary>
        /// <param name="level">The data level required.</param>
        private void ensure_storage_exists(DataLevel level)
        {
            if (_store == null)
            {
                init_store();
            }
            else if (level == DataLevel.Latest)
            {
                update_store();
            }
        }

        /// <summary>
        /// Creates and initializes the store from the file system.
        /// </summary>
        private void init_store()
        {
            _store = new ShimStore(_fileSystem);
            var exeFiles = get_shim_exe_files();

            foreach (string file in exeFiles.or_empty_list_if_null())
            {
                _store.add_record(get_shim_data(file));
            }
        }

        /// <summary>
        /// Updates the store from the file system.
        /// </summary>
        private void update_store()
        {
            var exeFiles = get_shim_exe_files();

            foreach (string file in _store.get_files_to_update(exeFiles))
            {
                _store.add_record(get_shim_data(file));
            }
        }

        /// <summary>
        /// Gets the exe files in the shim directory.
        /// </summary>
        /// <returns>The exe files.</returns>
        private IEnumerable<string> get_shim_exe_files()
        {
            return _fileSystem.get_files(_shimsLocation, pattern: "*.exe");
        }

        /// <summary>
        /// Extracts any shim data from an exe file.
        /// </summary>
        /// <param name="exeFile">The exe file</param>
        /// <returns>A record containing full or partial shim data.</returns>
        private ShimRecord get_shim_data(string exeFile)
        {
            var item = new ShimRecord(exeFile);

            // check the exe is shimgen generated from the file info
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(exeFile);

            // note that this will not catch chocolatey specific shims
            if (!info.ProductName.contains("shimgen")) return item;

            // extract the target file from the binary
            string target = find_shim_target(exeFile);

            item.TargetFile = check_and_get_targetFile(target);

            // try and extract the package name
            if (item.TargetFile.contains(_packagesLocation))
            {
                var match = _packageRegex.Match(item.TargetFile).Groups[1];
                if (match != null)
                {
                    item.PackageName = match.Value;
                }
            }

            return item;
        }

        /// <summary>
        /// Searches an exe file for the shim target file name.
        /// </summary>
        /// <param name="exeFile">The exe file.</param>
        /// <returns>The target file name or null.</returns>
        private string find_shim_target(string exeFile)
        {
            string target = null;
            byte[] bytes = File.ReadAllBytes(exeFile);

            // search for the pattern: file at '<filename>'
            int startIndex = 0;
            string pattern = "file at '";
            int foundIndex = search_binary(bytes, pattern, startIndex);

            if (foundIndex == -1)
            {
                return target;
            }

            // search for the closing single-quote
            startIndex = foundIndex + (pattern.Length * 2);
            pattern = "'";
            foundIndex = search_binary(bytes, pattern, startIndex);

            if (foundIndex != -1)
            {
                int count = foundIndex - startIndex;
                var slice = new byte[count];
                Array.Copy(bytes, startIndex, slice, 0, count);
                target = Encoding.Unicode.GetString(slice);
            }

            return target;
        }

        /// <summary>
        /// Searches an array of bytes for a pattern.
        /// </summary>
        /// <param name="bytes">The array of bytes.</param>
        /// <param name="search">The pattern to search for.</param>
        /// <param name="startIndex">The index to start searching from.</param>
        /// <returns>The index of the pattern or -1.</returns>
        private int search_binary(byte[] bytes, string search, int startIndex)
        {
            int foundIndex = -1;
            byte[] pattern = Encoding.Unicode.GetBytes(search);
            int end = bytes.Length - pattern.Length + 1;
            int i = startIndex;
            int m = 0;

            while (i < end)
            {
                if (bytes[i] == pattern[m])
                {
                    ++m;
                    if (m == pattern.Length) {
                        foundIndex = i - pattern.Length + 1;
                        break;
                    }
                }
                else if (m > 0)
                {
                    i -= m;
                    m = 0;
                }
                ++i;
            }

            return foundIndex;
        }

        /// <summary>
        /// Checks that the extracted file name is valid.
        /// </summary>
        /// <param name="target">The target file name.</param>
        /// <returns>The fully qualified path or an empty string.</returns>
        private string check_and_get_targetFile(string target)
        {
            if (string.IsNullOrEmpty(target)) return string.Empty;

            if (!target.Replace('/', '\\').StartsWith(_pathRoot, StringComparison.OrdinalIgnoreCase))
            {
                target = _fileSystem.combine_paths(_packagesLocation, target);
            }

            try
            {
                return _fileSystem.get_full_path(target);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
