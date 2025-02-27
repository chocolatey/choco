// Copyright © 2017 - 2025 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
