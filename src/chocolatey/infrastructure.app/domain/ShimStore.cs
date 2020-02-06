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
    using filesystem;

    public class ShimStore
    {
        private readonly IFileSystem _fileSystem;
        private readonly IDictionary<string, BaseRecord> _items;

        public class BaseRecord
        {
            /// <summary>
            /// The exe file from the shim directory.
            /// </summary>
            public string ExeFile { get; set; }

            /// <summary>
            /// The package name (could be empty).
            /// </summary>
            public string PackageName { get; set; }

            /// <summary>
            /// The file being shimmed (could be empty).
            /// </summary>
            public string TargetFile { get; set; }

            /// <summary>
            /// ExeFile last modification time.
            /// </summary>
            public Int64 LastModified { get; set; }

            /// <summary>
            /// Internal flag for synchronizing.
            /// </summary>
            public bool Current { get; set; }

            /// <summary>
            /// Creates a BaseRecord instance.
            /// </summary>
            /// <param name="record">The data record.</param>
            /// <param name="lastModified">Last modification time of the exe file.</param>
            public BaseRecord(ShimRecord record, Int64 lastModified)
            {
                ExeFile = record.ExeFile;
                PackageName = record.PackageName;
                TargetFile = record.TargetFile;
                LastModified = lastModified;
                Current = false;
            }
        }

        /// <summary>
        /// Creates a ShimStore instance.
        /// </summary>
        /// <param name="filesystem">The filesystem.</param>
        public ShimStore(IFileSystem filesystem)
        {
            _fileSystem = filesystem;
            _items = new Dictionary<string, BaseRecord>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Adds a record to the store.
        /// </summary>
        /// <param name="record">The record.</param>
        public void add_record(ShimRecord record)
        {
            string key = get_key(record.ExeFile);
            _items[key] = new BaseRecord(record, get_timestamp(record.ExeFile));
        }

        /// <summary>
        /// Removes a record from the store.
        /// </summary>
        /// <param name="exeFile">The exe file to remove.</param>
        public void remove_record(string exeFile)
        {
            string key = get_key(exeFile);
            _items.Remove(key);
        }

        /// <summary>
        /// Gets a record from the store if it exsists.
        /// </summary>
        /// <param name="exeFile">The exe file to get</param>
        /// <returns>The record or null.</returns>
        public ShimRecord get_record(string exeFile)
        {
            BaseRecord record;
            if (_items.TryGetValue(get_key(exeFile), out record))
            {
                return new ShimRecord(record.ExeFile, record.PackageName, record.TargetFile);
            }

            return null;
        }

        /// <summary>
        /// Synchronizes the store with the file system.
        /// </summary>
        /// <param name="exeFiles">The exe files in the shims directory.</param>
        /// <returns>A list of exe files that need adding or updating.</returns>
        public IList<string> get_files_to_update(IEnumerable<string> exeFiles)
        {
            var updates = new List<string>();
            BaseRecord record;

            // set found records as current and collect new or out-of-date ones
            foreach (string file in exeFiles.or_empty_list_if_null())
            {
                if (_items.TryGetValue(get_key(file), out record))
                {
                    if (get_timestamp(file) > record.LastModified)
                    {
                        updates.Add(file);
                    }

                    record.Current = true;
                }
                else
                {
                    updates.Add(file);
                }
            }

            remove_non_current();

            return updates;
        }

        /// <summary>
        /// Returns all records for the package.
        /// </summary>
        /// <param name="forPackage">The package name.</param>
        /// <returns>The list of records.</returns>
        public IList<ShimRecord> get_all_records(string forPackage)
        {
            var records = new List<ShimRecord>();
            var values = _items.Values;

            foreach (BaseRecord record in values)
            {
                if (record.PackageName.Equals(forPackage, StringComparison.OrdinalIgnoreCase))
                {
                    records.Add(get_record_from_base(record));
                }
            }

            return records;
        }

        /// <summary>
        /// Returns all records for the package that have not been modified.
        /// </summary>
        /// <param name="exeFiles">The exe files in the shims directory.</param>
        /// <param name="forPackage">The package name.</param>
        /// <returns>The list of unmodified records.</returns>
        public IList<ShimRecord> get_snapshot_records(IEnumerable<string> exeFiles, string forPackage)
        {
            var records = new List<ShimRecord>();
            BaseRecord record;

            // set found records as current and collect non-changed items
            foreach (string file in exeFiles.or_empty_list_if_null())
            {
                if (_items.TryGetValue(get_key(file), out record))
                {
                    if (record.PackageName.Equals(forPackage, StringComparison.OrdinalIgnoreCase))
                    {
                        if (get_timestamp(file) == record.LastModified)
                        {
                            records.Add(get_record_from_base(record));
                        }

                    }

                    record.Current = true;
                }
            }

            remove_non_current();

            return records;
        }

        /// <summary>
        /// Gets the storage key.
        /// </summary>
        /// <param name="exeFile">The exe file.</param>
        /// <returns>The file name of the exe file.</returns>
        private string get_key(string exeFile)
        {
            return _fileSystem.get_file_name(exeFile);
        }

        /// <summary>
        /// Create a ShimRecord from an internal base record.
        /// </summary>
        /// <param name="record">The internal base record.</param>
        /// <returns>The ShimRecord.</returns>
        private ShimRecord get_record_from_base(BaseRecord record)
        {
            return new ShimRecord(record.ExeFile, record.PackageName, record.TargetFile);
        }

        /// <summary>
        /// Gets the timestamp of a file.
        /// </summary>
        /// <param name="exeFile">The exe file.</param>
        /// <returns>The last modified UTC time.</returns>
        private Int64 get_timestamp(string exeFile)
        {
            return _fileSystem.get_file_modified_date(exeFile).ToFileTimeUtc();
        }

        /// <summary>
        /// Removes records marked as not current and resets the property.
        /// </summary>
        /// <remarks>Only call this after setting the current records to true.</remarks>
        private void remove_non_current()
        {
            var removed = new List<string>();

            // collect the non-current records and reset property
            foreach (KeyValuePair<string, BaseRecord> record in _items)
            {
                if (!record.Value.Current)
                {
                    removed.Add(record.Key);
                }

                record.Value.Current = false;
            }

            // remove the non-current records
            foreach (string key in removed)
            {
                _items.Remove(key);
            }
        }
    }
}
