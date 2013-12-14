using System;
using System.IO;

namespace chocolatey.infrastructure.platforms
{
    public static class Platform
    {

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
    }

}