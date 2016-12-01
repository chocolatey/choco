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

function Start-ChocolateyProcessAsAdmin {
<#
.SYNOPSIS
**NOTE:** Administrative Access Required.

Runs a process with administrative privileges. If `-ExeToRun` is not
specified, it is run with PowerShell.

.NOTES
This command will assert UAC/Admin privileges on the machine.

Starting in 0.9.10, will automatically call Set-PowerShellExitCode to
set the package exit code in the following ways:

- 4 if the binary turns out to be a text file.
- The same exit code returned from the process that is run. If a 3010 is returned, it will set 3010 for the package.

Aliases `Start-ChocolateyProcess` and `Invoke-ChocolateyProcess`
available in 0.10.2+.

.INPUTS
None

.OUTPUTS
None

.PARAMETER Statements
Arguments to pass to `ExeToRun` or the PowerShell script block to be
run.

.PARAMETER ExeToRun
The executable/application/installer to run. Defaults to `'powershell'`.

.PARAMETER Elevated
Indicate whether the process should run elevated.

Available in 0.10.2+.

.PARAMETER Minimized
Switch indicating if a Windows pops up (if not called with a silent
argument) that it should be minimized.

.PARAMETER NoSleep
Used only when calling PowerShell - indicates the window that is opened
should return instantly when it is complete.

.PARAMETER ValidExitCodes
Array of exit codes indicating success. Defaults to `@(0)`.

.PARAMETER WorkingDirectory
The working directory for the running process. Defaults to
`Get-Location`.

Available in 0.10.1+.

.PARAMETER SensitiveStatements
Arguments to pass to  `ExeToRun` that are not logged.

Note that only licensed versions of Chocolatey provide a way to pass
those values completely through without having them in the install
script or on the system in some way.

Available in 0.10.1+.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
Start-ChocolateyProcessAsAdmin -Statements "$msiArgs" -ExeToRun 'msiexec'

.EXAMPLE
Start-ChocolateyProcessAsAdmin -Statements "$silentArgs" -ExeToRun $file

.EXAMPLE
Start-ChocolateyProcessAsAdmin -Statements "$silentArgs" -ExeToRun $file -ValidExitCodes @(0,21)

.EXAMPLE
>
# Run PowerShell statements
$psFile = Join-Path "$(Split-Path -parent $MyInvocation.MyCommand.Definition)" 'someInstall.ps1'
Start-ChocolateyProcessAsAdmin "& `'$psFile`'"

.EXAMPLE
# This also works for cmd and is required if you have any spaces in the paths within your command
$appPath = "$env:ProgramFiles\myapp"
$cmdBatch = "/c `"$appPath\bin\installmyappservice.bat`""
Start-ChocolateyProcessAsAdmin $cmdBatch cmd 
# or more explicitly
Start-ChocolateyProcessAsAdmin -Statements $cmdBatch -ExeToRun "cmd.exe"

.LINK
Install-ChocolateyPackage

.LINK
Install-ChocolateyInstallPackage
#>
param(
  [parameter(Mandatory=$false, Position=0)][string[]] $statements,
  [parameter(Mandatory=$false, Position=1)][string] $exeToRun = 'powershell',
  [parameter(Mandatory=$false)][switch] $elevated = $true,
  [parameter(Mandatory=$false)][switch] $minimized,
  [parameter(Mandatory=$false)][switch] $noSleep,
  [parameter(Mandatory=$false)] $validExitCodes = @(0),
  [parameter(Mandatory=$false)][string] $workingDirectory = $(Get-Location),
  [parameter(Mandatory=$false)][string] $sensitiveStatements = '',
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)
  [string]$statements = $statements -join ' '

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

  try{
    if ($exeToRun -ne $null) { $exeToRun = $exeToRun -replace "`0", "" }
    if ($statements -ne $null) { $statements = $statements -replace "`0", "" }
  } catch {
    Write-Debug "Removing null characters resulted in an error - $($_.Exception.Message)"
  }

  $wrappedStatements = $statements
  if ($wrappedStatements -eq $null) { $wrappedStatements = ''}

  if ($exeToRun -eq 'powershell') {
    $exeToRun = "$($env:SystemRoot)\System32\WindowsPowerShell\v1.0\powershell.exe"
    $importChocolateyHelpers = ""
    Get-ChildItem "$helpersPath" -Filter *.psm1 | ForEach-Object { $importChocolateyHelpers = "& import-module -name  `'$($_.FullName)`' | Out-Null; $importChocolateyHelpers" };
    $block = @"
      `$noSleep = `$$noSleep
      $importChocolateyHelpers
      try{
        `$progressPreference="SilentlyContinue"
        $statements
        if(!`$noSleep){start-sleep 6}
      }
      catch{
        if(!`$noSleep){start-sleep 8}
        throw
      }
"@
    $encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($block))
    $wrappedStatements = "-NoProfile -ExecutionPolicy bypass -EncodedCommand $encoded"
    $dbgMessage = @"
Elevating Permissions and running powershell block:
$block
This may take a while, depending on the statements.
"@
  }
  else
  {
    $dbgMessage = @"
Elevating Permissions and running [`"$exeToRun`" $wrappedStatements]. This may take a while, depending on the statements.
"@
  }

  if (!$elevated) {
  $dbgMessage = @"
Running [`"$exeToRun`" $wrappedStatements]. This may take a while, depending on the statements.
"@
  }

  Write-Debug $dbgMessage

  try {
    $exeIsTextFile = [System.IO.Path]::GetFullPath($exeToRun) + ".istext"
    if (([System.IO.File]::Exists($exeIsTextFile))) {
      Set-PowerShellExitCode 4
      throw "The file was a text file but is attempting to be run as an executable - '$exeToRun'"
    }
  } catch {
    Write-Debug "Unable to detect whether the file is a text file or not - $($_.Exception.Message)"
  }

  if ($exeToRun -eq 'msiexec' -or $exeToRun -eq 'msiexec.exe') {
    $exeToRun = "$($env:SystemRoot)\System32\msiexec.exe"
  }

  if (!([System.IO.File]::Exists($exeToRun)) -and $exeToRun -notmatch 'msiexec') {
    Write-Warning "May not be able to find '$exeToRun'. Please use full path for executables."
    # until we have search paths enabled, let's just pass a warning
    #Set-PowerShellExitCode 2
    #throw "Could not find '$exeToRun'"
  }

  # Redirecting output slows things down a bit.
  $writeOutput = {
    if ($EventArgs.Data -ne $null) {
      Write-Verbose "$($EventArgs.Data)"
    }
  }

  $writeError = {
    if ($EventArgs.Data -ne $null) {
      Write-Error "$($EventArgs.Data)"
    }
  }

  $process = New-Object System.Diagnostics.Process
  $process.EnableRaisingEvents = $true
  Register-ObjectEvent -InputObject $process -SourceIdentifier "LogOutput_ChocolateyProc" -EventName OutputDataReceived -Action $writeOutput | Out-Null
  Register-ObjectEvent -InputObject $process -SourceIdentifier "LogErrors_ChocolateyProc" -EventName ErrorDataReceived -Action  $writeError | Out-Null

  #$process.StartInfo = New-Object System.Diagnostics.ProcessStartInfo($exeToRun, $wrappedStatements)
  # in case empty args makes a difference, try to be compatible with the older
  # version
  $psi = New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName = $exeToRun
  if ($wrappedStatements -ne '') {
    $psi.Arguments = "$wrappedStatements"
  }
  if ($sensitiveStatements -ne $null -and $sensitiveStatements -ne '') {
    Write-Host "Sensitive arguments have been passed. Adding to arguments."
    $psi.Arguments += " $sensitiveStatements"
  }
  $process.StartInfo =  $psi

  # process start info
  $process.StartInfo.RedirectStandardOutput = $true
  $process.StartInfo.RedirectStandardError = $true
  $process.StartInfo.UseShellExecute = $false
  $process.StartInfo.WorkingDirectory = $workingDirectory

  if ($elevated -and [Environment]::OSVersion.Version -ge (New-Object 'Version' 6,0)){
    # this doesn't actually currently work - because we are not running under shell execute
    Write-Debug "Setting RunAs for elevation"
    $process.StartInfo.Verb = "RunAs"
  }
  if ($minimized) {
    $process.StartInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Minimized
  }

  $process.Start() | Out-Null
  if ($process.StartInfo.RedirectStandardOutput) { $process.BeginOutputReadLine() }
  if ($process.StartInfo.RedirectStandardError) { $process.BeginErrorReadLine() }
  $process.WaitForExit()

  # For some reason this forces the jobs to finish and waits for
  # them to do so. Without this it never finishes.
  Unregister-Event -SourceIdentifier "LogOutput_ChocolateyProc"
  Unregister-Event -SourceIdentifier "LogErrors_ChocolateyProc"

  # sometimes the process hasn't fully exited yet.
  for ($loopCount=1; $loopCount -le 15; $loopCount++) {
    if ($process.HasExited) { break; }
    Write-Debug "Waiting for process to exit - $loopCount/15 seconds";
    Start-Sleep 1;
  }

  $exitCode = $process.ExitCode
  $process.Dispose()

  Write-Debug "Command [`"$exeToRun`" $wrappedStatements] exited with `'$exitCode`'."
  if ($validExitCodes -notcontains $exitCode) {
    Set-PowerShellExitCode $exitCode
    throw "Running [`"$exeToRun`" $wrappedStatements] was not successful. Exit code was '$exitCode'. See log for possible error messages."
  } else {
    $chocoSuccessCodes = @(0, 1605, 1614, 1641, 3010)
    if ($chocoSuccessCodes -notcontains $exitCode) {
      Write-Warning "Exit code '$exitCode' was considered valid by script, but not as a Chocolatey success code. Returning '0'."
      $exitCode = 0
    }
  }

  Write-Debug "Finishing 'Start-ChocolateyProcessAsAdmin'"

  return $exitCode
}

Set-Alias Start-ChocolateyProcess Start-ChocolateyProcessAsAdmin
Set-Alias Invoke-ChocolateyProcess Start-ChocolateyProcessAsAdmin
