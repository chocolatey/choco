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
        }

        private PackageExtractionContext _extractionContext;

        public void Log(MessageLevel level, string message, params object[] args)
        {
            switch (level)
            {
                case MessageLevel.Debug:
                    this.Log().Debug("[NuGet] " + message, args);
                    break;
                case MessageLevel.Info:
                    this.Log().Info("[NuGet] " + message, args);
                    break;
                case MessageLevel.Warning:
                    this.Log().Warn("[NuGet] " + message, args);
                    break;
                case MessageLevel.Error:
                    this.Log().Error("[NuGet] " + message, args);
                    break;
            }
        }

        public void Log(ILogMessage message)
        {
            switch (message.Level)
            {
                case LogLevel.Debug:
                    this.Log().Debug("[NuGet] " + message.Message);
                    break;
                case LogLevel.Warning:
                    this.Log().Warn("[NuGet] " + message.Message);
                    break;
                case LogLevel.Error:
                    this.Log().Error("[NuGet] " + message.Message);
                    break;
                case LogLevel.Verbose:
                    this.Log().Info(ChocolateyLoggers.Verbose, "[NuGet] " + message.Message);
                    break;
                case LogLevel.Information:
                    this.Log().Info(ChocolateyLoggers.Verbose, "[NuGet] " + message.Message);
                    break;
                case LogLevel.Minimal:
                    this.Log().Info("[NuGet] " + message.Message);
                    break;
            }
        }

        public void ReportError(string message)
        {
            this.Log().Error("[NuGet] " + message);
        }

        public void ReportError(ILogMessage message)
        {
            switch (message.Level)
            {
                case LogLevel.Debug:
                    this.Log().Debug("[NuGet] " + message.Message);
                    break;
                case LogLevel.Warning:
                    this.Log().Warn("[NuGet] " + message.Message);
                    break;
                case LogLevel.Error:
                    this.Log().Error("[NuGet] " + message.Message);
                    break;
                case LogLevel.Verbose:
                    this.Log().Info(ChocolateyLoggers.Verbose, "[NuGet] " + message.Message);
                    break;
                case LogLevel.Information:
                    this.Log().Info(ChocolateyLoggers.Verbose, "[NuGet] " + message.Message);
                    break;
                case LogLevel.Minimal:
                    this.Log().Info("[NuGet] " + message.Message);
                    break;
            }
        }

        public FileConflictAction ResolveFileConflict(string message)
        {
            this.Log().Warn("[NuGet] File conflict, overwriting all: " + message);
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
