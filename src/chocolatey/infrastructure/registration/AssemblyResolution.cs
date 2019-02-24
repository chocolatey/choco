// Copyright © 2017 - 2019 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.registration
{
    using System;
    using System.Linq;
    using System.Threading;
    using adapters;
    using filesystem;

    public class AssemblyResolution
    {
        private const int LOCK_RESOLUTION_TIMEOUT_SECONDS = 5;
        private static readonly object _lockObject = new object();

        /// <summary>
        /// Resolves or loads an assembly. If an assembly is already loaded, no need to reload it.
        /// </summary>
        /// <param name="assemblySimpleName">Simple Name of the assembly, such as "chocolatey"</param>
        /// <param name="publicKeyToken">The public key token.</param>
        /// <param name="assemblyFileLocation">The assembly file location. Typically the path to the DLL on disk.</param>
        /// <returns>An assembly</returns>
        /// <exception cref="Exception">Unable to enter synchronized code to determine assembly loading</exception>
        public static IAssembly resolve_or_load_assembly(string assemblySimpleName, string publicKeyToken, string assemblyFileLocation)
        {
            var lockTaken = false;
            try
            {
                Monitor.TryEnter(_lockObject, TimeSpan.FromSeconds(LOCK_RESOLUTION_TIMEOUT_SECONDS), ref lockTaken);
            }
            catch (Exception)
            {
                throw new Exception("Unable to enter synchronized code to determine assembly loading");
            }

            IAssembly resolvedAssembly = null;
            if (lockTaken)
            {
                try
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name.is_equal_to(assemblySimpleName)).or_empty_list_if_null())
                    {
                        if (string.IsNullOrWhiteSpace(publicKeyToken) || assembly.GetName().get_public_key_token().is_equal_to(publicKeyToken))
                        {
                            "AssemblyResolver".Log().Debug("Returning loaded assembly type for '{0}'".format_with(assemblySimpleName));
                            resolvedAssembly = Assembly.set_assembly(assembly);
                            break;
                        }
                    }

                    if (resolvedAssembly == null)
                    {
                        "AssemblyResolver".Log().Debug("Loading up '{0}' assembly type from '{1}'".format_with(assemblySimpleName, assemblyFileLocation));
                        // avoid locking by reading in the bytes of the file and then passing that to Assembly.Load
                        resolvedAssembly = Assembly.Load(FileSystem.read_binary_file_into_byte_array(assemblyFileLocation));
                    }
                }
                finally
                {
                    Monitor.Pulse(_lockObject);
                    Monitor.Exit(_lockObject);
                }
            }

            return resolvedAssembly;
        }
    }
}
