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
    using configuration;
    using logging;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NuGet.Common;
    using NuGet.Configuration;
    using NuGet.Protocol;
    using NuGet.Protocol.Core.Types;

    public class NugetPush
    {
        public static void push_package(ChocolateyConfiguration config, string nupkgFilePath, ILogger nugetLogger, string nupkgFileName)
        {
            var timeout = TimeSpan.FromSeconds(Math.Abs(config.CommandExecutionTimeoutSeconds));
            if (timeout.Seconds <= 0)
            {
                timeout = TimeSpan.FromMinutes(300); // Default to 5 hours if there is a zero (infinite) timeout
            }
            const bool disableBuffering = false;

            // Controls adding /api/v2/packages to the end of the source url
            const bool noServiceEndpoint = true;

            //OK to use FirstOrDefault in this case as the command validates that there is only one source
            SourceRepository sourceRepository = NugetCommon.GetRemoteRepositories(config, nugetLogger).FirstOrDefault();
            PackageUpdateResource packageUpdateResource = sourceRepository.GetResource<PackageUpdateResource>();
            var nupkgFilePaths = new List<string>() { nupkgFilePath };
            UserAgent.SetUserAgentString(new UserAgentStringBuilder("{0}/{1} via NuGet Client".format_with(ApplicationParameters.UserAgent, config.Information.ChocolateyProductVersion)));

            try
            {
                packageUpdateResource.Push(
                    nupkgFilePaths,
                    "",
                    Convert.ToInt32(timeout.TotalSeconds),
                    disableBuffering,
                    endpoint => config.PushCommand.Key,
                    null,
                    noServiceEndpoint,
                    true,
                    null,
                    nugetLogger).GetAwaiter().GetResult();
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

            "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "{0} was pushed successfully to {1}".format_with(nupkgFileName, config.Sources));
        }
    }
}
