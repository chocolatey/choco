namespace chocolatey.infrastructure.adapters
{
    using System;
    using System.ComponentModel;
    using System.IO;

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
    }

    // ReSharper restore InconsistentNaming
}