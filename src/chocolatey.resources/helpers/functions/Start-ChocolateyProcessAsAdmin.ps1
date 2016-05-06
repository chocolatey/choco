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
param(
  [string] $statements,
  [string] $exeToRun = 'powershell',
  [switch] $minimized,
  [switch] $noSleep,
  $validExitCodes = @(0)
)
  Write-Debug "Running 'Start-ChocolateyProcessAsAdmin' with exeToRun:`'$exeToRun`', statements: `'$statements`' ";

  $wrappedStatements = $statements
  if ($wrappedStatements -eq $null) { $wrappedStatements = ''}

  if ($exeToRun -eq 'powershell') {
    $exeToRun = "$($env:SystemRoot)\System32\WindowsPowerShell\v1.0\powershell.exe"
    $importChocolateyHelpers = ""
    Get-ChildItem "$helpersPath" -Filter *.psm1 | ForEach-Object { $importChocolateyHelpers = "& import-module -name  `'$($_.FullName)`';$importChocolateyHelpers" };
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
  
  Write-Debug $dbgMessage

  $exeIsTextFile = [System.IO.Path]::GetFullPath($exeToRun) + ".istext"
  if (([System.IO.File]::Exists($exeIsTextFile))) {
    Set-PowerShellExitCode 4
    throw "The file was a text file but is attempting to be run as an executable - '$exeToRun'"
  }

  if (!([System.IO.File]::Exists($exeToRun)) -and $exeToRun -notmatch 'msiexec') {
    Set-PowerShellExitCode 2
    throw "Could not find '$exeToRun'"
  }

  # Redirecting output slows things down a bit.
  $writeOutput = {
    if ($EventArgs.Data -ne $null) {
      Write-Host "$($EventArgs.Data)"
    }
  }

  $writeError = {
    if ($EventArgs.Data -ne $null) {
      Write-Error "$($EventArgs.Data)"
    }
  }

  $process = New-Object System.Diagnostics.Process
  $process.EnableRaisingEvents = $true
  Register-ObjectEvent  -InputObject $process -SourceIdentifier "LogOutput_ChocolateyProc" -EventName OutputDataReceived -Action $writeOutput | Out-Null
  Register-ObjectEvent -InputObject $process -SourceIdentifier "LogErrors_ChocolateyProc" -EventName ErrorDataReceived -Action  $writeError | Out-Null

  #$process.StartInfo = New-Object System.Diagnostics.ProcessStartInfo($exeToRun, $wrappedStatements)
  # in case empty args makes a difference, try to be compatible with the older
  # version
  $psi = New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName = $exeToRun
  if ($wrappedStatements -ne '') {
    $psi.Arguments = "$wrappedStatements"
  }
  $process.StartInfo =  $psi

  # process start info
  $process.StartInfo.RedirectStandardOutput = $true
  $process.StartInfo.RedirectStandardError = $true
  $process.StartInfo.UseShellExecute = $false
  $process.StartInfo.WorkingDirectory = Get-Location
  if ([Environment]::OSVersion.Version -ge (New-Object 'Version' 6,0)){
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

  # This handles installers which exit immediately, and spawn lingering children tasks
  # which may be carrying on additional installation tasks.
  #
  # Few installers spawn child processes which outlive the parent process. So,
  # let's monitor the exit of these children (and any of their children).
  #
  # Implementation inspired by:
  # http://powershell.com/cs/blogs/tobias/archive/2012/05/09/managing-child-processes.aspx
  #
  # TODO: Will some installers provide meaningful exit-codes here?
  #
  # BEGIN handling of process children
    function Get-ProcessChildren {
    param(
      $Id
    )

      Write-Debug "Searching for any active children to process ($($Id))"

      $children = Get-WmiObject -Class Win32_Process -Filter "ParentProcessID=$($Id)"

      $result = $null

      if ( $children ) {
        # filter to only include results with ProcessId
        $children = $children | Where-Object { $_.ProcessId -ne $null }

        # add children to result
        $result = @( $children )

        # recursively collect grandchildren processes
        foreach ( $child in $children ) {

          Write-Debug "Found active child process ($($child.ProcessId))"

          $grandchildren = Get-ProcessChildren -Id $child.ProcessId

          # add grandchildren to result
          $result += @( $grandchildren )

        }

      }

      Return $result

    }

    $children = Get-ProcessChildren -Id $process.Id

    Write-Debug "Children processes: $($children)"

    foreach ($child in $children)
    {
      try {

        if ( $child.ProcessId ) {

          Write-Debug "Waiting for child process ($($child.Processid))"

          Wait-Process -Id $child.ProcessId

        }

      } catch [Microsoft.PowerShell.Commands.ProcessCommandException] {
        # Okay, this happens when child process already exited
      }
    }
  # END handling of process children

  # For some reason this forces the jobs to finish and waits for
  # them to do so. Without this it never finishes.
  Unregister-Event -SourceIdentifier "LogOutput_ChocolateyProc"
  Unregister-Event -SourceIdentifier "LogErrors_ChocolateyProc"

  $exitCode = $process.ExitCode
  $process.Dispose()

  Write-Debug "Command [`"$exeToRun`" $wrappedStatements] exited with `'$exitCode`'."
  if ($validExitCodes -notcontains $exitCode) {
    Set-PowerShellExitCode $exitCode
    throw "Running [`"$exeToRun`" $statements] was not successful. Exit code was '$exitCode'. See log for possible error messages."
  }
 
  Write-Debug "Finishing 'Start-ChocolateyProcessAsAdmin'"

  return $exitCode
}
