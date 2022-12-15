// Copyright © 2017 - 2022 Chocolatey Software, Inc
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

namespace chocolatey
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using chocolatey.infrastructure.adapters;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.logging;
    using chocolatey.infrastructure.registration;

    public static class FileSystemExtensions
    {
        internal static IEnumerable<IAssembly> get_extension_assemblies(this IFileSystem fileSystem)
        {
            var result = new List<IAssembly>();

            if (!fileSystem.directory_exists(ApplicationParameters.ExtensionsLocation))
            {
                return result;
            }

            var extensionDllFiles = fileSystem.get_files(ApplicationParameters.ExtensionsLocation, "*.dll", SearchOption.AllDirectories);

            foreach (var extensionFile in extensionDllFiles)
            {
                var name = fileSystem.get_file_name_without_extension(extensionFile);

                try
                {
                    var assembly = AssemblyResolution.load_extension(name);

                    if (assembly == null)
                    {
                        "chocolatey".Log().Debug("Unable to load extension from path {0}.\n The assembly is not signed with official key token.", extensionFile);
                    }
                    else
                    {
                        result.Add(assembly);
                    }
                }
                catch (Exception ex)
                {
                    "chocolatey".Log().Error("Unable to load extension from path {0}.\n Message:{1}", extensionFile, ex.Message);
                    "chocolatey".Log().Error(ChocolateyLoggers.LogFileOnly, ex.StackTrace);
                }
            }

            return result.Distinct();
        }
    }
}
