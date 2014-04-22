namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.IO;
    using System.Linq;
    using filesystem;
    using infrastructure.commands;
    using logging;
    using results;

    public class PowershellService : IPowershellService
    {
        private readonly IFileSystem _fileSystem;

        public PowershellService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void install(PackageResult packageResult)
        {
            var packageDirectory = _fileSystem.combine_paths(ApplicationParameters.PackagesLocation, "{0}.{1}".format_with(packageResult.Name, packageResult.Version));

            if (!_fileSystem.directory_exists(packageDirectory))
            {
                packageResult.Messages.Add(new ResultMessage(ResultType.Error, "Package install not found:'{0}'".format_with(packageDirectory)));
                return;
            }

            var installScript = _fileSystem.get_files(packageDirectory, "chocolateyInstall.ps1", SearchOption.AllDirectories);
            if (installScript.Count != 0)
            {
                var chocoInstall = installScript.FirstOrDefault();

                this.Log().Debug(ChocolateyLoggers.Important, "Contents of '{0}':".format_with(chocoInstall));
                this.Log().Debug(_fileSystem.read_file(chocoInstall));

                PowershellExecutor.execute(
                    PowershellExecutor.wrap_command_with_module(installScript.FirstOrDefault(), _fileSystem),
                    _fileSystem,
                    (s, e) =>
                        {
                            var logMessage = e.Data;
                            if (string.IsNullOrWhiteSpace(logMessage)) return;
                            this.Log().Info(e.Data);
                        },
                    (s, e) =>
                        {
                            if (string.IsNullOrWhiteSpace(e.Data)) return;
                            packageResult.Messages.Add(new ResultMessage(ResultType.Error, "Error while running '{0}':{1}{2}".format_with(installScript.FirstOrDefault(), Environment.NewLine, e.Data)));
                        });
            }

            packageResult.Messages.Add(new ResultMessage(ResultType.Note, "Ran '{0}'".format_with(installScript)));
        }
    }
}