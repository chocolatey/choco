namespace chocolatey
{
    using System.IO;
    using System.Reflection;

    public static class AssemblyExtensions
    {
        /// <summary>
        ///     Get the manifest resource string from the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="manifestResourceStreamLocation">The manifest resource stream location.</param>
        /// <returns></returns>
        public static string get_manifest_string(this Assembly assembly, string manifestResourceStreamLocation)
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
        ///     Get manifest resource stream from the specified assembly. Useful when grabbing binaries.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="manifestResourceStreamLocation">The manifest resource stream location.</param>
        /// <returns></returns>
        public static Stream get_manifest_stream(this Assembly assembly, string manifestResourceStreamLocation)
        {
            return assembly.GetManifestResourceStream(manifestResourceStreamLocation);
        }
    }
}