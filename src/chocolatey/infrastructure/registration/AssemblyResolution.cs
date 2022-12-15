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

namespace chocolatey.infrastructure.registration
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using adapters;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.runners;
    using filesystem;
    using Assembly = adapters.Assembly;

    public class AssemblyResolution
    {
        private const int LOCK_RESOLUTION_TIMEOUT_SECONDS = 5;
        private static readonly object _lockObject = new object();

        private static readonly ConcurrentDictionary<string, IAssembly> _loadedAssemblies = new ConcurrentDictionary<string, IAssembly>();

        /// <summary>
        /// Resolves or loads an assembly. If an assembly is already loaded, no need to reload it.
        /// </summary>
        /// <param name="assemblySimpleName">Simple Name of the assembly, such as "chocolatey"</param>
        /// <param name="publicKeyToken">The public key token.</param>
        /// <param name="assemblyFileLocation">The assembly file location. Typically the path to the DLL on disk.</param>
        /// <returns>An assembly</returns>
        /// <exception cref="Exception">Unable to enter synchronized code to determine assembly loading</exception>
        public static IAssembly resolve_or_load_assembly(string assemblySimpleName, string publicKeyToken, string assemblyFileLocation) {
            return resolve_or_load_assembly(assemblySimpleName, publicKeyToken, assemblyFileLocation, false);
        }

        /// <summary>
        /// Resolves or loads an assembly. If an assembly is already loaded, no need to reload it.
        /// </summary>
        /// <param name="assemblySimpleName">Simple Name of the assembly, such as "chocolatey"</param>
        /// <param name="publicKeyToken">The public key token.</param>
        /// <param name="assemblyFileLocation">The assembly file location. Typically the path to the DLL on disk.</param>
        /// <param name="ignoreExisting">Whether any existing library that has previously been loaded should be ignored or not.</param>
        /// <returns>An assembly</returns>
        /// <exception cref="Exception">Unable to enter synchronized code to determine assembly loading</exception>
        public static IAssembly resolve_or_load_assembly(string assemblySimpleName, string publicKeyToken, string assemblyFileLocation, bool ignoreExisting = false)
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
                    if (!ignoreExisting)
                    {
                        resolvedAssembly = resolve_assembly(assemblySimpleName, publicKeyToken);
                    }

                    if (resolvedAssembly == null)
                    {
                        var tempAssembly = Assembly.Load(FileSystem.read_binary_file_into_byte_array(assemblyFileLocation));

                        if (tempAssembly == null)
                        {
                            return null;
                        }

                        if (string.IsNullOrWhiteSpace(publicKeyToken) || tempAssembly.GetName().get_public_key_token().is_equal_to(publicKeyToken))
                        {
                            "chocolatey".Log().Debug("Loading up '{0}' assembly type from '{1}'".format_with(assemblySimpleName, assemblyFileLocation));
                            resolvedAssembly = tempAssembly;
                            _loadedAssemblies.TryAdd(assemblySimpleName.to_lower(), resolvedAssembly);

                            if (assemblySimpleName.is_equal_to("choco"))
                            {
                                _loadedAssemblies.TryAdd("chocolatey", resolvedAssembly);
                            }
                            else if (assemblySimpleName.is_equal_to("chocolatey"))
                            {
                                _loadedAssemblies.TryAdd("choco", resolvedAssembly);
                            }
                        }
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

        public static IAssembly load_assembly(string assemblySimpleName, string assemblyFileLocation, params string[] publicKeyTokens)
        {
            if (publicKeyTokens == null || publicKeyTokens.Length == 0)
            {
                publicKeyTokens = new[] { string.Empty };
            }

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
                    IAssembly tempAssembly;
#if FORCE_CHOCOLATEY_OFFICIAL_KEY
                    tempAssembly = Assembly.Load(FileSystem.read_binary_file_into_byte_array(assemblyFileLocation));
#else

                    var symbolFile = System.IO.Path.ChangeExtension(assemblyFileLocation, ".pdb");
                    if (System.IO.File.Exists(symbolFile))
                    {
                        tempAssembly = Assembly.Load(
                            FileSystem.read_binary_file_into_byte_array(assemblyFileLocation),
                            FileSystem.read_binary_file_into_byte_array(symbolFile));
                    }
                    else
                    {
                        tempAssembly = Assembly.Load(FileSystem.read_binary_file_into_byte_array(assemblyFileLocation));
                    }
#endif

                    if (tempAssembly == null)
                    {
                        return null;
                    }

                    foreach (var publicKeyToken in publicKeyTokens)
                    {
                        if (string.IsNullOrWhiteSpace(publicKeyToken) || tempAssembly.GetName().get_public_key_token().is_equal_to(publicKeyToken))
                        {
                            "chocolatey".Log().Debug("Loading up '{0}' assembly type from '{1}'".format_with(assemblySimpleName, assemblyFileLocation));
                            resolvedAssembly = tempAssembly;

                            _loadedAssemblies.TryAdd(assemblySimpleName.to_lower(), resolvedAssembly);

                            if (assemblySimpleName.is_equal_to("choco"))
                            {
                                _loadedAssemblies.TryAdd("chocolatey", resolvedAssembly);
                            }
                            else if (assemblySimpleName.is_equal_to("chocolatey"))
                            {
                                _loadedAssemblies.TryAdd("choco", resolvedAssembly);
                            }

                            break;
                        }
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

        /// <summary>
        /// Resolves or loads an assembly. If an assembly is already loaded, no need to reload it.
        /// </summary>
        /// <param name="assemblySimpleName">Simple Name of the assembly, such as "chocolatey"</param>
        /// <param name="publicKeyTokens">The public key tokens the assembly may be signed with.</param>
        ///
        /// <returns>An assembly</returns>
        /// <exception cref="Exception">Unable to enter synchronized code to determine assembly loading</exception>
        public static IAssembly resolve_existing_assembly(string assemblySimpleName, params string[] publicKeyTokens)
        {
            if (publicKeyTokens == null || publicKeyTokens.Length == 0)
            {
                publicKeyTokens = new[] { string.Empty };
            }

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
                    foreach (var publicKeyToken in publicKeyTokens)
                    {
                        resolvedAssembly = resolve_assembly(assemblySimpleName, publicKeyToken);

                        if (resolvedAssembly != null)
                        {
                            break;
                        }
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

        private static IAssembly resolve_assembly(string assemblySimpleName, string publicKeyToken)
        {
            IAssembly resolvedAssembly = null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name.is_equal_to(assemblySimpleName)).or_empty_list_if_null())
            {
                if (string.IsNullOrWhiteSpace(publicKeyToken) || assembly.GetName().get_public_key_token().is_equal_to(publicKeyToken))
                {
                    "chocolatey".Log().Debug("Returning loaded assembly type for '{0}'".format_with(assemblySimpleName));
                    resolvedAssembly = Assembly.set_assembly(assembly);
                    break;
                }
            }

            IAssembly tempAssembly;

            if (_loadedAssemblies.TryGetValue(assemblySimpleName.to_lower(), out tempAssembly))
            {
                if (string.IsNullOrWhiteSpace(publicKeyToken) || tempAssembly.GetName().get_public_key_token().is_equal_to(publicKeyToken))
                {
                    "chocolatey".Log().Debug("Returning loaded assembly type for '{0}'".format_with(assemblySimpleName));
                    resolvedAssembly = tempAssembly;
                }
            }

            return resolvedAssembly;
        }

        public static IAssembly load_extension(string assemblySimpleName)
        {
#if FORCE_CHOCOLATEY_OFFICIAL_KEY
            var chocolateyPublicKey = ApplicationParameters.OfficialChocolateyPublicKey;
#else
            var chocolateyPublicKey = ApplicationParameters.UnofficialChocolateyPublicKey;
#endif

            var fullName = "{0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken={1}".format_with(
                assemblySimpleName,
                chocolateyPublicKey);

            try
            {
                // We use Reflection Assembly Load directly to allow .NET assembly resolving
                // to handle everything.
                var assembly = System.Reflection.Assembly.Load(fullName);

                if (assembly != null)
                {
                    return Assembly.set_assembly(assembly);
                }
            }
            // Ignore load failures, so we return null and let the caller handle the failure to load the extension.
            // A failure here will pretty much always be a public key mismatch, an invalid DLL, or a failure to find
            // the licensed extension, all of which should be handled by the caller.
            catch (FileNotFoundException)
            {
            }
            catch (FileLoadException)
            {
            }
            catch (BadImageFormatException)
            {
            }

            return null;
        }

        public static System.Reflection.Assembly resolve_extension_or_merged_assembly(object sender, ResolveEventArgs args)
        {
            var requestedAssembly = new AssemblyName(args.Name);

#if FORCE_CHOCOLATEY_OFFICIAL_KEY
            var chocolateyPublicKey = ApplicationParameters.OfficialChocolateyPublicKey;
#else
            var chocolateyPublicKey = ApplicationParameters.UnofficialChocolateyPublicKey;
#endif

            if (!requestedAssembly.get_public_key_token().is_equal_to(chocolateyPublicKey))
            {
                // This resolver is only loading official extensions for Chocolatey and ILMerged assemblies.
                // Everything else is not handled by this resolver.
                return null;
            }

            // Check if the requested assembly is already loaded
            var resolvedAssembly = resolve_existing_assembly(requestedAssembly.Name, chocolateyPublicKey);

            if (resolvedAssembly != null)
            {
                return resolvedAssembly.UnderlyingType;
            }

            if (Directory.Exists(ApplicationParameters.ExtensionsLocation))
            {
                bool extensionsFound = false;
                foreach (var extensionDll in Directory.EnumerateFiles(ApplicationParameters.ExtensionsLocation, requestedAssembly.Name + ".dll", SearchOption.AllDirectories))
                {
                    extensionsFound = true;
                    try
                    {
                        resolvedAssembly = load_assembly(requestedAssembly.Name, extensionDll, chocolateyPublicKey);

                        if (resolvedAssembly != null)
                        {
                            return resolvedAssembly.UnderlyingType;
                        }
                    }
                    catch (Exception ex)
                    {
                        // This catch statement is empty on purpose, we do
                        // not want to do anything if it fails to load.
                    }
                }

                if (extensionsFound)
                {
                    // Return to avoid an extension load call accidentally loading the `chocolatey` assembly.
                    // This is necessary as loading `chocolatey` itself as an extension can overwrite
                    // command registration for other extensions and break their functionality.
                    return null;
                }
            }

            // There are things that are ILMerged into Chocolatey. Anything with
            // the right public key except extensions should use the choco/chocolatey assembly
            if (!requestedAssembly.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase)
                && !requestedAssembly.Name.is_equal_to(ApplicationParameters.LicensedChocolateyAssemblySimpleName))
            {
                return typeof(ConsoleApplication).Assembly;
            }

            return null;
        }
    }
}
