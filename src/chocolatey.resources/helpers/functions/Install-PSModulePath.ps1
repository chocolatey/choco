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

function Install-PSModulePath {
param(
  [string] $pathToInstall,
  [System.EnvironmentVariableTarget] $pathType = [System.EnvironmentVariableTarget]::User
)
  Write-Debug "Running 'Install-PSModulePath' with pathToInstall:`'$pathToInstall`'";
  $originalPathToInstall = $pathToInstall

  #get the PSModulePath variable
  Update-SessionEnvironment
  $envPath = $env:PSModulePath
  if (!$envPath.ToLower().Contains($pathToInstall.ToLower()))
  {
    Write-Host "PSModulePath environment variable does not have $pathToInstall in it. Adding..."
    $actualPath = Get-EnvironmentVariable -Name 'PSModulePath' -Scope $pathType

    $statementTerminator = ";"
    #does the path end in ';'?
    $hasStatementTerminator = $actualPath -ne $null -and $actualPath.EndsWith($statementTerminator)
    # if the last digit is not ;, then we are adding it
    If (!$hasStatementTerminator -and $actualPath -ne $null) {$pathToInstall = $statementTerminator + $pathToInstall}
    if (!$pathToInstall.EndsWith($statementTerminator)) {$pathToInstall = $pathToInstall + $statementTerminator}
    $actualPath = $actualPath + $pathToInstall

    if ($pathType -eq [System.EnvironmentVariableTarget]::Machine) {
      if (Test-ProcessAdminRights) {
        Set-EnvironmentVariable -Name 'PSModulePath' -Value $actualPath -Scope $pathType
      } else {
        $psArgs = "Install-PSModulePath -pathToInstall `'$originalPathToInstall`' -pathType `'$pathType`'"
        Start-ChocolateyProcessAsAdmin "$psArgs"
      }
    } else {
      Set-EnvironmentVariable -Name 'PSModulePath' -Value $actualPath -Scope $pathType
    }

    #add it to the local path as well so users will be off and running
    $envPSPath = $env:PSModulePath
    $env:PSModulePath = $envPSPath + $statementTerminator + $pathToInstall
  }
}

# [System.Text.RegularExpressions.Regex]::Match($Path,[System.Text.RegularExpressions.Regex]::Escape('locationtoMatch') + '(?>;)?', '', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)