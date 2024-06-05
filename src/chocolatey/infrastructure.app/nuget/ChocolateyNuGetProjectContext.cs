using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using chocolatey.infrastructure.app.configuration;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.ProjectManagement;
using chocolatey.infrastructure.logging;
using NuGet.Configuration;
using NuGet.Packaging.Signing;

namespace chocolatey.infrastructure.app.nuget
{
    public class ChocolateyNuGetProjectContext : INuGetProjectContext
    {
        private readonly ILogger _logger;

        public ChocolateyNuGetProjectContext(ChocolateyConfiguration config, ILogger logger)
        {
            //TODO, set client policy correctly here with settings, fix in chocolatey implementation of ISettings for this purpose
            var chocolateyNugetSettings = new ChocolateyNuGetSettings(config);
            var clientPolicyContext = ClientPolicyContext.GetClientPolicy(chocolateyNugetSettings, logger);
            PackageExtractionContext = new PackageExtractionContext(
                PackageSaveMode.Nupkg | PackageSaveMode.Nuspec | PackageSaveMode.Files,
                XmlDocFileSaveMode.None,
                clientPolicyContext,
                logger
                );
            _logger = logger;
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
            switch (level)
            {
                case MessageLevel.Debug:
                    _logger.LogDebug(message.FormatWith(args));
                    break;
                case MessageLevel.Info:
                    _logger.LogInformation(message.FormatWith(args));
                    break;
                case MessageLevel.Warning:
                    _logger.LogWarning(message.FormatWith(args));
                    break;
                case MessageLevel.Error:
                    _logger.LogError(message.FormatWith(args));
                    break;
            }
        }

        public void Log(ILogMessage message)
        {
            _logger.Log(message);
        }

        public void ReportError(string message)
        {
            _logger.LogError(message);
        }

        public void ReportError(ILogMessage message)
        {
            _logger.Log(message);
        }

        public FileConflictAction ResolveFileConflict(string message)
        {
            _logger.LogWarning("File conflict, overwriting all: {0}".FormatWith(message));
            return FileConflictAction.OverwriteAll;
        }

        public PackageExtractionContext PackageExtractionContext { get; set; }

        public ISourceControlManagerProvider SourceControlManagerProvider
        {
            get
            {
                return null;
            }
        }

        public ExecutionContext ExecutionContext
        {
            get
            {
                return null;
            }
        }

        public XDocument OriginalPackagesConfig { get; set; }
        public NuGetActionType ActionType { get; set; }
        public Guid OperationId { get; set; }
    }
}
