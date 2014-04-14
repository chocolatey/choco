namespace chocolatey.infrastructure.information
{
    using System.Diagnostics;
    using System.Reflection;

    public class VersionInformation
    {
        public static string get_current_assembly_version()
        {
            string version = null;
            Assembly executingAssembly = Assembly.GetCallingAssembly();
            string location = executingAssembly != null ? executingAssembly.Location : string.Empty;

            if (!string.IsNullOrEmpty(location))
            {
                version = FileVersionInfo.GetVersionInfo(location).FileVersion;
            }

            return version;
        }
    }
}