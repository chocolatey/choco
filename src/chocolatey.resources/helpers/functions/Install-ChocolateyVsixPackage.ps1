function Install-ChocolateyVsixPackage {
<#
.SYNOPSIS
Downloads and installs a VSIX package for Visual Studio

.PARAMETER PackageName
The name of the package we want to download - this is
arbitrary, call it whatever you want. It's recommended
you call it the same as your nuget package id.

.PARAMETER VsixUrl
The URL of the package to be installed

.PARAMETER VsVersion
The Major version number of Visual Studio where the
package should be installed. This is optional. If not
specified, the most recent Visual Studio installation
will be targetted.

.PARAMETER Checksum
OPTIONAL (Right now) - This allows a checksum to be validated for files that are not local

.PARAMETER ChecksumType
OPTIONAL (Right now) - 'md5' or 'sha1' - defaults to 'md5'

.EXAMPLE
Install-ChocolateyVsixPackage "MyPackage" http://visualstudiogallery.msdn.microsoft.com/ea3a37c9-1c76-4628-803e-b10a109e7943/file/73131/1/AutoWrockTestable.vsix

This downloads the AutoWrockTestable VSIX from the Visual Studio Gallery and installs it to the latest version of VS.

.EXAMPLE
Install-ChocolateyVsixPackage "MyPackage" http://visualstudiogallery.msdn.microsoft.com/ea3a37c9-1c76-4628-803e-b10a109e7943/file/73131/1/AutoWrockTestable.vsix 11

This downloads the AutoWrockTestable VSIX from the Visual Studio Gallery and installs it to Visual Studio 2012 (v11.0).

.NOTES
VSIX packages are Extensions for the Visual Studio IDE.
The Visual Sudio Gallery at
http://visualstudiogallery.msdn.microsoft.com/ is the
public extension feed and hosts thousands of extensions.
You can locate a VSIX Url by finding the download link
of Visual Studio extensions on the Visual Studio Gallery.

#>
param(
  [string]$packageName,
  [string]$vsixUrl,
  [int]$vsVersion=0,
  [string] $checksum = '',
  [string] $checksumType = ''
)
    Write-Debug "Running 'Install-ChocolateyVsixPackage' for $packageName with vsixUrl:`'$vsixUrl`', vsVersion: `'$vsVersion`', checksum: `'$checksum`', checksumType: `'$checksumType`' ";
    if($vsVersion -eq 0) {
        $versions=(get-ChildItem HKLM:SOFTWARE\Wow6432Node\Microsoft\VisualStudio -ErrorAction SilentlyContinue | ? { ($_.PSChildName -match "^[0-9\.]+$") } | ? {$_.property -contains "InstallDir"} | sort {[int]($_.PSChildName)} -descending)
        if($versions -and $versions.Length){
            $version = $versions[0]
        }elseif($versions){
            $version = $versions
        }
    }
    else {
        $version=(get-ChildItem HKLM:SOFTWARE\Wow6432Node\Microsoft\VisualStudio -ErrorAction SilentlyContinue | ? { ($_.PSChildName.EndsWith("$vsVersion.0")) } | ? {$_.property -contains "InstallDir"})
    }
    if($version){
        $vnum=$version.PSPath.Substring($version.PSPath.LastIndexOf('\')+1)
        if($vnum -as [int] -lt 10) {
            Write-ChocolateyFailure $packageName "This installed VS version, $vnum, does not support installing VSIX packages. Version 10 is the minimum acceptable version."
            return
        }
        $dir=(get-itemProperty $version.PSPath "InstallDir").InstallDir
        $installer = Join-Path $dir "VsixInstaller.exe"
    }
    if($installer) {
        $download="$env:temp\$($packageName.Replace(' ','')).vsix"
        try{
            Get-ChocolateyWebFile $packageName $download $vsixUrl -checksum $checksum -checksumType $checksumType
        }
        catch {
            Write-ChocolateyFailure $packageName "There were errors attempting to retrieve the vsix from $vsixUrl. The error message was '$_'."
            return
        }
        Write-Debug "Installing VSIX using $installer"
        $exitCode = Install-Vsix "$installer" "$download"
        if($exitCode -gt 0 -and $exitCode -ne 1001) { #1001: Already installed
            Write-ChocolateyFailure $packageName "There was an error installing '$packageName'. The exit code returned was $exitCode."
            return
        }
        Write-ChocolateySuccess $packageName
    }
    else {
        Write-ChocolateyFailure $packageName "Visual Studio is not installed or the specified version is not present."
    }
}

function Install-Vsix($installer, $installFile) {
    Write-Host "Installing $installFile using $installer"
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName=$installer
    $psi.Arguments="/q $installFile"
    $s = [System.Diagnostics.Process]::Start($psi)
    $s.WaitForExit()
    return $s.ExitCode
}
