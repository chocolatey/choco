namespace chocolatey.infrastructure.app.configuration
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using domain;
    using platforms;
    
    /// <summary>
    ///   The chocolatey configuration.
    /// </summary>
    public partial class ChocolateyConfiguration
    {
        public ChocolateyConfiguration()
        {
            RegularOuptut = true;
            PromptForConfirmation = true;
            Information = new InformationCommandConfiguration();
            NewCommand = new NewCommandConfiguration();
            ListCommand = new ListCommandConfiguration();
            SourceCommand = new SourcesCommandConfiguration();
            ApiKeyCommand = new ApiKeyCommandConfiguration();
            PushCommand = new PushCommandConfiguration();
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


        /// <summary>
        /// Gets or sets the name of the command.
        /// This is the command that choco runs.
        /// </summary>
        /// <value>
        /// The name of the command.
        /// </value>
        public string CommandName { get; set; }

        // configuration set variables
        
        public bool CheckSumFiles { get; set; }
        public bool VirusCheckFiles { get; set; }
        public string CacheLocation { get; set; }
        public bool ContainsLegacyPackageInstalls { get; set; }
        public int CommandExecutionTimeoutSeconds { get; set; }

        /// <summary>
        /// One or more source locations set by configuration or by command line. Separated by semi-colon
        /// </summary>
        public string Sources { get; set; }

        // top level commands
        
        public bool Debug { get; set; }
        public bool Verbose { get; set; }
        public bool Force { get; set; }
        public bool Noop { get; set; }
        public bool HelpRequested { get; set; }
        public bool RegularOuptut { get; set; }
        public bool PromptForConfirmation { get; set; }
        public bool AcceptLicense { get; set; }


        /// <summary>
        /// Usually related to unparsed arguments.
        /// </summary>
        public string Input { get; set; }

        // command level options
        public string Version { get; set; }
        public bool AllVersions { get; set; }
        public bool SkipPackageInstallProvider { get; set; }

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

        /// <summary>
        /// Configuration values provided by choco.
        /// </summary>
        public InformationCommandConfiguration Information { get; private set; }
        /// <summary>
        /// Configuration related specifically to List command
        /// </summary>
        public ListCommandConfiguration ListCommand { get; private set; }
        /// <summary>
        /// Configuration related specifically to New command
        /// </summary>
        public NewCommandConfiguration NewCommand { get; private set; }
        /// <summary>
        /// Configuration related specifically to Source command
        /// </summary>
        public SourcesCommandConfiguration SourceCommand { get; private set; }
        /// <summary>
        /// Configuration related specifically to ApiKey command
        /// </summary>
        public ApiKeyCommandConfiguration ApiKeyCommand { get; private set; }
        /// <summary>
        /// Configuration related specifically to Push command
        /// </summary>
        public PushCommandConfiguration PushCommand { get; private set; } 
    }

    public sealed class InformationCommandConfiguration
    {
        // application set variables
        public PlatformType PlatformType { get; set; }
        public Version PlatformVersion { get; set; }
        public string ChocolateyVersion { get; set; }
        public bool Is64Bit { get; set; }
        public bool IsInteractive { get; set; }
    }

    //todo: retrofit other command configs this way

    public sealed class ListCommandConfiguration
    {
        // list
        public bool LocalOnly { get; set; }
        public bool IncludeRegistryPrograms { get; set; }
    } 
    
    public sealed class NewCommandConfiguration
    {
       public NewCommandConfiguration()
        {
            TemplateProperties = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        }

        public string Name { get; set; }
        public bool AutomaticPackage { get; set; }
        public IDictionary<string, string> TemplateProperties { get; private set; }
    }

    public sealed class SourcesCommandConfiguration
    {
        public string Name { get; set; }
        public SourceCommandType Command { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public sealed class ApiKeyCommandConfiguration
    {
        public string Key { get; set; }
    }

    public sealed class PushCommandConfiguration
    {
        public string Key { get; set; }
        public int TimeoutInSeconds { get; set; }
        //DisableBuffering?
    }
}