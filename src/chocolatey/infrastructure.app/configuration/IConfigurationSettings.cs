namespace chocolatey.infrastructure.app.configuration
{
    using System;
    using platforms;

    public interface IConfigurationSettings
    {
        // overrides
        string ToString();

        // configuration set variables
        bool DisablePush { get; set; }

        // application set variables
        PlatformType PlatformType { get; set; }
        Version PlatformVersion { get; set; }
        string ChocolateyVersion { get; set; }

        // top level settings
        string CommandName { get; set; }
        bool Debug { get; set; }
        bool Force { get; set; }
        bool Noop { get; set; }
        bool HelpRequested { get; set; }

        // command level options
        string Source { get; set; }

        /// <summary>
        /// Gets or sets the package names. Space separated
        /// </summary>
        /// <value>
        /// Space separated package names.
        /// </value>
        string PackageNames { get; set; }

        string Version { get; set; }
        bool LocalOnly { get; set; }
        string Filter { get; set; }
        bool Prerelease { get; set; }
    }
}