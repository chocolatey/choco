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
    using System.Reflection;
    using chocolatey.infrastructure.app.registration;
    using chocolatey.infrastructure.logging;
    using infrastructure.adapters;

    /// <summary>
    ///   Extensions for Assembly
    /// </summary>
    public static class AssemblyExtensions
    {
        /// <summary>
        ///   Get the manifest resource string from the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="manifestResourceStreamLocation">The manifest resource stream location.</param>
        /// <returns></returns>
        public static string get_manifest_string(this IAssembly assembly, string manifestResourceStreamLocation)
        {
            string manifestFileText = "";

            using (Stream manifestFileStream = assembly.GetManifestResourceStream(manifestResourceStreamLocation))
            {
                if (manifestFileStream != null)
                {
                    using (var streamReader = new StreamReader(manifestFileStream))
                    {
                        manifestFileText = streamReader.ReadToEnd();
                        streamReader.Close();
                        manifestFileStream.Close();
                    }
                }
            }

            return manifestFileText;
        }

        /// <summary>
        ///   Get manifest resource stream from the specified assembly. Useful when grabbing binaries.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="manifestResourceStreamLocation">The manifest resource stream location.</param>
        /// <returns></returns>
        public static Stream get_manifest_stream(this IAssembly assembly, string manifestResourceStreamLocation)
        {
            return assembly.GetManifestResourceStream(manifestResourceStreamLocation);
        }

        /// <summary>
        ///   Gets the public key token of an assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns></returns>
        /// <remarks>Borrowed heavily from http://dhvik.blogspot.com/2009/05/assemblynamegetpublickeytoken-tostring.html </remarks>
        public static string get_public_key_token(this IAssembly assembly)
        {
            if (assembly == null) return string.Empty;

            return assembly.GetName().get_public_key_token();
        }

        public static string get_public_key_token(this AssemblyName assemblyName)
        {
            if (assemblyName == null) return string.Empty;

            byte[] publicKeyToken = assemblyName.GetPublicKeyToken();

            if (publicKeyToken == null || publicKeyToken.Length == 0) return string.Empty;

            return publicKeyToken.Select(x => x.ToString("x2")).Aggregate((x, y) => x + y);
        }

        public static IEnumerable<Type> get_loadable_types(this IAssembly assembly)
        {
            // Code originates from the following stack overflow answer: https://stackoverflow.com/a/11915414
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        public static IEnumerable<IExtensionModule> get_extension_modules(this IAssembly assembly)
        {
            var result = new List<IExtensionModule>();

            "chocolatey".Log().Debug("Gathering exported extension registration modules!");

            var registrationTypes = assembly
                .get_loadable_types()
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType && typeof(IExtensionModule).IsAssignableFrom(t));

            foreach (var extensionType in registrationTypes)
            {
                try
                {
                    var module = (IExtensionModule)Activator.CreateInstance(extensionType);
                    result.Add(module);
                }
                catch (Exception ex)
                {
                    "chocolatey".Log().Error("Unable to activate extension module '{0}' in assembly '{1}'.\n Message:{2}",
                        extensionType.Name,
                        assembly.GetName().Name,
                        ex.Message);
                    "chocolatey".Log().Error(ChocolateyLoggers.LogFileOnly, ex.StackTrace);
                }
            }

            return result;
        }
    }
}
