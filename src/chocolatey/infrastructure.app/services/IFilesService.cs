﻿// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

using System;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.domain;
using chocolatey.infrastructure.results;

namespace chocolatey.infrastructure.app.services
{
    /// <summary>
    /// The files service for capturing and handling file snapshots.
    /// </summary>
    public interface IFilesService
    {
        /// <summary>
        /// Read the package files file from the specified filepath.
        /// </summary>
        /// <param name="filepath">The filepath.</param>
        /// <returns>PackageFiles with entries based on the file if it exists, otherwise null</returns>
        PackageFiles ReadPackageSnapshot(string filepath);

        /// <summary>
        /// Saves the files snapshot to the specified file path.
        /// </summary>
        /// <param name="snapshot">The snapshot.</param>
        /// <param name="filePath">The file path.</param>
        void SavePackageSnapshot(PackageFiles snapshot, string filePath);

        /// <summary>
        /// Ensure that the package files have compatible file attributes (e.g. no readonly).
        /// </summary>
        /// <param name="packageResult">The package result.</param>
        /// <param name="config">The configuration.</param>
        void EnsureCompatibleFileAttributes(PackageResult packageResult, ChocolateyConfiguration config);

        /// <summary>
        /// Ensure that files in a directory have compatible file attributes (e.g. no readonly).
        /// </summary>
        /// <param name="directory">The directory.</param>
        /// <param name="config">The configuration.</param>
        void EnsureCompatibleFileAttributes(string directory, ChocolateyConfiguration config);

        /// <summary>
        /// Captures the snapshot of the package files
        /// </summary>
        /// <param name="packageResult">The package result.</param>
        /// <param name="config">The configuration.</param>
        /// <returns>PackageFiles with entries based on the install location of the package.</returns>
        PackageFiles CaptureSnapshot(PackageResult packageResult, ChocolateyConfiguration config);

        /// <summary>
        /// Captures the snapshot of the package files
        /// </summary>
        /// <param name="directory">The directory.</param>
        /// <param name="config">The configuration.</param>
        /// <returns>PackageFiles with entries based on the install location of the package.</returns>
        PackageFiles CaptureSnapshot(string directory, ChocolateyConfiguration config);

        /// <summary>
        /// Gets a PackageFile from the filepath
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>PackageFile object</returns>
        PackageFile GetPackageFile(string file);

        bool MovePackageUsingBackupStrategy(string sourceFolder, string destinationFolder, bool restoreSource);

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        PackageFiles read_from_file(string filepath);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void save_to_file(PackageFiles snapshot, string filePath);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void ensure_compatible_file_attributes(PackageResult packageResult, ChocolateyConfiguration config);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void ensure_compatible_file_attributes(string directory, ChocolateyConfiguration config);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        PackageFiles capture_package_files(PackageResult packageResult, ChocolateyConfiguration config);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        PackageFiles capture_package_files(string directory, ChocolateyConfiguration config);
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        PackageFile get_package_file(string file);
#pragma warning restore IDE0022, IDE1006
    }
}
