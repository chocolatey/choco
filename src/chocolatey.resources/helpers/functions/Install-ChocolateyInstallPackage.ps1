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

function Install-ChocolateyInstallPackage {
<#
.SYNOPSIS
**NOTE:** Administrative Access Required.

Installs software into "Programs and Features". Use
Install-ChocolateyPackage when software must be downloaded first.

.DESCRIPTION
This will run an installer (local file) on your machine.

.NOTES
This command will assert UAC/Admin privileges on the machine.

If you are embedding files into a package, ensure that you have the
rights to redistribute those files if you are sharing this package
publicly (like on the community feed). Otherwise, please use
Install-ChocolateyPackage to download those resources from their
official distribution points.

This is a native installer wrapper function. A "true" package will
contain all the run time files and not an installer. That could come
pre-zipped and require unzipping in a PowerShell script. Chocolatey
works best when the packages contain the software it is managing. Most
software in the Windows world comes as installers and Chocolatey
understands how to work with that, hence this wrapper function.

.INPUTS
None

.OUTPUTS
None

.PARAMETER PackageName
The name of the package - while this is an arbitrary value, it's
recommended that it matches the package id.

.PARAMETER FileType
This is the extension of the file. This can be 'exe', 'msi', or 'msu'.
Licensed editions of Chocolatey use this to automatically determine
silent arguments. If this is not provided, Chocolatey will
automatically determine this using the downloaded file's extension.

.PARAMETER SilentArgs
OPTIONAL - These are the parameters to pass to the native installer,
including any arguments to make the installer silent/unattended.
Pro/Business Editions of Chocolatey will automatically determine the
installer type and merge the arguments with what is provided here.

Try any of the to get the silent installer -
`/s /S /q /Q /quiet /silent /SILENT /VERYSILENT`. With msi it is always
`/quiet`. Please pass it in still but it will be overridden by
Chocolatey to `/quiet`. If you don't pass anything it could invoke the
installer with out any arguments. That means a nonsilent installer.

Please include the `notSilent` tag in your Chocolatey package if you
are not setting up a silent/unattended package. Please note that if you
are submitting to the community repository, it is nearly a requirement
for the package to be completely unattended.

When you are using this with an MSI, it will set up the arguments as
follows: `"C:\Full\Path\To\msiexec.exe" /i "$fileFullPath" $silentArgs`,
where `$fileFullPath` is `$file` or `$file64`, depending on what has been
decided to be used. Previous to 0.10.4, it will be just `$file` as
passing `$file64` would not have been available yet.

When you use this with MSU, it is similar to MSI above in that it finds
the right executable to run.

When you use this with executable installers, the `$fileFullPath` will
be `$file` (or `$file64` starting with 0.10.4+) and expects to be a full
path to the file. If the file is in the package, see the parameters for
"File" and "File64" to determine how you can get that path at runtime in
a deterministic way. SilentArgs is everything you call against that
file, as in `"$fileFullPath" $silentArgs"`. An example would be
`"c:\path\setup.exe" /S`, where `$fileFullPath = "c:\path\setup.exe"`
and `$silentArgs = "/S"`.

.PARAMETER File
Full file path to native installer to run. If embedding in the package,
you can get it to the path with
`"$(Split-Path -parent $MyInvocation.MyCommand.Definition)\\INSTALLER_FILE"`

In 0.10.1+, `FileFullPath` is an alias for File.

This can be a 32-bit or 64-bit file. This is mandatory in earlier versions
of Chocolatey, but optional if File64 has been provided.

.PARAMETER File64
Full file path to a 64-bit native installer to run. Available in 0.10.4+.
If embedding in the package, you can get it to the path with
`"$(Split-Path -parent $MyInvocation.MyCommand.Definition)\\INSTALLER_FILE"`

Provide this when you want to provide both 32-bit and 64-bit
installers or explicitly only a 64-bit installer (which will cause a package
install failure on 32-bit systems).

.PARAMETER ValidExitCodes
Array of exit codes indicating success. Defaults to `@(0)`.

.PARAMETER UseOnlyPackageSilentArguments
Do not allow choco to provide/merge additional silent arguments and
only use the ones available with the package. Available in 0.9.10+.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
>
$packageName= 'bob'
$toolsDir   = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$fileLocation = Join-Path $toolsDir 'INSTALLER_EMBEDDED_IN_PACKAGE'

$packageArgs = @{
  packageName   = $packageName
  fileType      = 'msi'
  file          = $fileLocation
  silentArgs    = "/qn /norestart"
  validExitCodes= @(0, 3010, 1641)
  softwareName  = 'Bob*'
}

Install-ChocolateyInstallPackage @packageArgs

.EXAMPLE
>
$packageArgs = @{
  packageName   = 'bob'
  fileType      = 'exe'
  file          = '\\SHARE_LOCATION\to\INSTALLER_FILE'
  silentArgs    = "/S"
  validExitCodes= @(0)
  softwareName  = 'Bob*'
}

Install-ChocolateyInstallPackage @packageArgs


.EXAMPLE
>
$packageName= 'bob'
$toolsDir   = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$fileLocation = Join-Path $toolsDir 'someinstaller.msi'

$packageArgs = @{
  packageName   = $packageName
  fileType      = 'msi'
  file          = $fileLocation
  silentArgs    = "/qn /norestart MSIPROPERTY=`"true`""
  validExitCodes= @(0, 3010, 1641)
  softwareName  = 'Bob*'
}

Install-ChocolateyInstallPackage @packageArgs

.EXAMPLE
>
$packageName= 'bob'
$toolsDir   = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$fileLocation = Join-Path $toolsDir 'someinstaller.msi'
$mstFileLocation = Join-Path $toolsDir 'transform.mst'

$packageArgs = @{
  packageName   = $packageName
  fileType      = 'msi'
  file          = $fileLocation
  silentArgs    = "/qn /norestart TRANSFORMS=`"$mstFileLocation`""
  validExitCodes= @(0, 3010, 1641)
  softwareName  = 'Bob*'
}

Install-ChocolateyInstallPackage @packageArgs


.EXAMPLE
Install-ChocolateyInstallPackage 'bob' 'exe' '/S' "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)\bob.exe"

.EXAMPLE
>
Install-ChocolateyInstallPackage -PackageName 'bob' -FileType 'exe' `
  -SilentArgs '/S' `
  -File "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)\bob.exe" `
  -ValidExitCodes @(0)

.LINK
Install-ChocolateyPackage

.LINK
Uninstall-ChocolateyPackage

.LINK
Get-UninstallRegistryKey

.LINK
Start-ChocolateyProcessAsAdmin
#>
param(
  [parameter(Mandatory=$true, Position=0)][string] $packageName,
  [parameter(Mandatory=$false, Position=1)]
  [alias("installerType","installType")][string] $fileType = 'exe',
  [parameter(Mandatory=$false, Position=2)][string[]] $silentArgs = '',
  [alias("fileFullPath")][parameter(Mandatory=$false, Position=3)][string] $file,
  [alias("fileFullPath64")][parameter(Mandatory=$false)][string] $file64,
  [parameter(Mandatory=$false)] $validExitCodes = @(0),
  [parameter(Mandatory=$false)]
  [alias("useOnlyPackageSilentArgs")][switch] $useOnlyPackageSilentArguments = $false,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)
  [string]$silentArgs = $silentArgs -join ' '

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

  $bitnessMessage = ''
  $fileFullPath = $file
  if ((Get-ProcessorBits 32) -or $env:ChocolateyForceX86 -eq 'true') {
    if (!$file) { throw "32-bit installation is not supported for $packageName"; }
    if ($file64) { $bitnessMessage = '32-bit '; }
  } elseif ($file64) {
    $fileFullPath = $file64
    $bitnessMessage = '64-bit '
  }

  if ($fileFullPath -eq '' -or $fileFullPath -eq $null) {
    throw 'Package parameters incorrect, either File or File64 must be specified.'
  }

  Write-Host "Installing $bitnessMessage$packageName..."

  if ($fileType -eq '' -or $fileType -eq $null) {
    Write-Debug 'No FileType supplied. Using the file extension to determine FileType'
    $fileType = [System.IO.Path]::GetExtension("$fileFullPath").Replace(".", "")
  }

  $installerTypeLower = $fileType.ToLower()
  if ($installerTypeLower -ne 'msi' -and $installerTypeLower -ne 'exe' -and $installerTypeLower -ne 'msu') {
    Write-Warning "FileType '$fileType' is unrecognized, using 'exe' instead."
    $fileType = 'exe'
  }

  $env:ChocolateyInstallerType = $fileType

  $additionalInstallArgs = $env:chocolateyInstallArguments;
  if ($additionalInstallArgs -eq $null) {
    $additionalInstallArgs = '';
  } else {
    if ($additionalInstallArgs -match 'INSTALLDIR' -or `
      $additionalInstallArgs -match 'TARGETDIR' -or `
      $additionalInstallArgs -match 'dir\=' -or `
      $additionalInstallArgs -match '\/D\='
    ) {
@"
Pro / Business supports a single, ubiquitous install directory option.
 Stop the hassle of determining how to pass install directory overrides
 to install arguments for each package / installer type.
 Check out Pro / Business - https://chocolatey.org/compare"
"@ | Write-Warning
    }
  }
  $overrideArguments = $env:chocolateyInstallOverride;

  # remove \chocolatey\chocolatey\
  # might be a slight issue here if the download path is the older
  $silentArgs = $silentArgs -replace '\\chocolatey\\chocolatey\\', '\chocolatey\'
  $additionalInstallArgs = $additionalInstallArgs -replace '\\chocolatey\\chocolatey\\', '\chocolatey\'
  $updatedFilePath = $fileFullPath -replace '\\chocolatey\\chocolatey\\', '\chocolatey\'
  if ([System.IO.File]::Exists($updatedFilePath)) {
    $fileFullPath = $updatedFilePath
  }

  $ignoreFile = $fileFullPath + '.ignore'
  if ($env:ChocolateyInstall -and $ignoreFile -match [System.Text.RegularExpressions.Regex]::Escape($env:ChocolateyInstall)) {
    try {
      '' | out-file $ignoreFile
    } catch {
      Write-Warning "Unable to generate `'$ignoreFile`'"
    }
  }

  $workingDirectory = Get-Location
  try {
    $workingDirectory = [System.IO.Path]::GetDirectoryName($fileFullPath)
  } catch {
    Write-Warning "Unable to set the working directory for installer to location of '$fileFullPath'"
  }

  try {
    # make sure any logging folder exists
    $pattern = "(?:['`"])([a-zA-Z]\:\\[^'`"]+)(?:[`"'])|([a-zA-Z]\:\\[\S]+)"
    $silentArgs, $additionalInstallArgs | %{ Select-String $pattern -input $_ -AllMatches } |
      % { $_.Matches } | % {
        $argDirectory = $_.Groups[1]
        if ($argDirectory -eq $null -or $argDirectory -eq '') { continue }
        $argDirectory = [System.IO.Path]::GetFullPath([System.IO.Path]::GetDirectoryName($argDirectory))
        Write-Debug "Ensuring '$argDirectory' exists"
        if (![System.IO.Directory]::Exists($argDirectory)) { [System.IO.Directory]::CreateDirectory($argDirectory) | Out-Null }
      }
  } catch {
    Write-Debug "Error ensuring directories exist -  $($_.Exception.Message)"
  }

  if ($fileType -like 'msi') {
    $msiArgs = "/i `"$fileFullPath`""
    if ($overrideArguments) {
      Write-Host "Overriding package arguments with '$additionalInstallArgs' (replacing '$silentArgs')";
      $msiArgs = "$msiArgs $additionalInstallArgs";
    } else {
      $msiArgs = "$msiArgs $silentArgs $additionalInstallArgs";
    }

    $env:ChocolateyExitCode = Start-ChocolateyProcessAsAdmin "$msiArgs" "$($env:SystemRoot)\System32\msiexec.exe" -validExitCodes $validExitCodes -workingDirectory $workingDirectory
  }

  if ($fileType -like 'exe') {
    if ($overrideArguments) {
      Write-Host "Overriding package arguments with '$additionalInstallArgs' (replacing '$silentArgs')";
      $env:ChocolateyExitCode = Start-ChocolateyProcessAsAdmin "$additionalInstallArgs" $fileFullPath -validExitCodes $validExitCodes -workingDirectory $workingDirectory
    } else {
      $env:ChocolateyExitCode = Start-ChocolateyProcessAsAdmin "$silentArgs $additionalInstallArgs" $fileFullPath -validExitCodes $validExitCodes -workingDirectory $workingDirectory
    }
  }

  if($fileType -like 'msu') {
    if ($overrideArguments) {
      Write-Host "Overriding package arguments with '$additionalInstallArgs' (replacing '$silentArgs')";
      $msuArgs = "`"$fileFullPath`" $additionalInstallArgs"
    } else {
      $msuArgs = "`"$fileFullPath`" $silentArgs $additionalInstallArgs"
    }
    $env:ChocolateyExitCode = Start-ChocolateyProcessAsAdmin "$msuArgs" "$($env:SystemRoot)\System32\wusa.exe" -validExitCodes $validExitCodes -workingDirectory $workingDirectory
  }

  Write-Host "$packageName has been installed."
}
