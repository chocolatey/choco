// Copyright © 2017 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.adapters
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Reflection;

    // ReSharper disable InconsistentNaming

    public sealed class Assembly : IAssembly
    {
        private readonly System.Reflection.Assembly _assembly;

        private Assembly(System.Reflection.Assembly assembly)
        {
            _assembly = assembly;
        }

        public string FullName
        {
            get { return _assembly.FullName; }
        }

        public string Location
        {
            get { return _assembly.Location; }
        }
        
        public string CodeBase
        {
            get { return _assembly.CodeBase; }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public System.Reflection.Assembly UnderlyingType
        {
            get { return _assembly; }
        }

        public string[] GetManifestResourceNames()
        {
            return _assembly.GetManifestResourceNames();
        }

        public Stream GetManifestResourceStream(string name)
        {
            return _assembly.GetManifestResourceStream(name);
        }

        public Stream GetManifestResourceStream(Type type, string name)
        {
            return _assembly.GetManifestResourceStream(type, name);
        }

        public AssemblyName GetName()
        {
            return _assembly.GetName();
        }

        public Type GetType(String name)
        {
            return _assembly.GetType(name);
        }   
        
        public Type GetType(String name, bool throwOnError)
        {
            return _assembly.GetType(name,throwOnError);
        }    
        
        public Type GetType(String name, bool throwOnError, bool ignoreCase)
        {
            return _assembly.GetType(name,throwOnError, ignoreCase);
        }

        public static IAssembly Load(byte[] rawAssembly)
        {
            return new Assembly(System.Reflection.Assembly.Load(rawAssembly));
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

        public static IAssembly set_assembly(System.Reflection.Assembly value)
        {
            return new Assembly(value);
        }

        public static implicit operator Assembly(System.Reflection.Assembly value)
        {
            return new Assembly(value);
        }
    }

    // ReSharper restore InconsistentNaming
}