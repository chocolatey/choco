namespace chocolatey.infrastructure.app.configuration
{
    using System;
    using System.Text;
    using platforms;

    public class ConfigurationSettings : IConfigurationSettings
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

        // configuration set variables
        public bool DisablePush { get; set; }

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
        public string PackageNames { get; set; }
        public string Version { get; set; }
        public bool LocalOnly { get; set; }
        public string Filter { get; set; }
        public bool Prerelease { get; set; }
    }
}