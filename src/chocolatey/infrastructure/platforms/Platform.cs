namespace chocolatey.infrastructure.platforms
{
    using System;
    using System.ComponentModel;
    using adapters;
    using filesystem;
    using Environment = adapters.Environment;

    /// <summary>
    ///   OS Platform detection
    /// </summary>
    /// <remarks>
    ///   Based on http://stackoverflow.com/questions/10138040/how-to-detect-properly-windows-linux-mac-operating-systems
    /// </remarks>
    public static class Platform
    {
        private static Lazy<IEnvironment> environment_initializer = new Lazy<IEnvironment>(() => new Environment());
        private static Lazy<IFileSystem> file_system_initializer = new Lazy<IFileSystem>(() => new DotNetFileSystem());

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void initialize_with(Lazy<IEnvironment> environment, Lazy<IFileSystem> file_system)
        {
            environment_initializer = environment;
            file_system_initializer = file_system;
        }

        private static IFileSystem file_system
        {
            get { return file_system_initializer.Value; }
        }

        private static IEnvironment Environment
        {
            get { return environment_initializer.Value; }
        }

        public static PlatformType get_platform()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                    return PlatformType.Mac;

                case PlatformID.Unix:
                    // Well, there are chances MacOSX is reported as Unix instead of MacOSX.
                    // Instead of platform check, we'll do a feature checks (Mac specific root folders)
                    if (file_system.directory_exists("/Applications")
                        & file_system.directory_exists("/System")
                        & file_system.directory_exists("/Users")
                        & file_system.directory_exists("/Volumes"))
                        return PlatformType.Mac;
                    else
                        return PlatformType.Linux;
                default:
                    return PlatformType.Windows;
            }
        }

        public static Version get_version()
        {
            return Environment.OSVersion.Version;
        }
    }
}