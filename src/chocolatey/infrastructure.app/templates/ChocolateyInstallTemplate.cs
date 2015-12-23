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
            @"# IMPORTANT: Before releasing this package, copy/paste the next 2 lines into PowerShell to remove all comments from this file:
#   $f='c:\path\to\thisFile.ps1'
#   gc $f | ? {$_ -notmatch ""^\s*#""} | % {$_ -replace '(^.*?)\s*?[^``]#.*','$1'} | Out-File $f+"".~"" -en utf8; mv -fo $f+"".~"" $f

$ErrorActionPreference = 'Stop'; # stop on all errors

[[AutomaticPackageNotesInstaller]]
$packageName= '[[PackageName]]' # arbitrary name for the package, used in messages
$toolsDir   = ""$(Split-Path -parent $MyInvocation.MyCommand.Definition)""
$url        = '[[Url]]' # download url
$url64      = '[[Url64]]' # 64bit URL here or remove - if installer is both, use $url
#$fileLocation = Join-Path $toolsDir 'NAME_OF_EMBEDDED_INSTALLER_FILE'
#$fileLocation = Join-Path $toolsDir 'SHARE_LOCATION_OF_INSTALLER_FILE'

$packageArgs = @{
  packageName   = $packageName
  unzipLocation = $toolsDir
  fileType      = '[[InstallerType]]' #only one of these: exe, msi, msu
  url           = $url
  url64bit      = $url64
  #file         = $fileLocation

  #MSI
  silentArgs    = ""[[SilentArgs]]"" # ALLUSERS=1 DISABLEDESKTOPSHORTCUT=1 ADDDESKTOPICON=0 ADDSTARTMENU=0
  validExitCodes= @(0, 3010, 1641)
  #OTHERS
  # Uncomment matching EXE type (sorted by most to least common)
  #silentArgs   = '/S'           # NSIS
  #silentArgs   = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-' # Inno Setup
  #silentArgs   = '/s'           # InstallShield
  #silentArgs   = '/s /v""/qn""' # InstallShield with MSI
  #silentArgs   = '/s'           # Wise InstallMaster
  #silentArgs   = '-s'           # Squirrel
  #silentArgs   = '-q'           # Install4j
  #silentArgs   = '-s -u'        # Ghost
  # Note that some installers, in addition to the silentArgs above, may also need assistance of AHK to achieve silence.
  #silentArgs   = ''             # none; make silent with input macro script like AutoHotKey (AHK)
                                 #       https://chocolatey.org/packages/autohotkey.portable
  #validExitCodes= @(0) #please insert other valid exit codes here

  # optional, highly recommended
  softwareName  = '[[PackageName]]*' #part or all of the Display Name as you see it in Programs and Features. It should be enough to be unique
  checksum      = '[[Checksum]]'
  checksumType  = '[[ChecksumType]]' #default is md5, can also be sha1
  checksum64    = '[[Checksum64]]'
  checksumType64= '[[ChecksumType64]]' #default is checksumType
}

Install-ChocolateyPackage @packageArgs
#Install-ChocolateyZipPackage @packageArgs
# if you are making your own internal packages (organizations), you can embed the installer or 
# put on internal file share and use the following instead (you'll need to add $file to the above)
#Install-ChocolateyInstallPackage @packageArgs 

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
# use this for embedding installers in the package when not going to community feed or when you have distribution rights
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