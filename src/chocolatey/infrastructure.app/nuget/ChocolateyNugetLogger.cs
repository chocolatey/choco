// Copyright © 2017 - 2018 Chocolatey Software, Inc
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
    using logging;
    using NuGet;

    // ReSharper disable InconsistentNaming

    public sealed class ChocolateyNugetLogger : ILogger
    {
        public FileConflictResolution ResolveFileConflict(string message)
        {
            return FileConflictResolution.OverwriteAll;
        }

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
                case MessageLevel.Fatal:
                    this.Log().Fatal("[NuGet] " + message, args);
                    break;
                case MessageLevel.Verbose:
                    this.Log().Info(ChocolateyLoggers.Verbose, "[NuGet] " + message, args);
                    break;
            }
        }
    }

    // ReSharper restore InconsistentNaming
}