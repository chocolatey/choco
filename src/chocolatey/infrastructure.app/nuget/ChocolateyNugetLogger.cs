// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.app.nuget
{
    using System.Threading.Tasks;
    using logging;
    using NuGet.Common;

    // ReSharper disable InconsistentNaming

    public sealed class ChocolateyNugetLogger : ILogger
    {
        public void LogDebug(string message)
        {
            this.Log().Debug("[NuGet] " + message);
        }

        public void LogVerbose(string message)
        {
            this.Log().Info(ChocolateyLoggers.Verbose, "[NuGet] " + message);
        }

        public void LogWarning(string message)
        {
            this.Log().Warn("[NuGet] " + message);
        }

        public void LogError(string message)
        {
            this.Log().Error("[NuGet] " + message);
        }

        public void LogMinimal(string message)
        {
            this.Log().Info( "[NuGet] " + message);
        }

        public void LogInformation(string message)
        {
            this.Log().Info("[NuGet] " + message);
        }

        public void LogInformationSummary(string message)
        {
            this.Log().Info("[NuGet] " + message);
        }

        public void Log(LogLevel level, string message)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    this.Log().Debug("[NuGet] " + message);
                    break;
                case LogLevel.Warning:
                    this.Log().Warn("[NuGet] " + message);
                    break;
                case LogLevel.Error:
                    this.Log().Error("[NuGet] " + message);
                    break;
                case LogLevel.Verbose:
                    this.Log().Info(ChocolateyLoggers.Verbose, "[NuGet] " + message);
                    break;
                case LogLevel.Information:
                    this.Log().Info(ChocolateyLoggers.Verbose, "[NuGet] " + message);
                    break;
                case LogLevel.Minimal:
                    this.Log().Info("[NuGet] " + message);
                    break;
            }
        }

        public Task LogAsync(LogLevel level, string message)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    this.Log().Debug("[NuGet] " + message);
                    break;
                case LogLevel.Warning:
                    this.Log().Warn("[NuGet] " + message);
                    break;
                case LogLevel.Error:
                    this.Log().Error("[NuGet] " + message);
                    break;
                case LogLevel.Verbose:
                    this.Log().Info(ChocolateyLoggers.Verbose, "[NuGet] " + message);
                    break;
                case LogLevel.Information:
                    this.Log().Info(ChocolateyLoggers.Verbose, "[NuGet] " + message);
                    break;
                case LogLevel.Minimal:
                    this.Log().Info("[NuGet] " + message);
                    break;
            }

            return Task.CompletedTask;
        }

        public void Log(ILogMessage log)
        {
            switch (log.Level)
            {
                case LogLevel.Debug:
                    this.Log().Debug("[NuGet] " + log.Message);
                    break;
                case LogLevel.Warning:
                    this.Log().Warn("[NuGet] " + log.Message);
                    break;
                case LogLevel.Error:
                    this.Log().Error("[NuGet] " + log.Message);
                    break;
                case LogLevel.Verbose:
                    this.Log().Info(ChocolateyLoggers.Verbose, "[NuGet] " + log.Message);
                    break;
                case LogLevel.Information:
                    this.Log().Info(ChocolateyLoggers.Verbose, "[NuGet] " + log.Message);
                    break;
                case LogLevel.Minimal:
                    this.Log().Info("[NuGet] " + log.Message);
                    break;
            }
        }

        public Task LogAsync(ILogMessage log)
        {
            switch (log.Level)
            {
                case LogLevel.Debug:
                    this.Log().Debug("[NuGet] " + log.Message);
                    break;
                case LogLevel.Warning:
                    this.Log().Warn("[NuGet] " + log.Message);
                    break;
                case LogLevel.Error:
                    this.Log().Error("[NuGet] " + log.Message);
                    break;
                case LogLevel.Verbose:
                    this.Log().Info(ChocolateyLoggers.Verbose, "[NuGet] " + log.Message);
                    break;
                case LogLevel.Information:
                    this.Log().Info(ChocolateyLoggers.Verbose, "[NuGet] " + log.Message);
                    break;
                case LogLevel.Minimal:
                    this.Log().Info("[NuGet] " + log.Message);
                    break;
            }
            return Task.CompletedTask;
        }
    }

    // ReSharper restore InconsistentNaming
}
