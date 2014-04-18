namespace chocolatey.infrastructure.app
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using filesystem;

    public static class ApplicationParameters
    {
        private static readonly IFileSystem _fileSystem = new DotNetFileSystem();
        public static readonly string Name = "Chocolatey";
        public static readonly string InstallLocation = _fileSystem.get_directory_name(Assembly.GetExecutingAssembly().Location);
        public static readonly string CommonAppDataChocolatey = _fileSystem.combine_paths(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Name);
        public static readonly string LoggingLocation = _fileSystem.combine_paths(CommonAppDataChocolatey, "logs");
        public static readonly string LoggingFile = @"chocolatey.log";
        public static readonly string Log4NetConfigurationAssembly = @"chocolatey";
        public static readonly string Log4NetConfigurationResource = @"chocolatey.infrastructure.logging.log4net.config.xml";
        public static readonly string ChocolateyConfigFileResource = @"chocolatey.infrastructure.app.configuration.chocolatey.config";
        public static readonly string GlobalConfigFileLocation = _fileSystem.combine_paths(CommonAppDataChocolatey, "config", "chocolatey.config");
        public static readonly string LicenseFileLocation = _fileSystem.combine_paths(CommonAppDataChocolatey, "license", "chocolatey.license.xml");

        public static readonly string PackagesLocation = _fileSystem.combine_paths(InstallLocation, "lib");

        public static class Tools
        {
            public static readonly string NugetExe = _fileSystem.combine_paths(InstallLocation, "nuget.exe");
        }

        public static class OutputParser
        {
            public static class Nuget
            {
                public const string PACKAGE_NAME_GROUP = "PkgName";
                public const string PACKAGE_VERSION_GROUP = "PkgVersion";
                public static readonly Regex AlreadyInstalled = new Regex(@"already installed", RegexOptions.Compiled);
                public static readonly Regex NotInstalled = new Regex(@"not installed", RegexOptions.Compiled);
                public static readonly Regex ResolvingDependency = new Regex(@"Attempting to resolve dependency", RegexOptions.Compiled);
                public static readonly Regex PackageName = new Regex(@"'(?<{0}>[.\S]+)\s?".format_with(PACKAGE_NAME_GROUP), RegexOptions.Compiled);
                public static readonly Regex PackageVersion = new Regex(@"(?<{0}>[\d\.]+[\-\w]*)[[)]?'".format_with(PACKAGE_VERSION_GROUP), RegexOptions.Compiled);
            }
        }

        private static T TryGetConfig<T>(Func<T> func, T defaultValue)
        {
            try
            {
                return func.Invoke();
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        ///// <summary>
        /////   Are we in Debug Mode?
        ///// </summary>
        //public static bool IsDebug
        //{
        //    get { return TryGetConfig(() => Config.GetConfigurationSettings().Debug, false); }
        //}
    }
}