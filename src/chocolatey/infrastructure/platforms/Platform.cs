namespace chocolatey.infrastructure.platforms
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using adapters;
    using Environment = adapters.Environment;

    public static class Platform
    {
        private static Lazy<IEnvironment> environment_initializer = new Lazy<IEnvironment>(() => new Environment());

        public static void initialize_with(Lazy<IEnvironment> environment)
        {
            environment_initializer = environment;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        private static IEnvironment Environment {
            get { return environment_initializer.Value; }
        }

        public static PlatformType get_platform()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    // Well, there are chances MacOSX is reported as Unix instead of MacOSX.
                    // Instead of platform check, we'll do a feature checks (Mac specific root folders)
                    if (Directory.Exists("/Applications")
                        & Directory.Exists("/System")
                        & Directory.Exists("/Users")
                        & Directory.Exists("/Volumes"))
                        return PlatformType.Mac;
                    else
                        return PlatformType.Linux;

                case PlatformID.MacOSX:
                    return PlatformType.Mac;

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