namespace chocolatey.infrastructure.adapters
{
    using System;
    using System.ComponentModel;
    using System.IO;

    // ReSharper disable InconsistentNaming

    public interface IAssembly
    {
        /// <summary>
        /// Gets the display name of the assembly.
        /// 
        /// </summary>
        /// 
        /// <returns>
        /// The display name of the assembly.
        /// 
        /// </returns>
        string FullName { get; }

        /// <summary>
        /// Gets the path or UNC location of the loaded file that contains the manifest.
        /// 
        /// </summary>
        /// 
        /// <returns>
        /// The location of the loaded file that contains the manifest. If the loaded file was shadow-copied, the location is that of the file after being shadow-copied. If the assembly is loaded from a byte array, such as when using the <see cref="M:System.Reflection.Assembly.Load(System.Byte[])"/> method overload, the value returned is an empty string ("").
        /// 
        /// </returns>
        /// <PermissionSet><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
        string Location { get; }

        /// <summary>
        /// Returns the underlying Assembly when necessary for libraries outside of the internal.
        /// </summary>
        /// <value>
        /// System.Reflection.Assembly
        /// </value>
        [EditorBrowsable(EditorBrowsableState.Never)]
        System.Reflection.Assembly UnderlyingType { get; }

        /// <summary>
        /// Returns the names of all the resources in this assembly.
        /// 
        /// </summary>
        /// 
        /// <returns>
        /// An array of type String containing the names of all the resources.
        /// 
        /// </returns>
        string[] GetManifestResourceNames();

        /// <summary>
        /// Loads the specified manifest resource from this assembly.
        /// 
        /// </summary>
        /// 
        /// <returns>
        /// A <see cref="T:System.IO.Stream"/> representing the manifest resource; null if no resources were specified during compilation, or if the resource is not visible to the caller.
        /// 
        /// </returns>
        /// <param name="name">The case-sensitive name of the manifest resource being requested.
        ///                 </param><exception cref="T:System.ArgumentNullException">The <paramref name="name"/> parameter is null.
        ///                 </exception><exception cref="T:System.ArgumentException">The <paramref name="name"/> parameter is an empty string ("").
        ///                 </exception><exception cref="T:System.IO.FileLoadException">A file that was found could not be loaded.
        ///                 </exception><exception cref="T:System.IO.FileNotFoundException"><paramref name="name"/> was not found.
        ///                 </exception><exception cref="T:System.BadImageFormatException"><paramref name="name"/> is not a valid assembly.
        ///                 </exception>
        Stream GetManifestResourceStream(string name);

        /// <summary>
        /// Loads the specified manifest resource, scoped by the namespace of the specified type, from this assembly.
        /// 
        /// </summary>
        /// 
        /// <returns>
        /// A <see cref="T:System.IO.Stream"/> representing the manifest resource; null if no resources were specified during compilation or if the resource is not visible to the caller.
        /// 
        /// </returns>
        /// <param name="type">The type whose namespace is used to scope the manifest resource name.
        ///                 </param><param name="name">The case-sensitive name of the manifest resource being requested.
        ///                 </param><exception cref="T:System.ArgumentNullException">The <paramref name="name"/> parameter is null.
        ///                 </exception><exception cref="T:System.ArgumentException">The <paramref name="name"/> parameter is an empty string ("").
        ///                 </exception><exception cref="T:System.IO.FileLoadException">A file that was found could not be loaded.
        ///                 </exception><exception cref="T:System.IO.FileNotFoundException"><paramref name="name"/> was not found.
        ///                 </exception><exception cref="T:System.BadImageFormatException"><paramref name="name"/> is not a valid assembly.
        ///                 </exception>
        Stream GetManifestResourceStream(Type type, string name);
    }

    // ReSharper restore InconsistentNaming
}