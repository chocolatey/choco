namespace chocolatey.infrastructure.app.nuget
{
    using System.Collections.Generic;
    using NuGet;

    // ReSharper disable InconsistentNaming

    public class ChocolateyPhysicalFileSystem : PhysicalFileSystem
    {
        public ChocolateyPhysicalFileSystem(string root)
            : base(root)
        {
        }

        public override void AddFiles(IEnumerable<IPackageFile> files, string rootDir)
        {
            this.AddFiles(files, rootDir, preserveFilePath: true);
        }
    }

    // ReSharper restore InconsistentNaming
}