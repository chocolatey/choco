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

function Get-ChecksumValid {
<#
.SYNOPSIS
Checks a file's checksum versus a passed checksum and checksum type.

.DESCRIPTION
Makes a determination if a file meets an expected checksum signature.
This function is usually used when comparing a file that is downloaded
from an official distribution point. If the checksum fails to match the
expected output, this function throws an error.

Checksums have been used for years as a means of verification. A
checksum hash is a unique value or signature that corresponds to the
contents of a file. File names and extensions can be altered without
changing the checksum signature. However if you changed the contents of
the file, even one character, the checksum will be different.

Checksums are used to provide as a means of cryptographically ensuring
the contents of a file have not been changed. While some cryptographic
algorithms, including MD5 and SHA1, are no longer considered secure
against attack, the goal of a checksum algorithm is to make it
extremely difficult (near impossible with better algorithms) to alter
the contents of a file (whether by accident or for malicious reasons)
and still result in the same checksum signature.

When verifying a checksum using a secure algorithm, if the checksum
matches the expected signature, the contents of the file are identical
to what is expected.

.NOTES
This uses the checksum.exe tool available separately at
https://chocolatey.org/packages/checksum.

Options that affect checksum verification:

* `--ignore-checksums` - skips checksumming
* `--allow-empty-checksums` - skips checksumming when the package is missing a checksum
* `--allow-empty-checksums-secure` - skips checksumming when the package is missing a checksum for secure (HTTPS) locations
* `--require-checksums` - requires checksums for both non-secure and secure locations
* `--download-checksum`, `--download-checksum-type` - allows user to pass their own checksums
* `--download-checksum-x64`, `--download-checksum-type-x64` - allows user to pass their own checksums

Features that affect checksum verification:

* `checksumFiles` - when turned off, skips checksumming
* `allowEmptyChecksums` - when turned on, skips checksumming when the package is missing a checksum
* `allowEmptyChecksumsSecure` - when turned on, skips checksumming when the package is missing a checksum for secure (HTTPS) locations

.INPUTS
None

.OUTPUTS
None

.PARAMETER File
The full path to a binary file that is checksummed and compared to the
passed Checksum parameter value.

.PARAMETER Checksum
The expected checksum hash value of the File resource. The checksum
type is covered by ChecksumType.

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
The type of checkum that the file is validated with - 'md5', 'sha1',
'sha256' or 'sha512' - defaults to 'md5'.

MD5 is not recommended as certain organizations need to use FIPS
compliant algorithms for hashing - see
https://support.microsoft.com/en-us/kb/811833 for more details.

The recommendation is to use at least SHA256.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
Get-ChecksumValid -File $fileFullPath -CheckSum $checksum -ChecksumType $checksumType

.LINK
Get-ChocolateyWebFile

.LINK
Install-ChocolateyPackage
#>
param(
  [parameter(Mandatory=$true, Position=0)][string] $file,
  [parameter(Mandatory=$false, Position=1)][string] $checksum = '',
  [parameter(Mandatory=$false, Position=2)][string] $checksumType = 'md5',
  [parameter(Mandatory=$false, Position=3)][string] $originalUrl = '',
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

  if ($env:ChocolateyIgnoreChecksums -eq 'true') {
    Write-Warning "Ignoring checksums due to feature checksumFiles turned off or option --ignore-checksums set."
    return
  }

  if ($checksum -eq '' -or $checksum -eq $null) {
    $allowEmptyChecksums = $env:ChocolateyAllowEmptyChecksums
    $allowEmptyChecksumsSecure = $env:ChocolateyAllowEmptyChecksumsSecure
    if ($allowEmptyChecksums -eq 'true') {
      Write-Debug "Empty checksums are allowed due to allowEmptyChecksums feature or option."
      return
    }

    if ($originalUrl -ne $null -and $originalUrl.ToLower().StartsWith("https") -and $allowEmptyChecksumsSecure -eq 'true') {
      Write-Debug "Download from HTTPS source with feature 'allowEmptyChecksumsSecure' enabled."
      return
    }

    Write-Warning "Missing package checksums are not allowed (by default for HTTP/FTP, `n HTTPS when feature 'allowEmptyChecksumsSecure' is disabled) for `n safety and security reasons. Although we strongly advise against it, `n if you need this functionality, please set the feature `n 'allowEmptyChecksums' ('choco feature enable -n `n allowEmptyChecksums') `n or pass in the option '--allow-empty-checksums'. You can also pass `n checksums at runtime (recommended). See `choco install -?` for details."
    Write-Debug "If you are a maintainer attempting to determine the checksum for packaging purposes, please run `n 'choco install checksum' and run 'checksum -t sha256 -f $file' `n Ensure you do this for all remote resources."
    if ($PSVersionTable.PSVersion.Major -ge 4){ 
      Write-Debug "Because you are running Powershell with a major version of v4 or greater, you could also opt to run `n '(Get-FileHash -Path $file -Algorithm SHA256).Hash' `n rather than install a separate tool."
    }

    if ($env:ChocolateyPowerShellHost -eq 'true') {
      $statement = "The integrity of the file '$([System.IO.Path]::GetFileName($file))'"
      if ($originalUrl -ne $null -and $originalUrl -ne '') {
        $statement += " from '$originalUrl'"
      }
      $statement += " has not been verified by a checksum in the package scripts."
      $question = 'Do you wish to allow the install to continue (not recommended)?'
      $choices = New-Object System.Collections.ObjectModel.Collection[System.Management.Automation.Host.ChoiceDescription]
      $choices.Add((New-Object System.Management.Automation.Host.ChoiceDescription -ArgumentList '&Yes'))
      $choices.Add((New-Object System.Management.Automation.Host.ChoiceDescription -ArgumentList '&No'))

      $selection = $Host.UI.PromptForChoice($statement, $question, $choices, 1)

      if ($selection -eq 0) { return }
    }

    if ($originalUrl -ne $null -and $originalUrl.ToLower().StartsWith("https")) {
      throw "This package downloads over HTTPS but does not yet have package checksums to verify the package. We recommend asking the maintainer to add checksums to this package. In the meantime if you need this package to work correctly, please enable the feature allowEmptyChecksumsSecure, provide the runtime switch '--allow-empty-checksums-secure', or pass in checksums at runtime (recommended - see 'choco install -?' / 'choco upgrade -?' for details)."
    } else {
      throw "Empty checksums are no longer allowed by default for non-secure sources. Please ask the maintainer to add checksums to this package. In the meantime if you need this package to work correctly, please enable the feature allowEmptyChecksums, provide the runtime switch '--allow-empty-checksums', or pass in checksums at runtime (recommended - see 'choco install -?' / 'choco upgrade -?' for details). It is strongly advised against allowing empty checksums for non-internal HTTP/FTP sources."
    }
  }

  if (!([System.IO.File]::Exists($file))) { throw "Unable to checksum a file that doesn't exist - Could not find file `'$file`'" }

  if ($checksumType -eq $null -or $checksumType -eq ''){
    $checksumType = 'md5'
  }

  if ($checksumType -ne 'sha1' -and $checksumType -ne 'sha256' -and $checksumType -ne 'sha512' -and $checksumType -ne 'md5') {
    Write-Debug 'Setting checksumType to md5 due to non-set value or type is not specified correctly.'
    throw "Checksum type '$checksumType' is unsupported. This type may be supported in a newer version of Chocolatey."
  }

  $checksumExe = Join-Path "$helpersPath" '..\tools\checksum.exe'
  if (!([System.IO.File]::Exists($checksumExe))) {
    Update-SessionEnvironment
    $checksumExe = Join-Path "$env:ChocolateyInstall" 'tools\checksum.exe'
  }
  Write-Debug "checksum.exe found at `'$checksumExe`'"

  $params = "-c=`"$checksum`" -t=`"$checksumType`" -f=`"$file`""

  Write-Debug "Executing command ['$checksumExe' $params]"
  $process = New-Object System.Diagnostics.Process
  $process.StartInfo = New-Object System.Diagnostics.ProcessStartInfo($checksumExe, $params)
  $process.StartInfo.UseShellExecute = $false
  $process.StartInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden

  $process.Start() | Out-Null
  $process.WaitForExit()
  $exitCode = $process.ExitCode
  $process.Dispose()

  Write-Debug "Command [`'$checksumExe`' $params] exited with `'$exitCode`'."

  if ($exitCode -ne 0) {
    throw "Checksum for '$file' did not meet '$checksum' for checksum type '$checksumType'. Consider passing the actual checksums through with `--checksum --checksum64` once you validate the checksums are appropriate. A less secure option is to pass `--ignore-checksums` if necessary."
  }

  #$fileCheckSumActual = $md5Output.Split(' ')[0]
  # if ($fileCheckSumActual -ne $checkSum) {
  #   throw "CheckSum for `'$file'` did not meet `'$checkSum`'."
  # }
}
