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
# REMOVE ANYTHING BELOW THAT IS NOT NEEDED

$ErrorActionPreference = 'Stop'; # stop on all errors

[[AutomaticPackageNotesInstaller]]
$packageName = '[[PackageName]]' # arbitrary name for the package, used in messages
$toolsDir = ""$(Split-Path -parent $MyInvocation.MyCommand.Definition)""
$url = '[[Url]]' # download url
$url64 = '[[Url64]]' # 64bit URL here or remove - if installer is both, use $url

$packageArgs = @{
  packageName   = $packageName
  unzipLocation = $toolsDir
  fileType      = '[[InstallerType]]' #only one of these: exe, msi, msu
  url           = $url
  url64bit      = $url64

  #MSI
  silentArgs    = ""/qn /norestart /l*v '$env:TEMP\chocolatey\$packageName\install.log'"" # ALLUSERS=1 DISABLEDESKTOPSHORTCUT=1 ADDDESKTOPICON=0 ADDSTARTMENU=0
  validExitCodes= @(0, 3010, 1641)
  #OTHERS
  #silentArgs    ='[[SilentArgs]]' # ""/s /S /q /Q /quiet /silent /SILENT /VERYSILENT -s"" # try any of these to get the silent installer
  #validExitCodes= @(0) #please insert other valid exit codes here

  # optional
  registryUninstallerKey = '[[PackageName]]' #ensure this is the value in the registry
  checksum      = '[[Checksum]]'
  checksumType  = '[[ChecksumType]]' #default is md5, can also be sha1
  checksum64    = '[[Checksum64]]'
  checksumType64= '[[ChecksumType64]]' #default is checksumType
}

Install-ChocolateyPackage @packageArgs
#Install-ChocolateyZipPackage @packageArgs

## Main helper functions - these have error handling tucked into them already
## see https://github.com/chocolatey/choco/wiki/HelpersReference

## Install an application, will assert administrative rights
## add additional optional arguments as necessary
##Install-ChocolateyPackage $packageName $fileType $silentArgs $url [$url64 -validExitCodes $validExitCodes -checksum $checksum -checksumType $checksumType -checksum64 $checksum64 -checksumType64 $checksumType64]

## Download and unpack a zip file
##Install-ChocolateyZipPackage $packageName $url $toolsDir [$url64 -checksum $checksum -checksumType $checksumType -checksum64 $checksum64 -checksumType64 $checksumType64]

## Install Visual Studio Package
#Install-ChocolateyVsixPackage $packageName $url [$vsVersion] [-checksum $checksum -checksumType $checksumType]
#Install-ChocolateyVsixPackage @packageArgs

# see the full list at https://github.com/chocolatey/choco/wiki/HelpersReference
# downloader that the main helpers use to download items
# if removing $url64, please remove from here
#Get-ChocolateyWebFile $packageName 'DOWNLOAD_TO_FILE_FULL_PATH' $url $url64
# installer, will assert administrative rights - used by Install-ChocolateyPackage
#Install-ChocolateyInstallPackage $packageName $fileType $silentArgs '_FULLFILEPATH_' -validExitCodes $validExitCodes
# unzips a file to the specified location - auto overwrites existing content
#Get-ChocolateyUnzip ""FULL_LOCATION_TO_ZIP.zip"" $toolsDir
# Runs processes asserting UAC, will assert administrative rights - used by Install-ChocolateyInstallPackage
#Start-ChocolateyProcessAsAdmin 'STATEMENTS_TO_RUN' 'Optional_Application_If_Not_PowerShell' -validExitCodes $validExitCodes
# add specific folders to the path - any executables found in the chocolatey package folder will already be on the path. This is used in addition to that or for cases when a native installer doesn't add things to the path.
#Install-ChocolateyPath 'LOCATION_TO_ADD_TO_PATH' 'User_OR_Machine' # Machine will assert administrative rights
# add specific files as shortcuts to the desktop
#$target = Join-Path $toolsDir ""$($packageName).exe""
# Install-ChocolateyShortcut -shortcutFilePath ""<path>"" -targetPath ""<path>"" [-workDirectory ""C:\"" -arguments ""C:\test.txt"" -iconLocation ""C:\test.ico"" -description ""This is the description""]
# outputs the bitness of the OS (either ""32"" or ""64"")
#$osBitness = Get-ProcessorBits
#Install-ChocolateyEnvironmentVariable -variableName ""SOMEVAR"" -variableValue ""value"" [-variableType = 'Machine' #Defaults to 'User']

#Install-ChocolateyFileAssociation 
#Install-BinFile ## only use this for non-exe files - chocolatey will automatically pick up the exe files and shim them automatically
## https://github.com/chocolatey/choco/wiki/CreatePackages#how-do-i-exclude-executables-from-getting-batch-redirects

##PORTABLE EXAMPLE
#$toolsDir = ""$(Split-Path -parent $MyInvocation.MyCommand.Definition)""
# despite the name ""Install-ChocolateyZipPackage"" this also works with 7z archives
#Install-ChocolateyZipPackage $packageName $url $toolsDir $url64
## END PORTABLE EXAMPLE

## [DEPRECATING] PORTABLE EXAMPLE
#$binRoot = Get-BinRoot
#$installDir = Join-Path $binRoot ""$packageName""
#Write-Host ""Adding `'$installDir`' to the path and the current shell path""
#Install-ChocolateyPath ""$installDir""
#$env:Path = ""$($env:Path);$installDir""

# if removing $url64, please remove from here
# despite the name ""Install-ChocolateyZipPackage"" this also works with 7z archives
#Install-ChocolateyZipPackage ""$packageName"" ""$url"" ""$installDir"" ""$url64""
## END PORTABLE EXAMPLE
";

        public static string AutomaticPackageNotes =
            @"#Items that could be replaced based on what you call chocopkgup.exe with
#{{PackageName}} - Package Name (should be same as nuspec file and folder) |/p
#{{PackageVersion}} - The updated version | /v
#{{DownloadUrl}} - The url for the native file | /u
#{{PackageFilePath}} - Downloaded file if including it in package | /pp
#{{PackageGuid}} - This will be used later | /pg
#{{DownloadUrlx64}} - The 64-bit url for the native file | /u64
#{{Checksum}} - The checksum for the url | /c
#{{Checksumx64}} - The checksum for the 64-bit url | /c64
";
    }
}