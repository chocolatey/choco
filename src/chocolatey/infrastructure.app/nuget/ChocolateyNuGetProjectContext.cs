using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chocolatey.infrastructure.app.nuget
{
    using System.Xml.Linq;
    using configuration;
    using NuGet.Common;
    using NuGet.Packaging;
    using NuGet.ProjectManagement;
    using logging;
    using NuGet.Configuration;
    using NuGet.Packaging.Signing;

    public class ChocolateyNuGetProjectContext : INuGetProjectContext
    {
        private readonly ILogger _logger;

        public ChocolateyNuGetProjectContext(ChocolateyConfiguration config, ILogger logger)
        {
            //TODO, set client policy correctly here with settings, fix in chocolatey implementation of ISettings for this purpose
            var chocolateyNugetSettings = new ChocolateyNuGetSettings(config);
            var clientPolicyContext = ClientPolicyContext.GetClientPolicy(chocolateyNugetSettings, logger);
            _extractionContext = new PackageExtractionContext(
                PackageSaveMode.Nupkg | PackageSaveMode.Nuspec | PackageSaveMode.Files,
                XmlDocFileSaveMode.None,
                clientPolicyContext,
                logger
                );
            _logger = logger;
        }

        private PackageExtractionContext _extractionContext;

        public void Log(MessageLevel level, string message, params object[] args)
        {
            switch (level)
            {
                case MessageLevel.Debug:
                    _logger.LogDebug(message.format_with(args));
                    break;
                case MessageLevel.Info:
                    _logger.LogInformation(message.format_with(args));
                    break;
                case MessageLevel.Warning:
                    _logger.LogWarning(message.format_with(args));
                    break;
                case MessageLevel.Error:
                    _logger.LogError(message.format_with(args));
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
            _logger.LogWarning("File conflict, overwriting all: {0}".format_with(message));
            return FileConflictAction.OverwriteAll;
        }

        public PackageExtractionContext PackageExtractionContext
        {
            get
            {
                return _extractionContext;
            }
            set
            {
                _extractionContext = value;
            }
        }

        public ISourceControlManagerProvider SourceControlManagerProvider => null;
        public ExecutionContext ExecutionContext => null;
        public XDocument OriginalPackagesConfig { get; set; }
        public NuGetActionType ActionType { get; set; }
        public Guid OperationId { get; set; }
    }
}
