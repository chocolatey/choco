namespace chocolatey.infrastructure.results
{
    using System.Linq;
    using NuGet;

    /// <summary>
    ///   Outcome of package installation
    /// </summary>
    public sealed class PackageResult : Result
    {
        public bool Inconclusive
        {
            get { return _messages.Value.Any(x => x.MessageType == ResultType.Inconclusive); }
        }

        public string Name { get; private set; }
        public string Version { get; private set; }
        public IPackage Package { get; private set; }
        public string InstallLocation { get; set; }

        public PackageResult(IPackage package, string installLocation) : this(package.Id.to_lower(), package.Version.to_string(), installLocation)
        {
            Package = package;
        }

        public PackageResult(string name, string version, string installLocation)
        {
            Name = name;
            Version = version;
            InstallLocation = installLocation;
        }
    }
}