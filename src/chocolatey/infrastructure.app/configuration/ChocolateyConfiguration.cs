namespace chocolatey.infrastructure.app.configuration
{
    using System;
    using System.Text;
    using platforms;

    /// <summary>
    /// The chocolatey configuration.
    /// </summary>
    public sealed partial class ChocolateyConfiguration
    {
        public ChocolateyConfiguration()
        {
            RegularOuptut = true;
        }

        // overrides
        public override string ToString()
        {
            var properties = new StringBuilder();

            foreach (var propertyInfo in GetType().GetProperties())
            {
                properties.AppendFormat("{0}='{1}'|", propertyInfo.Name, propertyInfo.GetValue(this, null).to_string());
            }

            return properties.ToString();
        }

        // application set variables
        public PlatformType PlatformType { get; set; }
        public Version PlatformVersion { get; set; }
        public string ChocolateyVersion { get; set; }
        public bool Is64Bit { get; set; }
        public bool IsInteractive { get; set; }
        //public bool 

        // top level commands
        public string CommandName { get; set; }
        public bool Debug { get; set; }
        public bool Verbose { get; set; }
        public bool Force { get; set; }
        public bool Noop { get; set; }
        public bool HelpRequested { get; set; }
        public bool RegularOuptut { get; set; }

        // command level options
        public string Source { get; set; }
        public string Version { get; set; }
        public string Input { get; set; }

        // list
        public bool AllVersions { get; set; }
        public bool LocalOnly { get; set; }

        // configuration set variables
        public bool UseNugetForSources { get; set; }
        public bool CheckSumFiles { get; set; }
        public bool VirusCheckFiles { get; set; }

        // push
        public int TimeoutInSeconds { get; set; } //default to 300
        //DisableBuffering?

        // install/update
        /// <summary>
        ///   Gets or sets the package names. Space separated
        /// </summary>
        /// <value>
        ///   Space separated package names.
        /// </value>
        public string PackageNames { get; set; }
        public bool Prerelease { get; set; }
        public bool ForceX86 { get; set; }
        public string InstallArguments { get; set; }
        public bool OverrideArguments { get; set; }
        public bool NotSilent { get; set; }
        public string PackageParameters { get; set; }
        public bool IgnoreDependencies { get; set; }
        public bool AllowMultipleVersions { get; set; }
        public bool ForceDependencies { get; set; }

    }
}