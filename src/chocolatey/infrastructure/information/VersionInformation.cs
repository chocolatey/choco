namespace chocolatey.infrastructure.information
{
    using System.Diagnostics;
    using adapters;

    public sealed class VersionInformation
    {
        public static string get_current_assembly_version()
        {
            string version = null;
            var executingAssembly = Assembly.GetCallingAssembly();
            string location = executingAssembly != null ? executingAssembly.Location : string.Empty;

            if (!string.IsNullOrEmpty(location))
            {
                version = FileVersionInfo.GetVersionInfo(location).FileVersion;
            }

            return version;
        }
    }
}