// Copyright © 2017 - 2022 Chocolatey Software, Inc
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
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using logging;
    using NuGet.Common;

    // ReSharper disable InconsistentNaming

    public sealed class ChocolateyNugetLogger : ILogger
    {
        public void LogDebug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public void LogVerbose(string message)
        {
            Log(LogLevel.Verbose, message);
        }

        public void LogWarning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        public void LogError(string message)
        {
            Log(LogLevel.Error, message);
        }

        public void LogMinimal(string message)
        {
            // We log this as informational as we do not want
            // the output being shown to the user by default.
            // This includes information such as the time taken
            // to resolve dependencies, where the package was added
            // and so on.
            Log(LogLevel.Information, message);
        }

        public void LogInformation(string message)
        {
            // We log this as informational as we do not want
            // the output being shown to the user by default.
            // This includes information such as the time taken
            // to resolve dependencies, where the package was added
            // and so on.
            Log(LogLevel.Information, message);
        }

        public void LogInformationSummary(string message)
        {
            // We log it as minimal as we want the output to
            // be shown as an informational message in this case.
            Log(LogLevel.Minimal, message);
        }

        public void Log(LogLevel level, string message)
        {
            var prefixedMessage = prefix_all_lines("[NuGet]", message);

            switch (level)
            {
                case LogLevel.Debug:
                    this.Log().Debug(prefixedMessage);
                    break;
                case LogLevel.Warning:
                    this.Log().Warn(prefixedMessage);
                    break;
                case LogLevel.Error:
                    this.Log().Error(prefixedMessage);
                    break;
                case LogLevel.Verbose:
                    this.Log().Info(ChocolateyLoggers.Verbose, prefixedMessage);
                    break;
                case LogLevel.Information:
                    this.Log().Info(ChocolateyLoggers.Verbose, prefixedMessage);
                    break;
                case LogLevel.Minimal:
                    this.Log().Info(prefixedMessage);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(level));
            }
        }

        public Task LogAsync(LogLevel level, string message)
        {
            Log(level, message);

            return Task.CompletedTask;
        }

        public void Log(ILogMessage log)
        {
            Log(log.Level, log.Message);
        }

        public Task LogAsync(ILogMessage log)
        {
            return LogAsync(log.Level, log.Message);
        }

        private static string prefix_all_lines(string prefix, string message)
        {
            if (message == null || (string.IsNullOrWhiteSpace(message) && message.IndexOf('\n') < 0))
            {
                return prefix;
            }
            else if (message.IndexOf('\n') < 0)
            {
                return "{0} {1}".format_with(prefix, message);
            }

            var builder = new StringBuilder(message.Length);
            using (var reader = new StringReader(message))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    builder.Append(prefix);

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        builder.Append(' ').Append(line);
                    }

                    builder.AppendLine();
                }
            }

            // We specify the length we want, to ensure that we doesn't add any
            // new newlines to the output.
            return builder.ToString(0, builder.Length - Environment.NewLine.Length);
        }
    }

    // ReSharper restore InconsistentNaming
}
