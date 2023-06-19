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
    using filesystem;
    using NuGet.Common;
    using NuGet.Protocol.Core.Types;
    using System.Net.Http;

    public class NugetPush
    {
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static void PushPackage(ChocolateyConfiguration config, string nupkgFilePath, ILogger nugetLogger, string nupkgFileName, IFileSystem filesystem)
        {
            PushPackage(config, nupkgFilePath, nugetLogger, nupkgFileName, filesystem, cacheContext: null);
        }

        public static void PushPackage(ChocolateyConfiguration config, string nupkgFilePath, ILogger nugetLogger, string nupkgFileName, IFileSystem filesystem, ChocolateySourceCacheContext cacheContext)
        {
            var timeout = TimeSpan.FromSeconds(Math.Abs(config.CommandExecutionTimeoutSeconds));
            if (timeout.Seconds <= 0)
            {
                timeout = TimeSpan.FromMinutes(300); // Default to 5 hours if there is a zero (infinite) timeout
            }
            const bool disableBuffering = false;

            // Controls adding /api/v2/packages to the end of the source url, IF that source url is missing an endpoint (is just a host with no path like https://push.chocolatey.org/)
            const bool noServiceEndpoint = false;
            // Controls if NuGet throws on 409 HTTP response
            const bool skipDuplicate = false;

            //OK to use FirstOrDefault in this case as the command validates that there is only one source
            NuGetEndpointResources sourceEndpoint = NugetCommon.GetRepositoryResources(config, nugetLogger, filesystem, cacheContext).FirstOrDefault();
            PackageUpdateResource packageUpdateResource = sourceEndpoint.PackageUpdateResource;
            var nupkgFilePaths = new List<string>() { nupkgFilePath };

            try
            {
                packageUpdateResource.Push(
                    nupkgFilePaths,
                    symbolSource: "",
                    Convert.ToInt32(timeout.TotalSeconds),
                    disableBuffering,
                    endpoint => config.PushCommand.Key,
                    getSymbolApiKey: null,
                    noServiceEndpoint,
                    skipDuplicate,
                    symbolPackageUpdateResource: null,
                    nugetLogger).GetAwaiter().GetResult();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is HttpRequestException)
            {

                var message = ex.Message;
                if (!string.IsNullOrWhiteSpace(message))
                {
                    if (config.Sources == ApplicationParameters.ChocolateyCommunityFeedPushSource && message.Contains("already exists and cannot be modified"))
                    {
                        throw new ApplicationException("An error has occurred. This package version already exists on the repository and cannot be modified.{0}Package versions that are approved, rejected, or exempted cannot be modified.{0}See https://docs.chocolatey.org/en-us/community-repository/moderation/ for more information".FormatWith(Environment.NewLine), ex);
                    }

                    if (message.Contains("406") || message.Contains("409"))
                    {
                        // Let this fall through so the actual error message is shown when the exception is re-thrown
                        "chocolatey".Log().Error("An error has occurred. It's possible the package version already exists on the repository or a nuspec element is invalid. See error below...");
                    }
                }

                throw;
            }

            "chocolatey".Log().Info(ChocolateyLoggers.Important, () => "{0} was pushed successfully to {1}".FormatWith(nupkgFileName, config.Sources));
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static void push_package(ChocolateyConfiguration config, string nupkgFilePath, ILogger nugetLogger, string nupkgFileName, IFileSystem filesystem)
            => PushPackage(config, nupkgFilePath, nugetLogger, nupkgFileName, filesystem);
#pragma warning restore IDE1006
    }
}
