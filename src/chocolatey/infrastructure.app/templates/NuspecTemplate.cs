// Copyright © 2011 - Present RealDimensions Software, LLC
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

namespace chocolatey.infrastructure.app.templates
{
    public class NuspecTemplate
    {
        public static string Template =
            @"<?xml version=""1.0"" encoding=""utf-8""?>
<!-- Do not remove this test for UTF-8: if “Ω” doesn’t appear as greek uppercase omega letter enclosed in quotation marks, you should use an editor that supports UTF-8, not this one. -->
<package xmlns=""http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd"">
  <metadata>
    <!-- Read this before publishing packages to chocolatey.org: https://github.com/chocolatey/chocolatey/wiki/CreatePackages -->
    <id>[[PackageNameLower]]</id>
    <title>[[PackageName]] (Install)</title>
    <version>[[PackageVersion]]</version>
    <authors>__REPLACE_AUTHORS_OF_SOFTWARE_COMMA_SEPARATED__</authors>
    <owners>[[MaintainerName]]</owners>
    <summary>__REPLACE__</summary>
    <description>__REPLACE__MarkDown_Okay [[AutomaticPackageNotesNuspec]]
    </description>
    <projectUrl></projectUrl>
    <packageSourceUrl></packageSourceUrl>
    <!--<projectSourceUrl></projectSourceUrl>
    <docsUrl></docsUrl>
    <mailingListUrl></mailingListUrl>
    <bugTrackerUrl></bugTrackerUrl>-->
    <tags>[[PackageNameLower]] admin SPACE_SEPARATED</tags>
    <copyright></copyright>
    <licenseUrl></licenseUrl>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <!--<iconUrl>http://cdn.rawgit.com/[[MaintainerRepo]]/master/icons/[[PackageNameLower]].png</iconUrl>-->
    <!--<dependencies>
      <dependency id="""" version=""__VERSION__"" />
      <dependency id="""" />
    </dependencies>-->
    <releaseNotes>__REPLACE_OR_REMOVE__MarkDown_Okay</releaseNotes>
    <!--<provides></provides>-->
  </metadata>
  <files>
    <file src=""tools\**"" target=""tools"" />
  </files>
</package>
";

        public static string AutomaticPackageNotes =
            @"


**Please Note**: This is an automatically updated package. If you find it is 
out of date by more than a day or two, please contact the maintainer(s) and
let them know the package is no longer updating correctly.
";
    }
}