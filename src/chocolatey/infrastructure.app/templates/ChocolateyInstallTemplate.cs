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
    public class ChocolateyInstallTemplate
    {
        public static string Template =
            @"#NOTE: Please remove any commented lines to tidy up prior to releasing the package, including this one

[[AutomaticPackageNotesInstaller]]
$packageName = '[[PackageName]]' # arbitrary name for the package, used in messages
$registryUninstallerKeyName = '[[PackageName]]' #ensure this is the value in the registry
$installerType = '[[InstallerType]]' #only one of these: exe, msi, msu
$url = '[[Url]]' # download url
$url64 = '[[Url64]]' # 64bit URL here or remove - if installer decides, then use $url
$silentArgs = '[[SilentArgs]]' # ""/s /S /q /Q /quiet /silent /SILENT /VERYSILENT"" # try any of these to get the silent installer #msi is always /quiet
$validExitCodes = @(0) #please insert other valid exit codes here, exit codes for ms http://msdn.microsoft.com/en-us/library/aa368542(VS.85).aspx
$toolsDir = ""$(Split-Path -parent $MyInvocation.MyCommand.Definition)""

# main helper functions - these have error handling tucked into them already
# see https://github.com/chocolatey/choco/wiki/HelpersReference
# Install an application, will assert administrative rights
# add additional optional arguments as necessary
#Install-ChocolateyPackage ""$packageName"" ""$installerType"" ""$silentArgs"" ""$url"" [""$url64""  -validExitCodes $validExitCodes -checksum $checksum -checksumType $checksumType -checksum64 $checksum64 -checksumType64 $checksumType64]
Install-ChocolateyPackage ""$packageName"" ""$installerType"" ""$silentArgs"" ""$url""
# Download and unpack a zip file
#Install-ChocolateyZipPackage ""$packageName"" ""$url"" ""$toolsDir"" [""$url64"" -checksum $checksum -checksumType $checksumType -checksum64 $checksum64 -checksumType64 $checksumType64]
Install-ChocolateyZipPackage ""$packageName"" ""$url"" ""$toolsDir""
# Install Visual Studio Package
#Install-ChocolateyVsixPackage ""$packageName"" ""$url"" [$vsVersion] [-checksum $checksum -checksumType $checksumType]
Install-ChocolateyVsixPackage ""$packageName"" ""$url""

# see the full list at https://github.com/chocolatey/choco/wiki/HelpersReference
# downloader that the main helpers use to download items
# if removing $url64, please remove from here
#Get-ChocolateyWebFile ""$packageName"" 'DOWNLOAD_TO_FILE_FULL_PATH' ""$url"" ""$url64""
# installer, will assert administrative rights - used by Install-ChocolateyPackage
#Install-ChocolateyInstallPackage ""$packageName"" ""$installerType"" ""$silentArgs"" '_FULLFILEPATH_' -validExitCodes $validExitCodes
# unzips a file to the specified location - auto overwrites existing content
#Get-ChocolateyUnzip ""FULL_LOCATION_TO_ZIP.zip"" ""$toolsDir""
# Runs processes asserting UAC, will assert administrative rights - used by Install-ChocolateyInstallPackage
#Start-ChocolateyProcessAsAdmin 'STATEMENTS_TO_RUN' 'Optional_Application_If_Not_PowerShell' -validExitCodes $validExitCodes
# add specific folders to the path - any executables found in the chocolatey package folder will already be on the path. This is used in addition to that or for cases when a native installer doesn't add things to the path.
#Install-ChocolateyPath 'LOCATION_TO_ADD_TO_PATH' 'User_OR_Machine' # Machine will assert administrative rights
# add specific files as shortcuts to the desktop
#$target = Join-Path $toolsDir ""$($packageName).exe""
#Install-ChocolateyDesktopLink $target
# outputs the bitness of the OS (either ""32"" or ""64"")
#$osBitness = Get-ProcessorBits

# PORTABLE EXAMPLE
#$binRoot = Get-BinRoot
#$installDir = Join-Path $binRoot ""$packageName""
#Write-Host ""Adding `'$installDir`' to the path and the current shell path""
#Install-ChocolateyPath ""$installDir""
#$env:Path = ""$($env:Path);$installDir""

# if removing $url64, please remove from here
# despite the name ""Install-ChocolateyZipPackage"" this also works with 7z archives
Install-ChocolateyZipPackage ""$packageName"" ""$url"" ""$installDir"" ""$url64""
# END PORTABLE EXAMPLE
";

        public static string AutomaticPackageNotes =
            @"#Items that could be replaced based on what you call chocopkgup.exe with
#{{PackageName}} - Package Name (should be same as nuspec file and folder) |/p
#{{PackageVersion}} - The updated version | /v
#{{DownloadUrl}} - The url for the native file | /u
#{{PackageFilePath}} - Downloaded file if including it in package | /pp
#{{PackageGuid}} - This will be used later | /pg
#{{DownloadUrlx64}} - The 64bit url for the native file | /u64

";
    }
}