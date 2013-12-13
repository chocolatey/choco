using System.IO;
using System.Reflection;

namespace chocolatey
{
    public static class AssemblyExtensions
    {
        /// <summary>
        /// Get the manifest resource string from the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="manifest_resource_stream_location">The manifest resource stream location.</param>
        /// <returns></returns>
        public static string get_manifest_string(this Assembly assembly, string manifest_resource_stream_location)
        {
            var manifest_file_text = "";

            using (var manifest_file_stream = assembly.GetManifestResourceStream(manifest_resource_stream_location))
            {
                if (manifest_file_stream != null)
                {
                    using (var stream_reader = new StreamReader(manifest_file_stream))
                    {
                        manifest_file_text = stream_reader.ReadToEnd();
                        stream_reader.Close();
                        manifest_file_stream.Close();
                    }
                }
            }

            return manifest_file_text;
        }

        /// <summary>
        /// Get manifest resource stream from the specified assembly. Useful when grabbing binaries.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="manifest_resource_stream_location">The manifest resource stream location.</param>
        /// <returns></returns>
        public static Stream get_manifest_stream(this Assembly assembly, string manifest_resource_stream_location)
        {
            return assembly.GetManifestResourceStream(manifest_resource_stream_location);
        }
    }
}