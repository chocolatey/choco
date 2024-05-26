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
using System.Reflection;
using chocolatey.infrastructure.app.registration;
using chocolatey.infrastructure.logging;
using chocolatey.infrastructure.adapters;

namespace chocolatey
{
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
        public static string GetManifestString(this IAssembly assembly, string manifestResourceStreamLocation)
        {
            var manifestFileText = "";

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
        public static Stream GetManifestStream(this IAssembly assembly, string manifestResourceStreamLocation)
        {
            return assembly.GetManifestResourceStream(manifestResourceStreamLocation);
        }

        /// <summary>
        ///   Gets the public key token of an assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns></returns>
        /// <remarks>Borrowed heavily from http://dhvik.blogspot.com/2009/05/assemblynamegetpublickeytoken-tostring.html </remarks>
        public static string GetPublicKeyToken(this IAssembly assembly)
        {
            if (assembly == null)
            {
                return string.Empty;
            }

            return assembly.GetName().GetPublicKeyTokenString();
        }

        public static string GetPublicKeyTokenString(this AssemblyName assemblyName)
        {
            if (assemblyName == null)
            {
                return string.Empty;
            }

            var publicKeyToken = assemblyName.GetPublicKeyToken();

            if (publicKeyToken == null || publicKeyToken.Length == 0)
            {
                return string.Empty;
            }

            return publicKeyToken.Select(x => x.ToString("x2")).Aggregate((x, y) => x + y);
        }

        public static IEnumerable<Type> GetLoadableTypes(this IAssembly assembly)
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

        public static IEnumerable<IExtensionModule> GetExtensionModules(this IAssembly assembly)
        {
            var result = new List<IExtensionModule>();

            "chocolatey".Log().Debug("Gathering exported extension registration modules!");

            var registrationTypes = assembly
                .GetLoadableTypes()
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

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static string get_manifest_string(this IAssembly assembly, string manifestResourceStreamLocation)
            => GetManifestString(assembly, manifestResourceStreamLocation);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static Stream get_manifest_stream(this IAssembly assembly, string manifestResourceStreamLocation)
            => GetManifestStream(assembly, manifestResourceStreamLocation);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static string get_public_key_token(this IAssembly assembly)
            => GetPublicKeyToken(assembly);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static string get_public_key_token(this AssemblyName assemblyName)
            => GetPublicKeyTokenString(assemblyName);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static IEnumerable<Type> get_loadable_types(this IAssembly assembly)
            => GetLoadableTypes(assembly);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static IEnumerable<IExtensionModule> get_extension_modules(this IAssembly assembly)
            => GetExtensionModules(assembly);
#pragma warning restore IDE0022, IDE1006
    }
}
