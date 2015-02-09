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
  if ($exeToRun -eq 'powershell') {
    $exeToRun = "$($env:windir)\System32\WindowsPowerShell\v1.0\powershell.exe"
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
  else {
    $dbgMessage = @"
Elevating Permissions and running $exeToRun $wrappedStatements. This may take a while, depending on the statements.
"@
  }
  $dbgMessage | Write-Debug

  $psi = new-object System.Diagnostics.ProcessStartInfo
  $psi.RedirectStandardError = $true
  $psi.UseShellExecute = $false
  $psi.FileName = $exeToRun
  if ($wrappedStatements -ne '') {
    $psi.Arguments = "$wrappedStatements"
  }

  if ([Environment]::OSVersion.Version -ge (new-object 'Version' 6,0)){
    $psi.Verb = "runas"
  }

  $psi.WorkingDirectory = get-location

  if ($minimized) {
    $psi.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Minimized
  }

  $s = [System.Diagnostics.Process]::Start($psi)

  $chocTempDir = Join-Path $env:TEMP "chocolatey"
  if (![System.IO.Directory]::Exists($chocTempDir)) { [System.IO.Directory]::CreateDirectory($chocTempDir) | Out-Null }
  $errorFile = Join-Path $chocTempDir "$($s.Id)-error.stream"
  $s.StandardError.ReadToEnd() | Out-File $errorFile
  $s.WaitForExit()
  if ($validExitCodes -notcontains $s.ExitCode) {
    try {
      $innerError = Import-CLIXML $errorFile | ? { $_.GetType() -eq [String] } | Out-String
    }
    catch{
      $innerError = Get-Content $errorFile | Out-String
    }
    $errorMessage = "[ERROR] Running $exeToRun with $statements was not successful. Exit code was `'$($s.ExitCode)`' Error Message: $innerError."
    Remove-Item $errorFile -Force -ErrorAction SilentlyContinue
    throw $errorMessage
  }

  Write-Debug "Finishing 'Start-ChocolateyProcessAsAdmin'"
}
