# Copyright 2011 - Present RealDimensions Software, LLC & original authors/contributors from https://github.com/chocolatey/chocolatey
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
param(
  [string] $file,
  [string] $checksum = '',
  [string] $checksumType = 'md5'
)
  Write-Debug "Running 'Get-ChecksumValid' with file:`'$file`', checksum: `'$checksum`', checksumType: `'$checksumType`'";
  if ($env:chocolateyIgnoreChecksums -eq 'true') { 
	Write-Warning "Ignoring checksums due to feature checksumFiles = false or config ignoreChecksums = true."
	return 
  }
  if ($checksum -eq '' -or $checksum -eq $null) { return }

  if (!([System.IO.File]::Exists($file))) { throw "Unable to checksum a file that doesn't exist - Could not find file `'$file`'" }

  if ($checksumType -ne 'sha1' -and $checksumType -ne 'sha256' -and $checksumType -ne 'sha512' -and $checksumType -ne 'md5') { 
    Write-Debug 'Setting checksumType to md5 due to non-set value or type isn't specified correctly.'
    $checksumType = 'md5'
  }
    
  $checksumExe = Join-Path "$helpersPath" '..\tools\checksum.exe'
  if (!([System.IO.File]::Exists($checksumExe))) {
	Update-SessionEnvironment
	$checksumExe = Join-Path "$env:ChocolateyInstall" 'tools\checksum.exe'
  }
  Write-Debug "checksum.exe found at `'$checksumExe`'"

  Write-Debug "Calling command [`'$checksumExe`' -c$checksum `"$file`"] to retrieve checksum"
  $process = Start-Process "$checksumExe" -ArgumentList " -c=`"$checksum`" -t=`"$checksumType`" -f=`"$file`"" -Wait -WindowStyle Hidden -PassThru
  
  # this is here for specific cases in Posh v3 where -Wait is not honored
  $currentPreference = $ErrorActionPreference
  $ErrorActionPreference = 'SilentlyContinue'
  if (!($process.HasExited)) { 
    Wait-Process -Id $process.Id 
  } 
  $ErrorActionPreference = $currentPreference

  Write-Debug "`'$checksumExe`' exited with $($process.ExitCode)"

  if ($process.ExitCode -ne 0) {
    throw "Checksum for `'$file'` did not meet `'$checksum`' for checksum type `'$checksumType`'."
  }

  #$fileCheckSumActual = $md5Output.Split(' ')[0]
  # if ($fileCheckSumActual -ne $checkSum) {
  #   throw "CheckSum for `'$file'` did not meet `'$checkSum`'."
  # }
}
