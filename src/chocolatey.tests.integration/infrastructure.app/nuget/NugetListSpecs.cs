// Copyright © 2017 - 2024 Chocolatey Software, Inc
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
using System.IO;
using System.Linq;
using System.Text;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.nuget;
using chocolatey.infrastructure.filesystem;
using FluentAssertions;
using Moq;
using NuGet.Common;
using NuGet.Protocol.Core.Types;
using WireMock.FluentAssertions;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace chocolatey.tests.integration.infrastructure.app.services
{
    public class NugetListSpecs
    {
        public abstract class NugetListSpecsBase : TinySpec
        {
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();
            protected readonly ILogger Logger = new ChocolateyNugetLogger();
            protected readonly Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();
            protected Lazy<WireMockServer> MockServer;
            protected string HttpCacheLocation = Path.Combine(
                Environment.CurrentDirectory,
                Guid.NewGuid().ToString());

            public override void Context()
            {
                Configuration.CacheExpirationInMinutes = 0;
                Configuration.CacheLocation = HttpCacheLocation;
                MockServer = new Lazy<WireMockServer>(() => WireMockServer.Start());
            }

            public override void AfterObservations()
            {
                if (MockServer.IsValueCreated)
                {
                    MockServer.Value.Stop();
                    MockServer.Value.Dispose();
                }

                if (Directory.Exists(HttpCacheLocation))
                {
                    Directory.Delete(HttpCacheLocation, recursive: true);
                }

                base.AfterObservations();
            }

            protected void AddMockServerV2ApiUrl(ChocolateyConfiguration config)
            {
                config.Sources = $"{config.Sources};${MockServer.Value.Url}/api/v2/".TrimStart(';');
            }

            protected void AddMockServerV3ApiUrl(ChocolateyConfiguration config)
            {
                config.Sources = $"{config.Sources};${MockServer.Value.Url}/v3/index.json".TrimStart(';');
            }

            protected void AddSimpleResponse(string path, string body, string destination = BodyDestinationFormat.Json, Encoding encoding = null)
            {
                if (encoding is null)
                {
                    encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                }

                var matcher = path.Contains('*')
                    ? (IStringMatcher)new WildcardMatcher(path)
                    : new ExactMatcher(path);

                MockServer.Value.Given(Request.Create().WithPath(matcher).UsingGet())
                    .RespondWith(Response.Create()
                        .WithStatusCode(200)
                        .WithBody(body, destination, encoding));
            }

            protected void AddSimpleResponse(string path, object body, Encoding encoding = null)
            {
                if (encoding is null)
                {
                    encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                }

                var matcher = path.Contains('*')
                    ? (IStringMatcher)new WildcardMatcher(path)
                    : new ExactMatcher(path);

                MockServer.Value.Given(Request.Create().WithPath(matcher).UsingGet())
                    .RespondWith(Response.Create()
                        .WithStatusCode(200)
                        .WithBodyAsJson(body, encoding, indented: false));
            }

            protected void AddV3OnlyIndexResponse(string path, string host = null)
            {
                if (string.IsNullOrEmpty(host))
                {
                    host = MockServer.Value.Url;
                }

                AddSimpleResponse(path, @"{
    ""version"": ""3.0.0"",
    ""resources"": [
      {
        ""@id"": """ + host + @"/nuget/internal-choco/v3/search"",
        ""@type"": ""SearchQueryService"",
        ""comment"": ""Query endpoint of NuGet Search service""
      },
      {
        ""@id"": """ + host + @"/nuget/internal-choco/v3/search"",
        ""@type"": ""SearchQueryService/3.0.0-rc"",
        ""comment"": ""Query endpoint of NuGet Search service""
      },
      {
        ""@id"": """ + host + @"/nuget/internal-choco/v3/search"",
        ""@type"": ""SearchQueryService/3.0.0-beta"",
        ""comment"": ""Query endpoint of NuGet Search service""
      },
      {
        ""@id"": """ + host + @"/nuget/internal-choco/v3/autocomplete"",
        ""@type"": ""SearchAutocompleteService"",
        ""comment"": ""Autocomplete endpoint of NuGet Search service""
      },
      {
        ""@id"": """ + host + @"/nuget/internal-choco/v3/autocomplete"",
        ""@type"": ""SearchAutocompleteService/3.0.0-rc"",
        ""comment"": ""Autocomplete endpoint of NuGet Search service""
      },
      {
        ""@id"": """ + host + @"/nuget/internal-choco/v3/autocomplete"",
        ""@type"": ""SearchAutocompleteService/3.0.0-beta"",
        ""comment"": ""Autocomplete endpoint of NuGet Search service""
      },
      {
        ""@id"": """ + host + @"/nuget/internal-choco/v3/registrations/"",
        ""@type"": ""RegistrationsBaseUrl"",
        ""comment"": ""Base URL of Azure storage where NuGet package registration info is stored in GZIP format. This base URL includes SemVer 2.0.0 packages.""
      },
      {
        ""@id"": """ + host + @"/nuget/internal-choco/v3/registrations/"",
        ""@type"": ""RegistrationsBaseUrl/3.0.0-rc"",
        ""comment"": ""Base URL of Azure storage where NuGet package registration info is stored in GZIP format. This base URL includes SemVer 2.0.0 packages.""
      },
      {
        ""@id"": """ + host + @"/nuget/internal-choco/v3/registrations/"",
        ""@type"": ""RegistrationsBaseUrl/3.0.0-beta"",
        ""comment"": ""Base URL of Azure storage where NuGet package registration info is stored in GZIP format. This base URL includes SemVer 2.0.0 packages.""
      },
      {
        ""@id"": """ + host + @"/nuget/internal-choco/v3/registrations-gz/"",
        ""@type"": ""RegistrationsBaseUrl/3.4.0"",
        ""comment"": ""Base URL of Azure storage where NuGet package registration info is stored in GZIP format. This base URL includes SemVer 2.0.0 packages.""
      },
      {
        ""@id"": """ + host + @"/nuget/internal-choco/v3/registrations-gz/"",
        ""@type"": ""RegistrationsBaseUrl/3.6.0"",
        ""comment"": ""Base URL of Azure storage where NuGet package registration info is stored in GZIP format. This base URL includes SemVer 2.0.0 packages.""
      },
      {
        ""@id"": """ + host + @"/nuget/internal-choco/v3/flatcontainer"",
        ""@type"": ""PackageBaseAddress/3.0.0"",
        ""comment"": ""Base URL of where NuGet packages are stored, in the format https://api.nuget.org/v3-flatcontainer/{id-lower}/{version-lower}/{id-lower}.{version-lower}.nupkg""
      },
      {
        ""@id"": """ + host + @"/feeds/internal-choco/{id}/{version}"",
        ""@type"": ""PackageDetailsUriTemplate/5.1.0"",
        ""comment"": ""URI template used by NuGet Client to construct details URL for packages""
      },
      {
        ""@id"": """ + host + @"/nuget/internal-choco/v3/registrations/{id-lower}/index.json"",
        ""@type"": ""PackageDisplayMetadataUriTemplate/3.0.0-rc"",
        ""comment"": ""URI template used by NuGet Client to construct display metadata for Packages using ID""
      },
      {
        ""@id"": """ + host + @"/nuget/internal-choco/v3/registrations/{id-lower}/{version-lower}.json"",
        ""@type"": ""PackageVersionDisplayMetadataUriTemplate/3.0.0-rc"",
        ""comment"": ""URI template used by NuGet Client to construct display metadata for Packages using ID, Version""
      }
    ]
  }");
            }

            protected void AddV3IndexResponse(string path, string host = null)
            {
                if (string.IsNullOrEmpty(host))
                {
                    host = MockServer.Value.Url;
                }

                AddSimpleResponse(path, @"{
  ""version"": ""3.0.0"",
  ""resources"": [
    {
      ""@id"": """ + host + @"/nuget/internal-choco/v3/search"",
      ""@type"": ""SearchQueryService"",
      ""comment"": ""Query endpoint of NuGet Search service""
    },
    {
      ""@id"": """ + host + @"/nuget/internal-choco/v3/search"",
      ""@type"": ""SearchQueryService/3.0.0-rc"",
      ""comment"": ""Query endpoint of NuGet Search service""
    },
    {
      ""@id"": """ + host + @"/nuget/internal-choco/v3/search"",
      ""@type"": ""SearchQueryService/3.0.0-beta"",
      ""comment"": ""Query endpoint of NuGet Search service""
    },
    {
      ""@id"": """ + host + @"/nuget/internal-choco/v3/autocomplete"",
      ""@type"": ""SearchAutocompleteService"",
      ""comment"": ""Autocomplete endpoint of NuGet Search service""
    },
    {
      ""@id"": """ + host + @"/nuget/internal-choco/v3/autocomplete"",
      ""@type"": ""SearchAutocompleteService/3.0.0-rc"",
      ""comment"": ""Autocomplete endpoint of NuGet Search service""
    },
    {
      ""@id"": """ + host + @"/nuget/internal-choco/v3/autocomplete"",
      ""@type"": ""SearchAutocompleteService/3.0.0-beta"",
      ""comment"": ""Autocomplete endpoint of NuGet Search service""
    },
    {
      ""@id"": """ + host + @"/nuget/internal-choco/v3/registrations/"",
      ""@type"": ""RegistrationsBaseUrl"",
      ""comment"": ""Base URL of Azure storage where NuGet package registration info is stored in GZIP format. This base URL includes SemVer 2.0.0 packages.""
    },
    {
      ""@id"": """ + host + @"/nuget/internal-choco/v3/registrations/"",
      ""@type"": ""RegistrationsBaseUrl/3.0.0-rc"",
      ""comment"": ""Base URL of Azure storage where NuGet package registration info is stored in GZIP format. This base URL includes SemVer 2.0.0 packages.""
    },
    {
      ""@id"": """ + host + @"/nuget/internal-choco/v3/registrations/"",
      ""@type"": ""RegistrationsBaseUrl/3.0.0-beta"",
      ""comment"": ""Base URL of Azure storage where NuGet package registration info is stored in GZIP format. This base URL includes SemVer 2.0.0 packages.""
    },
    {
      ""@id"": """ + host + @"/nuget/internal-choco/v3/registrations-gz/"",
      ""@type"": ""RegistrationsBaseUrl/3.4.0"",
      ""comment"": ""Base URL of Azure storage where NuGet package registration info is stored in GZIP format. This base URL includes SemVer 2.0.0 packages.""
    },
    {
      ""@id"": """ + host + @"/nuget/internal-choco/v3/registrations-gz/"",
      ""@type"": ""RegistrationsBaseUrl/3.6.0"",
      ""comment"": ""Base URL of Azure storage where NuGet package registration info is stored in GZIP format. This base URL includes SemVer 2.0.0 packages.""
    },
    {
      ""@id"": """ + host + @"/nuget/internal-choco/v3/flatcontainer"",
      ""@type"": ""PackageBaseAddress/3.0.0"",
      ""comment"": ""Base URL of where NuGet packages are stored, in the format https://api.nuget.org/v3-flatcontainer/{id-lower}/{version-lower}/{id-lower}.{version-lower}.nupkg""
    },
    {
      ""@id"": """ + host + @"/feeds/internal-choco/{id}/{version}"",
      ""@type"": ""PackageDetailsUriTemplate/5.1.0"",
      ""comment"": ""URI template used by NuGet Client to construct details URL for packages""
    },
    {
      ""@id"": """ + host + @"/nuget/internal-choco/v3/registrations/{id-lower}/index.json"",
      ""@type"": ""PackageDisplayMetadataUriTemplate/3.0.0-rc"",
      ""comment"": ""URI template used by NuGet Client to construct display metadata for Packages using ID""
    },
    {
      ""@id"": """ + host + @"/nuget/internal-choco/v3/registrations/{id-lower}/{version-lower}.json"",
      ""@type"": ""PackageVersionDisplayMetadataUriTemplate/3.0.0-rc"",
      ""comment"": ""URI template used by NuGet Client to construct display metadata for Packages using ID, Version""
    },
    {
      ""@id"": """ + host + @"/nuget/internal-choco/"",
      ""@type"": ""LegacyGallery""
    },
    {
      ""@id"": """ + host + @"/nuget/internal-choco/"",
      ""@type"": ""LegacyGallery/2.0.0""
    },
    {
      ""@id"": """ + host + @"/nuget/internal-choco/package"",
      ""@type"": ""PackagePublish/2.0.0""
    }
  ]
}
");

                AddV2MetadataResponse("/nuget/internal-choco/");

            }

            protected void AddV2MetadataResponse(string path)
            {
                AddSimpleResponse(path, @"<?xml version=""1.0"" encoding=""utf-8""?>
<service
    xmlns=""http://www.w3.org/2007/app"">
    <workspace>
        <title
            xmlns=""http://www.w3.org/2005/Atom"">Default
        </title>
        <collection href=""Packages"">
            <title
                xmlns=""http://www.w3.org/2005/Atom"">Packages
            </title>
        </collection>
    </workspace>
</service>", destination: BodyDestinationFormat.SameAsSource);

                AddSimpleResponse(path + "$metadata", @"<Edmx xmlns=""http://schemas.microsoft.com/ado/2007/06/edmx"" Version=""1.0"">
<DataServices xmlns:p2=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" p2:MaxDataServiceVersion=""2.0"" p2:DataServiceVersion=""2.0"">
<Schema xmlns=""http://schemas.microsoft.com/ado/2006/04/edm"" Namespace=""NuGetGallery"">
<EntityType p2:HasStream=""true"" Name=""V2FeedPackage"">
<Key>
<PropertyRef Name=""Id""/>
<PropertyRef Name=""Version""/>
</Key>
<Property Name=""Id"" Type=""Edm.String"" Nullable=""false"" p2:FC_TargetPath=""SyndicationTitle"" p2:FC_ContentKind=""text"" p2:FC_KeepInContent=""false""/>
<Property Name=""Version"" Type=""Edm.String"" Nullable=""false""/>
<Property Name=""NormalizedVersion"" Type=""Edm.String"" Nullable=""false""/>
<Property Name=""Title"" Type=""Edm.String"" Nullable=""true""/>
<Property Name=""Authors"" Type=""Edm.String"" Nullable=""true"" p2:FC_TargetPath=""SyndicationAuthorName"" p2:FC_ContentKind=""text"" p2:FC_KeepInContent=""false""/>
<Property Name=""Icon"" Type=""Edm.String"" Nullable=""true""/>
<Property Name=""IconUrl"" Type=""Edm.String"" Nullable=""true""/>
<Property Name=""License"" Type=""Edm.String"" Nullable=""true""/>
<Property Name=""LicenseUrl"" Type=""Edm.String"" Nullable=""true""/>
<Property Name=""ProjectUrl"" Type=""Edm.String"" Nullable=""true""/>
<Property Name=""ReportAbuseUrl"" Type=""Edm.String"" Nullable=""true""/>
<Property Name=""GalleryDetailsUrl"" Type=""Edm.String"" Nullable=""true""/>
<Property Name=""DownloadCount"" Type=""Edm.Int32"" Nullable=""false""/>
<Property Name=""VersionDownloadCount"" Type=""Edm.Int32"" Nullable=""false""/>
<Property Name=""RequireLicenseAcceptance"" Type=""Edm.Boolean"" Nullable=""false""/>
<Property Name=""Description"" Type=""Edm.String"" Nullable=""true""/>
<Property Name=""Language"" Type=""Edm.String"" Nullable=""true""/>
<Property Name=""Summary"" Type=""Edm.String"" Nullable=""true"" p2:FC_TargetPath=""SyndicationSummary"" p2:FC_ContentKind=""text"" p2:FC_KeepInContent=""false""/>
<Property Name=""ReleaseNotes"" Type=""Edm.String"" Nullable=""true""/>
<Property Name=""Published"" Type=""Edm.DateTime"" Nullable=""false""/>
<Property Name=""Created"" Type=""Edm.DateTime"" Nullable=""false""/>
<Property Name=""LastUpdated"" Type=""Edm.DateTime"" Nullable=""false"" p2:FC_TargetPath=""SyndicationUpdated"" p2:FC_ContentKind=""text"" p2:FC_KeepInContent=""false""/>
<Property Name=""Dependencies"" Type=""Edm.String"" Nullable=""true""/>
<Property Name=""PackageHash"" Type=""Edm.String"" Nullable=""true""/>
<Property Name=""PackageSize"" Type=""Edm.Int64"" Nullable=""false""/>
<Property Name=""PackageHashAlgorithm"" Type=""Edm.String"" Nullable=""false""/>
<Property Name=""Copyright"" Type=""Edm.String"" Nullable=""true""/>
<Property Name=""Tags"" Type=""Edm.String"" Nullable=""true""/>
<Property Name=""IsAbsoluteLatestVersion"" Type=""Edm.Boolean"" Nullable=""false""/>
<Property Name=""IsLatestVersion"" Type=""Edm.Boolean"" Nullable=""false""/>
<Property Name=""IsPrerelease"" Type=""Edm.Boolean"" Nullable=""false""/>
<Property Name=""Listed"" Type=""Edm.Boolean"" Nullable=""false""/>
</EntityType>
<EntityContainer Name=""V2FeedContext"" p2:IsDefaultEntityContainer=""true"">
<EntitySet Name=""Packages"" EntityType=""NuGetGallery.V2FeedPackage""/>
<FunctionImport Name=""Search"" p2:HttpMethod=""GET"" EntitySet=""Packages"" ReturnType=""Collection(NuGetGallery.V2FeedPackage)"">
<Parameter Name=""searchTerm"" Type=""Edm.String""/>
<Parameter Name=""targetFramework"" Type=""Edm.String""/>
<Parameter Name=""includePrerelease"" Type=""Edm.Boolean""/>
</FunctionImport>
<FunctionImport Name=""FindPackagesById"" p2:HttpMethod=""GET"" EntitySet=""Packages"" ReturnType=""Collection(NuGetGallery.V2FeedPackage)"">
<Parameter Name=""id"" Type=""Edm.String""/>
</FunctionImport>
<FunctionImport Name=""GetUpdates"" p2:HttpMethod=""GET"" EntitySet=""Packages"" ReturnType=""Collection(NuGetGallery.V2FeedPackage)"">
<Parameter Name=""packageIds"" Type=""Edm.String""/>
<Parameter Name=""versions"" Type=""Edm.String""/>
<Parameter Name=""includePrerelease"" Type=""Edm.Boolean""/>
<Parameter Name=""includeAllVersions"" Type=""Edm.Boolean""/>
<Parameter Name=""targetFrameworks"" Type=""Edm.String""/>
<Parameter Name=""versionConstraints"" Type=""Edm.String""/>
</FunctionImport>
</EntityContainer>
</Schema>
</DataServices>
</Edmx>", destination: BodyDestinationFormat.SameAsSource);
            }
        }

        public class When_Searching_For_A_Package_With_Version_On_A_V3_Only_Feed : NugetListSpecsBase
        {
            private List<IPackageSearchMetadata> _result;

            public override void Context()
            {
                base.Context();

                AddV3OnlyIndexResponse("/endpoints/test-jsons/content/index.json");
                MockServer.Value.Given(Request.Create()
                        .WithPath(new ExactMatcher("/nuget/internal-choco/v3/search"))
                        .UsingMethod("GET")
                        .WithParam("q", new ExactMatcher("7zip"))
                        .WithParam("skip", new ExactMatcher("0"))
                        .WithParam("take", new ExactMatcher("30"))
                        .WithParam("semVerLevel", new ExactMatcher("2.0.0")))
                    .RespondWith(Response.Create()
                        .WithStatusCode(200)
                        .WithBody(@"{
      ""totalHits"": 2,
      ""data"": [
        {
          ""id"": ""7zip"",
          ""version"": ""23.1.0"",
          ""description"": ""7-Zip is a file archiver with a high compression ratio.\n\n## Features\n- High compression ratio in [7z format](http://www.7-zip.org/7z.html) with **LZMA** and **LZMA2** compression\n- Supported formats:\n- Packing / unpacking: 7z, XZ, BZIP2, GZIP, TAR, ZIP and WIM\n- Unpacking only: AR, ARJ, CAB, CHM, CPIO, CramFS, DMG, EXT, FAT, GPT, HFS, IHEX, ISO, LZH, LZMA, MBR, MSI, NSIS, NTFS, QCOW2, RAR, RPM, SquashFS, UDF, UEFI, VDI, VHD, VMDK, WIM, XAR and Z.\n- For ZIP and GZIP formats, **7-Zip** provides a compression ratio that is 2-10 % better than the ratio provided by PKZip and WinZip\n- Strong AES-256 encryption in 7z and ZIP formats\n- Self-extracting capability for 7z format\n- Integration with Windows Shell\n- Powerful File Manager\n- Powerful command line version\n- Plugin for FAR Manager\n- Localizations for 87 languages\n\n## Notes\n- The installer for 7-Zip is known to close the Explorer process. This means you may lose current work. If it doesn't automatically restart explorer, type `explorer` on the command shell to restart it.\n- **If the package is out of date please check [Version History](#versionhistory) for the latest submitted version. If you have a question, please ask it in [Chocolatey Community Package Discussions](https://github.com/chocolatey-community/chocolatey-packages/discussions) or raise an issue on the [Chocolatey Community Packages Repository](https://github.com/chocolatey-community/chocolatey-packages/issues) if you have problems with the package. Disqus comments will generally not be responded to.**"",
          ""versions"": [
            {
              ""version"": ""22.1"",
              ""downloads"": 1
            },
            {
              ""version"": ""23.1.0"",
              ""downloads"": 0
            }
          ],
          ""authors"": ""Igor Pavlov"",
          ""packageTypes"": [
            {
              ""name"": ""Dependency""
            }
          ],
          ""iconUrl"": ""https://cdn.jsdelivr.net/gh/chocolatey-community/chocolatey-packages@68b91a851cee97e55c748521aa6da6211dd37c98/icons/7zip.svg"",
          ""licenseUrl"": ""http://www.7-zip.org/license.txt"",
          ""owners"": [
            ""chocolatey-community"",
            ""Rob Reynolds""
          ],
          ""projectUrl"": ""http://www.7-zip.org/"",
          ""registration"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/registrations/7zip/index.json"",
          ""summary"": ""7-Zip is a file archiver with a high compression ratio."",
          ""tags"": [
            ""7zip"",
            ""zip"",
            ""archiver"",
            ""admin"",
            ""foss""
          ],
          ""title"": ""7-Zip"",
          ""totalDownloads"": 1
        },
        {
          ""id"": ""7zip.install"",
          ""version"": ""23.1.0"",
          ""description"": ""7-Zip is a file archiver with a high compression ratio.\n\n## Features\n\n- High compression ratio in [7z format](http://www.7-zip.org/7z.html) with **LZMA** and **LZMA2** compression\n- Supported formats:\n- Packing / unpacking: 7z, XZ, BZIP2, GZIP, TAR, ZIP and WIM\n- Unpacking only: AR, ARJ, CAB, CHM, CPIO, CramFS, DMG, EXT, FAT, GPT, HFS, IHEX, ISO, LZH, LZMA, MBR, MSI, NSIS, NTFS, QCOW2, RAR, RPM, SquashFS, UDF, UEFI, VDI, VHD, VMDK, WIM, XAR and Z.\n- For ZIP and GZIP formats, **7-Zip** provides a compression ratio that is 2-10 % better than the ratio provided by PKZip and WinZip\n- Strong AES-256 encryption in 7z and ZIP formats\n- Self-extracting capability for 7z format\n- Integration with Windows Shell\n- Powerful File Manager\n- Powerful command line version\n- Plugin for FAR Manager\n- Localizations for 87 languages\n\n## Notes\n- The installer for 7-Zip is known to close the Explorer process. This means you may lose current work. If it doesn't automatically restart explorer, type `explorer` on the command shell to restart it.\n- **If the package is out of date please check [Version History](#versionhistory) for the latest submitted version. If you have a question, please ask it in [Chocolatey Community Package Discussions](https://github.com/chocolatey-community/chocolatey-packages/discussions) or raise an issue on the [Chocolatey Community Packages Repository](https://github.com/chocolatey-community/chocolatey-packages/issues) if you have problems with the package. Disqus comments will generally not be responded to.**"",
          ""versions"": [
            {
              ""version"": ""22.1.0"",
              ""downloads"": 1
            },
            {
              ""version"": ""23.1.0"",
              ""downloads"": 0
            }
          ],
          ""authors"": ""Igor Pavlov"",
          ""packageTypes"": [
            {
              ""name"": ""Dependency""
            }
          ],
          ""iconUrl"": ""https://cdn.jsdelivr.net/gh/chocolatey-community/chocolatey-packages@68b91a851cee97e55c748521aa6da6211dd37c98/icons/7zip.svg"",
          ""licenseUrl"": ""http://www.7-zip.org/license.txt"",
          ""owners"": [
            ""chocolatey-community"",
            ""Rob Reynolds""
          ],
          ""projectUrl"": ""http://www.7-zip.org/"",
          ""registration"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/registrations/7zip.install/index.json"",
          ""summary"": ""7-Zip is a file archiver with a high compression ratio."",
          ""tags"": [
            ""7zip"",
            ""zip"",
            ""archiver"",
            ""admin"",
            ""cross-platform"",
            ""cli"",
            ""foss""
          ],
          ""title"": ""7-Zip (Install)"",
          ""totalDownloads"": 1
        }
      ]
    }", destination: BodyDestinationFormat.Json));
                AddSimpleResponse("/nuget/internal-choco/v3/registrations-gz/7zip/index.json", @"{
      ""count"": 1,
      ""items"": [
        {
          ""count"": 2,
          ""items"": [
            {
              ""@id"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/registrations-gz/7zip/22.1.json"",
              ""@type"": ""Package"",
              ""catalogEntry"": {
                ""@id"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/catalog/7zip/22.1.json"",
                ""@type"": ""PackageDetails"",
                ""authors"": ""Igor Pavlov"",
                ""dependencyGroups"": [
                  {
                    ""dependencies"": [
                      {
                        ""id"": ""7zip.install"",
                        ""range"": ""[22.1]""
                      }
                    ]
                  }
                ],
                ""description"": ""7-Zip is a file archiver with a high compression ratio.\n\n## Features\n- High compression ratio in [7z format](http://www.7-zip.org/7z.html) with **LZMA** and **LZMA2** compression\n- Supported formats:\n- Packing / unpacking: 7z, XZ, BZIP2, GZIP, TAR, ZIP and WIM\n- Unpacking only: AR, ARJ, CAB, CHM, CPIO, CramFS, DMG, EXT, FAT, GPT, HFS, IHEX, ISO, LZH, LZMA, MBR, MSI, NSIS, NTFS, QCOW2, RAR, RPM, SquashFS, UDF, UEFI, VDI, VHD, VMDK, WIM, XAR and Z.\n- For ZIP and GZIP formats, **7-Zip** provides a compression ratio that is 2-10 % better than the ratio provided by PKZip and WinZip\n- Strong AES-256 encryption in 7z and ZIP formats\n- Self-extracting capability for 7z format\n- Integration with Windows Shell\n- Powerful File Manager\n- Powerful command line version\n- Plugin for FAR Manager\n- Localizations for 87 languages\n\n## Notes\n- The installer for 7-Zip is known to close the Explorer process. This means you may lose current work. If it doesn't automatically restart explorer, type `explorer` on the command shell to restart it.\n- **If the package is out of date please check [Version History](#versionhistory) for the latest submitted version. If you have a question, please ask it in [Chocolatey Community Package Discussions](https://github.com/chocolatey-community/chocolatey-packages/discussions) or raise an issue on the [Chocolatey Community Packages Repository](https://github.com/chocolatey-community/chocolatey-packages/issues) if you have problems with the package. Disqus comments will generally not be responded to.**"",
                ""iconUrl"": ""https://cdn.jsdelivr.net/gh/chocolatey-community/chocolatey-packages@68b91a851cee97e55c748521aa6da6211dd37c98/icons/7zip.svg"",
                ""id"": ""7zip"",
                ""licenseUrl"": ""http://www.7-zip.org/license.txt"",
                ""projectUrl"": ""http://www.7-zip.org/"",
                ""published"": ""2023-05-08T15:25:36.157Z"",
                ""summary"": ""7-Zip is a file archiver with a high compression ratio."",
                ""tags"": [
                  ""7zip"",
                  ""zip"",
                  ""archiver"",
                  ""admin"",
                  ""foss""
                ],
                ""title"": ""7-Zip"",
                ""version"": ""22.1""
              },
              ""packageContent"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/flatcontainer/7zip/22.1/7zip.22.1.nupkg"",
              ""registration"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/registrations-gz/7zip/index.json""
            },
            {
              ""@id"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/registrations-gz/7zip/23.1.0.json"",
              ""@type"": ""Package"",
              ""catalogEntry"": {
                ""@id"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/catalog/7zip/23.1.0.json"",
                ""@type"": ""PackageDetails"",
                ""authors"": ""Igor Pavlov"",
                ""dependencyGroups"": [
                  {
                    ""dependencies"": [
                      {
                        ""id"": ""7zip.install"",
                        ""range"": ""[23.1.0]""
                      }
                    ]
                  }
                ],
                ""description"": ""7-Zip is a file archiver with a high compression ratio.\n\n## Features\n- High compression ratio in [7z format](http://www.7-zip.org/7z.html) with **LZMA** and **LZMA2** compression\n- Supported formats:\n- Packing / unpacking: 7z, XZ, BZIP2, GZIP, TAR, ZIP and WIM\n- Unpacking only: AR, ARJ, CAB, CHM, CPIO, CramFS, DMG, EXT, FAT, GPT, HFS, IHEX, ISO, LZH, LZMA, MBR, MSI, NSIS, NTFS, QCOW2, RAR, RPM, SquashFS, UDF, UEFI, VDI, VHD, VMDK, WIM, XAR and Z.\n- For ZIP and GZIP formats, **7-Zip** provides a compression ratio that is 2-10 % better than the ratio provided by PKZip and WinZip\n- Strong AES-256 encryption in 7z and ZIP formats\n- Self-extracting capability for 7z format\n- Integration with Windows Shell\n- Powerful File Manager\n- Powerful command line version\n- Plugin for FAR Manager\n- Localizations for 87 languages\n\n## Notes\n- The installer for 7-Zip is known to close the Explorer process. This means you may lose current work. If it doesn't automatically restart explorer, type `explorer` on the command shell to restart it.\n- **If the package is out of date please check [Version History](#versionhistory) for the latest submitted version. If you have a question, please ask it in [Chocolatey Community Package Discussions](https://github.com/chocolatey-community/chocolatey-packages/discussions) or raise an issue on the [Chocolatey Community Packages Repository](https://github.com/chocolatey-community/chocolatey-packages/issues) if you have problems with the package. Disqus comments will generally not be responded to.**"",
                ""iconUrl"": ""https://cdn.jsdelivr.net/gh/chocolatey-community/chocolatey-packages@68b91a851cee97e55c748521aa6da6211dd37c98/icons/7zip.svg"",
                ""id"": ""7zip"",
                ""licenseUrl"": ""http://www.7-zip.org/license.txt"",
                ""projectUrl"": ""http://www.7-zip.org/"",
                ""published"": ""2024-02-06T14:48:14.26Z"",
                ""summary"": ""7-Zip is a file archiver with a high compression ratio."",
                ""tags"": [
                  ""7zip"",
                  ""zip"",
                  ""archiver"",
                  ""admin"",
                  ""foss""
                ],
                ""title"": ""7-Zip"",
                ""version"": ""23.1.0""
              },
              ""packageContent"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/flatcontainer/7zip/23.1.0/7zip.23.1.0.nupkg"",
              ""registration"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/registrations-gz/7zip/index.json""
            }
          ],
          ""parent"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/registrations-gz/7zip/index.json"",
          ""lower"": ""22.1"",
          ""upper"": ""23.1.0""
        }
      ]
    }");
                AddSimpleResponse("/nuget/internal-choco/v3/registrations-gz/7zip.install/index.json", @"{
      ""count"": 1,
      ""items"": [
        {
          ""count"": 2,
          ""items"": [
            {
              ""@id"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/registrations-gz/7zip.install/22.1.0.json"",
              ""@type"": ""Package"",
              ""catalogEntry"": {
                ""@id"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/catalog/7zip.install/22.1.0.json"",
                ""@type"": ""PackageDetails"",
                ""authors"": ""Igor Pavlov"",
                ""dependencyGroups"": [
                  {
                    ""dependencies"": [
                      {
                        ""id"": ""chocolatey-core.extension"",
                        ""range"": ""1.3.3""
                      }
                    ]
                  }
                ],
                ""description"": ""7-Zip is a file archiver with a high compression ratio.\n\n## Features\n\n- High compression ratio in [7z format](http://www.7-zip.org/7z.html) with **LZMA** and **LZMA2** compression\n- Supported formats:\n- Packing / unpacking: 7z, XZ, BZIP2, GZIP, TAR, ZIP and WIM\n- Unpacking only: AR, ARJ, CAB, CHM, CPIO, CramFS, DMG, EXT, FAT, GPT, HFS, IHEX, ISO, LZH, LZMA, MBR, MSI, NSIS, NTFS, QCOW2, RAR, RPM, SquashFS, UDF, UEFI, VDI, VHD, VMDK, WIM, XAR and Z.\n- For ZIP and GZIP formats, **7-Zip** provides a compression ratio that is 2-10 % better than the ratio provided by PKZip and WinZip\n- Strong AES-256 encryption in 7z and ZIP formats\n- Self-extracting capability for 7z format\n- Integration with Windows Shell\n- Powerful File Manager\n- Powerful command line version\n- Plugin for FAR Manager\n- Localizations for 87 languages\n\n## Notes\n- The installer for 7-Zip is known to close the Explorer process. This means you may lose current work. If it doesn't automatically restart explorer, type `explorer` on the command shell to restart it.\n- **If the package is out of date please check [Version History](#versionhistory) for the latest submitted version. If you have a question, please ask it in [Chocolatey Community Package Discussions](https://github.com/chocolatey-community/chocolatey-packages/discussions) or raise an issue on the [Chocolatey Community Packages Repository](https://github.com/chocolatey-community/chocolatey-packages/issues) if you have problems with the package. Disqus comments will generally not be responded to.**"",
                ""iconUrl"": ""https://cdn.jsdelivr.net/gh/chocolatey-community/chocolatey-packages@68b91a851cee97e55c748521aa6da6211dd37c98/icons/7zip.svg"",
                ""id"": ""7zip.install"",
                ""licenseUrl"": ""http://www.7-zip.org/license.txt"",
                ""projectUrl"": ""http://www.7-zip.org/"",
                ""published"": ""2023-05-08T15:25:39.903Z"",
                ""summary"": ""7-Zip is a file archiver with a high compression ratio."",
                ""tags"": [
                  ""7zip"",
                  ""zip"",
                  ""archiver"",
                  ""admin"",
                  ""cross-platform"",
                  ""cli"",
                  ""foss""
                ],
                ""title"": ""7-Zip (Install)"",
                ""version"": ""22.1.0""
              },
              ""packageContent"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/flatcontainer/7zip.install/22.1.0/7zip.install.22.1.0.nupkg"",
              ""registration"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/registrations-gz/7zip.install/index.json""
            },
            {
              ""@id"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/registrations-gz/7zip.install/23.1.0.json"",
              ""@type"": ""Package"",
              ""catalogEntry"": {
                ""@id"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/catalog/7zip.install/23.1.0.json"",
                ""@type"": ""PackageDetails"",
                ""authors"": ""Igor Pavlov"",
                ""dependencyGroups"": [
                  {
                    ""dependencies"": [
                      {
                        ""id"": ""chocolatey-core.extension"",
                        ""range"": ""1.3.3""
                      }
                    ]
                  }
                ],
                ""description"": ""7-Zip is a file archiver with a high compression ratio.\n\n## Features\n\n- High compression ratio in [7z format](http://www.7-zip.org/7z.html) with **LZMA** and **LZMA2** compression\n- Supported formats:\n- Packing / unpacking: 7z, XZ, BZIP2, GZIP, TAR, ZIP and WIM\n- Unpacking only: AR, ARJ, CAB, CHM, CPIO, CramFS, DMG, EXT, FAT, GPT, HFS, IHEX, ISO, LZH, LZMA, MBR, MSI, NSIS, NTFS, QCOW2, RAR, RPM, SquashFS, UDF, UEFI, VDI, VHD, VMDK, WIM, XAR and Z.\n- For ZIP and GZIP formats, **7-Zip** provides a compression ratio that is 2-10 % better than the ratio provided by PKZip and WinZip\n- Strong AES-256 encryption in 7z and ZIP formats\n- Self-extracting capability for 7z format\n- Integration with Windows Shell\n- Powerful File Manager\n- Powerful command line version\n- Plugin for FAR Manager\n- Localizations for 87 languages\n\n## Notes\n- The installer for 7-Zip is known to close the Explorer process. This means you may lose current work. If it doesn't automatically restart explorer, type `explorer` on the command shell to restart it.\n- **If the package is out of date please check [Version History](#versionhistory) for the latest submitted version. If you have a question, please ask it in [Chocolatey Community Package Discussions](https://github.com/chocolatey-community/chocolatey-packages/discussions) or raise an issue on the [Chocolatey Community Packages Repository](https://github.com/chocolatey-community/chocolatey-packages/issues) if you have problems with the package. Disqus comments will generally not be responded to.**"",
                ""iconUrl"": ""https://cdn.jsdelivr.net/gh/chocolatey-community/chocolatey-packages@68b91a851cee97e55c748521aa6da6211dd37c98/icons/7zip.svg"",
                ""id"": ""7zip.install"",
                ""licenseUrl"": ""http://www.7-zip.org/license.txt"",
                ""projectUrl"": ""http://www.7-zip.org/"",
                ""published"": ""2024-02-06T14:48:19.44Z"",
                ""summary"": ""7-Zip is a file archiver with a high compression ratio."",
                ""tags"": [
                  ""7zip"",
                  ""zip"",
                  ""archiver"",
                  ""admin"",
                  ""cross-platform"",
                  ""cli"",
                  ""foss""
                ],
                ""title"": ""7-Zip (Install)"",
                ""version"": ""23.1.0""
              },
              ""packageContent"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/flatcontainer/7zip.install/23.1.0/7zip.install.23.1.0.nupkg"",
              ""registration"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/registrations-gz/7zip.install/index.json""
            }
          ],
          ""parent"": """ + MockServer.Value.Url + @"/nuget/internal-choco/v3/registrations-gz/7zip.install/index.json"",
          ""lower"": ""22.1.0"",
          ""upper"": ""23.1.0""
        }
      ]
    }");

                Configuration.Sources = $"{MockServer.Value.Url}/endpoints/test-jsons/content/index.json";
                Configuration.Input = "7zip";
                Configuration.Version = "22.1.0";
                Configuration.SourceCommand.Username = "kim";
                Configuration.SourceCommand.Password = "P@ssword123";
            }

            public override void Because()
            {
                _result = NugetList.GetPackages(Configuration, Logger, FileSystem.Object).ToList();
            }

            [Fact]
            public void Should_Have_Found_Two_Packages()
            {
                _result.Should().HaveCount(2);
            }

            [InlineData("7zip", "22.1.0")]
            [InlineData("7zip.install", "22.1.0")]
            public void Should_Contain_Expected_Package(string id, string version)
            {
                _result.Should()
                    .ContainSingle(c => c.Identity.Id == id && c.Identity.Version.ToNormalizedString() == version);
            }

            [InlineData("/endpoints/test-jsons/content/index.json")]
            [InlineData("/nuget/internal-choco/v3/search?q=7zip&skip=0&take=30&prerelease=false&semVerLevel=2.0.0")]
            [InlineData("/nuget/internal-choco/v3/registrations-gz/7zip/index.json")]
            [InlineData("/nuget/internal-choco/v3/registrations-gz/7zip.install/index.json")]
            public void Should_Have_Called_Expected_Paths(string path)
            {
                MockServer.Value.Should()
                    .HaveReceivedACall()
                    .AtUrl(MockServer.Value.Url + path)
                    .And.UsingMethod("GET");
            }
        }

        public class When_Searching_For_A_Package_With_Version_On_A_Combined_Feed : NugetListSpecsBase
        {
            private List<IPackageSearchMetadata> _result;

            public override void Context()
            {
                base.Context();

                AddV3IndexResponse("/nuget/internal-choco/v3/index.json");
                MockServer.Value.Given(Request.Create()
                        .WithPath(new ExactMatcher("/nuget/internal-choco/Search()"))
                        .UsingGet()
                        .WithParam("$orderby",
                            new ExactMatcher("Id"),
                            new ExactMatcher("Version desc"))
                        .WithParam("searchTerm", new ExactMatcher("'7zip'"))
                        .WithParam("$skip", new ExactMatcher("0"))
                        .WithParam("$top", new ExactMatcher("30"))
                        .WithParam("semVerLevel", new ExactMatcher("2.0.0")))
                    .RespondWith(Response.Create()
                        .WithStatusCode(200)
                        .WithBody(@"<?xml version=""1.0"" encoding=""utf-8""?>
<feed xml:base=""" + MockServer.Value.Url + @"/nuget/internal-choco/""
    xmlns:d=""http://schemas.microsoft.com/ado/2007/08/dataservices""
    xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata""
    xmlns=""http://www.w3.org/2005/Atom"">
    <title type=""text"">Packages</title>
    <id>" + MockServer.Value.Url + @"/nuget/internal-choco/Search()/</id>
    <updated>2024-11-19T13:38:35Z</updated>
    <link rel=""self"" title=""Search"" href=""Search"" />
    <entry>
        <id>" + MockServer.Value.Url + @"/nuget/internal-choco/Packages(Id='7zip',Version='23.1.0')</id>
        <title type=""text"">7zip</title>
        <summary type=""text"">7-Zip is a file archiver with a high compression ratio.</summary>
        <updated>2024-02-06T14:48:14Z</updated>
        <author>
            <name>Igor Pavlov</name>
        </author>
        <link rel=""edit-media"" title=""Package"" href=""Packages(Id='7zip',Version='23.1.0')/$value"" />
        <link rel=""edit"" title=""Package"" href=""Packages(Id='7zip',Version='23.1.0')"" />
        <category term=""NuGet.Server.DataServices.Package"" scheme=""http://schemas.microsoft.com/ado/2007/08/dataservices/scheme"" />
        <content type=""application/zip"" src=""" + MockServer.Value.Url + @"/nuget/internal-choco/package/7zip/23.1.0"" />
        <m:properties
            xmlns:d=""http://schemas.microsoft.com/ado/2007/08/dataservices""
            xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"">
            <d:Version>23.1.0</d:Version>
            <d:Title>7-Zip</d:Title>
            <d:RequireLicenseAcceptance m:type=""Edm.Boolean"">false</d:RequireLicenseAcceptance>
            <d:Description>7-Zip is a file archiver with a high compression ratio.

## Features
- High compression ratio in [7z format](http://www.7-zip.org/7z.html) with **LZMA** and **LZMA2** compression
- Supported formats:
- Packing / unpacking: 7z, XZ, BZIP2, GZIP, TAR, ZIP and WIM
- Unpacking only: AR, ARJ, CAB, CHM, CPIO, CramFS, DMG, EXT, FAT, GPT, HFS, IHEX, ISO, LZH, LZMA, MBR, MSI, NSIS, NTFS, QCOW2, RAR, RPM, SquashFS, UDF, UEFI, VDI, VHD, VMDK, WIM, XAR and Z.
- For ZIP and GZIP formats, **7-Zip** provides a compression ratio that is 2-10 % better than the ratio provided by PKZip and WinZip
- Strong AES-256 encryption in 7z and ZIP formats
- Self-extracting capability for 7z format
- Integration with Windows Shell
- Powerful File Manager
- Powerful command line version
- Plugin for FAR Manager
- Localizations for 87 languages

## Notes
- The installer for 7-Zip is known to close the Explorer process. This means you may lose current work. If it doesn't automatically restart explorer, type `explorer` on the command shell to restart it.
- **If the package is out of date please check [Version History](#versionhistory) for the latest submitted version. If you have a question, please ask it in [Chocolatey Community Package Discussions](https://github.com/chocolatey-community/chocolatey-packages/discussions) or raise an issue on the [Chocolatey Community Packages Repository](https://github.com/chocolatey-community/chocolatey-packages/issues) if you have problems with the package. Disqus comments will generally not be responded to.**</d:Description>
            <d:ReleaseNotes>http://www.7-zip.org/history.txt</d:ReleaseNotes>
            <d:Summary>7-Zip is a file archiver with a high compression ratio.</d:Summary>
            <d:ProjectUrl>http://www.7-zip.org/</d:ProjectUrl>
            <d:Icon></d:Icon>
            <d:IconUrl>https://cdn.jsdelivr.net/gh/chocolatey-community/chocolatey-packages@68b91a851cee97e55c748521aa6da6211dd37c98/icons/7zip.svg</d:IconUrl>
            <d:LicenseExpression></d:LicenseExpression>
            <d:LicenseUrl>http://www.7-zip.org/license.txt</d:LicenseUrl>
            <d:Copyright></d:Copyright>
            <d:Tags>7zip zip archiver admin foss</d:Tags>
            <d:Dependencies>7zip.install:[23.1.0]</d:Dependencies>
            <d:IsLocalPackage m:type=""Edm.Boolean"">true</d:IsLocalPackage>
            <d:Created m:type=""Edm.DateTime"">2024-02-06T14:48:14.2600000Z</d:Created>
            <d:Published m:type=""Edm.DateTime"">2024-02-06T14:48:14.2600000Z</d:Published>
            <d:PackageSize m:type=""Edm.Int64"">3608</d:PackageSize>
            <d:PackageHash>hsYlJkAOzwVQ8+/hVjxaVkV6obzPflj9p3GJVX1B5KOGfKCOMZf0r/GuLYCFeNFXdQG0Og3zXAv6Sl5K+S54HQ==</d:PackageHash>
            <d:IsLatestVersion m:type=""Edm.Boolean"">true</d:IsLatestVersion>
            <d:IsAbsoluteLatestVersion m:type=""Edm.Boolean"">true</d:IsAbsoluteLatestVersion>
            <d:IsProGetHosted m:type=""Edm.Boolean"">true</d:IsProGetHosted>
            <d:IsPrerelease m:type=""Edm.Boolean"">false</d:IsPrerelease>
            <d:IsCached m:type=""Edm.Boolean"">false</d:IsCached>
            <d:NormalizedVersion>23.1.0</d:NormalizedVersion>
            <d:Listed m:type=""Edm.Boolean"">true</d:Listed>
            <d:PackageHashAlgorithm>SHA512</d:PackageHashAlgorithm>
            <d:HasSymbols m:type=""Edm.Boolean"">false</d:HasSymbols>
            <d:HasSource m:type=""Edm.Boolean"">false</d:HasSource>
            <d:DownloadCount m:type=""Edm.Int32"">0</d:DownloadCount>
            <d:VersionDownloadCount m:type=""Edm.Int32"">0</d:VersionDownloadCount>
        </m:properties>
    </entry>
    <entry>
        <id>" + MockServer.Value.Url + @"/nuget/internal-choco/Packages(Id='7zip',Version='22.1')</id>
        <title type=""text"">7zip</title>
        <summary type=""text"">7-Zip is a file archiver with a high compression ratio.</summary>
        <updated>2023-05-08T15:25:36Z</updated>
        <author>
            <name>Igor Pavlov</name>
        </author>
        <link rel=""edit-media"" title=""Package"" href=""Packages(Id='7zip',Version='22.1')/$value"" />
        <link rel=""edit"" title=""Package"" href=""Packages(Id='7zip',Version='22.1')"" />
        <category term=""NuGet.Server.DataServices.Package"" scheme=""http://schemas.microsoft.com/ado/2007/08/dataservices/scheme"" />
        <content type=""application/zip"" src=""" + MockServer.Value.Url + @"/nuget/internal-choco/package/7zip/22.1"" />
        <m:properties
            xmlns:d=""http://schemas.microsoft.com/ado/2007/08/dataservices""
            xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"">
            <d:Version>22.1</d:Version>
            <d:Title>7-Zip</d:Title>
            <d:RequireLicenseAcceptance m:type=""Edm.Boolean"">false</d:RequireLicenseAcceptance>
            <d:Description>7-Zip is a file archiver with a high compression ratio.

## Features
- High compression ratio in [7z format](http://www.7-zip.org/7z.html) with **LZMA** and **LZMA2** compression
- Supported formats:
- Packing / unpacking: 7z, XZ, BZIP2, GZIP, TAR, ZIP and WIM
- Unpacking only: AR, ARJ, CAB, CHM, CPIO, CramFS, DMG, EXT, FAT, GPT, HFS, IHEX, ISO, LZH, LZMA, MBR, MSI, NSIS, NTFS, QCOW2, RAR, RPM, SquashFS, UDF, UEFI, VDI, VHD, VMDK, WIM, XAR and Z.
- For ZIP and GZIP formats, **7-Zip** provides a compression ratio that is 2-10 % better than the ratio provided by PKZip and WinZip
- Strong AES-256 encryption in 7z and ZIP formats
- Self-extracting capability for 7z format
- Integration with Windows Shell
- Powerful File Manager
- Powerful command line version
- Plugin for FAR Manager
- Localizations for 87 languages

## Notes
- The installer for 7-Zip is known to close the Explorer process. This means you may lose current work. If it doesn't automatically restart explorer, type `explorer` on the command shell to restart it.
- **If the package is out of date please check [Version History](#versionhistory) for the latest submitted version. If you have a question, please ask it in [Chocolatey Community Package Discussions](https://github.com/chocolatey-community/chocolatey-packages/discussions) or raise an issue on the [Chocolatey Community Packages Repository](https://github.com/chocolatey-community/chocolatey-packages/issues) if you have problems with the package. Disqus comments will generally not be responded to.**</d:Description>
            <d:ReleaseNotes>http://www.7-zip.org/history.txt</d:ReleaseNotes>
            <d:Summary>7-Zip is a file archiver with a high compression ratio.</d:Summary>
            <d:ProjectUrl>http://www.7-zip.org/</d:ProjectUrl>
            <d:Icon></d:Icon>
            <d:IconUrl>https://cdn.jsdelivr.net/gh/chocolatey-community/chocolatey-packages@68b91a851cee97e55c748521aa6da6211dd37c98/icons/7zip.svg</d:IconUrl>
            <d:LicenseExpression></d:LicenseExpression>
            <d:LicenseUrl>http://www.7-zip.org/license.txt</d:LicenseUrl>
            <d:Copyright></d:Copyright>
            <d:Tags>7zip zip archiver admin foss</d:Tags>
            <d:Dependencies>7zip.install:[22.1]</d:Dependencies>
            <d:IsLocalPackage m:type=""Edm.Boolean"">true</d:IsLocalPackage>
            <d:Created m:type=""Edm.DateTime"">2023-05-08T15:25:36.1570000Z</d:Created>
            <d:Published m:type=""Edm.DateTime"">2023-05-08T15:25:36.1570000Z</d:Published>
            <d:PackageSize m:type=""Edm.Int64"">5112</d:PackageSize>
            <d:PackageHash>NX/wvBxlO66YVGIAnop8TkSxcIptt7on/33AfkudbP+u9cjgrsxV+YAZxK5yMD0hx8O0BJjjU3MVbv+70EO6lw==</d:PackageHash>
            <d:IsLatestVersion m:type=""Edm.Boolean"">false</d:IsLatestVersion>
            <d:IsAbsoluteLatestVersion m:type=""Edm.Boolean"">false</d:IsAbsoluteLatestVersion>
            <d:IsProGetHosted m:type=""Edm.Boolean"">true</d:IsProGetHosted>
            <d:IsPrerelease m:type=""Edm.Boolean"">false</d:IsPrerelease>
            <d:IsCached m:type=""Edm.Boolean"">false</d:IsCached>
            <d:NormalizedVersion>22.1</d:NormalizedVersion>
            <d:Listed m:type=""Edm.Boolean"">true</d:Listed>
            <d:PackageHashAlgorithm>SHA512</d:PackageHashAlgorithm>
            <d:HasSymbols m:type=""Edm.Boolean"">false</d:HasSymbols>
            <d:HasSource m:type=""Edm.Boolean"">false</d:HasSource>
            <d:DownloadCount m:type=""Edm.Int32"">1</d:DownloadCount>
            <d:VersionDownloadCount m:type=""Edm.Int32"">1</d:VersionDownloadCount>
        </m:properties>
    </entry>
    <entry>
        <id>" + MockServer.Value.Url + @"/nuget/internal-choco/Packages(Id='7zip.install',Version='23.1.0')</id>
        <title type=""text"">7zip.install</title>
        <summary type=""text"">7-Zip is a file archiver with a high compression ratio.</summary>
        <updated>2024-02-06T14:48:19Z</updated>
        <author>
            <name>Igor Pavlov</name>
        </author>
        <link rel=""edit-media"" title=""Package"" href=""Packages(Id='7zip.install',Version='23.1.0')/$value"" />
        <link rel=""edit"" title=""Package"" href=""Packages(Id='7zip.install',Version='23.1.0')"" />
        <category term=""NuGet.Server.DataServices.Package"" scheme=""http://schemas.microsoft.com/ado/2007/08/dataservices/scheme"" />
        <content type=""application/zip"" src=""" + MockServer.Value.Url + @"/nuget/internal-choco/package/7zip.install/23.1.0"" />
        <m:properties
            xmlns:d=""http://schemas.microsoft.com/ado/2007/08/dataservices""
            xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"">
            <d:Version>23.1.0</d:Version>
            <d:Title>7-Zip (Install)</d:Title>
            <d:RequireLicenseAcceptance m:type=""Edm.Boolean"">false</d:RequireLicenseAcceptance>
            <d:Description>7-Zip is a file archiver with a high compression ratio.

## Features

- High compression ratio in [7z format](http://www.7-zip.org/7z.html) with **LZMA** and **LZMA2** compression
- Supported formats:
- Packing / unpacking: 7z, XZ, BZIP2, GZIP, TAR, ZIP and WIM
- Unpacking only: AR, ARJ, CAB, CHM, CPIO, CramFS, DMG, EXT, FAT, GPT, HFS, IHEX, ISO, LZH, LZMA, MBR, MSI, NSIS, NTFS, QCOW2, RAR, RPM, SquashFS, UDF, UEFI, VDI, VHD, VMDK, WIM, XAR and Z.
- For ZIP and GZIP formats, **7-Zip** provides a compression ratio that is 2-10 % better than the ratio provided by PKZip and WinZip
- Strong AES-256 encryption in 7z and ZIP formats
- Self-extracting capability for 7z format
- Integration with Windows Shell
- Powerful File Manager
- Powerful command line version
- Plugin for FAR Manager
- Localizations for 87 languages

## Notes
- The installer for 7-Zip is known to close the Explorer process. This means you may lose current work. If it doesn't automatically restart explorer, type `explorer` on the command shell to restart it.
- **If the package is out of date please check [Version History](#versionhistory) for the latest submitted version. If you have a question, please ask it in [Chocolatey Community Package Discussions](https://github.com/chocolatey-community/chocolatey-packages/discussions) or raise an issue on the [Chocolatey Community Packages Repository](https://github.com/chocolatey-community/chocolatey-packages/issues) if you have problems with the package. Disqus comments will generally not be responded to.**</d:Description>
            <d:ReleaseNotes>[Software Changelog](http://www.7-zip.org/history.txt)
[Package Changelog](https://github.com/chocolatey-community/chocolatey-coreteampackages/blob/master/automatic/7zip.install/Changelog.md)</d:ReleaseNotes>
            <d:Summary>7-Zip is a file archiver with a high compression ratio.</d:Summary>
            <d:ProjectUrl>http://www.7-zip.org/</d:ProjectUrl>
            <d:Icon></d:Icon>
            <d:IconUrl>https://cdn.jsdelivr.net/gh/chocolatey-community/chocolatey-packages@68b91a851cee97e55c748521aa6da6211dd37c98/icons/7zip.svg</d:IconUrl>
            <d:LicenseExpression></d:LicenseExpression>
            <d:LicenseUrl>http://www.7-zip.org/license.txt</d:LicenseUrl>
            <d:Copyright></d:Copyright>
            <d:Tags>7zip zip archiver admin cross-platform cli foss</d:Tags>
            <d:Dependencies>chocolatey-core.extension:1.3.3</d:Dependencies>
            <d:IsLocalPackage m:type=""Edm.Boolean"">true</d:IsLocalPackage>
            <d:Created m:type=""Edm.DateTime"">2024-02-06T14:48:19.4400000Z</d:Created>
            <d:Published m:type=""Edm.DateTime"">2024-02-06T14:48:19.4400000Z</d:Published>
            <d:PackageSize m:type=""Edm.Int64"">2867604</d:PackageSize>
            <d:PackageHash>fYqj9nY0NqGqYuyuov5AbdZ0JHC0hBwnNiBOPRNBOo1sFCOibU3ZYlBc1aleh6I3T0gCagUti9wQLl0bfAFyJg==</d:PackageHash>
            <d:IsLatestVersion m:type=""Edm.Boolean"">true</d:IsLatestVersion>
            <d:IsAbsoluteLatestVersion m:type=""Edm.Boolean"">true</d:IsAbsoluteLatestVersion>
            <d:IsProGetHosted m:type=""Edm.Boolean"">true</d:IsProGetHosted>
            <d:IsPrerelease m:type=""Edm.Boolean"">false</d:IsPrerelease>
            <d:IsCached m:type=""Edm.Boolean"">false</d:IsCached>
            <d:NormalizedVersion>23.1.0</d:NormalizedVersion>
            <d:Listed m:type=""Edm.Boolean"">true</d:Listed>
            <d:PackageHashAlgorithm>SHA512</d:PackageHashAlgorithm>
            <d:HasSymbols m:type=""Edm.Boolean"">false</d:HasSymbols>
            <d:HasSource m:type=""Edm.Boolean"">false</d:HasSource>
            <d:DownloadCount m:type=""Edm.Int32"">0</d:DownloadCount>
            <d:VersionDownloadCount m:type=""Edm.Int32"">0</d:VersionDownloadCount>
        </m:properties>
    </entry>
    <entry>
        <id>" + MockServer.Value.Url + @"/nuget/internal-choco/Packages(Id='7zip.install',Version='22.1.0')</id>
        <title type=""text"">7zip.install</title>
        <summary type=""text"">7-Zip is a file archiver with a high compression ratio.</summary>
        <updated>2023-05-08T15:25:39Z</updated>
        <author>
            <name>Igor Pavlov</name>
        </author>
        <link rel=""edit-media"" title=""Package"" href=""Packages(Id='7zip.install',Version='22.1.0')/$value"" />
        <link rel=""edit"" title=""Package"" href=""Packages(Id='7zip.install',Version='22.1.0')"" />
        <category term=""NuGet.Server.DataServices.Package"" scheme=""http://schemas.microsoft.com/ado/2007/08/dataservices/scheme"" />
        <content type=""application/zip"" src=""" + MockServer.Value.Url + @"/nuget/internal-choco/package/7zip.install/22.1.0"" />
        <m:properties
            xmlns:d=""http://schemas.microsoft.com/ado/2007/08/dataservices""
            xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"">
            <d:Version>22.1.0</d:Version>
            <d:Title>7-Zip (Install)</d:Title>
            <d:RequireLicenseAcceptance m:type=""Edm.Boolean"">false</d:RequireLicenseAcceptance>
            <d:Description>7-Zip is a file archiver with a high compression ratio.

## Features

- High compression ratio in [7z format](http://www.7-zip.org/7z.html) with **LZMA** and **LZMA2** compression
- Supported formats:
- Packing / unpacking: 7z, XZ, BZIP2, GZIP, TAR, ZIP and WIM
- Unpacking only: AR, ARJ, CAB, CHM, CPIO, CramFS, DMG, EXT, FAT, GPT, HFS, IHEX, ISO, LZH, LZMA, MBR, MSI, NSIS, NTFS, QCOW2, RAR, RPM, SquashFS, UDF, UEFI, VDI, VHD, VMDK, WIM, XAR and Z.
- For ZIP and GZIP formats, **7-Zip** provides a compression ratio that is 2-10 % better than the ratio provided by PKZip and WinZip
- Strong AES-256 encryption in 7z and ZIP formats
- Self-extracting capability for 7z format
- Integration with Windows Shell
- Powerful File Manager
- Powerful command line version
- Plugin for FAR Manager
- Localizations for 87 languages

## Notes
- The installer for 7-Zip is known to close the Explorer process. This means you may lose current work. If it doesn't automatically restart explorer, type `explorer` on the command shell to restart it.
- **If the package is out of date please check [Version History](#versionhistory) for the latest submitted version. If you have a question, please ask it in [Chocolatey Community Package Discussions](https://github.com/chocolatey-community/chocolatey-packages/discussions) or raise an issue on the [Chocolatey Community Packages Repository](https://github.com/chocolatey-community/chocolatey-packages/issues) if you have problems with the package. Disqus comments will generally not be responded to.**</d:Description>
            <d:ReleaseNotes>[Software Changelog](http://www.7-zip.org/history.txt)
[Package Changelog](https://github.com/chocolatey-community/chocolatey-coreteampackages/blob/master/automatic/7zip.install/Changelog.md)</d:ReleaseNotes>
            <d:Summary>7-Zip is a file archiver with a high compression ratio.</d:Summary>
            <d:ProjectUrl>http://www.7-zip.org/</d:ProjectUrl>
            <d:Icon></d:Icon>
            <d:IconUrl>https://cdn.jsdelivr.net/gh/chocolatey-community/chocolatey-packages@68b91a851cee97e55c748521aa6da6211dd37c98/icons/7zip.svg</d:IconUrl>
            <d:LicenseExpression></d:LicenseExpression>
            <d:LicenseUrl>http://www.7-zip.org/license.txt</d:LicenseUrl>
            <d:Copyright></d:Copyright>
            <d:Tags>7zip zip archiver admin cross-platform cli foss</d:Tags>
            <d:Dependencies>chocolatey-core.extension:1.3.3</d:Dependencies>
            <d:IsLocalPackage m:type=""Edm.Boolean"">true</d:IsLocalPackage>
            <d:Created m:type=""Edm.DateTime"">2023-05-08T15:25:39.9030000Z</d:Created>
            <d:Published m:type=""Edm.DateTime"">2023-05-08T15:25:39.9030000Z</d:Published>
            <d:PackageSize m:type=""Edm.Int64"">2842805</d:PackageSize>
            <d:PackageHash>KMGAfQdcR3Vmk8fun0XoOfH2ySi5Et18mhgvEHVpWKFVBERnxDyBvqmdl2zCdiScjgvrWD+0KWZdUZGxGGuEqg==</d:PackageHash>
            <d:IsLatestVersion m:type=""Edm.Boolean"">false</d:IsLatestVersion>
            <d:IsAbsoluteLatestVersion m:type=""Edm.Boolean"">false</d:IsAbsoluteLatestVersion>
            <d:IsProGetHosted m:type=""Edm.Boolean"">true</d:IsProGetHosted>
            <d:IsPrerelease m:type=""Edm.Boolean"">false</d:IsPrerelease>
            <d:IsCached m:type=""Edm.Boolean"">false</d:IsCached>
            <d:NormalizedVersion>22.1.0</d:NormalizedVersion>
            <d:Listed m:type=""Edm.Boolean"">true</d:Listed>
            <d:PackageHashAlgorithm>SHA512</d:PackageHashAlgorithm>
            <d:HasSymbols m:type=""Edm.Boolean"">false</d:HasSymbols>
            <d:HasSource m:type=""Edm.Boolean"">false</d:HasSource>
            <d:DownloadCount m:type=""Edm.Int32"">1</d:DownloadCount>
            <d:VersionDownloadCount m:type=""Edm.Int32"">1</d:VersionDownloadCount>
        </m:properties>
    </entry>
</feed>", destination: BodyDestinationFormat.SameAsSource));

                Configuration.Sources = $"{MockServer.Value.Url}/nuget/internal-choco/v3/index.json";
                Configuration.Input = "7zip";
                Configuration.Version = "22.1.0";
                Configuration.SourceCommand.Username = "kim";
                Configuration.SourceCommand.Password = "P@ssword123";
            }

            public override void Because()
            {
                _result = NugetList.GetPackages(Configuration, Logger, FileSystem.Object).ToList();
            }

            [Fact]
            public void Should_Have_Found_Two_Packages()
            {
                _result.Should().HaveCount(2);
            }

            [InlineData("7zip", "22.1.0")]
            [InlineData("7zip.install", "22.1.0")]
            public void Should_Contain_Expected_Package(string id, string version)
            {
                _result.Should()
                    .ContainSingle(c => c.Identity.Id == id && c.Identity.Version.ToNormalizedString() == version);
            }

            [InlineData("/nuget/internal-choco/v3/index.json")]
            [InlineData("/nuget/internal-choco/")]
            [InlineData("/nuget/internal-choco/$metadata")]
            [InlineData("/nuget/internal-choco/Search()?$orderby=Id,Version desc&searchTerm='7zip'&targetFramework=''&includePrerelease=false&$skip=0&$top=30&semVerLevel=2.0.0")]
            public void Should_Have_Called_Expected_Paths(string path)
            {
                MockServer.Value.Should()
                    .HaveReceivedACall()
                    .AtUrl(MockServer.Value.Url + path)
                    .And.UsingMethod("GET");
            }
        }
    }
}
