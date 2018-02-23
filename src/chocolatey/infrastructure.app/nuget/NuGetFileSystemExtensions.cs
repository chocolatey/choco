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

namespace chocolatey.infrastructure.app.nuget
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NuGet;

    // ReSharper disable InconsistentNaming

    public static class NuGetFileSystemExtensions
    {
        public static void AddFiles(this IFileSystem fileSystem, IEnumerable<IPackageFile> files, string rootDir, bool preserveFilePath)
        {
            foreach (IPackageFile file in files)
            {
                string path = Path.Combine(rootDir, preserveFilePath ? file.Path : Path.GetFileName(file.Path));
                fileSystem.AddFileWithCheck(path, file.GetStream);
            }
        }

        internal static void AddFileWithCheck(this IFileSystem fileSystem, string path, Func<Stream> streamFactory)
        {
            using (Stream stream = streamFactory())
            {
                fileSystem.AddFile(path, stream);
            }
        }

        internal static void AddFileWithCheck(this IFileSystem fileSystem, string path, Action<Stream> write)
        {
            fileSystem.AddFile(path, write);
        }
    }

    // ReSharper restore InconsistentNaming
}
