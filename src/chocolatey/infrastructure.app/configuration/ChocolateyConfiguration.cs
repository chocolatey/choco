namespace chocolatey.infrastructure.app.configuration
{
    using System;
    using System.Text;
    using platforms;

    public sealed class ChocolateyConfiguration
    {
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

        // top level commands
        public string CommandName { get; set; }
        public bool Debug { get; set; }
        public bool Force { get; set; }
        public bool Noop { get; set; }
        public bool HelpRequested { get; set; }

        // command level options
        public string Source { get; set; }

        /// <summary>
        ///   Gets or sets the package names. Space separated
        /// </summary>
        /// <value>
        ///   Space separated package names.
        /// </value>
        public string PackageNames { get; set; }
        public string Version { get; set; }
        public bool LocalOnly { get; set; }
        public string Filter { get; set; }
        public bool Prerelease { get; set; }
        public bool ForceX86 { get; set; }
        public string InstallArguments { get; set; }
        public bool OverrideArguments { get; set; }
        public bool NotSilent { get; set; }
        public string PackageParameters { get; set; }
        public bool IgnoreDependencies { get; set; }

        //list
        public bool AllVersions { get; set; }

        // configuration set variables
        public bool UseNugetForSources { get; set; }
        public bool CheckSumFiles { get; set; }
        public bool VirusCheckFiles { get; set; }
    }
}