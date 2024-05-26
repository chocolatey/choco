﻿// Copyright © 2017 - 2022 Chocolatey Software, Inc
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using chocolatey.infrastructure.adapters;
using chocolatey.infrastructure.app;
using chocolatey.infrastructure.filesystem;
using chocolatey.infrastructure.logging;
using chocolatey.infrastructure.registration;

namespace chocolatey
{
    public static class FileSystemExtensions
    {
        internal static IEnumerable<IAssembly> GetExtensionAssemblies(this IFileSystem fileSystem)
        {
            var result = new List<IAssembly>();

            if (!fileSystem.DirectoryExists(ApplicationParameters.ExtensionsLocation))
            {
                return result;
            }

            var extensionDllFiles = fileSystem.GetFiles(ApplicationParameters.ExtensionsLocation, "*.dll", SearchOption.AllDirectories);

            foreach (var extensionFile in extensionDllFiles)
            {
                var name = fileSystem.GetFilenameWithoutExtension(extensionFile);

                try
                {
                    var assembly = AssemblyResolution.LoadExtension(name);

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

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        internal static IEnumerable<IAssembly> get_extension_assemblies(this IFileSystem fileSystem)
            => GetExtensionAssemblies(fileSystem);
#pragma warning restore IDE0022, IDE1006
    }
}
