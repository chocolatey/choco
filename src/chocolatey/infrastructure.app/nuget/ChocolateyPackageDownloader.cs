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

namespace chocolatey.infrastructure.app.nuget
{
    using System;
    using System.IO;
    using NuGet;

    internal class ChocolateyPackageDownloader : PackageDownloader
    {
        public override void DownloadPackage(Uri uri, IPackageMetadata package, Stream targetStream)
        {
            ChocolateyNugetCredentialProvider.add_download_url_from_package(package);

            base.DownloadPackage(uri, package, targetStream);
        }

        public override void DownloadPackage(IHttpClient downloadClient, IPackageName package, Stream targetStream)
        {
            ChocolateyNugetCredentialProvider.add_download_url_from_package(package);

            base.DownloadPackage(downloadClient, package, targetStream);
        }
    }
}
