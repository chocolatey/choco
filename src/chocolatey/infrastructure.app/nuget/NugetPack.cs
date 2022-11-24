// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.app.nuget
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using IFileSystem = filesystem.IFileSystem;
    using chocolatey.infrastructure.platforms;
    using NuGet.Common;
    using NuGet.Configuration;
    using NuGet.Packaging;

    // ReSharper disable InconsistentNaming

    public sealed class NugetPack
    {
        public static bool BuildPackage(PackageBuilder builder, IFileSystem fileSystem, string outputPath)
        {
            ExcludeFiles(builder.Files);

            // Track if the package file was already present on disk
            bool isExistingPackage = fileSystem.file_exists(outputPath);
            try
            {
                using (Stream stream = fileSystem.create_file(outputPath))
                {
                    // Truncate if needed, as Mono fails to truncate
                    if (stream.Length > 0)
                    {
                        stream.SetLength(0);
                    }
                    builder.Save(stream);
                }
            }
            catch
            {
                if (!isExistingPackage && fileSystem.file_exists(outputPath))
                {
                    fileSystem.delete_file(outputPath);
                }
                throw;
            }

            return true;
        }

        private static void ExcludeFiles(ICollection<IPackageFile> packageFiles)
        {
            // Always exclude the nuspec file
            // Review: This exclusion should be done by the package builder because it knows which file would collide with the auto-generated
            // manifest file.
            var filter = Platform.get_platform() == PlatformType.Windows ? @"**\*" : "**/*";
            var excludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var wildCards = excludes.Concat(new[] { filter + PackagingConstants.ManifestExtension, filter + NuGetConstants.PackageExtension });

            PathResolver.FilterPackageFiles(packageFiles, ResolvePath, wildCards);
        }

        private static string ResolvePath(IPackageFile packageFile)
        {
            var physicalPackageFile = packageFile as PhysicalPackageFile;
            // For PhysicalPackageFiles, we want to filter by SourcePaths, the path on disk. The Path value maps to the TargetPath
            if (physicalPackageFile == null)
            {
                return packageFile.Path;
            }
            return physicalPackageFile.SourcePath;
        }
    }

    // ReSharper restore InconsistentNaming
}
