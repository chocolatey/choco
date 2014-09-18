namespace chocolatey.infrastructure.app.templates
{
    public class TemplateValues
    {
        public TemplateValues()
        {
            set_normal();
        }

        public void set_normal()
        {
            PackageName = "__NAME_REPLACE__";
            PackageVersion = "__REPLACE__";
            MaintainerName = "__REPLACE_YOUR_NAME__";
            MaintainerRepo = "__REPLACE_YOUR_REPO__";
            AutomaticPackageNotesInstaller = "";
            InstallerType = "EXE_MSI_OR_MSU";
            Url = "";
            Url64 = "";
            SilentArgs = "";
            AutomaticPackageNotesNuspec = "";
        }

        public void set_auto()
        {
            PackageName = "{{PackageName}}";
            PackageVersion = "{{PackageVersion}}";
            AutomaticPackageNotesInstaller = ChocolateyInstallTemplate.AutomaticPackageNotes;
            AutomaticPackageNotesNuspec = NuspecTemplate.AutomaticPackageNotes;
            Url = "{{DownloadUrl}}";
            Url64 = "{{DownloadUrlx64}}";
        }


        public string PackageName { get; set; } 
        public string PackageNameLower {
            get { return PackageName.to_lower(); }
        }

        public string PackageVersion { get; set; } 
        public string MaintainerName { get; set; } 
        public string MaintainerRepo { get; set; }
        public string AutomaticPackageNotesInstaller { get; set; }
        public string AutomaticPackageNotesNuspec { get; set; } 
        public string InstallerType { get; set; } 
        public string Url { get; set; } 
        public string Url64 { get; set; } 
        public string SilentArgs { get; set; }

        public readonly static string NamePropertyName = "PackageName";
        public readonly static string VersionPropertyName = "PackageVersion";
        public readonly static string MaintainerPropertyName = "MaintainerName";

    }
}