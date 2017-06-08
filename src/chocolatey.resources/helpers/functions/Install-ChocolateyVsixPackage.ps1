# Copyright © 2017 Chocolatey Software, Inc.
# Copyright © 2015 - 2017 RealDimensions Software, LLC
# Copyright © 2011 - 2015 RealDimensions Software, LLC & original authors/contributors from https://github.com/chocolatey/chocolatey
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

function Install-ChocolateyVsixPackage {
<#
.SYNOPSIS
Downloads and installs a VSIX package for Visual Studio

.DESCRIPTION
VSIX packages are Extensions for the Visual Studio IDE. The Visual
Studio Gallery at  http://visualstudiogallery.msdn.microsoft.com/ is the
public extension feed and hosts thousands of extensions. You can locate
a VSIX Url by finding the download link of Visual Studio extensions on
the Visual Studio Gallery.

.NOTES
Chocolatey works best when the packages contain the software it is
managing and doesn't require downloads. However most software in the
Windows world requires redistribution rights and when sharing packages
publicly (like on the community feed), maintainers may not have those
aforementioned rights. Chocolatey understands how to work with that,
hence this function. You are not subject to this limitation with
internal packages.

.INPUTS
None

.OUTPUTS
None

.PARAMETER PackageName
The name of the package - while this is an arbitrary value, it's
recommended that it matches the package id.

In 0.10.4+, `Name` is an alias for PackageName.

.PARAMETER VsixUrl
The URL of the package to be installed.

Prefer HTTPS when available. Can be HTTP, FTP, or File URIs.

In 0.10.4+, `Url` is an alias for VsixUrl.

.PARAMETER VsVersion
The major version number of Visual Studio where the
package should be installed. This is optional. If not
specified, the most recent Visual Studio installation
will be targetted.

NOTE: For Visual Studio 2015, the VsVersion is 14. It can be determined
by looking at the folders under Program Files / Program Files (x86).

In 0.10.4+, `VisualStudioVersion` is an alias for VsVersion.

.PARAMETER Checksum
The checksum hash value of the Url resource. This allows a checksum to 
be validated for files that are not local. The checksum type is covered
by ChecksumType. 

**NOTE:** Checksums in packages are meant as a measure to validate the 
originally intended file that was used in the creation of a package is
the same file that is received at a future date. Since this is used for
other steps in the process related to the community repository, it 
ensures that the file a user receives is the same file a maintainer
and a moderator (if applicable), plus any moderation review has 
intended for you to receive with this package. If you are looking at a 
remote source that uses the same url for updates, you will need to 
ensure the package also stays updated in line with those remote 
resource updates. You should look into [automatic packaging](https://chocolatey.org/docs/automatic-packages) 
to help provide that functionality.

**NOTE:** To determine checksums, you can get that from the original 
site if provided. You can also use the [checksum tool available on 
the community feed](https://chocolatey.org/packages/checksum) (`choco install checksum`) 
and use it e.g. `checksum -t sha256 -f path\to\file`. Ensure you 
provide checksums for all remote resources used.

.PARAMETER ChecksumType
The type of checkum that the file is validated with - valid
values are 'md5', 'sha1', 'sha256' or 'sha512' - defaults to 'md5'.

MD5 is not recommended as certain organizations need to use FIPS
compliant algorithms for hashing - see
https://support.microsoft.com/en-us/kb/811833 for more details.

The recommendation is to use at least SHA256.

.PARAMETER Options
OPTIONAL - Specify custom headers. Available in 0.9.10+.

.PARAMETER File
Will be used for VsixUrl if VsixUrl is empty. Available in 0.10.7+.

This parameter provides compatibility, but should not be used directly
and not with the community package repository until January 2018.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
>
# This downloads the AutoWrockTestable VSIX from the Visual Studio
# Gallery and installs it to the latest version of VS.

Install-ChocolateyVsixPackage -PackageName "MyPackage" `
  -VsixUrl http://visualstudiogallery.msdn.microsoft.com/ea3a37c9-1c76-4628-803e-b10a109e7943/file/73131/1/AutoWrockTestable.vsix

.EXAMPLE
>
# This downloads the AutoWrockTestable VSIX from the Visual Studio
# Gallery and installs it to Visual Studio 2012 (v11.0).

Install-ChocolateyVsixPackage -PackageName "MyPackage" `
  -VsixUrl http://visualstudiogallery.msdn.microsoft.com/ea3a37c9-1c76-4628-803e-b10a109e7943/file/73131/1/AutoWrockTestable.vsix `
  -VsVersion 11

.LINK
Install-ChocolateyPackage

.LINK
Install-ChocolateyInstallPackage

.LINK
Install-ChocolateyZipPackage
#>
param(
  [alias("name")][parameter(Mandatory=$true, Position=0)][string] $packageName,
  [alias("url")][parameter(Mandatory=$false, Position=1)][string] $vsixUrl,
  [alias("visualStudioVersion")][parameter(Mandatory=$false, Position=2)][int] $vsVersion = 0,
  [parameter(Mandatory=$false)][string] $checksum = '',
  [parameter(Mandatory=$false)][string] $checksumType = '',
  [parameter(Mandatory=$false)][hashtable] $options = @{Headers=@{}},
  [alias("fileFullPath")][parameter(Mandatory=$false)][string] $file = '',
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

    Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

    if ($vsixUrl -eq '' -or $vsixUrl -eq $null) {
        $vsixUrl = $file
    }

    if($vsVersion -eq 0) {
        if ([System.IntPtr]::Size -eq 4)
        {
            <# 32bits system case #>
            $versions=(get-ChildItem HKLM:SOFTWARE\Microsoft\VisualStudio -ErrorAction SilentlyContinue | ? { ($_.PSChildName -match "^[0-9\.]+$") } | ? {$_.property -contains "InstallDir"} | sort {[int]($_.PSChildName)} -descending)
        }
        else
        {
            $versions=(get-ChildItem HKLM:SOFTWARE\Wow6432Node\Microsoft\VisualStudio -ErrorAction SilentlyContinue | ? { ($_.PSChildName -match "^[0-9\.]+$") } | ? {$_.property -contains "InstallDir"} | sort {[int]($_.PSChildName)} -descending)
        }
        if($versions -and $versions.Length){
            $version = $versions[0]
        }elseif($versions){
            $version = $versions
        }
    }
    else {
        if ([System.IntPtr]::Size -eq 4)
        {
            <# 32bits system case #>
            $versions=(get-ChildItem HKLM:SOFTWARE\Microsoft\VisualStudio -ErrorAction SilentlyContinue | ? { ($_.PSChildName.EndsWith("$vsVersion.0")) } | ? {$_.property -contains "InstallDir"})
        }
        else
        {
            $version=(get-ChildItem HKLM:SOFTWARE\Wow6432Node\Microsoft\VisualStudio -ErrorAction SilentlyContinue | ? { ($_.PSChildName.EndsWith("$vsVersion.0")) } | ? {$_.property -contains "InstallDir"})
        }
    }

    if ($version) {
        $vnum=$version.PSPath.Substring($version.PSPath.LastIndexOf('\')+1)
        if($vnum -as [int] -lt 10) {
            throw "This installed VS version, $vnum, does not support installing VSIX packages. Version 10 is the minimum acceptable version."
        }
        $dir=(get-itemProperty $version.PSPath "InstallDir").InstallDir
        $installer = Join-Path $dir "VsixInstaller.exe"
    }

    if ($installer) {
        $download="$env:TEMP\$($packageName.Replace(' ','')).vsix"
        try{
            Get-ChocolateyWebFile $packageName $download $vsixUrl -checksum $checksum -checksumType $checksumType -Options $options
        }
        catch {
            throw "There were errors attempting to retrieve the vsix from $vsixUrl. The error message was '$_'."
        }

        Write-Debug "Installing VSIX using $installer"
        $exitCode = Install-Vsix "$installer" "$download"
        if($exitCode -gt 0 -and $exitCode -ne 1001) { #1001: Already installed
           throw "There was an error installing '$packageName'. The exit code returned was $exitCode."
        }
    }
    else {
        throw "Visual Studio is not installed or the specified version is not present."
    }
}
