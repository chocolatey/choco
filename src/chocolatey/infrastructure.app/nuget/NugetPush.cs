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
    using System;
    using System.IO;
    using NuGet;
    using configuration;
    using logging;

    public class NugetPush
    {
        public static void push_package(ChocolateyConfiguration config, string nupkgFilePath)
        {
            var timeout = TimeSpan.FromSeconds(Math.Abs(config.CommandExecutionTimeoutSeconds));
            if (timeout.Seconds <= 0)
            {
                timeout = TimeSpan.FromMinutes(300); // Default to 5 hours if there is a zero (infinite) timeout
            }
            const bool disableBuffering = false;

            var packageServer = new PackageServer(config.Sources, ApplicationParameters.UserAgent);
            
            packageServer.SendingRequest += (sender, e) => { if (config.Verbose) "chocolatey".Log().Info(ChocolateyLoggers.Verbose, "{0} {1}".format_with(e.Request.Method, e.Request.RequestUri)); };

            var package = new OptimizedZipPackage(nupkgFilePath);

            try
            {
                packageServer.PushPackage(
                    config.PushCommand.Key,
                    package,
                    new FileInfo(nupkgFilePath).Length,
                    Convert.ToInt32(timeout.TotalMilliseconds),
                    disableBuffering);
            }
            catch (InvalidOperationException ex)
            {
                var message = ex.Message;
                if (!string.IsNullOrWhiteSpace(message))
                {
                    if (config.Sources == ApplicationParameters.ChocolateyCommunityFeedPushSource && message.Contains("already exists and cannot be modified"))
                    {
                        throw new ApplicationException("An error has occurred. This package version already exists on the repository and cannot be modified.{0}Package versions that are approved, rejected, or exempted cannot be modified.{0}See https://docs.chocolatey.org/en-us/community-repository/moderation/ for more information".format_with(Environment.NewLine), ex);
                    }

                    if (message.Contains("(406)") || message.Contains("(409)"))
                    {
                        // Let this fall through so the actual error message is shown when the exception is re-thrown
                        "chocolatey".Log().Error("An error has occurred. It's possible the package version already exists on the repository or a nuspec element is invalid. See error below...");
                    }
                }

                throw;
            }

            "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "{0} was pushed successfully to {1}".format_with(package.GetFullName(), config.Sources));
        }
    }
}