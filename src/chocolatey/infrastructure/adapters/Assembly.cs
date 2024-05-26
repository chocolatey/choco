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

using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace chocolatey.infrastructure.adapters
{
    public sealed class Assembly : IAssembly
    {
        private Assembly(System.Reflection.Assembly assembly)
        {
            UnderlyingType = assembly;
        }

        public string FullName
        {
            get { return UnderlyingType.FullName; }
        }

        public string Location
        {
            get { return UnderlyingType.Location; }
        }

        public string CodeBase
        {
            get { return UnderlyingType.CodeBase; }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public System.Reflection.Assembly UnderlyingType { get; }

        public string[] GetManifestResourceNames()
        {
            return UnderlyingType.GetManifestResourceNames();
        }

        public Stream GetManifestResourceStream(string name)
        {
            return UnderlyingType.GetManifestResourceStream(name);
        }

        public Stream GetManifestResourceStream(Type type, string name)
        {
            return UnderlyingType.GetManifestResourceStream(type, name);
        }

        public AssemblyName GetName()
        {
            return UnderlyingType.GetName();
        }

        public Type GetType(string name)
        {
            return UnderlyingType.GetType(name);
        }

        public Type GetType(string name, bool throwOnError)
        {
            return UnderlyingType.GetType(name, throwOnError);
        }

        public Type GetType(string name, bool throwOnError, bool ignoreCase)
        {
            return UnderlyingType.GetType(name, throwOnError, ignoreCase);
        }

        public Type[] GetTypes()
        {
            return UnderlyingType.GetTypes();
        }

        public static IAssembly Load(byte[] rawAssembly)
        {
            return new Assembly(System.Reflection.Assembly.Load(rawAssembly));
        }

        public static IAssembly Load(byte[] rawAssembly, byte[] rawSymbols)
        {
            return new Assembly(System.Reflection.Assembly.Load(rawAssembly, rawSymbols));
        }

        public static IAssembly LoadFile(string path)
        {
            return new Assembly(System.Reflection.Assembly.LoadFile(path));
        }

        public static IAssembly GetAssembly(Type type)
        {
            return new Assembly(System.Reflection.Assembly.GetAssembly(type));
            //return System.Reflection.Assembly.GetAssembly(type);
        }

        public static IAssembly GetExecutingAssembly()
        {
            return new Assembly(System.Reflection.Assembly.GetExecutingAssembly());
            //return System.Reflection.Assembly.GetExecutingAssembly();
        }

        public static IAssembly GetCallingAssembly()
        {
            return new Assembly(System.Reflection.Assembly.GetCallingAssembly());
        }

        public static IAssembly GetEntryAssembly()
        {
            return new Assembly(System.Reflection.Assembly.GetEntryAssembly());
        }

        public static IAssembly SetAssembly(System.Reflection.Assembly value)
        {
            return new Assembly(value);
        }

        public static implicit operator Assembly(System.Reflection.Assembly value)
        {
            return new Assembly(value);
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static IAssembly set_assembly(System.Reflection.Assembly value)
            => SetAssembly(value);
#pragma warning restore IDE0022, IDE1006
    }
}
